using NetTopologySuite.Geometries;
using System.Diagnostics;
using WebApi.Models;
using WebApi.Models.ControllersModels.RideControllerModels;
using WebApi.Repositories;

namespace Tests;

public class RideRepositoryTests : BaseRepositoryTest
{
	private readonly IRideRepository _rideRepository;
	private readonly IUserRepository _userRepository;
	private readonly ICarRepository _carRepository;
	private readonly IWaypointRepository _waypointRepository;
	private readonly ILegRepository _legRepository;
	private readonly IReservationRepository _reservationRepository;

	public RideRepositoryTests(TestAppFactoryWithDb fixture) : base(fixture)
	{
		_rideRepository = _provider.GetRequiredService<IRideRepository>();
		_userRepository = _provider.GetRequiredService<IUserRepository>();
		_carRepository = _provider.GetRequiredService<ICarRepository>();
		_waypointRepository = _provider.GetRequiredService<IWaypointRepository>();
		_legRepository = _provider.GetRequiredService<ILegRepository>();
		_reservationRepository = _provider.GetRequiredService<IReservationRepository>();
	}

	[Fact]
	public async Task InsertTest()
	{
		var ct = CancellationToken.None;

		var user = _fixture.Create<User>();
		var ride = _fixture.Build<Ride>()
			.With(x => x.AuthorId, user.Id)
			.With(x => x.DriverId, user.Id)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, ct);
			var result = await _rideRepository.Insert(session, ride, ct);
			await session.CommitAsync(ct);

