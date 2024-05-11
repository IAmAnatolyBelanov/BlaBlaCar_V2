using WebApi.Models;
using WebApi.Repositories;

namespace Tests;

public class UserRepositoryTests : BaseRepositoryTest
{
	private readonly IUserRepository _repo;
	public UserRepositoryTests(TestAppFactoryWithDb fixture) : base(fixture)
	{
		_repo = _provider.GetRequiredService<IUserRepository>();
	}

	[Fact]
	public async Task InsertTest()
	{
		var fixture = Shared.BuildDefaultFixture();
		var user = fixture.Create<User>();

		using var session = _sessionFactory.OpenPostgresConnection().BeginTransaction();
		var result = await _repo.Insert(session, user, CancellationToken.None);
		await session.CommitAsync(CancellationToken.None);

		result.Should().Be(1);
	}

	[Fact]
	public async Task GetBySingleExistingId()
	{
		var fixture = Shared.BuildDefaultFixture();
		var user = fixture.Create<User>();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			await _repo.Insert(session, user, CancellationToken.None);
			await session.CommitAsync(CancellationToken.None);
		}

		using (var session = _sessionFactory.OpenPostgresConnection())
		{
			var result = await _repo.GetById(session, user.Id, CancellationToken.None);
			result.Should().BeEquivalentTo(user);
		}
	}

	[Fact]
	public async Task GetBySingleNonExistingId()
	{
		using (var session = _sessionFactory.OpenPostgresConnection())
		{
			var result = await _repo.GetById(session, Guid.NewGuid(), CancellationToken.None);
			result.Should().BeNull();
		}
	}

	[Fact]
	public async Task GetMoreThan2100Users()
	{
		var fixture = Shared.BuildDefaultFixture();
		var users = fixture.CreateMany<User>(2200).ToArray();

		using (var session = _sessionFactory.OpenPostgresConnection().BeginTransaction())
		{
			foreach (var user in users)
				await _repo.Insert(session, user, CancellationToken.None);
			await session.CommitAsync(CancellationToken.None);
		}

		using (var session = _sessionFactory.OpenPostgresConnection())
		{
			var result = await _repo.GetByIds(session, users.Select(x => x.Id).ToArray(), CancellationToken.None);
			result.Should().BeEquivalentTo(users);
		}
	}

	[Fact]
	public async Task ValidateMultipleCommits()
	{
		using var session = _sessionFactory.OpenPostgresConnection().BeginTransaction();
		var users = _fixture.CreateMany<User>(10).ToArray();
		foreach (var user in users)
		{
			await _repo.Insert(session, user, CancellationToken.None);
			lock (user)
			{
				session.CommitAsync(CancellationToken.None).Wait();
			}
		}
	}


	[Fact]
	public async Task ParallelTransactionsTest()
	{
		using var session1 = _sessionFactory.OpenPostgresConnection();
		using var session2 = _sessionFactory.OpenPostgresConnection();

		var generatedUser = _fixture.Create<User>();
		await _repo.Insert(session2, generatedUser, CancellationToken.None);

		var fetchedUser = await _repo.GetById(session1, generatedUser.Id, CancellationToken.None);

		fetchedUser.Should()
			.NotBeNull()
			.And
			.BeEquivalentTo(generatedUser);

		session1.Dispose();
		session2.Dispose();

		var session3 = _sessionFactory.OpenPostgresConnection();

		fetchedUser = await _repo.GetById(session3, generatedUser.Id, CancellationToken.None);

		fetchedUser.Should()
			.NotBeNull()
			.And
			.BeEquivalentTo(generatedUser);
	}

	[Fact]
	public async Task TestBeggingTransactionAfterExploringSession()
	{
		using var session = _sessionFactory.OpenPostgresConnection();
		await _repo.Insert(session, _fixture.Create<User>(), CancellationToken.None);

		session.BeginTransaction();

		await _repo.Insert(session, _fixture.Create<User>(), CancellationToken.None);
		await session.CommitAsync(CancellationToken.None);
	}
}
