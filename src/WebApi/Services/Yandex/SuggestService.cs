﻿using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json;

using Polly;

using WebApi.Models;
using WebApi.Services.InMemoryCaches;
using WebApi.Services.Redis;

namespace WebApi.Services.Yandex
{
	public interface ISuggestService
	{
		ValueTask<YandexSuggestResponseDto?> GetSuggestion(string input, CancellationToken ct);
	}

	public class SuggestService : ISuggestService
	{
		private static readonly YandexSuggestResponseDto _failResponse = new()
		{
			Success = false,
		};
		private static readonly HttpClient _httpClient = new(new SocketsHttpHandler
		{
			PooledConnectionLifetime = TimeSpan.FromMinutes(15),
		});
		private static ulong ExternalRequestsCount = 0;
		private static DateTimeOffset LastExternalRequestLimitSet = DateTimeOffset.UtcNow;

		private readonly ILogger _logger = Log.ForContext<SuggestService>();

		private readonly ISuggestServiceConfig _config;
		private readonly IRedisCacheService _redisCacheService;
		private readonly IYandexSuggestResponseMapper _yandexSuggestResponseDtoMapper;

		private readonly IAsyncPolicy<string> _asyncPolicy;
		private readonly IInMemoryCache<YandexSuggestResponseDto> _memoryCache;

		public SuggestService(
			ISuggestServiceConfig config,
			IRedisCacheService redisCacheService,
			IYandexSuggestResponseMapper yandexSuggestResponseDtoMapper,
			IInMemoryCacheConfigMapper inMemoryCacheConfigMapper)
		{
			_config = config;
			_redisCacheService = redisCacheService;
			_yandexSuggestResponseDtoMapper = yandexSuggestResponseDtoMapper;

			_asyncPolicy = Policy<string>
				.Handle<HttpRequestException>()
				.OrResult(string.IsNullOrWhiteSpace)
				.WaitAndRetryAsync(_config.RetryCount,
				attempt => TimeSpan.FromMilliseconds(100 + 40 * attempt),
				(result, timeSpan, attempt, context) =>
				{
					_logger.Error("Failed to fetch yandex suggestion. Attempt {Attempt}, time delay {TimeDelay}, context {Context}, result {Result}, exception {Exception}",
						attempt, timeSpan, context, result.Result, result.Exception);
				});

			if (_config.IsDebug)
				_ = Task.Run(async () =>
				{
					while (true)
					{
						await Task.Delay(TimeSpan.FromHours(24));
						Interlocked.Exchange(ref ExternalRequestsCount, 0);
						LastExternalRequestLimitSet = DateTimeOffset.UtcNow;
					}
				});

			_memoryCache = new InMemoryCache<YandexSuggestResponseDto>(inMemoryCacheConfigMapper, _config.InMemoryCacheConfig);
		}

		public async ValueTask<YandexSuggestResponseDto?> GetSuggestion(string input, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(input) || input.Length < _config.MinInput)
				return _failResponse;

			input = input.ToLowerInvariant();

			if (_memoryCache.TryGetValue(input, out var cachedResponseDto))
				return cachedResponseDto;

			var redisKey = $"https://suggest-maps.yandex.ru/v1/suggest?text={input}&highlight=0&print_address=1&attrs=uri";

			var (cacheExists, cachedResponse)
				= _redisCacheService.TryGet<YandexSuggestResponse>(redisKey);

			if (cacheExists)
			{
				if (cachedResponse is null)
					return _failResponse;

				cachedResponseDto = _yandexSuggestResponseDtoMapper.ToResponseDto(cachedResponse);
				_memoryCache.Set(input, cachedResponseDto, _config.InMemoryCacheObjectLifetime);

				return cachedResponseDto;
			}

			if (_config.IsDebug && ExternalRequestsCount > 999)
				throw new Exception($"Для дебага доступно только 1000 запросов в день. Лимит исчерпан. Лимит будет сброшен через {TimeSpan.FromHours(24) - (DateTimeOffset.UtcNow - LastExternalRequestLimitSet)}. Всё ещё можно использовать запросы к кешу.");

			var request = $"{redisKey}&apikey={_config.ApiKey}";
			PolicyResult<string> suggestionBody = default!;
			YandexSuggestResponse suggestion = default!;
			try
			{
				suggestionBody = await _asyncPolicy.ExecuteAndCaptureAsync(async internalCt =>
				{
					using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request);

					var response = await _httpClient.SendAsync(httpRequest, internalCt);
					Interlocked.Increment(ref ExternalRequestsCount);
					response.EnsureSuccessStatusCode();

					var body = await response.Content.ReadAsStringAsync(internalCt);

					return body;
				}, ct);

				suggestion = JsonConvert.DeserializeObject<YandexSuggestResponse>(suggestionBody.Result)!;
			}
			catch (Exception exception)
			{
				_logger.Error(exception, "Fail to get suggestion for {Input}", input);
				_memoryCache.Set(input, _failResponse, _config.FailExpiry);
				return _failResponse;
			}

			if (suggestionBody.Outcome == OutcomeType.Failure)
			{
				_logger.Error(suggestionBody.FinalException, "Fail to get suggestion for {Input}", input);
				_memoryCache.Set(input, _failResponse, _config.FailExpiry);
				return _failResponse;
			}

			_ = _redisCacheService.SetStringAsync(redisKey, suggestionBody.Result, _config.DistributedCacheExpiry, CancellationToken.None);
			cachedResponseDto = _yandexSuggestResponseDtoMapper.ToResponseDto(suggestion);
			_memoryCache.Set(input, cachedResponseDto, _config.DistributedCacheExpiry);
			return cachedResponseDto;
		}
	}
}
