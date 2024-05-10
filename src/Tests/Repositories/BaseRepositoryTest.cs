using FluentAssertions.Equivalency;
using WebApi.DataAccess;

namespace Tests;

public class BaseRepositoryTest : IClassFixture<TestAppFactoryWithDb>
{
	protected readonly IServiceProvider _provider;
	protected readonly ISessionFactory _sessionFactory;
	protected readonly IFixture _fixture;

	public BaseRepositoryTest(TestAppFactoryWithDb fixture)
	{
		_provider = fixture.Services;

		fixture.MigrateDb();

		_sessionFactory = _provider.GetRequiredService<ISessionFactory>();

		_fixture = BuildFixture();
	}

	protected virtual IFixture BuildFixture()
		=> Shared.BuildDefaultFixture();
}
