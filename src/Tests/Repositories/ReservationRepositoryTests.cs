using WebApi.Models;
using WebApi.Repositories;

namespace Tests;

public class ReservationRepositoryTests : BaseRepositoryTest
{
	private readonly IRideRepository _rideRepository;
	private readonly IUserRepository _userRepository;
	private readonly IWaypointRepository _waypointRepository;
	private readonly IReservationRepository _reservationRepository;
	private readonly ILegRepository _legRepository;

	public ReservationRepositoryTests(TestAppFactoryWithDb fixture) : base(fixture)
	{
		_rideRepository = _provider.GetRequiredService<IRideRepository>();
		_userRepository = _provider.GetRequiredService<IUserRepository>();
		_waypointRepository = _provider.GetRequiredService<IWaypointRepository>();
		_reservationRepository = _provider.GetRequiredService<IReservationRepository>();
		_legRepository = _provider.GetRequiredService<ILegRepository>();
	}

	[Fact]
	public async Task InsertTest()
	{
		var ct = CancellationToken.None;

		var user = _fixture.Create<User>();
		var ride = _fixture.Build<Ride>()
			.With(x => x.AuthorId, user.Id)
			.Without(x => x.DriverId)
			.Without(x => x.CarId)
			.Create();

		var waypoints = _fixture.Build<Waypoint>()
			.With(x => x.RideId, ride.Id)
			.Without(x => x.NextWaypointId)
			.Without(x => x.PreviousWaypointId)
			.CreateMany(3)
			.ToArray();

		var legs = new Leg[]
		{
			new Leg
			{
				Id = Guid.NewGuid(),
				RideId = ride.Id,
				WaypointFromId = waypoints[0].Id,
				WaypointToId = waypoints[1].Id,
				IsManual = false,
				IsMinimal = true,
				PriceInRub = _fixture.Create<int>(),
			},
			new Leg
			{
				Id = Guid.NewGuid(),
				RideId = ride.Id,
				WaypointFromId = waypoints[1].Id,
				WaypointToId = waypoints[2].Id,
				IsManual = false,
				IsMinimal = true,
				PriceInRub = _fixture.Create<int>(),
			},
		};

		var reservation = _fixture.Build<Reservation>()
			.With(x => x.RideId, ride.Id)
			.With(x => x.PassengerId, user.Id)
			.With(x => x.LegId, legs[0].Id)
			.With(x => x.IsDeleted, false)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, ct);
			await _rideRepository.Insert(session, ride, ct);
			await _waypointRepository.BulkInsert(session, waypoints, ct);
			await _legRepository.BulkInsert(session, legs, ct);
			var result = await _reservationRepository.InsertLeg(session, reservation, ct);
			await session.CommitAsync(ct);

			result.Should().Be(1);
		}
	}

	[Fact]
	public async Task GetByDefaultFilterTest()
	{
		var ct = CancellationToken.None;

		var user = _fixture.Create<User>();
		var ride = _fixture.Build<Ride>()
			.With(x => x.AuthorId, user.Id)
			.Without(x => x.DriverId)
			.Without(x => x.CarId)
			.Create();

		var waypoints = _fixture.Build<Waypoint>()
			.With(x => x.RideId, ride.Id)
			.Without(x => x.NextWaypointId)
			.Without(x => x.PreviousWaypointId)
			.CreateMany(3)
			.ToArray();

		var legs = new Leg[]
		{
			new Leg
			{
				Id = Guid.NewGuid(),
				RideId = ride.Id,
				WaypointFromId = waypoints[0].Id,
				WaypointToId = waypoints[1].Id,
				IsManual = false,
				IsMinimal = true,
				PriceInRub = _fixture.Create<int>(),
			},
			new Leg
			{
				Id = Guid.NewGuid(),
				RideId = ride.Id,
				WaypointFromId = waypoints[1].Id,
				WaypointToId = waypoints[2].Id,
				IsManual = false,
				IsMinimal = true,
				PriceInRub = _fixture.Create<int>(),
			},
		};

		var reservation = _fixture.Build<Reservation>()
			.With(x => x.RideId, ride.Id)
			.With(x => x.PassengerId, user.Id)
			.With(x => x.LegId, legs[0].Id)
			.With(x => x.IsDeleted, false)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, ct);
			await _rideRepository.Insert(session, ride, ct);
			await _waypointRepository.BulkInsert(session, waypoints, ct);
			await _legRepository.BulkInsert(session, legs, ct);
			await _reservationRepository.InsertLeg(session, reservation, ct);

			await session.CommitAsync(ct);
		}

		using (var session = _sessionFactory.OpenPostgresConnection())
		{
			var filter = new ReservationDbFilter
			{
				Offset = 0,
				Limit = int.MaxValue,
			};
			var result = await _reservationRepository.GetLegsByFilter(session, filter, ct);
			result.Should().ContainEquivalentOf(reservation);
		}
	}

	[Fact]
	public async Task GetByAllFilters()
	{
		var ct = CancellationToken.None;
		using (var session = _sessionFactory.OpenPostgresConnection())
		{
			var filter = _fixture.Build<ReservationDbFilter>()
				.With(x => x.HideDeleted, false)
				.With(x => x.Offset, 0)
				.With(x => x.Limit, int.MaxValue)
				.Create();
			var result = await _reservationRepository.GetLegsByFilter(session, filter, ct);
			result.Should().NotBeNull();
		}
	}

	[Fact]
	public async Task InsertAffectedLegsTest()
	{
		var ct = CancellationToken.None;

		var user = _fixture.Create<User>();
		var ride = _fixture.Build<Ride>()
			.With(x => x.AuthorId, user.Id)
			.Without(x => x.DriverId)
			.Without(x => x.CarId)
			.Create();

		var waypoints = _fixture.Build<Waypoint>()
			.With(x => x.RideId, ride.Id)
			.Without(x => x.NextWaypointId)
			.Without(x => x.PreviousWaypointId)
			.CreateMany(3)
			.ToArray();

		var legs = new Leg[]
		{
			new Leg
			{
				Id = Guid.NewGuid(),
				RideId = ride.Id,
				WaypointFromId = waypoints[0].Id,
				WaypointToId = waypoints[1].Id,
				IsManual = false,
				IsMinimal = true,
				PriceInRub = _fixture.Create<int>(),
			},
			new Leg
			{
				Id = Guid.NewGuid(),
				RideId = ride.Id,
				WaypointFromId = waypoints[1].Id,
				WaypointToId = waypoints[2].Id,
				IsManual = false,
				IsMinimal = true,
				PriceInRub = _fixture.Create<int>(),
			},
		};

		var reservation = _fixture.Build<Reservation>()
			.With(x => x.RideId, ride.Id)
			.With(x => x.PassengerId, user.Id)
			.With(x => x.LegId, legs[0].Id)
			.With(x => x.IsDeleted, false)
			.Create();

		Guid[] affectedLegs = [legs[0].Id];

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, ct);
			await _rideRepository.Insert(session, ride, ct);
			await _waypointRepository.BulkInsert(session, waypoints, ct);
			await _legRepository.BulkInsert(session, legs, ct);
			await _reservationRepository.InsertLeg(session, reservation, ct);
			var result = await _reservationRepository.BulkInsertAffectedLegs(session, reservation.Id, affectedLegs, ct);
			await session.CommitAsync(ct);

			result.Should().Be(1);
		}
	}
}