using WebApi.Models;
using WebApi.Repositories;

namespace Tests;

public class ReservationRepositoryTests : BaseRepositoryTest
{
	private readonly IRideRepository _rideRepository;
	private readonly IUserRepository _userRepository;
	private readonly IWaypointRepository _waypointRepository;
	private readonly IReservationRepository _reservationRepository;
	public ReservationRepositoryTests(TestAppFactoryWithDb fixture) : base(fixture)
	{
		_rideRepository = _provider.GetRequiredService<IRideRepository>();
		_userRepository = _provider.GetRequiredService<IUserRepository>();
		_waypointRepository = _provider.GetRequiredService<IWaypointRepository>();
		_reservationRepository = _provider.GetRequiredService<IReservationRepository>();
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

		var reservation = _fixture.Build<Reservation>()
			.With(x => x.RideId, ride.Id)
			.With(x => x.PassengerId, user.Id)
			.With(x => x.WaypointFromId, waypoints[0].Id)
			.With(x => x.WaypointToId, waypoints[1].Id)
			.With(x => x.IsDeleted, false)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, ct);
			await _rideRepository.Insert(session, ride, ct);
			await _waypointRepository.BulkInsert(session, waypoints, ct);
			var result = await _reservationRepository.Insert(session, reservation, ct);
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

		var reservation = _fixture.Build<Reservation>()
			.With(x => x.RideId, ride.Id)
			.With(x => x.PassengerId, user.Id)
			.With(x => x.WaypointFromId, waypoints[0].Id)
			.With(x => x.WaypointToId, waypoints[1].Id)
			.With(x => x.IsDeleted, false)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, ct);
			await _rideRepository.Insert(session, ride, ct);
			await _waypointRepository.BulkInsert(session, waypoints, ct);
			await _reservationRepository.Insert(session, reservation, ct);

			await session.CommitAsync(ct);
		}

		using (var session = _sessionFactory.OpenPostgresConnection())
		{
			var filter = new ReservationDbFilter
			{
				Offset = 0,
				Limit = int.MaxValue,
			};
			var result = await _reservationRepository.GetByFilter(session, filter, ct);
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
			var result = await _reservationRepository.GetByFilter(session, filter, ct);
			result.Should().NotBeNull();
		}
	}
}