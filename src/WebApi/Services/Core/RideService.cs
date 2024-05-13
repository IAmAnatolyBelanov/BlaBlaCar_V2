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
		ValueTask<RideDto_Obsolete> CreateRide(ApplicationContext context, RidePreparationDto_Obsolete rideDto, CancellationToken ct);
		ValueTask<RideDto_Obsolete> CreateRide(RideDto_Obsolete rideDto, CancellationToken ct);
		ValueTask<(decimal Low, decimal High)> GetRecommendedPriceAsync(ApplicationContext context, Point pointFrom, Point pointTo, CancellationToken ct);
		ValueTask<(decimal Low, decimal High)> GetRecommendedPriceAsync(Point from, Point to, CancellationToken ct);
		ValueTask<ReservationDto> Reserve(ReservationDto reserveDto, CancellationToken ct);
		ValueTask<ReservationDto> Reserve(ApplicationContext context, ReservationDto reserveDto, CancellationToken ct);
	}

	public class RideService : IRideService
	{
		private const char _addressesDelimiter = '@';

		private static readonly IReadOnlyDictionary<Guid, LegDto_Obsolete> _emptyLegsDict
			= new Dictionary<Guid, LegDto_Obsolete>();

		private readonly IServiceScopeFactory _serviceScopeFactory;
		private readonly IRideServiceConfig _config;
		private readonly IRideDtoMapper _rideDtoMapper;
		private readonly ILegDtoMapper _legDtoMapper;
		private readonly IGeocodeService _geocodeService;
		private readonly IValidator<(FormattedPoint Point, YandexGeocodeResponseDto GeocodeResponse)> _yaGeocodeResponseValidator;
		private readonly IReservationDtoMapper _reservationDtoMapper;
		private readonly IClock _clock;
		private readonly IValidator<RideDto_Obsolete> _rideValidator;
		private readonly IPriceDtoMapper _priceDtoMapper;
		private readonly IValidator<RidePreparationDto_Obsolete> _ridePreparationValidator;
		private readonly IRidePreparationDtoMapper _ridePreparationDtoMapper;

		public RideService(
			IServiceScopeFactory serviceScopeFactory,
			IRideServiceConfig config,
			IRideDtoMapper rideDtoMapper,
			ILegDtoMapper legDtoMapper,
			IGeocodeService geocodeService,
			IValidator<(FormattedPoint Point, YandexGeocodeResponseDto GeocodeResponse)> yaGeocodeResponseValidator,
			IReservationDtoMapper reservationDtoMapper,
			IClock clock,
			IValidator<RideDto_Obsolete> rideValidator,
			IPriceDtoMapper priceDtoMapper,
			IValidator<RidePreparationDto_Obsolete> ridePreparationValidator,
			IRidePreparationDtoMapper ridePreparationDtoMapper)
		{
			_serviceScopeFactory = serviceScopeFactory;
			_config = config;
			_rideDtoMapper = rideDtoMapper;
			_legDtoMapper = legDtoMapper;
			_geocodeService = geocodeService;
			_yaGeocodeResponseValidator = yaGeocodeResponseValidator;
			_reservationDtoMapper = reservationDtoMapper;
			_clock = clock;
			_rideValidator = rideValidator;
			_priceDtoMapper = priceDtoMapper;
			_ridePreparationValidator = ridePreparationValidator;
			_ridePreparationDtoMapper = ridePreparationDtoMapper;
		}

		public async ValueTask<RideDto_Obsolete> CreateRide(RideDto_Obsolete rideDto, CancellationToken ct)
		{
			using var scope = BuildScope();
			using var context = GetDbContext(scope);
			return await CreateRide(context, rideDto, ct);
		}

		public async ValueTask<RideDto_Obsolete> CreateRide(ApplicationContext context, RidePreparationDto_Obsolete rideDto, CancellationToken ct)
		{
			rideDto.Id = Guid.NewGuid();

			var legDtos = rideDto.Legs ?? Array.Empty<LegDto_Obsolete>();

			for (var i = 0; i < legDtos.Count; i++)
			{
				var legDto = legDtos[i];

				legDto.Ride = rideDto;
				legDto.RideId = rideDto.Id;
			}

			rideDto.NormalizeLegs();

			_ridePreparationValidator.ValidateAndThrowFriendly(rideDto);

			await Parallel.ForEachAsync(legDtos, ct, FillLegDescription);

			var mappedObjects = new Dictionary<object, object>();

			var ride = _ridePreparationDtoMapper.FromDto(rideDto, mappedObjects);
			var legs = _legDtoMapper.FromDtoList(legDtos, mappedObjects);

			ride.Status = RideStatus.Draft;


			throw new NotImplementedException();

			//var legsDict = legDtos.Count == 0
			//	? _emptyLegsDict
			//	: legDtos.ToDictionary(x => x.Id);

			//var priceDtos = rideDto.Prices ?? Array.Empty<PriceDto>();

			//for (int i = 0; i < priceDtos.Count; i++)
			//{
			//	var price = priceDtos[i];

			//	price.StartLeg = legsDict.TryGetValue(price.StartLegId, out var leg) ? leg : null!;
			//	price.EndLeg = legsDict.TryGetValue(price.EndLegId, out leg) ? leg : null!;
			//}

			//rideDto.NormalizeLegs();

			//_rideValidator.ValidateAndThrowFriendly(rideDto);

			//await Parallel.ForEachAsync(legDtos, ct, FillLegDescription);

			//var mappedObjects = new Dictionary<object, object>((legDtos.Count + priceDtos.Count + 1) * 2);

			//var ride = _rideDtoMapper.FromDto(rideDto, mappedObjects);

			//var legs = _legDtoMapper.FromDtoList(legDtos, mappedObjects);
			//var prices = _priceDtoMapper.FromDtoList(priceDtos, mappedObjects);

			//context.Rides.Add(ride);
			//context.Legs.AddRange(legs);
			//context.Prices.AddRange(prices);

			//await context.SaveChangesAsync(ct);

			//mappedObjects.Clear();
			//var result = _rideDtoMapper.ToDto(ride, mappedObjects);
			//result.Legs = _legDtoMapper.ToDtoList(legs, mappedObjects);
			//result.Prices = _priceDtoMapper.ToDtoList(prices, mappedObjects);

			//return result;
		}

		private async ValueTask FillLegDescription(LegDto_Obsolete leg, CancellationToken ct)
		{
			var from = await _geocodeService.PointToGeoCode(leg.From.Point, ct);
			_yaGeocodeResponseValidator.ValidateAndThrowFriendly((leg.From.Point, from));

			var to = await _geocodeService.PointToGeoCode(leg.To.Point, ct);
			_yaGeocodeResponseValidator.ValidateAndThrowFriendly((leg.To.Point, to));


			var fromStr = from!.Geoobjects[0].FormattedAddress;
			var toStr = to!.Geoobjects[0].FormattedAddress;

			leg.Description = $"{fromStr}{_addressesDelimiter}{toStr}";
		}

		public async ValueTask<ReservationDto> Reserve(ReservationDto reserveDto, CancellationToken ct)
		{
			using var scope = BuildScope();
			using var context = GetDbContext(scope);
			return await Reserve(reserveDto, ct);
		}

		public async ValueTask<ReservationDto> Reserve(ApplicationContext context, ReservationDto reserveDto, CancellationToken ct)
		{
			throw new NotImplementedException();

			//if (reserveDto.LegId == default)
			//	throw new UserFriendlyException("InvalidLegId", $"Leg with id {reserveDto.LegId} does not exist");

			//var leg = context.Legs.FirstOrDefault(x => x.Id == reserveDto.LegId);

			//if (leg is null)
			//	throw new UserFriendlyException("InvalidLegId", $"Leg with id {reserveDto.LegId} does not exist");

			//reserveDto.Id = Guid.NewGuid();
			//reserveDto.IsActive = true;
			//reserveDto.CreateDateTime = _clock.Now;

			//var reserve = _reservationDtoMapper.FromDtoLight(reserveDto);

			//// Можем упасть из-за триггера. Надо try-catch расставить, причём с возможностью по триггеру особое действие делать.
			//context.Reservations.Add(reserve);
			//await context.SaveChangesAsync(ct);

			//var lol = context.Reservations
			//	.Include(x => x.StartLeg)
			//		.ThenInclude(x => x.Ride)
			//	.ToList();

			//var kek = _reservationDtoMapper.ToDtoList(lol);

			//var result = _reservationDtoMapper.ToDtoLight(reserve);
			//return result;
		}

		//		public async ValueTask<(decimal low, decimal high)> GetRecommendedPriceAsync(Point from, Point to, CancellationToken ct)
		//		{
		//			using var scope = BuildScope();
		//			using var context = GetDbContext(scope);
		//			return await GetRecommendedPriceAsync(context, from, to, ct);
		//		}

		//		public async ValueTask<(decimal low, decimal high)> GetRecommendedPriceAsync(ApplicationContext context, Point pointFrom, Point pointTo, CancellationToken ct)
		//		{
		//			var connection = context.Database.GetDbConnection();

		//			var maxDistanceInMeters = _config.PriceStatisticsRadiusMeters;
		//			var lowPercentile = 0 + (1 - _config.PriceStatisticsPercentile) / 2f;
		//			var highPercentile = 1 - (1 - _config.PriceStatisticsPercentile) / 2f;
		//			var now = _clock.Now;
		//			var minEndTime = now - _config.PriceStatisticsMaxPastPeriod;

		//			const string query = $@"
		//WITH prices AS (
		//	SELECT ""{nameof(Leg.PriceInRub)}""
		//	FROM public.""{nameof(ApplicationContext.Legs)}""
		//	WHERE
		//		ST_Distance(""{nameof(Leg.From)}"", @{nameof(pointFrom)}) <= @{nameof(maxDistanceInMeters)}
		//		AND ST_Distance(""{nameof(Leg.To)}"", @{nameof(pointTo)}) <= @{nameof(maxDistanceInMeters)}
		//		AND ""{nameof(Leg.EndTime)}"" < @{nameof(now)}
		//		AND ""{nameof(Leg.EndTime)}"" >= @{nameof(minEndTime)}
		//	ORDER BY ""{nameof(Leg.PriceInRub)}""
		//)
		//SELECT
		//	CASE WHEN (SELECT COUNT(*) FROM prices) > @{nameof(_config.PriceStatisticsMinRowsCount)}
		//		THEN PERCENTILE_CONT(@{nameof(lowPercentile)}) WITHIN GROUP (ORDER BY ""{nameof(Leg.PriceInRub)}"")
		//		ELSE -1
		//		END AS {nameof(Tuple<double, double>.Item1)},
		//	CASE WHEN (SELECT COUNT(*) FROM prices) > @{nameof(_config.PriceStatisticsMinRowsCount)}
		//		THEN PERCENTILE_CONT(@{nameof(highPercentile)}) WITHIN GROUP (ORDER BY ""{nameof(Leg.PriceInRub)}"")
		//		ELSE -1
		//		END AS {nameof(Tuple<double, double>.Item2)}
		//FROM prices
		//LIMIT 1;
		//";

		//			var command = new CommandDefinition(
		//				commandText: query,
		//				parameters: new
		//				{
		//					pointFrom,
		//					pointTo,
		//					now,
		//					minEndTime,
		//					_config.PriceStatisticsMinRowsCount,
		//					maxDistanceInMeters,
		//					lowPercentile,
		//					highPercentile,
		//				},
		//				cancellationToken: ct);
		//			var result = await connection.QueryFirstAsync<Tuple<double, double>>(command);

		//			return ((decimal)result.Item1, (decimal)result.Item2);

		//			throw new NotImplementedException();
		//		}

		public async ValueTask<(decimal Low, decimal High)> GetRecommendedPriceAsync(ApplicationContext context, Point pointFrom, Point pointTo, CancellationToken ct)
		{
			const string suitedPrices = "suited_prices"
				, startLegRow = "start_leg_row"
				, endLegRow = "end_leg_row"
				, priceRow = "price_row"
				, rideRow = "ride_row"
				;

			var connection = context.Database.GetDbConnection();

			var maxDistanceInMeters = _config.PriceStatisticsRadiusMeters;
			var lowPercentile = 0 + (1 - _config.PriceStatisticsPercentile) / 2f;
			var highPercentile = 1 - (1 - _config.PriceStatisticsPercentile) / 2f;
			var now = _clock.Now;
			var minEndTime = now - _config.PriceStatisticsMaxPastPeriod;

			var query = @$"
WITH {suitedPrices} AS (
	SELECT {priceRow}.""{nameof(Price.PriceInRub)}""
	FROM ""{nameof(ApplicationContext.Prices)}"" {priceRow}
	LEFT JOIN ""{nameof(ApplicationContext.Legs)}"" {startLegRow} ON {startLegRow}.""{nameof(Leg_Obsolete.Id)}"" = {priceRow}.""{nameof(Price.StartLegId)}""
	LEFT JOIN ""{nameof(ApplicationContext.Legs)}"" {endLegRow} ON {endLegRow}.""{nameof(Leg_Obsolete.Id)}"" = {priceRow}.""{nameof(Price.EndLegId)}""
	LEFT JOIN ""{nameof(ApplicationContext.Rides)}"" {rideRow} ON {rideRow}.""{nameof(Ride_Obsolete.Id)}"" = {startLegRow}.""{nameof(Leg_Obsolete.RideId)}"" AND {rideRow}.""{nameof(Ride_Obsolete.Id)}"" = {endLegRow}.""{nameof(Leg_Obsolete.RideId)}""
	WHERE
		{rideRow}.""{nameof(Ride_Obsolete.Status)}"" = {(int)RideStatus.StartedOrDone}
		AND ST_Distance({startLegRow}.""{nameof(Leg_Obsolete.From)}"", @{nameof(pointFrom)}) <= {maxDistanceInMeters}
		AND ST_Distance({endLegRow}.""{nameof(Leg_Obsolete.To)}"", @{nameof(pointTo)}) <= {maxDistanceInMeters}
		AND {endLegRow}.""{nameof(Leg_Obsolete.EndTime)}"" < @{nameof(now)}
		AND {endLegRow}.""{nameof(Leg_Obsolete.EndTime)}"" >= @{nameof(minEndTime)}
	ORDER BY {priceRow}.""{nameof(Price.PriceInRub)}""
)
SELECT
	CASE WHEN (SELECT COUNT(*) FROM {suitedPrices}) >= {_config.PriceStatisticsMinRowsCount}
		THEN PERCENTILE_CONT({lowPercentile:F2}) WITHIN GROUP (ORDER BY {suitedPrices}.""{nameof(Price.PriceInRub)}"")
		ELSE -1
		END AS {nameof(Tuple<double, double>.Item1)},
	CASE WHEN (SELECT COUNT(*) FROM {suitedPrices}) > {_config.PriceStatisticsMinRowsCount}
		THEN PERCENTILE_CONT({highPercentile:F2}) WITHIN GROUP (ORDER BY {suitedPrices}.""{nameof(Price.PriceInRub)}"")
		ELSE -1
		END AS {nameof(Tuple<double, double>.Item2)}
FROM {suitedPrices}
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
				},
				cancellationToken: ct);
			var result = await connection.QueryFirstAsync<Tuple<double, double>>(command);

			return ((decimal)result.Item1, (decimal)result.Item2);
		}

		public async ValueTask<(decimal Low, decimal High)> GetRecommendedPriceAsync(Point from, Point to, CancellationToken ct)
		{
			using var scope = BuildScope();
			using var context = GetDbContext(scope);
			return await GetRecommendedPriceAsync(context, from, to, ct);
		}

		private AsyncServiceScope BuildScope()
			=> _serviceScopeFactory.CreateAsyncScope();
		private ApplicationContext GetDbContext(AsyncServiceScope scope)
			=> scope.ServiceProvider.GetRequiredService<ApplicationContext>();

	}
}
