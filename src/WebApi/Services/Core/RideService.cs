using FluentValidation;

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
		ValueTask<ReservationDto> Reserv(ReservationDto reservDto, CancellationToken ct);
		ValueTask<ReservationDto> Reserv(ApplicationContext context, ReservationDto reservDto, CancellationToken ct);
	}

	public class RideService : IRideService
	{
		private const char _addressesDelimiter = '@';

		private static readonly IReadOnlyDictionary<Guid, LegDto> _emptyLegsDict
			= new Dictionary<Guid, LegDto>();

		private readonly IServiceScopeFactory _serviceScopeFactory;
		private readonly IRideServiceConfig _config;
		private readonly IRideDtoMapper _rideDtoMapper;
		private readonly ILegDtoMapper _legDtoMapper;
		private readonly IGeocodeService _geocodeService;
		private readonly IValidator<(FormattedPoint Point, YandexGeocodeResponseDto GeocodeResponse)> _yaGeocodeResponseValidator;
		private readonly IReservationDtoMapper _reservationDtoMapper;
		private readonly IClock _clock;
		private readonly IValidator<RideDto> _rideValidator;
		private readonly IPriceDtoMapper _priceDtoMapper;

		public RideService(
			IServiceScopeFactory serviceScopeFactory,
			IRideServiceConfig config,
			IRideDtoMapper rideDtoMapper,
			ILegDtoMapper legDtoMapper,
			IGeocodeService geocodeService,
			IValidator<(FormattedPoint Point, YandexGeocodeResponseDto GeocodeResponse)> yaGeocodeResponseValidator,
			IReservationDtoMapper reservationDtoMapper,
			IClock clock,
			IValidator<RideDto> rideValidator,
			IPriceDtoMapper priceDtoMapper)
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
		}

		public async ValueTask<RideDto> CreateRide(RideDto rideDto, CancellationToken ct)
		{
			using var scope = BuildScope();
			using var context = GetDbContext(scope);
			return await CreateRide(context, rideDto, ct);
		}

		public async ValueTask<RideDto> CreateRide(ApplicationContext context, RideDto rideDto, CancellationToken ct)
		{
			var legDtos = rideDto.Legs ?? Array.Empty<LegDto>();

			for (var i = 0; i < legDtos.Count; i++)
			{
				var legDto = legDtos[i];

				legDto.Ride = rideDto;
				legDto.RideId = rideDto.Id;
			}

			var legsDict = legDtos.Count == 0
				? _emptyLegsDict
				: legDtos.ToDictionary(x => x.Id);

			var priceDtos = rideDto.Prices ?? Array.Empty<PriceDto>();

			for (int i = 0; i < priceDtos.Count; i++)
			{
				var price = priceDtos[i];

				price.StartLeg = legsDict.TryGetValue(price.StartLegId, out var leg) ? leg : null!;
				price.EndLeg = legsDict.TryGetValue(price.EndLegId, out leg) ? leg : null!;
			}

			rideDto.NormalizeLegs();

			_rideValidator.ValidateAndThrowFriendly(rideDto);

			await Parallel.ForEachAsync(legDtos, ct, FillLegDesription);

			var mappedObjects = new Dictionary<object, object>((legDtos.Count + priceDtos.Count + 1) * 2);

			var ride = _rideDtoMapper.FromDto(rideDto, mappedObjects);

			var legs = _legDtoMapper.FromDtoList(legDtos, mappedObjects);
			var prices = _priceDtoMapper.FromDtoList(priceDtos, mappedObjects);

			context.Rides.Add(ride);
			context.Legs.AddRange(legs);
			context.Prices.AddRange(prices);

			await context.SaveChangesAsync(ct);

			mappedObjects.Clear();
			var result = _rideDtoMapper.ToDto(ride, mappedObjects);
			result.Legs = _legDtoMapper.ToDtoList(legs, mappedObjects);
			result.Prices = _priceDtoMapper.ToDtoList(prices, mappedObjects);

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

		public async ValueTask<ReservationDto> Reserv(ReservationDto reservDto, CancellationToken ct)
		{
			using var scope = BuildScope();
			using var context = GetDbContext(scope);
			return await Reserv(reservDto, ct);
		}

		public async ValueTask<ReservationDto> Reserv(ApplicationContext context, ReservationDto reservDto, CancellationToken ct)
		{
			throw new NotImplementedException();

			//if (reservDto.LegId == default)
			//	throw new UserFriendlyException("InvalidLegId", $"Leg with id {reservDto.LegId} does not exist");

			//var leg = context.Legs.FirstOrDefault(x => x.Id == reservDto.LegId);

			//if (leg is null)
			//	throw new UserFriendlyException("InvalidLegId", $"Leg with id {reservDto.LegId} does not exist");

			//reservDto.Id = Guid.NewGuid();
			//reservDto.IsActive = true;
			//reservDto.CreateDateTime = _clock.Now;

			//var reserv = _reservationDtoMapper.FromDtoLight(reservDto);

			//// Можем упасть из-за триггера. Надо try-catch расставить, причём с возможностью по триггеру особое действие делать.
			//context.Reservations.Add(reserv);
			//await context.SaveChangesAsync(ct);

			//var lol = context.Reservations
			//	.Include(x => x.StartLeg)
			//		.ThenInclude(x => x.Ride)
			//	.ToList();

			//var kek = _reservationDtoMapper.ToDtoList(lol);

			//var result = _reservationDtoMapper.ToDtoLight(reserv);
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
		//			var lowPercentale = 0 + (1 - _config.PriceStatisticsPercentale) / 2f;
		//			var highPercentale = 1 - (1 - _config.PriceStatisticsPercentale) / 2f;
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
		//		THEN PERCENTILE_CONT(@{nameof(lowPercentale)}) WITHIN GROUP (ORDER BY ""{nameof(Leg.PriceInRub)}"")
		//		ELSE -1
		//		END AS {nameof(Tuple<double, double>.Item1)},
		//	CASE WHEN (SELECT COUNT(*) FROM prices) > @{nameof(_config.PriceStatisticsMinRowsCount)}
		//		THEN PERCENTILE_CONT(@{nameof(highPercentale)}) WITHIN GROUP (ORDER BY ""{nameof(Leg.PriceInRub)}"")
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
		//					lowPercentale,
		//					highPercentale,
		//				},
		//				cancellationToken: ct);
		//			var result = await connection.QueryFirstAsync<Tuple<double, double>>(command);

		//			return ((decimal)result.Item1, (decimal)result.Item2);

		//			throw new NotImplementedException();
		//		}

		public ValueTask<(decimal low, decimal high)> GetRecommendedPriceAsync(ApplicationContext context, Point pointFrom, Point pointTo, CancellationToken ct) => throw new NotImplementedException();
		public ValueTask<(decimal low, decimal high)> GetRecommendedPriceAsync(Point from, Point to, CancellationToken ct) => throw new NotImplementedException();

		private AsyncServiceScope BuildScope()
			=> _serviceScopeFactory.CreateAsyncScope();
		private ApplicationContext GetDbContext(AsyncServiceScope scope)
			=> scope.ServiceProvider.GetRequiredService<ApplicationContext>();

	}
}
