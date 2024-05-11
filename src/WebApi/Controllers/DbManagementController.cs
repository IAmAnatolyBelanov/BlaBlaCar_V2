using FluentMigrator.Runner;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers
{
	[ApiController]
	[Route("api/[controller]/[action]")]
	public class DbManagementController : ControllerBase
	{
		private readonly ILogger _logger = Log.ForContext<DbManagementController>();
		private readonly IServiceProvider _services;
		private readonly IMigrationRunner _postgresMigrationRunner;

		public DbManagementController(IServiceProvider services)
		{
			_services = services;

			_postgresMigrationRunner = _services.GetRequiredKeyedService<IMigrationRunner>(Constants.PostgresMigratorKey);
		}

		[HttpGet]
		public void MigratePostgres()
		{
			_logger.Information("Start to migrate postgres db.");
			_postgresMigrationRunner.MigrateUp();
			_logger.Information("Postgres migration is completed.");
		}
	}
}
