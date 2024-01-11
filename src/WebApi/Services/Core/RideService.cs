using Dapper;

using FluentValidation;

using Microsoft.EntityFrameworkCore;

using NetTopologySuite.Geometries;

using WebApi.DataAccess;
using WebApi.Models;
using WebApi.Services.Yandex;

namespace WebApi.Services.Core
{
	public interface IRideService
	{
		ValueTask<RideDto> CreateRide(ApplicationContext context, RideDto rideDto, CancellationToken ct);
		ValueTask<RideDto> CreateRide(RideDto rideDto, CancellationToken ct);
		ValueTask<(decimal low, decimal high)> GetRecommendedPriceAsync(ApplicationContext context, Point pointFrom, Point pointTo, CancellationToken ct);
		ValueTask<(decimal low, decimal high)> GetRecommendedPriceAsync(Point from, Point to, CancellationToken ct);
	}

	public class RideService : IRideService
	{
		private const char _addressesDelimiter = '@';

		private readonly IServiceScopeFactory _serviceScopeFactory;
		private readonly IRideServiceConfig _config;
		private readonly IRideDtoMapper _rideDtoMapper;
		private readonly ILegDtoMapper _legDtoMapper;
		private readonly IValidator<IReadOnlyList<LegDto>> _legsCollectionValidatior;
		private readonly IGeocodeService _geocodeService;
		private readonly IValidator<(FormattedPoint Point, YandexGeocodeResponseDto GeocodeResponse)> _yaGeocodeResponseValidator;

		public RideService(
			IServiceScopeFactory serviceScopeFactory,
			IRideServiceConfig config,
			IRideDtoMapper rideDtoMapper,
			ILegDtoMapper legDtoMapper,
			IValidator<IReadOnlyList<LegDto>> legsCollectionValidatior,
			IGeocodeService geocodeService,
			IValidator<(FormattedPoint Point, YandexGeocodeResponseDto GeocodeResponse)> yaGeocodeResponseValidator)
		{
			_serviceScopeFactory = serviceScopeFactory;
			_config = config;
			_rideDtoMapper = rideDtoMapper;
			_legDtoMapper = legDtoMapper;
			_legsCollectionValidatior = legsCollectionValidatior;
			_geocodeService = geocodeService;
			_yaGeocodeResponseValidator = yaGeocodeResponseValidator;
		}

		public async ValueTask<RideDto> CreateRide(RideDto rideDto, CancellationToken ct)
		{
			using var scope = BuildScope();
			using var context = GetDbContext(scope);
			return await CreateRide(context, rideDto, ct);
		}

		public async ValueTask<RideDto> CreateRide(ApplicationContext context, RideDto rideDto, CancellationToken ct)
		{
			rideDto.Id = Guid.NewGuid();

			var legDtos = rideDto.Legs ?? Array.Empty<LegDto>();

			for (var i = 0; i < legDtos.Count; i++)
			{
				var legDto = legDtos[i];
				legDto.Ride = rideDto;
				legDto.RideId = rideDto.Id;
				legDto.Id = Guid.NewGuid();
			}

			_legsCollectionValidatior.ValidateAndThrowFriendly(legDtos);

			if (legDtos.Count > 10)
			{
				await Parallel.ForEachAsync(legDtos, ct, FillLegDesription);
			}
			else
			{
				for (var i = 0; i < legDtos.Count; i++)
				{
					var leg = legDtos[i];
					await FillLegDesription(leg, ct);
				}
			}

			var mappedObjects = new Dictionary<object, object>((legDtos.Count + 1) * 2);

			var ride = _rideDtoMapper.FromDto(rideDto, mappedObjects);

			var legs = _legDtoMapper.FromDtoList(legDtos, mappedObjects);

			context.Rides.Add(ride);
			context.Legs.AddRange(legs);

			await context.SaveChangesAsync(ct);

			mappedObjects.Clear();
			var result = _rideDtoMapper.ToDto(ride, mappedObjects);
			result.Legs = _legDtoMapper.ToDtoList(legs, mappedObjects);

			result.FullyLeg = result.Legs
				.OrderByDescending(x => x.Duration)
				.First();

			result.FullyLegId = result.FullyLeg.Id;

			return result;
		}

