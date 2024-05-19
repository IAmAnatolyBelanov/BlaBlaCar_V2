using Dapper;
using FluentValidation;
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
		public const string CarIdIsEmpty = "RideService_CarIdIsEmpty";
		public const string DriverDataDoesNotExist = "RideService_DriverDataDoesNotExist";
		public const string CarHasLessSeatsThanRideAvailablePlaces = "RideService_CarHasLessSeatsThanRideAvailablePlaces";
		public const string UnableToReserveRide = "RideService_UnableToReserveRide";
		public const string UnknownCoordinates = "RideService_UnknownCoordinates";
		public const string NotEnoughDataForStatistics = "RideService_NotEnoughDataForStatistics";

		public const string UnableToUpdateRide = "RideService_UnableToUpdateRide";
		public const string UnableToUpdateRideInPast = "RideService_UnableToUpdateRideInPast";
		public const string UnableToSetPassengersCountLessThanReserved = "RideService_UnableToSetPassengersCountLessThanReserved";
	}

	public interface IRideService
	{
		Task<RideDto> CreateRide(RideDto dto, CancellationToken ct);
		Task<IReadOnlyList<SearchRideResponse>> SearchRides(RideFilter filter, CancellationToken ct);
		Task<RideDto?> GetRideById(Guid rideId, CancellationToken ct);
		Task<ReservationDto> MakeReservation(MakeReservationRequest request, CancellationToken ct);
		Task<PriceRecommendation?> GetPriceRecommendation(GetPriceRecommendationRequest request, CancellationToken ct);
		Task<RideCounts?> GetCounts(RideFilter filter, CancellationToken ct);
		Task UpdateRideAvailablePlacesCount(Guid rideId, int count, CancellationToken ct);
	}

	public class RideService : IRideService
	{
		private const char _addressesDelimiter = '@';

		private readonly IRideServiceConfig _config;
		private readonly IRideMapper _rideDtoMapper;
		private readonly ILegDtoMapper _legDtoMapper;
		private readonly IGeocodeService _geocodeService;
		private readonly IValidator<(FormattedPoint Point, YandexGeocodeResponseDto GeocodeResponse)> _yaGeocodeResponseValidator;
		private readonly IReservationMapper _reservationMapper;
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
		private readonly IValidator<MakeReservationRequest> _makeReservationRequestValidator;
		private readonly IReservationRepository _reservationRepository;
		private readonly IRideCountsMapper _rideCountsMapper;

		public RideService(
			IRideServiceConfig config,
			IRideMapper rideDtoMapper,
			ILegDtoMapper legDtoMapper,
			IGeocodeService geocodeService,
			IValidator<(FormattedPoint Point, YandexGeocodeResponseDto GeocodeResponse)> yaGeocodeResponseValidator,
			IReservationMapper reservationMapper,
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
			ICarMapper carMapper,
			IValidator<MakeReservationRequest> makeReservationRequestValidator,
			IReservationRepository reservationRepository,
			IRideCountsMapper rideCountsMapper)
		{
			_config = config;
			_rideDtoMapper = rideDtoMapper;
			_legDtoMapper = legDtoMapper;
			_geocodeService = geocodeService;
			_yaGeocodeResponseValidator = yaGeocodeResponseValidator;
			_reservationMapper = reservationMapper;
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
			_makeReservationRequestValidator = makeReservationRequestValidator;
			_reservationRepository = reservationRepository;
			_rideCountsMapper = rideCountsMapper;
		}

		public async Task<RideDto> CreateRide(RideDto rideDto, CancellationToken ct)
		{
			var start = _clock.Now;

			ValidateRideOnCreation(rideDto);

			using var session = _sessionFactory.OpenPostgresConnection().BeginTransaction().StartTrace();

			var getAuthorTask = _userRepository.GetById(session, rideDto.AuthorId, ct);
			var getDriverTask = rideDto.DriverId == rideDto.AuthorId
				? getAuthorTask
				: _userRepository.GetById(session, rideDto.DriverId, ct);

			var author = await getAuthorTask;
			if (author is null)
				throw new UserFriendlyException(CommonValidationCodes.UserNotFound, $"Пользователь {rideDto.AuthorId} не найден");
			var driver = getAuthorTask is null ? null : await getAuthorTask;
			if (driver is null)
				throw new UserFriendlyException(CommonValidationCodes.UserNotFound, $"Пользователь {rideDto.DriverId} не найден");

			if (driver is not null)
			{
				var driverData = await _driverDataRepository.GetByUserId(session, driver.Id, ct);
				if (driverData is null)
					throw new UserFriendlyException(RideServiceValidationCodes.DriverDataDoesNotExist, "У пользователя, указанного как водитель, нет водительского удостоверения");
			}

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

		public async Task UpdateRideAvailablePlacesCount(Guid rideId, int count, CancellationToken ct)
		{
			if (count < 1)
				throw new UserFriendlyException(RideValidationCodes.TooLittlePassengerSeats, "Количество мест для пассажиров должно быть минимум 1");

			using var session = _sessionFactory.OpenPostgresConnection().BeginTransaction().StartTrace();

			var dbFilter = new RideDbFilter
			{
				RideIds = [rideId],
				Offset = 0,
				Limit = int.MaxValue,
			};

			var rides = await _rideRepository.GetByFilter(session, dbFilter, ct);

			if (rides.Count == 0)
				throw new UserFriendlyException(CommonValidationCodes.RideNotFound, $"Поездка {rideId} не найдена");

			var arrival = rides.Min(x => x.FromArrival);
			if (arrival < _clock.Now)
				throw new UserFriendlyException(RideServiceValidationCodes.UnableToUpdateRideInPast, "Невозможно изменить данные об уже начавшейся либо закончившейся поездке");

			var alreadyReserved = rides.Max(x => x.AlreadyReservedSeatsCount);
			if (count < alreadyReserved)
				throw new UserFriendlyException(RideServiceValidationCodes.CarHasLessSeatsThanRideAvailablePlaces, "Невозможно выставить количество доступных мест меньше, чем уже мест забронировано");

			await _rideRepository.UpdateAvailablePlacesCount(session, rideId, count, ct);
			await session.CommitAsync(ct);
		}

		public async Task<RideDto?> GetRideById(Guid rideId, CancellationToken ct)
		{
			using var session = _sessionFactory.OpenPostgresConnection();
			var ride = await _rideRepository.GetById(session, rideId, ct);

			if (ride is null)
				return null;

			var waypointsTask = _waypointRepository.GetByRideId(session, ride.Id, ct);
			var legsTask = _legRepository.GetByRideId(session, ride.Id, ct, onlyManual: true);

			var rideDto = _rideDtoMapper.ToRideDto(ride);

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

			return rideDto;
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

		public async Task<RideCounts?> GetCounts(RideFilter filter, CancellationToken ct)
		{
			_rideFilterValidator.ValidateAndThrowFriendly(filter);

			var dbFilter = _rideFilterMapper.MapToDbCountsFilter(filter);
			dbFilter.CloseDistanceInKilometers = _config.CloseDistanceInKilometers;
			dbFilter.MiddleDistanceInKilometers = _config.MiddleDistanceInKilometers;
			dbFilter.FarAwayDistanceInKilometers = _config.FarAwayDistanceInKilometers;

			using var session = _sessionFactory.OpenPostgresConnection().StartTrace();

			var dbResult = await _rideRepository.GetCounts(session, dbFilter, ct);

			var result = dbResult is not null ? _rideCountsMapper.ToCounts(dbResult, dbFilter) : null;

			return result;
		}

		public async Task<ReservationDto> MakeReservation(MakeReservationRequest request, CancellationToken ct)
		{
			// Сравнивать двоичные числа просто на равенство опасно. Поэтому приходится вычислять дистанцию.
			// Конфигом указано минимально допустимое расстояние между точками. Это же число ещё меньше.
			const float minDistanceInKilometers = 0.3f;

			var start = _clock.Now;

			_makeReservationRequestValidator.ValidateAndThrowFriendly(request);

			var session = _sessionFactory.OpenPostgresConnection().StartTrace().BeginTransaction();

			var rideFilter = new RideDbFilter
			{
				RideIds = [request.RideId!.Value],
				ArrivalPoint = request.WaypointTo!.Value.ToPoint(),
				ArrivalPointSearchRadiusKilometers = 0.3f,
				DeparturePoint = request.WaypointFrom!.Value.ToPoint(),
				DeparturePointSearchRadiusKilometers = 0.3f,
				Limit = 1,
				Offset = 0,
			};
			var rideTask = _rideRepository.GetByFilter(session, rideFilter, ct);
			var passengerTask = _userRepository.GetById(session, request.PassengerId!.Value, ct);
			var waypointsTask = _waypointRepository.GetByRideId(session, request.RideId.Value, ct);
			var legsTask = _legRepository.GetByRideId(session, request.RideId.Value, ct, onlyManual: false);

			var rides = await rideTask;

			if (rides.Count == 0)
				throw new UserFriendlyException(RideServiceValidationCodes.UnableToReserveRide, $"Невозможно забронировать поездку {request.RideId} - поездка удалена, не находится в статусе \"Подготовка\", или на указанный сегмент поездки нет свободных мест");

			var passenger = await passengerTask;
			if (passenger is null)
				throw new UserFriendlyException(CommonValidationCodes.UserNotFound, $"Пользователь {request.PassengerId} не найден");

			var waypoints = await waypointsTask;
			var waypointsDict = waypoints.ToDictionary(x => x.Id);

			var legs = await legsTask;
			var reservingLeg = legs.FirstOrDefault(x =>
			{
				var pointFrom = FormattedPoint.FromPoint(waypointsDict[x.WaypointFromId].Point);
				var distanceFrom = Haversine.CalculateDistanceInKilometers(pointFrom, request.WaypointFrom.Value);
				if (distanceFrom > minDistanceInKilometers)
					return false;

				var pointTo = FormattedPoint.FromPoint(waypointsDict[x.WaypointToId].Point);

				var distanceTo = Haversine.CalculateDistanceInKilometers(pointTo, request.WaypointTo.Value);
				if (distanceTo > minDistanceInKilometers)
					return false;

				return true;
			});

			if (reservingLeg is null)
				throw new UserFriendlyException(RideServiceValidationCodes.UnknownCoordinates, $"Не удалось найти сегмент пути по координатам {request.WaypointFrom} - {request.WaypointTo}");

			var affectedLegs = GetAffectedLegIds(legs, waypointsDict, reservingLeg);

			var reservation = new Reservation
			{
				Id = Guid.NewGuid(),
				RideId = request.RideId.Value,
				Created = start,
				LegId = reservingLeg.Id,
				PassengerId = passenger.Id,
				PeopleCount = request.PassengersCount!.Value,
				IsDeleted = false,
			};
			var reservationTask = _reservationRepository.InsertReservation(session, reservation, ct);
			var affectedLegsTask = _reservationRepository.BulkInsertAffectedLegs(
				session: session,
				reservationId: reservation.Id,
				legIds: affectedLegs,
				ct: ct);

			await reservationTask;
			await affectedLegsTask;

			await session.CommitAsync(ct);

			var result = _reservationMapper.ToDto(reservation);
			result.Leg = _legDtoMapper.ToDto(reservingLeg);

			return result;
		}

		private IReadOnlyList<Guid> GetAffectedLegIds(IReadOnlyList<Leg> allLegs, IReadOnlyDictionary<Guid, Waypoint> waypoints, Leg checkingLeg)
		{
			var result = new List<Guid>();
			result.Add(checkingLeg.Id);

			// https://stackoverflow.com/a/7325268 - How check intersection of DateTime periods

			var a1 = waypoints[checkingLeg.WaypointFromId].Departure;
			var a2 = waypoints[checkingLeg.WaypointToId].Arrival;

			foreach (var leg in allLegs)
			{
				if (leg.Id == checkingLeg.Id)
					continue;

				var b1 = waypoints[leg.WaypointFromId].Departure;
				var b2 = waypoints[leg.WaypointToId].Arrival;

				if (a1 < b2 && b1 < a2)
					result.Add(leg.Id);
			}

			return result;
		}

		public async Task<PriceRecommendation?> GetPriceRecommendation(GetPriceRecommendationRequest request, CancellationToken ct)
		{
			var now = _clock.Now;

			var dbRequest = new PriceRecommendationDbRequest
			{
				ArrivalDateFrom = now.Add(-_config.PriceStatisticsMaxPastPeriod),
				ArrivalDateTo = now,

				HigherPercentile = _config.PriceStatisticsHigherPercentile,
				MiddlePercentile = _config.PriceStatisticsMiddlePercentile,
				LowerPercentile = _config.PriceStatisticsLowerPercentile,

				PointFrom = request.PointFrom.ToPoint(),
				PointTo = request.PointTo.ToPoint(),

				RadiusInKilometers = _config.PriceStatisticsRadiusKilometers,
			};

			using var session = _sessionFactory.OpenPostgresConnection().StartTrace();

			var result = await _rideRepository.GetPriceRecommendation(session, dbRequest, ct);

			if (result is null || result.RowsCount < _config.PriceStatisticsMinRowsCount)
				throw new UserFriendlyException(RideServiceValidationCodes.NotEnoughDataForStatistics, "Недостаточно данных для построения статистики");

			if (result.MiddleRecommendedPrice - result.LowerRecommendedPrice < _config.PriceRecommendationMinStep)
				result.LowerRecommendedPrice = result.MiddleRecommendedPrice - _config.PriceRecommendationMinStep;

			if (result.HigherRecommendedPrice - result.MiddleRecommendedPrice < _config.PriceRecommendationMinStep)
				result.HigherRecommendedPrice = result.MiddleRecommendedPrice + _config.PriceRecommendationMinStep;

			return result;
		}
	}
}
