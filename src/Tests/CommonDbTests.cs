using Dapper;

using WebApi.DataAccess;

namespace Tests;

public class CommonDbTests : IClassFixture<TestAppFactoryWithDb>
{
	private readonly IServiceProvider _provider;
	public CommonDbTests(TestAppFactoryWithDb fixture)
	{
		_provider = fixture.Services;

		fixture.MigrateDb();
	}

	// 	[Fact]
	// 	public void DbContainsFunctions()
	// 	{
	// 		var requiredFunctions = DbConstants.FunctionNames.AllConstants.Keys.AsList();

	// 		using var scope = _provider.CreateScope();
	// 		using var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

	// 		var allFunctions = context.Database.SqlQuery<string>($@"
	// SELECT routine_name
	// FROM  information_schema.routines
	// WHERE routine_type = 'FUNCTION'
	// AND routine_schema = 'public'")
	// 			.ToHashSet();

	// 		allFunctions.Should().Contain(requiredFunctions);
	// 	}
}
