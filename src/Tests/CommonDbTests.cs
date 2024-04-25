using Dapper;

using Microsoft.EntityFrameworkCore;

using Serilog;
using Serilog.Context;

using WebApi.DataAccess;
using WebApi.Models;

namespace Tests;

public class CommonDbTests : IClassFixture<TestAppFactoryWithDb>
{
	private readonly IServiceScope _scope;
	private readonly ApplicationContext _context;
	private readonly Fixture _fixture;
	public CommonDbTests(TestAppFactoryWithDb factory)
	{
		factory.MigrateDb();

		_fixture = Shared.BuildDefaultFixture();

		_scope = factory.Services.CreateScope();
		_context = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();
	}

	[Fact]
	public void DbContainsFunctions()
	{
		var requiredFunctions = DbConstants.FunctionNames.AllConstants.Keys.AsList();

		var allFunctions = _context.Database.SqlQuery<string>($@"
SELECT routine_name
FROM  information_schema.routines
WHERE routine_type = 'FUNCTION'
AND routine_schema = 'public'")
			.ToHashSet();

		allFunctions.Should().Contain(requiredFunctions);
	}
}
