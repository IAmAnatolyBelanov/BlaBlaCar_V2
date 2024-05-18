using WebApi.Models;
using WebApi.Repositories;

namespace Tests;

public class LegRepositoryTests : BaseRepositoryTest
{
	private readonly IRideRepository _rideRepository;
	private readonly IUserRepository _userRepository;
	private readonly IWaypointRepository _waypointRepository;
	private readonly ILegRepository _legRepository;
	public LegRepositoryTests(TestAppFactoryWithDb fixture) : base(fixture)
	{
		_rideRepository = _provider.GetRequiredService<IRideRepository>();
		_userRepository = _provider.GetRequiredService<IUserRepository>();
		_waypointRepository = _provider.GetRequiredService<IWaypointRepository>();
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
			.Create();

		var waypoints = _fixture.Build<Waypoint>()
			.With(x => x.RideId, ride.Id)
			.Without(x => x.NextWaypointId)
			.Without(x => x.PreviousWaypointId)
			.CreateMany(3)
			.ToArray();

		var legs = new List<Leg>();
		legs.Add(new Leg
		{
			Id = Guid.NewGuid(),
			PriceInRub = _fixture.Create<int>(),
			RideId = ride.Id,
			WaypointFromId = waypoints[0].Id,
			WaypointToId = waypoints[1].Id,
		});
		legs.Add(new Leg
		{
			Id = Guid.NewGuid(),
			PriceInRub = _fixture.Create<int>(),
			RideId = ride.Id,
			WaypointFromId = waypoints[1].Id,
			WaypointToId = waypoints[2].Id,
		});

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, ct);
			await _rideRepository.Insert(session, ride, ct);
			await _waypointRepository.BulkInsert(session, waypoints, ct);
			var result = await _legRepository.BulkInsert(session, legs, ct);
			await session.CommitAsync(ct);

			result.Should().Be((ulong)legs.Count);
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
			.Create();

		var waypoints = _fixture.Build<Waypoint>()
			.With(x => x.RideId, ride.Id)
			.Without(x => x.NextWaypointId)
			.Without(x => x.PreviousWaypointId)
			.CreateMany(3)
			.ToArray();
		waypoints.Last().Departure = null;

		var legs = new List<Leg>();
		legs.Add(new Leg
		{
			Id = Guid.NewGuid(),
			PriceInRub = _fixture.Create<int>(),
			RideId = ride.Id,
			WaypointFromId = waypoints[0].Id,
			WaypointToId = waypoints[1].Id,
		});
		legs.Add(new Leg
		{
			Id = Guid.NewGuid(),
			PriceInRub = _fixture.Create<int>(),
			RideId = ride.Id,
			WaypointFromId = waypoints[1].Id,
			WaypointToId = waypoints[2].Id,
		});

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, ct);
			await _rideRepository.Insert(session, ride, ct);
			await _waypointRepository.BulkInsert(session, waypoints, ct);
			await _legRepository.BulkInsert(session, legs, ct);

			await session.CommitAsync(ct);
		}

		using (var session = _sessionFactory.OpenPostgresConnection())
		{
			var result = await _legRepository.GetByRideId(session, ride.Id, ct, onlyManual: false);
			var orderedLegs = legs.Select(leg => new
			{
				Leg = leg,
				From = waypoints.First(x => x.Id == leg.WaypointFromId),
				To = waypoints.First(x => x.Id == leg.WaypointToId),
			}).OrderBy(x => x.From.Departure ?? DateTimeOffset.MaxValue)
				.ThenBy(x => x.To.Arrival)
				.Select(x => x.Leg);
			result.Should().BeEquivalentTo(orderedLegs, options => options.WithStrictOrdering());
		}
	}
}