		private async ValueTask FillLegDesription(LegDto leg, CancellationToken ct)
		{
			var from = await _geocodeService.PointToGeoCode(leg.From.Point, ct);
			_yaGeocodeResponseValidator.ValidateAndThrowFriendly((leg.From.Point, from));

			var to = await _geocodeService.PointToGeoCode(leg.To.Point, ct);
			_yaGeocodeResponseValidator.ValidateAndThrowFriendly((leg.To.Point, to));


			var fromStr = from!.Geoobjects[0].FormattedAddress;
			var toStr = to!.Geoobjects[0].FormattedAddress;

			leg.Description = $"{fromStr}{_addressesDelimiter}{toStr}";
		}

		public async ValueTask<(decimal low, decimal high)> GetRecommendedPriceAsync(Point from, Point to, CancellationToken ct)
		{
			using var scope = BuildScope();
			using var context = GetDbContext(scope);
			return await GetRecommendedPriceAsync(context, from, to, ct);
		}

		public async ValueTask<(decimal low, decimal high)> GetRecommendedPriceAsync(ApplicationContext context, Point pointFrom, Point pointTo, CancellationToken ct)
		{
			var connection = context.Database.GetDbConnection();

			var maxDistanceInMeters = _config.PriceStatisticsRadiusMeters;
			var lowPercentale = 0 + (1 - _config.PriceStatisticsPercentale) / 2f;
			var highPercentale = 1 - (1 - _config.PriceStatisticsPercentale) / 2f;
			var now = DateTimeOffset.UtcNow;
			var minEndTime = now - _config.PriceStatisticsMaxPastPeriod;

			const string query = $@"
WITH prices AS (
	SELECT ""{nameof(Leg.PriceInRub)}""
	FROM public.""{nameof(ApplicationContext.Legs)}""
	WHERE
		ST_Distance(""{nameof(Leg.From)}"", @{nameof(pointFrom)}) <= @{nameof(maxDistanceInMeters)}
		AND ST_Distance(""{nameof(Leg.To)}"", @{nameof(pointTo)}) <= @{nameof(maxDistanceInMeters)}
		AND ""{nameof(Leg.EndTime)}"" < @{nameof(now)}
		AND ""{nameof(Leg.EndTime)}"" >= @{nameof(minEndTime)}
	ORDER BY ""{nameof(Leg.PriceInRub)}""
)
SELECT
	CASE WHEN (SELECT COUNT(*) FROM prices) > @{nameof(_config.PriceStatisticsMinRowsCount)}
		THEN PERCENTILE_CONT(@{nameof(lowPercentale)}) WITHIN GROUP (ORDER BY ""{nameof(Leg.PriceInRub)}"")
		ELSE -1
		END AS {nameof(Tuple<double, double>.Item1)},
	CASE WHEN (SELECT COUNT(*) FROM prices) > @{nameof(_config.PriceStatisticsMinRowsCount)}
		THEN PERCENTILE_CONT(@{nameof(highPercentale)}) WITHIN GROUP (ORDER BY ""{nameof(Leg.PriceInRub)}"")
		ELSE -1
		END AS {nameof(Tuple<double, double>.Item2)}
FROM prices
LIMIT 1;
";

			var command = new CommandDefinition(
				commandText: query,
				parameters: new
				{
					pointFrom,
					pointTo,
					now,
					minEndTime,
					_config.PriceStatisticsMinRowsCount,
					maxDistanceInMeters,
					lowPercentale,
					highPercentale,
				},
				cancellationToken: ct);
			var result = await connection.QueryFirstAsync<Tuple<double, double>>(command);

			return ((decimal)result.Item1, (decimal)result.Item2);

			throw new NotImplementedException();
		}

		private AsyncServiceScope BuildScope()
			=> _serviceScopeFactory.CreateAsyncScope();
		private ApplicationContext GetDbContext(AsyncServiceScope scope)
			=> scope.ServiceProvider.GetRequiredService<ApplicationContext>();
	}
}
