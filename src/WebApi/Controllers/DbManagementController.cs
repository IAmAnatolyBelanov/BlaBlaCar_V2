using Microsoft.AspNetCore.Mvc;

using WebApi.DataAccess;

namespace WebApi.Controllers
{
	[ApiController]
	[Route("api/[controller]/[action]")]
	public class DbManagementController : ControllerBase
	{
		private readonly ILogger _logger = Log.ForContext<DbManagementController>();
		private readonly ApplicationContext _context;

		public DbManagementController(ApplicationContext context)
		{
			_context = context;
		}

		[HttpGet]
		public async ValueTask Migrate(CancellationToken ct)
		{
			await _context.MigrateAsync(ct);
		}
	}
}
