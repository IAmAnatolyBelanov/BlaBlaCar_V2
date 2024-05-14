using WebApi.Models;
using WebApi.Repositories;

namespace Tests;

public class WaypointRepositoryTests : BaseRepositoryTest
{
	private readonly IRideRepository _rideRepository;
	private readonly IUserRepository _userRepository;
	private readonly IWaypointRepository _waypointRepository;

	public WaypointRepositoryTests(TestAppFactoryWithDb fixture) : base(fixture)
	{
		_rideRepository = _provider.GetRequiredService<IRideRepository>();
		_userRepository = _provider.GetRequiredService<IUserRepository>();
		_waypointRepository = _provider.GetRequiredService<IWaypointRepository>();
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
			.CreateMany(3)
			.ToArray();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, ct);
			await _rideRepository.Insert(session, ride, ct);
			var result = await _waypointRepository.BulkInsert(session, waypoints, ct);
			await session.CommitAsync(ct);

			result.Should().Be((ulong)waypoints.Length);
		}
	}

	[Fact]
	public async Task GetByRideIdTest()
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
			.CreateMany(50)
			.ToArray();
		waypoints.Last().Departure = null;

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, ct);
			await _rideRepository.Insert(session, ride, ct);
			await _waypointRepository.BulkInsert(session, waypoints, ct);

			await session.CommitAsync(ct);
		}

		using (var session = _sessionFactory.OpenPostgresConnection())
		{
			var result = await _waypointRepository.GetByRideId(session, ride.Id, ct);
			var orderedWaypoints = waypoints.OrderBy(x => x.Arrival).ThenBy(x => x.Departure ?? DateTimeOffset.MaxValue);
			result.Should().BeEquivalentTo(orderedWaypoints, options => options.WithStrictOrdering());
		}
	}
}
