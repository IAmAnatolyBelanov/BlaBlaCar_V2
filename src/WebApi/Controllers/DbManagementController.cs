using FluentMigrator.Runner;
using Microsoft.AspNetCore.Mvc;

using WebApi.DataAccess;
using WebApi.Repositories;

namespace WebApi.Controllers
{
	[ApiController]
	[Route("api/[controller]/[action]")]
	public class DbManagementController : ControllerBase
	{
		private readonly ILogger _logger = Log.ForContext<DbManagementController>();
		private readonly ApplicationContext _context;
		private readonly IServiceProvider _services;
		private readonly IMigrationRunner _postgresMigrationRunner;
		private readonly SessionFactory _sessionFactory;
		private readonly CloudApiResponseInfoRepository _cloudApiResponseInfoRepository;

		public DbManagementController(ApplicationContext context, IServiceProvider services, SessionFactory sessionFactory, CloudApiResponseInfoRepository cloudApiResponseInfoRepository)
		{
			_context = context;
			_services = services;

			_postgresMigrationRunner = _services.GetRequiredKeyedService<IMigrationRunner>(Constants.PostgresMigratorKey);
			_sessionFactory = sessionFactory;
			_cloudApiResponseInfoRepository = cloudApiResponseInfoRepository;
		}

		[HttpGet]
		public async ValueTask Migrate(CancellationToken ct)
		{
			await _context.MigrateAsync(ct);
		}

		[HttpGet]
		public async Task MigratePostgres(CancellationToken ct)
		{
			_postgresMigrationRunner.MigrateUp();
		}

		[HttpGet]
		public async Task Test()
		{
			using var session = _sessionFactory.OpenPostgresConnection(trace: true).BeginTransaction();
			var result = await _cloudApiResponseInfoRepository.Select(session, 10, 0, CancellationToken.None);

			_logger.Information("{Result}", result);
		}
	}
}
