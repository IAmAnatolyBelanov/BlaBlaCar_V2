﻿using Newtonsoft.Json;

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
		private static ulong ExternalRequestsCount = 0;
		private static DateTimeOffset LastExternalRequestLimitSet = DateTimeOffset.UtcNow;

		private readonly ILogger _logger = Log.ForContext<RouteService>();

		private readonly IRouteServiceConfig _config;
		private readonly IRedisCacheService _redisCacheService;

		private readonly IAsyncPolicy<string> _asyncPolicy;

		public RouteService(
			IRouteServiceConfig config,
			IRedisCacheService redisCacheService)
		{
			_config = config;
			_redisCacheService = redisCacheService;

			_asyncPolicy = Policy<string>
				.Handle<HttpRequestException>()
				.OrResult(string.IsNullOrWhiteSpace)
				.WaitAndRetryAsync(_config.RetryCount,
				attempt => TimeSpan.FromMilliseconds(100 + 40 * attempt),
				(result, timeSpan, attempt, context) =>
				{
					_logger.Error("Failed to fetch yandex route. Attempt {Attempt}, time delay {TimeDelay}, context {Context}, result {Result}, exception {Exception}",
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
		}

		public async ValueTask<YandexRouteResponse?> GetRoute(IReadOnlyList<FormattedPoint> points, bool avoidTolls, CancellationToken ct)
		{
			if (points.Count < 2 || points.Count > _config.MaxPoints)
				return _failResponse;

			var pointsAsStr = string.Join('|', points.Select(p => $"{p.Latitude},{p.Longitude}"));

			var request = $"https://suggest-maps.yandex.ru/v1/suggest?apikey={_config.ApiKey}&waypoints={pointsAsStr}&avoid_tolls={avoidTolls}&mode=driving";

			var (cacheExists, cacheValue)
				= _redisCacheService.TryGet<YandexRouteResponse>(request);

			if (cacheExists)
				return cacheValue;

			if (_config.IsDebug && ExternalRequestsCount > 999)
				throw new Exception($"Для дебага доступно только 1000 запросов в день. Лимит исчерпан. Лимит будет сброшен через {TimeSpan.FromHours(24) - (DateTimeOffset.UtcNow - LastExternalRequestLimitSet)}. Всё ещё можно использовать запросы к кешу.");

			PolicyResult<string> routeBody = default!;
			YandexRouteResponse route = default!;
			try
			{
				routeBody = await _asyncPolicy.ExecuteAndCaptureAsync(async internalCt =>
				{
					using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request);

					var response = await _httpClient.SendAsync(httpRequest, internalCt);
					Interlocked.Increment(ref ExternalRequestsCount);
					response.EnsureSuccessStatusCode();

					var body = await response.Content.ReadAsStringAsync(internalCt);

					return body;
				}, ct);

				route = JsonConvert.DeserializeObject<YandexRouteResponse>(routeBody.Result)!;
			}
			catch (Exception exception)
			{
				_logger.Error("Fail to get route for {Input}. Exception: {Exception}", pointsAsStr, exception);
				_ = _redisCacheService.SetAsync(request, _failResponse, _config.FailExpiry, CancellationToken.None);
				return _failResponse;
			}

			if (routeBody.Outcome == OutcomeType.Failure)
			{
				_logger.Error("Fail to get route for {Input}. Exception: {Exception}", pointsAsStr, routeBody.FinalException);
				_ = _redisCacheService.SetAsync(request, _failResponse, _config.FailExpiry, CancellationToken.None);
				return _failResponse;
			}

			_ = _redisCacheService.SetStringAsync(request, routeBody.Result, _config.Expiry, CancellationToken.None);
			return route;
		}
	}
}
