using Dapper;
using FluentValidation;
using NetTopologySuite.Geometries;

using WebApi.DataAccess;
using WebApi.Models;
using WebApi.Models.ControllersModels.RideControllerModels;
using WebApi.Repositories;
using WebApi.Services.Validators;
using WebApi.Services.Yandex;

namespace WebApi.Services.Core
{
	public class RideServiceValidationCodes : ValidationCodes
	{
		public const string DriverIdIsEmpty = "RideService_DriverIdIsEmpty";
		public const string CarIdIsEmpty = "RideService_CarIdIsEmpty";
		public const string DriverDataDoesNotExist = "RideService_DriverDataDoesNotExist";
		public const string CarHasLessSeatsThanRideAvailablePlaces = "RideService_CarHasLessSeatsThanRideAvailablePlaces";
	}

	public interface IRideService
	{
		Task<RideDto> CreateRide(RideDto dto, CancellationToken ct);
		ValueTask<(decimal Low, decimal High)> GetRecommendedPriceAsync(Point from, Point to, CancellationToken ct);
		Task<IReadOnlyList<SearchRideResponse>> SearchRides(RideFilter filter, CancellationToken ct);
		Task<GetRideResponse?> GetRideById(Guid rideId, CancellationToken ct);
	}

	public class RideService : IRideService
	{
		private const char _addressesDelimiter = '@';

		private readonly IRideServiceConfig _config;
		private readonly IRideMapper _rideDtoMapper;
		private readonly ILegDtoMapper _legDtoMapper;
		private readonly IGeocodeService _geocodeService;
		private readonly IValidator<(FormattedPoint Point, YandexGeocodeResponseDto GeocodeResponse)> _yaGeocodeResponseValidator;
		private readonly IReservationDtoMapper _reservationDtoMapper;
		private readonly IClock _clock;
		private readonly IValidator<RideDto> _rideDtoValidator;
		private readonly IUserRepository _userRepository;
		private readonly ISessionFactory _sessionFactory;
		private readonly ICarRepository _carRepository;
		private readonly IWaypointMapper _waypointMapper;
		private readonly IRideRepository _rideRepository;
		private readonly IWaypointRepository _waypointRepository;
		private readonly ILegRepository _legRepository;
		private readonly IDriverDataRepository _driverDataRepository;
		private readonly IRideFilterMapper _rideFilterMapper;
		private readonly IValidator<RideFilter> _rideFilterValidator;
		private readonly ISearchRideResponseMapper _searchRideResponseMapper;
		private readonly ICarMapper _carMapper;

		public RideService(
			IRideServiceConfig config,
			IRideMapper rideDtoMapper,
			ILegDtoMapper legDtoMapper,
			IGeocodeService geocodeService,
			IValidator<(FormattedPoint Point, YandexGeocodeResponseDto GeocodeResponse)> yaGeocodeResponseValidator,
			IReservationDtoMapper reservationDtoMapper,
			IClock clock,
			IValidator<RideDto> rideDtoValidator,
			IUserRepository userRepository,
			ISessionFactory sessionFactory,
			ICarRepository carRepository,
			IWaypointMapper waypointMapper,
			IRideRepository rideRepository,
			IWaypointRepository waypointRepository,
			ILegRepository legRepository,
			IDriverDataRepository driverDataRepository,
			IRideFilterMapper rideFilterMapper,
			IValidator<RideFilter> rideFilterValidator,
			ISearchRideResponseMapper searchRideResponseMapper,
			ICarMapper carMapper)
		{
			_config = config;
			_rideDtoMapper = rideDtoMapper;
			_legDtoMapper = legDtoMapper;
			_geocodeService = geocodeService;
			_yaGeocodeResponseValidator = yaGeocodeResponseValidator;
			_reservationDtoMapper = reservationDtoMapper;
			_clock = clock;
			_rideDtoValidator = rideDtoValidator;
			_userRepository = userRepository;
			_sessionFactory = sessionFactory;
			_carRepository = carRepository;
			_waypointMapper = waypointMapper;
			_rideRepository = rideRepository;
			_waypointRepository = waypointRepository;
			_legRepository = legRepository;
			_driverDataRepository = driverDataRepository;
			_rideFilterMapper = rideFilterMapper;
			_rideFilterValidator = rideFilterValidator;
			_searchRideResponseMapper = searchRideResponseMapper;
			_carMapper = carMapper;
		}

