using Newtonsoft.Json;

using Polly;

using WebApi.Models;
using WebApi.Services.Redis;

namespace WebApi.Services.Yandex
{
	public interface IGeocodeService
	{
		ValueTask<YandexGeocodeResponse?> AddressToGeoCode(string address, CancellationToken ct);
		ValueTask<YandexGeocodeResponse?> PointToGeoCode(FormattedPoint point, CancellationToken ct);
		ValueTask<YandexGeocodeResponse?> UriToGeoCode(string uri, CancellationToken ct);
	}

	public class GeocodeService : IGeocodeService
	{
		private static readonly YandexGeocodeResponse _failResponse = new()
		{
			Success = false,
		};
		private static readonly HttpClient _httpClient = new(new SocketsHttpHandler
		{
			PooledConnectionLifetime = TimeSpan.FromMinutes(15),
		});
		private static ulong ExternalRequstsCount = 0;
		private static DateTimeOffset LastExternalRequestLimitSet = DateTimeOffset.UtcNow;

		private readonly ILogger _logger = Log.ForContext<GeocodeService>();

		private readonly IGeocodeServiceConfig _config;
		private readonly IRedisCacheService _redisCacheService;

		private readonly IAsyncPolicy<YandexGeocodeResponse?> _asyncPolicy;

		public GeocodeService(
			IGeocodeServiceConfig config,
			IRedisCacheService redisCacheService)
		{
			_config = config;
			_redisCacheService = redisCacheService;

			_asyncPolicy = Policy<YandexGeocodeResponse?>
				.Handle<HttpRequestException>()
				.WaitAndRetryAsync(_config.RetryCount,
				attempt => TimeSpan.FromMilliseconds(100 + 40 * attempt),
				(exception, timespan, attempt, context) =>
				{
					_logger.Error("Failed to fetch yandex geocode response. Attempt {Attempt}, time delay {TimeDelay}, context {Context}, exception {Exception}",
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

		public async ValueTask<YandexGeocodeResponse?> AddressToGeoCode(string address, CancellationToken ct)
		{
			var request = $"https://geocode-maps.yandex.ru/1.x?apikey={_config.ApiKey}&geocode={address}&format=json&results=1";

			return await GetGeocode(request, ct);
		}

		public async ValueTask<YandexGeocodeResponse?> UriToGeoCode(string uri, CancellationToken ct)
		{
			var request = $"https://geocode-maps.yandex.ru/1.x?apikey={_config.ApiKey}&uri={uri}&format=json&results=1";

			return await GetGeocode(request, ct);
		}

		public async ValueTask<YandexGeocodeResponse?> PointToGeoCode(FormattedPoint point, CancellationToken ct)
		{
			var request = $"https://geocode-maps.yandex.ru/1.x?apikey={_config.ApiKey}&geocode={point.Longitude:F6} {point.Latitude:F6}&sco=longlat&format=json&results=1";

			return await GetGeocode(request, ct);
		}

		private async ValueTask<YandexGeocodeResponse?> GetGeocode(string request, CancellationToken ct)
		{
			using var redis = _redisCacheService.Connect();
			var (cacheExists, cacheValue) = _redisCacheService.TryGet<YandexGeocodeResponse>(redis, request);

			if (cacheExists)
				return cacheValue;

			if (_config.IsDebug && ExternalRequstsCount > 999)
				throw new Exception($"Для дебага достпуно только 1000 запросов в день. Лимит исчерпан. Лимит будет сброшен через {TimeSpan.FromHours(24) - (DateTimeOffset.UtcNow - LastExternalRequestLimitSet)}. Всё ещё можно использовать запросы к кешу.");

			PolicyResult<YandexGeocodeResponse?> result = default!;
			try
			{
				result = await _asyncPolicy.ExecuteAndCaptureAsync(async internalCt =>
				{
					using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request);

					var response = await _httpClient.SendAsync(httpRequest, internalCt);
					Interlocked.Increment(ref ExternalRequstsCount);
					response.EnsureSuccessStatusCode();

					var body = await response.Content.ReadAsStringAsync(internalCt);

					var geocode = JsonConvert.DeserializeObject<YandexGeocodeResponse>(body);
					return geocode;
				}, ct);
			}
			catch (Exception exception)
			{
				_logger.Error("Fail to get geocode for {Input}. Exception: {Exception}", request, exception);
				_ = _redisCacheService.SetAsync(redis, request, _failResponse, _config.FailExpiry);
				return _failResponse;
			}

			if (result.Outcome == OutcomeType.Failure)
			{
				_logger.Error("Fail to get deocode for {Input}. Exception: {Exception}", request, result.FinalException);
				_ = _redisCacheService.SetAsync(redis, request, _failResponse, _config.FailExpiry);
				return _failResponse;
			}

			_ = _redisCacheService.SetAsync(redis, request, result.Result, _config.Expiry);
			return result.Result;
		}
	}
}
