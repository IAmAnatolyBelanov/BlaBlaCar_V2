using Newtonsoft.Json;

using Polly;

using WebApi.Models;
using WebApi.Services.Redis;
using WebApi.Shared;

namespace WebApi.Services.Yandex
{
	public interface ISuggestService
	{
		ValueTask<YandexSuggestResponse?> GetSuggestion(string input, CancellationToken ct);
	}

	public class SuggestService : ISuggestService
	{
		private static readonly YandexSuggestResponse _failResponse = new()
		{
			SuggestReqId = "Fail",
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

		private readonly IAsyncPolicy<YandexSuggestResponse?> _asyncPolicy;

		public SuggestService(
			ISuggestServiceConfig config,
			IRedisCacheService redisCacheService)
		{
			_config = config;
			_redisCacheService = redisCacheService;

			_asyncPolicy = Policy<YandexSuggestResponse?>
				.Handle<HttpRequestException>()
				.WaitAndRetryAsync(_config.RetryCount,
				attempt => TimeSpan.FromMilliseconds(100 + 40 * attempt),
				(exception, timespan, attempt, context) =>
				{
					_logger.Error("Failed to fetch yandex suggestion. Attempt {Attempt}, time delay {TimeDelay}, context {Context}, exception {Exception}",
						attempt, timespan, context, exception);
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
		}

		public async ValueTask<YandexSuggestResponse?> GetSuggestion(string input, CancellationToken ct)
		{
			if (string.IsNullOrWhiteSpace(input) || input.Length < _config.MinInput)
				return _failResponse;

			var request = $"https://suggest-maps.yandex.ru/v1/suggest?apikey={_config.ApiKey}&text={input}&highlight=0&print_address=1&attrs=uri";

			using var redis = _redisCacheService.Connect();
			var (cacheExists, cacheValue) = _redisCacheService.TryGet<YandexSuggestResponse>(redis, request);

			if (cacheExists)
				return cacheValue;

			if (_config.IsDebug && ExternalRequstsCount > 999)
				throw new Exception($"Для дебага достпуно только 1000 запросов в день. Лимит исчерпан. Лимит будет сброшен через {TimeSpan.FromHours(24) - (DateTimeOffset.UtcNow - LastExternalRequestLimitSet)}. Всё ещё можно использовать запросы к кешу.");

			PolicyResult<YandexSuggestResponse?> result = default!;
			try
			{
				result = await _asyncPolicy.ExecuteAndCaptureAsync(async internalCt =>
				{
					using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request);

					var response = await _httpClient.SendAsync(httpRequest, internalCt);
					Interlocked.Increment(ref ExternalRequstsCount);
					response.EnsureSuccessStatusCode();

					var body = await response.Content.ReadAsStringAsync(internalCt);

					var suggestion = JsonConvert.DeserializeObject<YandexSuggestResponse>(body);

					return suggestion;
				}, ct);
			}
			catch (Exception exception)
			{
				_logger.Error("Fail to get suggestion for {Input}. Exception: {Exception}", input, exception);
				_ = _redisCacheService.SetAsync(redis, request, _failResponse, _config.FailExpiry);
				return _failResponse;
			}

			if (result.Outcome == OutcomeType.Failure)
			{
				_logger.Error("Fail to get suggestion for {Input}. Exception: {Exception}", input, result.FinalException);
				_ = _redisCacheService.SetAsync(redis, request, _failResponse, _config.FailExpiry);
				return _failResponse;
			}

			_ = _redisCacheService.SetAsync(redis, request, result.Result, _config.Expiry);
			return result.Result;
		}
	}
}
