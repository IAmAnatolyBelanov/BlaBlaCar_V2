using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using WebApi.DataAccess;
using WebApi.Models;

namespace WebApi.Controllers
{
	[ApiController]
	[Route("api/[controller]/[action]")]
	public class WeatherForecastController : ControllerBase
	{
		private static readonly string[] Summaries = new[]
		{
			"Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
		};

		private readonly ILogger _logger = Log.ForContext< WeatherForecastController>();
		private readonly ApplicationContext _context;

		public WeatherForecastController(ApplicationContext context)
		{
			_context = context;
		}

		[HttpGet]
		public IEnumerable<WeatherForecast> Get()
		{
			_logger.Information("Test context");

			return Enumerable.Range(1, 5).Select(index => new WeatherForecast
			{
				Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
				TemperatureC = Random.Shared.Next(-20, 55),
				Summary = Summaries[Random.Shared.Next(Summaries.Length)]
			})
			.ToArray();
		}

		[HttpGet]
		public async ValueTask<IReadOnlyList<Ride>> GetAllRides(CancellationToken ct)
		{
			return await _context.Rides.ToListAsync(ct);
		}

		//[HttpPost]
		//public async ValueTask CreateRandomRide(CancellationToken ct)
		//{
		//	var ride = new Ride
		//	{
		//		Name = Guid.NewGuid().ToString(),
		//	};
		//	await _context.Rides.AddAsync(ride, ct);
		//	await _context.SaveChangesAsync(ct);
		//}
	}
}
