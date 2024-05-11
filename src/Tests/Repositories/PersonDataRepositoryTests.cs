using WebApi.Models;
using WebApi.Repositories;

namespace Tests;

public class PersonDataRepositoryTests : BaseRepositoryTest
{
	private readonly IPersonDataRepository _repo;
	private readonly IUserRepository _userRepository;

	public PersonDataRepositoryTests(TestAppFactoryWithDb fixture) : base(fixture)
	{
		_repo = _provider.GetRequiredService<IPersonDataRepository>();
		_userRepository = _provider.GetRequiredService<IUserRepository>();
	}

	[Fact]
	public async Task InsertWithUserIdTest()
	{
		var user = _fixture.Create<User>();
		var personData = _fixture.Build<PersonData>()
			.With(x => x.UserId, user.Id)
			.With(x => x.IsPassportValid, true)
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
		var personData = _fixture.Build<PersonData>()
			.With(x => x.UserId, (Guid?)null)
			.With(x => x.IsPassportValid, true)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			var result = await _repo.Insert(session, personData, CancellationToken.None);

			await session.CommitAsync(CancellationToken.None);

			result.Should().Be(1);
		}
	}

	[Fact]
	public async Task GetByPassportTest()
	{
		var personData = _fixture.Build<PersonData>()
			.With(x => x.UserId, (Guid?)null)
			.With(x => x.IsPassportValid, true)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _repo.Insert(session, personData, CancellationToken.None);

			await session.CommitAsync(CancellationToken.None);
		}

		using (var session = _sessionFactory.OpenPostgresConnection())
		{
			var result = await _repo.GetByPassport(
				session: session,
				passportSeries: personData.PassportSeries,
				passportNumber: personData.PassportNumber,
				ct: CancellationToken.None);

			result.Should().BeEquivalentTo(personData);
		}
	}

	[Fact]
	public async Task GetByUserIdTest()
	{
		var user = _fixture.Create<User>();
		var personData = _fixture.Build<PersonData>()
			.With(x => x.UserId, user.Id)
			.With(x => x.IsPassportValid, true)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, CancellationToken.None);
			await _repo.Insert(session, personData, CancellationToken.None);

			await session.CommitAsync(CancellationToken.None);
		}

		using (var session = _sessionFactory.OpenPostgresConnection())
		{
			var result = await _repo.GetByUserId(session, user.Id, CancellationToken.None);
			result.Should().BeEquivalentTo(personData);
		}
	}

	[Fact]
	public async Task GetByIdTest()
	{
		var personData = _fixture.Build<PersonData>()
			.With(x => x.UserId, (Guid?)null)
			.With(x => x.IsPassportValid, true)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _repo.Insert(session, personData, CancellationToken.None);

			await session.CommitAsync(CancellationToken.None);
		}

		using (var session = _sessionFactory.OpenPostgresConnection())
		{
			var result = await _repo.GetById(session, personData.Id, CancellationToken.None);
			result.Should().BeEquivalentTo(personData);
		}
	}

	[Fact]
	public async Task UpdateUserIdTest()
	{
		var user = _fixture.Create<User>();
		var personData = _fixture.Build<PersonData>()
			.With(x => x.UserId, (Guid?)null)
			.With(x => x.IsPassportValid, true)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _userRepository.Insert(session, user, CancellationToken.None);
			await _repo.Insert(session, personData, CancellationToken.None);

			await session.CommitAsync(CancellationToken.None);
		}

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			var result = await _repo.UpdateUserId(session, personData.Id, user.Id, CancellationToken.None);
			result.Should().Be(1);
			await session.CommitAsync(CancellationToken.None);
		}

		using (var session = _sessionFactory.OpenPostgresConnection())
		{
			var result = await _repo.GetById(session, personData.Id, CancellationToken.None);
			result.Should().NotBeNull();
			result!.UserId.Should().Be(user.Id);
		}
	}

	[Fact]
	public async Task DisablePersonDataTest()
	{
		var personData = _fixture.Build<PersonData>()
			.With(x => x.UserId, (Guid?)null)
			.With(x => x.IsPassportValid, true)
			.Create();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _repo.Insert(session, personData, CancellationToken.None);

			await session.CommitAsync(CancellationToken.None);
		}

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			var result = await _repo.DisablePersonData(session, personData.Id, CancellationToken.None);
			result.Should().Be(1);
			await session.CommitAsync(CancellationToken.None);
		}

		using (var session = _sessionFactory.OpenPostgresConnection())
		{
			var result = await _repo.GetById(
				session: session,
				id: personData.Id,
				ct: CancellationToken.None);

			result.Should().BeNull();
		}
	}
}
