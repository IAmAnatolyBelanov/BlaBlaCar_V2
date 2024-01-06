using Newtonsoft.Json;

using Polly;
using WebApi.Models;
using WebApi.Services.Redis;

namespace WebApi.Services.Yandex
{
	public interface IRouteService
	{
		ValueTask<YandexRouteResponse?> GetRoute(IReadOnlyList<FormattedPoint> points, bool avoidTolls, CancellationToken ct);
	}

	public class RouteService : IRouteService
	{
		private static readonly YandexRouteResponse _failResponse = new()
		{
			Success = false,
		};
		private static readonly HttpClient _httpClient = new(new SocketsHttpHandler
		{
			PooledConnectionLifetime = TimeSpan.FromMinutes(15),
		});
		private static ulong ExternalRequstsCount = 0;
		private static DateTimeOffset LastExternalRequestLimitSet = DateTimeOffset.UtcNow;

		private readonly ILogger _logger = Log.ForContext<RouteService>();

		private readonly IRouteServiceConfig _config;
		private readonly IRedisCacheService _redisCacheService;

		private readonly IAsyncPolicy<YandexRouteResponse?> _asyncPolicy;

		public RouteService(
			IRouteServiceConfig config,
			IRedisCacheService redisCacheService)
		{
			_config = config;
			_redisCacheService = redisCacheService;

			_asyncPolicy = Policy<YandexRouteResponse?>
				.Handle<HttpRequestException>()
				.WaitAndRetryAsync(_config.RetryCount,
				attempt => TimeSpan.FromMilliseconds(100 + 40 * attempt),
				(exception, timespan, attempt, context) =>
				{
					_logger.Error("Failed to fetch yandex route. Attempt {Attempt}, time delay {TimeDelay}, context {Context}, exception {Exception}",
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

		public async ValueTask<YandexRouteResponse?> GetRoute(IReadOnlyList<FormattedPoint> points, bool avoidTolls, CancellationToken ct)
		{
			if (points.Count < 2 || points.Count > _config.MaxPoints)
				return _failResponse;

			var pointsAsStr = string.Join('|', points.Select(p => $"{p.Latitude},{p.Longitude}"));

			var request = $"https://suggest-maps.yandex.ru/v1/suggest?apikey={_config.ApiKey}&waypoints={pointsAsStr}&avoid_tolls={avoidTolls}&mode=driving";

			using var redis = _redisCacheService.Connect();
			var (cacheExists, cacheValue) = _redisCacheService.TryGet<YandexRouteResponse>(redis, request);

			if (cacheExists)
				return cacheValue;

			if (_config.IsDebug && ExternalRequstsCount > 999)
				throw new Exception($"Для дебага достпуно только 1000 запросов в день. Лимит исчерпан. Лимит будет сброшен через {TimeSpan.FromHours(24) - (DateTimeOffset.UtcNow - LastExternalRequestLimitSet)}. Всё ещё можно использовать запросы к кешу.");

			PolicyResult<YandexRouteResponse?> result = default!;
			try
			{
				result = await _asyncPolicy.ExecuteAndCaptureAsync(async internalCt =>
				{
					using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request);

					var response = await _httpClient.SendAsync(httpRequest, internalCt);
					Interlocked.Increment(ref ExternalRequstsCount);
					response.EnsureSuccessStatusCode();

					var body = await response.Content.ReadAsStringAsync(internalCt);

					var route = JsonConvert.DeserializeObject<YandexRouteResponse>(body);

					return route;
				}, ct);
			}
			catch (Exception exception)
			{
				_logger.Error("Fail to get route for {Input}. Exception: {Exception}", pointsAsStr, exception);
				_ = _redisCacheService.SetAsync(redis, request, _failResponse, _config.FailExpiry);
				return _failResponse;
			}

			if (result.Outcome == OutcomeType.Failure)
			{
				_logger.Error("Fail to get route for {Input}. Exception: {Exception}", pointsAsStr, result.FinalException);
				_ = _redisCacheService.SetAsync(redis, request, _failResponse, _config.FailExpiry);
				return _failResponse;
			}

			_ = _redisCacheService.SetAsync(redis, request, result.Result, _config.Expiry);
			return result.Result;
		}
	}
}
