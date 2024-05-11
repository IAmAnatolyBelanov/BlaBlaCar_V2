using WebApi;
using WebApi.Models;
using WebApi.Repositories;

namespace Tests;

public class DriverDataRepositoryTests : BaseRepositoryTest
{
	private IDriverDataRepository _repo;
	private IUserRepository _userRepository;

	public DriverDataRepositoryTests(TestAppFactoryWithDb fixture) : base(fixture)
	{
		_repo = _provider.GetRequiredService<IDriverDataRepository>();
		_userRepository = _provider.GetRequiredService<IUserRepository>();
	}

	[Fact]
	public async Task InsertWithUserIdTest()
	{
		var user = _fixture.Create<User>();
		var personData = _fixture.Build<DriverData>()
			.With(x => x.UserId, user.Id)
			.With(x => x.IsValid, true)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, CancellationToken.None);
			var result = await _repo.Insert(session, personData, CancellationToken.None);

			await session.CommitAsync(CancellationToken.None);

			result.Should().Be(1);
		}
	}

	[Fact]
	public async Task InsertWithoutUserIdTest()
	{
		var personData = _fixture.Build<DriverData>()
			.With(x => x.UserId, (Guid?)null)
			.With(x => x.IsValid, true)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			var result = await _repo.Insert(session, personData, CancellationToken.None);

			await session.CommitAsync(CancellationToken.None);

			result.Should().Be(1);
		}
	}

	[Fact]
	public async Task GetByDrivingLicenseTest()
	{
		var driverData = _fixture.Build<DriverData>()
			.With(x => x.UserId, (Guid?)null)
			.With(x => x.IsValid, true)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _repo.Insert(session, driverData, CancellationToken.None);

			await session.CommitAsync(CancellationToken.None);
		}

		using (var session = _sessionFactory.OpenPostgresConnection())
		{
			var result = await _repo.GetByDrivingLicense(
				session: session,
				licenseSeries: driverData.LicenseSeries,
				licenseNumber: driverData.LicenseNumber,
				ct: CancellationToken.None);

			result.Should().BeEquivalentTo(driverData);
		}
	}

	[Fact]
	public async Task GetByUserIdTest()
	{
		var user = _fixture.Create<User>();
		var driverData = _fixture.Build<DriverData>()
			.With(x => x.UserId, user.Id)
			.With(x => x.IsValid, true)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, CancellationToken.None);
			await _repo.Insert(session, driverData, CancellationToken.None);

			await session.CommitAsync(CancellationToken.None);
		}

		using (var session = _sessionFactory.OpenPostgresConnection())
		{
			var result = await _repo.GetByUserId(
				session: session,
				userId: user.Id,
				ct: CancellationToken.None);

			result.Should().BeEquivalentTo(driverData);
		}
	}
}