		public async Task<RideDto> CreateRide(RideDto rideDto, CancellationToken ct)
		{
			var start = _clock.Now;

			ValidateRideOnCreation(rideDto);

			using var session = _sessionFactory.OpenPostgresConnection().BeginTransaction().StartTrace();

			var getAuthorTask = _userRepository.GetById(session, rideDto.AuthorId, ct);
			var getDriverTask = rideDto.DriverId is null
				? null
				: rideDto.DriverId.Value == rideDto.AuthorId
					? getAuthorTask
					: _userRepository.GetById(session, rideDto.DriverId.Value, ct);
			var getCarTask = rideDto.CarId is null
				? null
				: _carRepository.GetById(session, rideDto.CarId.Value, ct);

			var author = await getAuthorTask;
			if (author is null)
				throw new UserFriendlyException(CommonValidationCodes.UserNotFound, $"Пользователь {rideDto.AuthorId} не найден");
			var driver = getAuthorTask is null ? null : await getAuthorTask;
			if (rideDto.DriverId is not null && driver is null)
				throw new UserFriendlyException(CommonValidationCodes.UserNotFound, $"Пользователь {rideDto.DriverId} не найден");
			var car = getCarTask is null ? null : await getCarTask;
			if (rideDto.CarId is not null && car is null)
				throw new UserFriendlyException(CommonValidationCodes.CarNotFound, $"Автомобиль {rideDto.CarId} не найден");

			if (driver is not null && rideDto.Status == RideStatus.ActiveNotStarted)
			{
				var driverData = await _driverDataRepository.GetByUserId(session, driver.Id, ct);
				if (driverData is null)
					throw new UserFriendlyException(RideServiceValidationCodes.DriverDataDoesNotExist, "У пользователя, указанного как водитель, нет водительского удостоверения");
			}

			if (car is not null && car.PassengerSeatsCount < rideDto.AvailablePlacesCount)
				throw new UserFriendlyException(RideServiceValidationCodes.CarHasLessSeatsThanRideAvailablePlaces, "У автомобиля не может быть пассажирских сидений меньше, чем свободных мест в поездке");

			rideDto.Id = Guid.NewGuid();
			rideDto.Created = start;
			var ride = _rideDtoMapper.ToRide(rideDto);

			var waypoints = FillWaypointsOnRideCreation(rideDto);
			var legs = FillLegsOnRideCreation(rideDto, waypoints);

			await _rideRepository.Insert(session, ride, ct);
			await _waypointRepository.BulkInsert(session, waypoints, ct);
			await _legRepository.BulkInsert(session, legs, ct);

			await session.CommitAsync(ct);

			return rideDto;
		}

		private void ValidateRideOnCreation(RideDto rideDto)
		{
			if (rideDto.Status != RideStatus.Draft && rideDto.Status != RideStatus.ActiveNotStarted)
				throw new UserFriendlyException(RideValidationCodes.InvalidCreationStatus, $"При создании поездки доступны лишь статусы {nameof(RideStatus.Draft)} и {nameof(RideStatus.ActiveNotStarted)}");

			// TODO - для менеджеров условие не должно выполняться.
			if (rideDto.Status == RideStatus.ActiveNotStarted)
			{
				if (rideDto.DriverId is null)
					throw new UserFriendlyException(RideServiceValidationCodes.DriverIdIsEmpty, "Водитель может быть не указан только для черновика");
				if (rideDto.CarId is null)
					throw new UserFriendlyException(RideServiceValidationCodes.CarIdIsEmpty, "Автомобиль может быть не указан только для черновика");
			}

			_rideDtoValidator.ValidateAndThrowFriendly(rideDto);
		}

