using Microsoft.Extensions.Caching.Memory;

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
		private static ulong ExternalRequstsCount = 0;
		private static DateTimeOffset LastExternalRequestLimitSet = DateTimeOffset.UtcNow;

		private readonly ILogger _logger = Log.ForContext<SuggestService>();

		private readonly ISuggestServiceConfig _config;
		private readonly IRedisCacheService _redisCacheService;
		private readonly IYandexSuggestResponseDtoMapper _yandexSuggestResponseDtoMapper;

		private readonly IAsyncPolicy<string> _asyncPolicy;
		private readonly IInMemoryCache<string, YandexSuggestResponseDto> _memoryCache;

		public SuggestService(
			ISuggestServiceConfig config,
			IRedisCacheService redisCacheService,
			IYandexSuggestResponseDtoMapper yandexSuggestResponseDtoMapper)
		{
			_config = config;
			_redisCacheService = redisCacheService;
			_yandexSuggestResponseDtoMapper = yandexSuggestResponseDtoMapper;

			_asyncPolicy = Policy<string>
				.Handle<HttpRequestException>()
				.OrResult(string.IsNullOrWhiteSpace)
				.WaitAndRetryAsync(_config.RetryCount,
				attempt => TimeSpan.FromMilliseconds(100 + 40 * attempt),
				(result, timespan, attempt, context) =>
				{
					_logger.Error("Failed to fetch yandex suggestion. Attempt {Attempt}, time delay {TimeDelay}, context {Context}, result {Result}, exception {Exception}",
						attempt, timespan, context, result.Result, result.Exception);
				});

			if (_config.IsDebug)
				_ = Task.Run(async () =>
				{
					while (true)
					{
						await Task.Delay(TimeSpan.FromHours(24));
						Interlocked.Exchange(ref ExternalRequstsCount, 0);
						LastExternalRequestLimitSet = DateTimeOffset.UtcNow;
					}
				});

			_memoryCache = new InMemoryCache<string, YandexSuggestResponseDto>(new MemoryCacheOptions
			{
				SizeLimit = _config.InMemoryCacheMaxObjects,
			});
		}

		public async ValueTask<YandexSuggestResponseDto?> GetSuggestion(string input, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(input) || input.Length < _config.MinInput)
				return _failResponse;

			input = input.ToLowerInvariant();

			if (_memoryCache.TryGetValue(input, out var cachedResponseDto))
				return cachedResponseDto;

			var request = $"https://suggest-maps.yandex.ru/v1/suggest?apikey={_config.ApiKey}&text={input}&highlight=0&print_address=1&attrs=uri";

			var (cacheExists, cachedResponse)
				= _redisCacheService.TryGet<YandexSuggestResponse>(request);

			if (cacheExists)
			{
				if (cachedResponse is null)
					return _failResponse;

				cachedResponseDto = _yandexSuggestResponseDtoMapper.ToDtoLight(cachedResponse);
				_memoryCache.Set(input, cachedResponseDto, _config.InMemoryCacheObjectLifetime);
					return cachedResponseDto;
			}

			if (_config.IsDebug && ExternalRequstsCount > 999)
				throw new Exception($"Для дебага достпуно только 1000 запросов в день. Лимит исчерпан. Лимит будет сброшен через {TimeSpan.FromHours(24) - (DateTimeOffset.UtcNow - LastExternalRequestLimitSet)}. Всё ещё можно использовать запросы к кешу.");

			PolicyResult<string> suggestionBody = default!;
			YandexSuggestResponse suggestion = default!;
			try
			{
				suggestionBody = await _asyncPolicy.ExecuteAndCaptureAsync(async internalCt =>
				{
					using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request);

					var response = await _httpClient.SendAsync(httpRequest, internalCt);
					Interlocked.Increment(ref ExternalRequstsCount);
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

			_ = _redisCacheService.SetStringAsync(request, suggestionBody.Result, _config.DistributedCacheExpiry, CancellationToken.None);
			cachedResponseDto = _yandexSuggestResponseDtoMapper.ToDtoLight(suggestion);
			_memoryCache.Set(input, cachedResponseDto, _config.DistributedCacheExpiry);
			return cachedResponseDto;
		}
	}
}