			result.Should().Be(1);
		}
	}

	[Fact]
	public async Task GetByIdTest()
	{
		var ct = CancellationToken.None;

		var user = _fixture.Create<User>();
		var ride = _fixture.Build<Ride>()
			.With(x => x.AuthorId, user.Id)
			.With(x => x.DriverId, user.Id)
			.With(x => x.IsDeleted, false)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, ct);
			await _rideRepository.Insert(session, ride, ct);
			await session.CommitAsync(ct);
		}

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			var result = await _rideRepository.GetById(session, ride.Id, ct);
			result.Should().BeEquivalentTo(ride);
		}
	}

	[Fact]
	public async Task InsertWithAllForeignKeys()
	{
		var ct = CancellationToken.None;

		var user = _fixture.Create<User>();
		var car = _fixture.Create<Car>();
		var ride = _fixture.Build<Ride>()
			.With(x => x.AuthorId, user.Id)
			.With(x => x.DriverId, user.Id)
			.With(x => x.IsDeleted, false)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, ct);
			await _carRepository.Insert(session, car, ct);
			await _rideRepository.Insert(session, ride, ct);
			await session.CommitAsync(ct);
		}

		using (var session = _sessionFactory.OpenPostgresConnection())
		{
			var result = await _rideRepository.GetById(session, ride.Id, ct);
			result.Should().BeEquivalentTo(ride);
		}
	}

	//[Fact]
	// Тест очень дорогой. Запускать только вручную.
	public async Task GetByFilterTestOnHugeAmountOfRides()
	{
		var ct = CancellationToken.None;

		var options = new ParallelOptions { MaxDegreeOfParallelism = 100 };

		var kek = Parallel.ForAsync(0, 1000, options, async (index, ct) =>
		{
			await Task.Delay(index);
			try
			{
				var user = _fixture.Create<User>();
				var rides = _fixture.Build<Ride>()
					.With(x => x.AuthorId, user.Id)
					.With(x => x.DriverId)
					.With(x => x.IsDeleted, false)
					.CreateMany(300);

				using (var session = _sessionFactory.OpenPostgresConnection())
				{
					await _userRepository.Insert(session, user, ct);

					foreach (var ride in rides)
					{
						await _rideRepository.Insert(session, ride, ct);

						const int pointsCount = 8;

						var points = _fixture.Build<Waypoint>()
							.With(x => x.RideId, ride.Id)
							.Without(x => x.NextWaypointId)
							.Without(x => x.PreviousWaypointId)
							.CreateMany(pointsCount)
							.ToArray();

						points[0].Arrival = points[0].Departure!.Value;
						points[0].NextWaypointId = points[1].Id;
						points[pointsCount - 1].Departure = null;
						points[pointsCount - 1].PreviousWaypointId = points[pointsCount - 2].Id;

						for (int i = 1; i < pointsCount - 1; i++)
						{
							points[i - 1].NextWaypointId = points[i].Id;
							points[i].PreviousWaypointId = points[i - 1].Id;
							points[i].NextWaypointId = points[i + 1].Id;
							points[i + 1].PreviousWaypointId = points[i].Id;
							var formattedPoint = FormattedPoint.FromPoint(points[i].Point);
							formattedPoint = new FormattedPoint
							{
								Longitude = formattedPoint.Longitude + Random.Shared.NextDouble(),
								Latitude = formattedPoint.Latitude + Random.Shared.NextDouble(),
							};
							points[i].Point = formattedPoint.ToPoint();
						}

						await _waypointRepository.BulkInsert(session, points, ct);

						var legs = points.Where(x => x.NextWaypointId is not null)
							.Select(x => new Leg()
							{
								Id = Guid.NewGuid(),
								PriceInRub = Random.Shared.Next(10, 100),
								RideId = ride.Id,
								WaypointFromId = x.Id,
								WaypointToId = x.NextWaypointId!.Value,
							}).ToArray();

						await _legRepository.BulkInsert(session, legs, ct);
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		});

		await kek;

		var filter = _fixture.Build<RideDbFilter>()
			.With(x => x.HideDeleted, true)
			.With(x => x.SortType, RideSortType.ByPrice)
			.With(x => x.SortDirection, SortDirection.Asc)
			.With(x => x.PaymentMethods, new PaymentMethod[] { PaymentMethod.Cash, PaymentMethod.Cashless })
			.With(x => x.Offset, 0)
			.With(x => x.Limit, int.MaxValue)
			.Create();

		filter = new RideDbFilter
		{
			Offset = 0,
			Limit = int.MaxValue - 1000,
			ArrivalPoint = CityInfoManager.GetUnique().GetPoint().ToPoint(),
			ArrivalPointSearchRadiusKilometers = 2_000_000,
			DeparturePoint = CityInfoManager.GetUnique().GetPoint().ToPoint(),
			MaxPriceInRub = 8_000_000,
			SortDirection = SortDirection.Asc,
			SortType = RideSortType.ByStartTime,
			MinArrivalTime = DateTimeOffset.UtcNow.AddYears(-30),
			MaxArrivalTime = DateTimeOffset.UtcNow.AddYears(50),
			FreeSeatsCount = 1,
		};

		using (var session = _sessionFactory.OpenPostgresConnection().StartTrace())
		{
			var counts = await _rideRepository.GetCounts(session, ct);
			var timer = Stopwatch.StartNew();
			var result = await _rideRepository.GetByFilter(session, filter, ct);
			result.Should().NotBeNull();
			timer.Stop();
			var elapsedInfo = $"Elapsed {timer.Elapsed}, found {result.Count}, counts {counts}";
			elapsedInfo.Should().BeEmpty();
		}
	}

	[Fact]
	public async Task GetByFilterTest()
	{
		var ct = CancellationToken.None;

		var filter = _fixture.Build<RideDbFilter>()
			.With(x => x.SortType, RideSortType.ByFreeSeatsCount)
			.With(x => x.SortDirection, SortDirection.Asc)
			.With(x => x.PaymentMethods, new PaymentMethod[] { PaymentMethod.Cash, PaymentMethod.Cashless })
			.Create();

		using var session = _sessionFactory.OpenPostgresConnection();

		var result = await _rideRepository.GetByFilter(session, filter, ct);
		result.Should().NotBeNull();
	}

	[Fact]
	public async Task PricesTest()
	{
		var ct = CancellationToken.None;

		var pointFrom = _fixture.Create<FormattedPoint>();
		var pointTo = _fixture.Create<FormattedPoint>();

		var yesterday = DateTimeOffset.UtcNow.AddDays(-1);

		const int usersCount = 15;
		const int ridesCount = 10;
		const int reservationsCount = 4;

		await Parallel.ForAsync(0, usersCount, async (_, _) =>
		{
			using var session = _sessionFactory.OpenPostgresConnection();
			var user = _fixture.Create<User>();
			await _userRepository.Insert(session, user, ct);
			for (int i = 0; i < ridesCount; i++)
			{
				var ride = _fixture.Build<Ride>()
					.With(x => x.DriverId, user.Id)
					.With(x => x.AuthorId, user.Id)
					.With(x => x.IsDeleted, false)
					.With(x => x.Created, yesterday.AddHours(-8))
					.Create();

				await _rideRepository.Insert(session, ride, ct);

				var from = new Waypoint
				{
					Id = Guid.NewGuid(),
					Arrival = yesterday.AddHours(-1),
					Departure = yesterday.AddHours(-2),
					FullName = "test",
					NameToCity = "test",
					Point = pointFrom.ToPoint(),
					RideId = ride.Id,
				};
				var to = new Waypoint
				{
					Id = Guid.NewGuid(),
					Arrival = yesterday.AddHours(1),
					Departure = null,
					FullName = "test",
					NameToCity = "test",
					Point = pointTo.ToPoint(),
					RideId = ride.Id,
				};

				from.NextWaypointId = to.Id;
				to.PreviousWaypointId = from.Id;

				await _waypointRepository.BulkInsert(session, [from, to], ct);

				var leg = new Leg
				{
					Id = Guid.NewGuid(),
					IsBetweenNeighborPoints = true,
					IsManual = true,
					PriceInRub = Random.Shared.Next(1000, 15_000),
					RideId = ride.Id,
					WaypointFromId = from.Id,
					WaypointToId = to.Id,
				};

				await _legRepository.BulkInsert(session, [leg], ct);

				for (int j = 0; j < reservationsCount; j++)
				{
					var reservation = new Reservation
					{
						Id = Guid.NewGuid(),
						Created = yesterday.AddHours(-7),
						IsDeleted = false,
						LegId = leg.Id,
						PassengerId = user.Id,
						PeopleCount = 1,
						RideId = ride.Id,
					};
					await _reservationRepository.InsertReservation(session, reservation, ct);
					await _reservationRepository.BulkInsertAffectedLegs(session, reservation.Id, [leg.Id], ct);
				}
			}
		});

		var request = new PriceRecommendationDbRequest
		{
			ArrivalDateFrom = yesterday.AddDays(-3),
			ArrivalDateTo = yesterday.AddDays(3),
			HigherPercentile = 0.9f,
			MiddlePercentile = 0.8f,
			LowerPercentile = 0.7f,
			PointFrom = pointFrom.ToPoint(),
			PointTo = pointTo.ToPoint(),
			RadiusInKilometers = 1,
		};

		using var session = _sessionFactory.OpenPostgresConnection();

		var priceRecommendation = await _rideRepository.GetPriceRecommendation(session, request, ct);

		priceRecommendation.Should().NotBeNull();
		priceRecommendation!.RowsCount.Should().Be(usersCount * ridesCount * reservationsCount);
		priceRecommendation.HigherRecommendedPrice.Should().BeGreaterThanOrEqualTo(priceRecommendation.LowerRecommendedPrice);
	}

	[Fact]
	public async Task CountersTest()
	{
		var ct = CancellationToken.None;

		using var session = _sessionFactory.OpenPostgresConnection();

		var rideFilter = new RideDbCountsFilter
		{
			MinArrivalTime = DateTimeOffset.Now.AddYears(-50),
			DeparturePoint = CityInfoManager.GetUnique().GetPoint().ToPoint(),
			ArrivalPoint = CityInfoManager.GetUnique().GetPoint().ToPoint(),
			CloseDistanceInKilometers = 5,
			MiddleDistanceInKilometers = 15,
			FarAwayDistanceInKilometers = 40,
		};

		var result = await _rideRepository.GetCounts(session, rideFilter, ct);

		result.Should().NotBeNull();
	}

	[Fact]
	public async Task UpdateAvailablePlacesCountTest()
	{
		var ct = CancellationToken.None;

		var user = _fixture.Create<User>();
		var ride = _fixture.Build<Ride>()
			.With(x => x.AuthorId, user.Id)
			.With(x => x.DriverId, user.Id)
			.With(x => x.IsDeleted, false)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, ct);
			await _rideRepository.Insert(session, ride, ct);
			await session.CommitAsync(ct);
		}

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			var result = await _rideRepository.UpdateAvailablePlacesCount(session, ride.Id, _fixture.Create<int>(), ct);
			result.Should().Be(1);
		}
	}

	[Fact]
	public async Task DeleteRideTest()
	{
		var ct = CancellationToken.None;

		var user = _fixture.Create<User>();
		var ride = _fixture.Build<Ride>()
			.With(x => x.AuthorId, user.Id)
			.With(x => x.DriverId, user.Id)
			.With(x => x.IsDeleted, false)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, ct);
			await _rideRepository.Insert(session, ride, ct);
			await session.CommitAsync(ct);
		}

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			var result = await _rideRepository.DeleteRide(session, ride.Id, ct);
			result.Should().Be(1);
		}
	}
}