		private IReadOnlyList<Waypoint> FillWaypointsOnRideCreation(RideDto rideDto)
		{
			var waypoints = _waypointMapper.ToWaypoints(rideDto)
				.OrderBy(x => x.Arrival)
				.ThenBy(x => x.Departure ?? DateTimeOffset.MaxValue)
				.ToArray();

			waypoints[0].Id = Guid.NewGuid();
			waypoints[1].Id = Guid.NewGuid();
			waypoints[^1].Id = Guid.NewGuid();
			waypoints[^2].Id = Guid.NewGuid();

			waypoints[0].NextWaypointId = waypoints[1].Id;
			waypoints[^1].PreviousWaypointId = waypoints[^2].Id;

			for (int i = 1; i < waypoints.Length - 1; i++)
			{
				waypoints[i].Id = Guid.NewGuid();

				waypoints[i - 1].NextWaypointId = waypoints[i].Id;
				waypoints[i].PreviousWaypointId = waypoints[i - 1].Id;
				waypoints[i].NextWaypointId = waypoints[i + 1].Id;
				waypoints[i + 1].PreviousWaypointId = waypoints[i].Id;
			}

			return waypoints;
		}

		private IReadOnlyList<Leg> FillLegsOnRideCreation(RideDto rideDto, IReadOnlyList<Waypoint> waypoints)
		{
			var waypointsDictByCoordinates = waypoints.ToDictionary(x => FormattedPoint.FromPoint(x.Point));
			var waypointsDictById = waypoints.ToDictionary(x => x.Id);
			var legsDtoDictByCoordinates = rideDto.Legs
				.ToDictionary(x => (x.WaypointFrom, x.WaypointTo));

			var legs = new List<Leg>(rideDto.Legs.Count);
			foreach (var legDto in rideDto.Legs)
			{
				var leg = new Leg();
				leg.Id = Guid.NewGuid();
				leg.RideId = rideDto.Id;
				leg.PriceInRub = legDto.PriceInRub;
				leg.WaypointFromId = waypointsDictByCoordinates[legDto.WaypointFrom].Id;
				leg.WaypointToId = waypointsDictByCoordinates[legDto.WaypointTo].Id;
				leg.IsManual = true;
				var pointFrom = waypointsDictById[leg.WaypointFromId];
				leg.IsBetweenNeighborPoints = pointFrom.NextWaypointId == leg.WaypointToId;

				legs.Add(leg);
			}

			for (int i = 0; i < waypoints.Count; i++)
			{
				for (int j = i + 1; j < waypoints.Count; j++)
				{
					var startPoint = waypoints[i];
					var endPoint = waypoints[j];

					var startFormatted = FormattedPoint.FromPoint(startPoint.Point);
					var endFormatted = FormattedPoint.FromPoint(endPoint.Point);

					if (legsDtoDictByCoordinates.ContainsKey((startFormatted, endFormatted)))
						continue;

					var currentIndex = i;
					var currentPoint = startPoint;
					var currentFormatted = startFormatted;
					var nextPoint = waypoints[currentIndex + 1];
					var nextFormatted = FormattedPoint.FromPoint(nextPoint.Point);

					// Наличие такого leg'а гарантируется валидатором.
					var price = legsDtoDictByCoordinates[(currentFormatted, nextFormatted)].PriceInRub;

					while (true)
					{
						currentIndex++;

						if (currentIndex >= j)
						{
							var leg = new Leg
							{
								Id = Guid.NewGuid(),
								IsManual = false,
								RideId = rideDto.Id,
								PriceInRub = price,
								WaypointFromId = startPoint.Id,
								WaypointToId = endPoint.Id,

								// Все Leg'и соседей обязательны для ручного заполнения.
								IsBetweenNeighborPoints = false,
							};
							legs.Add(leg);
							break;
						}

						currentPoint = nextPoint;
						currentFormatted = nextFormatted;
						nextPoint = waypoints[currentIndex + 1];
						nextFormatted = FormattedPoint.FromPoint(nextPoint.Point);

						price += legsDtoDictByCoordinates[(currentFormatted, nextFormatted)].PriceInRub;
					}
				}
			}

			return legs;
		}


