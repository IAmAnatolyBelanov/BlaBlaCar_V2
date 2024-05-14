using WebApi.Models;
using WebApi.Repositories;

namespace Tests;

public class RideRepositoryTests : BaseRepositoryTest
{
	private readonly IRideRepository _rideRepository;
	private readonly IUserRepository _userRepository;
	private readonly ICarRepository _carRepository;

	public RideRepositoryTests(TestAppFactoryWithDb fixture) : base(fixture)
	{
		_rideRepository = _provider.GetRequiredService<IRideRepository>();
		_userRepository = _provider.GetRequiredService<IUserRepository>();
		_carRepository = _provider.GetRequiredService<ICarRepository>();
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
			.Without(x => x.DriverId)
			.Without(x => x.CarId)
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
			.With(x => x.CarId, car.Id)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, ct);
			await _carRepository.Insert(session, car, ct);
			await _rideRepository.Insert(session, ride, ct);
			await session.CommitAsync(ct);
		}

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			var result = await _rideRepository.GetById(session, ride.Id, ct);
			result.Should().BeEquivalentTo(ride);
		}
	}
}