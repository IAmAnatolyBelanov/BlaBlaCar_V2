using WebApi.Models;
using WebApi.Repositories;

namespace Tests;

public class CarRepositoryTest : BaseRepositoryTest
{
	private readonly ICarUserRepository _carUserRepository;
	private readonly ICarRepository _carRepository;
	private readonly IUserRepository _userRepository;

	public CarRepositoryTest(TestAppFactoryWithDb fixture) : base(fixture)
	{
		_carUserRepository = _provider.GetRequiredService<ICarUserRepository>();
		_carRepository = _provider.GetRequiredService<ICarRepository>();
		_userRepository = _provider.GetRequiredService<IUserRepository>();
	}

	[Fact]
	public async Task InsertTest()
	{
		var ct = CancellationToken.None;

		var car = _fixture.Build<Car>().Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			var result = await _carRepository.Insert(session, car, ct);
			await session.CommitAsync(ct);
			result.Should().Be(1);
		}
	}

	[Fact]
	public async Task InsertAndGetByUser()
	{
		var ct = CancellationToken.None;

		var user = _fixture.Build<User>().Create();
		var car = _fixture.Build<Car>().Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, ct);
			await _carRepository.Insert(session, car, ct);
			await _carUserRepository.Insert(session, car.Id, user.Id, ct);

			await session.CommitAsync(ct);
		}

		using (var session = _sessionFactory.OpenPostgresConnection())
		{
			var result = await _carRepository.GetByUserId(session, user.Id, ct);
			result.Should().HaveCount(1);
			result[0].Should().BeEquivalentTo(car);
		}
	}

	[Fact]
	public async Task InsertAndGetById()
	{
		var ct = CancellationToken.None;

		var user = _fixture.Build<User>().Create();
		var car = _fixture.Build<Car>().Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, ct);
			await _carRepository.Insert(session, car, ct);
			await _carUserRepository.Insert(session, car.Id, user.Id, ct);

			await session.CommitAsync(ct);
		}

		using (var session = _sessionFactory.OpenPostgresConnection())
		{
			var result = await _carRepository.GetById(session, car.Id, ct);
			result.Should().BeEquivalentTo(car);
		}
	}
}
