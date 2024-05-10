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
}
