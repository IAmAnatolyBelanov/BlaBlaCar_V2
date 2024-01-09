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
		ValueTask<YandexGeocodeResponseDto?> AddressToGeoCode(string address, CancellationToken ct);
		ValueTask<YandexGeocodeResponseDto?> PointToGeoCode(FormattedPoint point, CancellationToken ct);
		ValueTask<YandexGeocodeResponseDto?> UriToGeoCode(string uri, CancellationToken ct);
	}

	public class GeocodeService : IGeocodeService
	{
		private static readonly YandexGeocodeResponseDto _failResponse = new()
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
		private readonly IYandexGeocodeResponseDtoMapper _yandexGeocodeResponseDtoMapper;

		private readonly IAsyncPolicy<YandexGeocodeResponse?> _asyncPolicy;
		private readonly IInMemoryCache<string, YandexGeocodeResponseDto> _memoryCache;

		public GeocodeService(
			IGeocodeServiceConfig config,
			IRedisCacheService redisCacheService,
			IYandexGeocodeResponseDtoMapper yandexGeocodeResponseDtoMapper)
		{
			_config = config;
			_redisCacheService = redisCacheService;
			_yandexGeocodeResponseDtoMapper = yandexGeocodeResponseDtoMapper;

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

			_memoryCache = new InMemoryCache<string, YandexGeocodeResponseDto>(new MemoryCacheOptions
			{
				SizeLimit = _config.InMemoryCacheMaxObjects
			});
		}

		public async ValueTask<YandexGeocodeResponseDto?> AddressToGeoCode(string address, CancellationToken ct)
		{
			var request = $"https://geocode-maps.yandex.ru/1.x?apikey={_config.ApiKey}&geocode={address}&format=json&results=1";

			return await GetGeocode(request, ct);
		}

		public async ValueTask<YandexGeocodeResponseDto?> UriToGeoCode(string uri, CancellationToken ct)
		{
			var request = $"https://geocode-maps.yandex.ru/1.x?apikey={_config.ApiKey}&uri={uri}&format=json&results=1";

			return await GetGeocode(request, ct);
		}

		public async ValueTask<YandexGeocodeResponseDto?> PointToGeoCode(FormattedPoint point, CancellationToken ct)
		{
			var request = $"https://geocode-maps.yandex.ru/1.x?apikey={_config.ApiKey}&geocode={point.Longitude:F6} {point.Latitude:F6}&sco=longlat&format=json&results=1";

			return await GetGeocode(request, ct);
		}

		private async ValueTask<YandexGeocodeResponseDto?> GetGeocode(string request, CancellationToken ct)
		{
			if (_memoryCache.TryGetValue(request, out var cachedResponseDto))
				return cachedResponseDto;

			using var redis = _redisCacheService.Connect();
			var (cacheExists, cachedResponse
				) = _redisCacheService.TryGet<YandexGeocodeResponse>(redis, request);

			if (cacheExists)
			{
				if (cachedResponse is null)
					return _failResponse;

				cachedResponseDto = _yandexGeocodeResponseDtoMapper.ToDtoLight(cachedResponse);
				_memoryCache.Set(request, cachedResponseDto, _config.InMemoryCacheObjectLifetime);
				return cachedResponseDto;
			}

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
				_logger.Error(exception, "Fail to get suggestion for {Input}", request);
				_memoryCache.Set(request, _failResponse, _config.FailExpiry);
				return _failResponse;
			}

			if (result.Outcome == OutcomeType.Failure)
			{
				_logger.Error(result.FinalException, "Fail to get suggestion for {Input}", request);
				_memoryCache.Set(request, _failResponse, _config.FailExpiry);
				return _failResponse;
			}

			_ = _redisCacheService.SetAsync(redis, request, result.Result, _config.DistributedCacheExpiry);
			cachedResponseDto = _yandexGeocodeResponseDtoMapper.ToDtoLight(result.Result!);
			_memoryCache.Set(request, cachedResponseDto, _config.DistributedCacheExpiry);
			return cachedResponseDto;
		}
	}
}
