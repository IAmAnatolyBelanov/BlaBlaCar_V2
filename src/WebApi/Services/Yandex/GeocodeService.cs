using Microsoft.Extensions.Caching.Memory;

using Newtonsoft.Json;

using Polly;

using WebApi.Models;
using WebApi.Services.InMemoryCaches;
using WebApi.Services.Redis;

namespace WebApi.Services.Yandex
{
	public interface IGeocodeService
	{
		ValueTask<YandexGeocodeResponseDto> AddressToGeoCode(string address, CancellationToken ct);
		ValueTask<YandexGeocodeResponseDto> PointToGeoCode(FormattedPoint point, CancellationToken ct);
		ValueTask<YandexGeocodeResponseDto> UriToGeoCode(string uri, CancellationToken ct);
	}

	public class GeocodeService : IGeocodeService
	{
		private static readonly YandexGeocodeResponseDto _failResponse = new()
		{
			Success = false,
		};
		private static readonly YandexGeocodeResponseDto _emptyResponse = new()
		{
			Success = true,
			Geoobjects = Array.Empty<YandexGeocodeResponseDto.YandexGeocodeResponseGeoobjectDto>(),
		};
		private static readonly HttpClient _httpClient = new(new SocketsHttpHandler
		{
			PooledConnectionLifetime = TimeSpan.FromMinutes(15),
		});
		private static ulong ExternalRequestsCount = 0;
		private static DateTimeOffset LastExternalRequestLimitSet = DateTimeOffset.UtcNow;

		private readonly ILogger _logger = Log.ForContext<GeocodeService>();

		private readonly IGeocodeServiceConfig _config;
		private readonly IRedisCacheService _redisCacheService;
		private readonly IYandexGeocodeResponseDtoMapper _yandexGeocodeResponseDtoMapper;

		private readonly IAsyncPolicy<string> _asyncPolicy;
		private readonly IInMemoryCache<YandexGeocodeResponseDto> _memoryCache;

		public GeocodeService(
			IGeocodeServiceConfig config,
			IRedisCacheService redisCacheService,
			IYandexGeocodeResponseDtoMapper yandexGeocodeResponseDtoMapper,
			IInMemoryCacheConfigMapper inMemoryCacheConfigMapper)
		{
			_config = config;
			_redisCacheService = redisCacheService;
			_yandexGeocodeResponseDtoMapper = yandexGeocodeResponseDtoMapper;

			_asyncPolicy = Policy<string>
				.Handle<HttpRequestException>()
				.OrResult(string.IsNullOrWhiteSpace)
				.WaitAndRetryAsync(_config.RetryCount,
				attempt => TimeSpan.FromMilliseconds(100 + 40 * attempt),
				(result, timeSpan, attempt, context) =>
				{
					_logger.Error("Failed to fetch yandex geocode response. Attempt {Attempt}, time delay {TimeDelay}, context {Context}, result {Result}, exception {Exception}",
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

			_memoryCache = new InMemoryCache<YandexGeocodeResponseDto>(inMemoryCacheConfigMapper, _config.InMemoryCacheConfig);
		}

		public async ValueTask<YandexGeocodeResponseDto> AddressToGeoCode(string address, CancellationToken ct)
		{
			var request = $"https://geocode-maps.yandex.ru/1.x?geocode={address.ToLowerInvariant()}&format=json&results=1";

			return await GetGeocode(request, ct);
		}

		public async ValueTask<YandexGeocodeResponseDto> UriToGeoCode(string uri, CancellationToken ct)
		{
			var request = $"https://geocode-maps.yandex.ru/1.x?uri={uri}&format=json&results=1";

			return await GetGeocode(request, ct);
		}

		public async ValueTask<YandexGeocodeResponseDto> PointToGeoCode(FormattedPoint point, CancellationToken ct)
		{
			var request = $"https://geocode-maps.yandex.ru/1.x?geocode={point.Longitude:F6} {point.Latitude:F6}&sco=longlat&format=json&results=1";

			return await GetGeocode(request, ct);
		}

		private async ValueTask<YandexGeocodeResponseDto> GetGeocode(string cacheKey, CancellationToken ct)
		{
			if (_memoryCache.TryGetValue(cacheKey, out var cachedGeocodeDto))
				return cachedGeocodeDto;

			var (cacheExists, cachedResponse)
				= _redisCacheService.TryGet<YandexGeocodeResponse>(cacheKey);

			if (cacheExists)
			{
				if (cachedResponse is null)
					return _failResponse;

				cachedGeocodeDto = cachedResponse.Response.GeoObjectCollection.FeatureMember.Length > 0
					? _yandexGeocodeResponseDtoMapper.ToDtoLight(cachedResponse)
					: _emptyResponse;
				_memoryCache.Set(cacheKey, cachedGeocodeDto, _config.InMemoryCacheObjectLifetime);
				return cachedGeocodeDto;
			}

			if (_config.IsDebug && ExternalRequestsCount > 999)
				throw new Exception($"Для дебага доступно только 1000 запросов в день. Лимит исчерпан. Лимит будет сброшен через {TimeSpan.FromHours(24) - (DateTimeOffset.UtcNow - LastExternalRequestLimitSet)}. Всё ещё можно использовать запросы к кешу.");

			var request = $"{cacheKey}&apikey={_config.ApiKey}";
			PolicyResult<string> resultBody = default!;
			YandexGeocodeResponse geocode = default!;
			try
			{
				resultBody = await _asyncPolicy.ExecuteAndCaptureAsync(async internalCt =>
				{
					using var httpRequest = new HttpRequestMessage(HttpMethod.Get, request);

					var response = await _httpClient.SendAsync(httpRequest, internalCt);
					Interlocked.Increment(ref ExternalRequestsCount);
					response.EnsureSuccessStatusCode();

					var body = await response.Content.ReadAsStringAsync(internalCt);

					return body;
				}, ct);

				geocode = JsonConvert.DeserializeObject<YandexGeocodeResponse>(resultBody.Result)!;
			}
			catch (Exception exception)
			{
				_logger.Error(exception, "Fail to get geocode for {Input}", cacheKey);
				_memoryCache.Set(cacheKey, _failResponse, _config.FailExpiry);
				return _failResponse;
			}

			if (resultBody.Outcome == OutcomeType.Failure)
			{
				_logger.Error(resultBody.FinalException, "Fail to get geocode for {Input}", cacheKey);
				_memoryCache.Set(cacheKey, _failResponse, _config.FailExpiry);
				return _failResponse;
			}

			_ = _redisCacheService.SetStringAsync(cacheKey, resultBody.Result, _config.DistributedCacheExpiry, CancellationToken.None);
			cachedGeocodeDto = geocode.Response.GeoObjectCollection.FeatureMember.Length > 0
				? _yandexGeocodeResponseDtoMapper.ToDtoLight(geocode)
				: _emptyResponse;
			_memoryCache.Set(cacheKey, cachedGeocodeDto, _config.DistributedCacheExpiry);
			return cachedGeocodeDto;
		}
	}
}