		public async Task<GetRideResponse?> GetRideById(Guid rideId, CancellationToken ct)
		{
			using var session = _sessionFactory.OpenPostgresConnection();
			var ride = await _rideRepository.GetById(session, rideId, ct);

			if (ride is null)
				return null;

			var carTask = ride.CarId.HasValue
				? _carRepository.GetById(session, ride.CarId.Value, ct)
				: null;

			var waypointsTask = _waypointRepository.GetByRideId(session, ride.Id, ct);
			var legsTask = _legRepository.GetByRideId(session, ride.Id, ct, onlyManual: true);

			var rideDto = _rideDtoMapper.ToRideDto(ride);

			var car = carTask is null ? null : await carTask;
			var carDto = car is null ? null : _carMapper.ToCarDto(car);

			var waypoints = await waypointsTask;
			var waypointsDict = waypoints.ToDictionary(x => x.Id);
			var waypointDtos = waypoints.Select(_waypointMapper.ToWaypointDto).ToArray();

			var legs = await legsTask;
			var legDtos = new List<LegDto>(legs.Count);
			foreach (var leg in legs)
			{
				var departurePoint = waypointsDict[leg.WaypointFromId];
				var arrivalPoint = waypointsDict[leg.WaypointToId];
				legDtos.Add(new LegDto
				{
					WaypointFrom = departurePoint.Point,
					WaypointTo = arrivalPoint.Point,
					PriceInRub = leg.PriceInRub,
					IsBetweenNeighborPoints = leg.IsBetweenNeighborPoints,
				});
			}

			rideDto.Waypoints = waypointDtos;
			rideDto.Legs = legDtos;

			return new GetRideResponse
			{
				Car = carDto,
				Ride = rideDto,
			};
		}

		public async Task<IReadOnlyList<SearchRideResponse>> SearchRides(RideFilter filter, CancellationToken ct)
		{
			_rideFilterValidator.ValidateAndThrowFriendly(filter);

			var dbFilter = _rideFilterMapper.MapToDbFilter(filter);

			using var session = _sessionFactory.OpenPostgresConnection().StartTrace();

			var dbResult = await _rideRepository.GetByFilter(session, dbFilter, ct);

			var result = dbResult.Select(_searchRideResponseMapper.MapToResponse).ToArray();

			return result;
		}

		// private async ValueTask FillLegDescription(LegDto_Obsolete leg, CancellationToken ct)
		// {
		// 	var from = await _geocodeService.PointToGeoCode(leg.From.Point, ct);
		// 	_yaGeocodeResponseValidator.ValidateAndThrowFriendly((leg.From.Point, from));

		// 	var to = await _geocodeService.PointToGeoCode(leg.To.Point, ct);
		// 	_yaGeocodeResponseValidator.ValidateAndThrowFriendly((leg.To.Point, to));


		// 	var fromStr = from!.Geoobjects[0].FormattedAddress;
		// 	var toStr = to!.Geoobjects[0].FormattedAddress;

		// 	leg.Description = $"{fromStr}{_addressesDelimiter}{toStr}";
		// }

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

		public async ValueTask<(decimal Low, decimal High)> GetRecommendedPriceAsync(Point from, Point to, CancellationToken ct)
		{
			throw new NotImplementedException();
		}
	}
}
