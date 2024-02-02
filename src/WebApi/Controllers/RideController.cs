using Microsoft.AspNetCore.Mvc;

using WebApi.Models;
using WebApi.Services.Core;

namespace WebApi.Controllers
{
	[ApiController]
	[Route("api/[controller]/[action]")]
	public class RideController : ControllerBase
	{
		private readonly IRideService _rideService;

		public RideController(IRideService rideService)
		{
			_rideService = rideService;
		}

		[HttpPost]
		public async ValueTask<BaseResponse<RideDto>> CreateRide(RideDto ride, CancellationToken ct)
		{
			var result = await _rideService.CreateRide(ride, ct);
			return result;
		}

		[HttpPost]
		public async ValueTask<BaseResponse<ReservationDto>> Reserv(ReservationDto reserv, CancellationToken ct)
		{
			var result = await _rideService.Reserve(reserv, ct);
			return result;
		}

		[HttpPost]
		public async ValueTask<BaseResponse<Tuple<decimal, decimal>>> GetRecommendedPrice(Tuple<FormattedPoint, FormattedPoint> coordinates, CancellationToken ct)
		{
			(var from, var to) = coordinates;
			var result = await _rideService.GetRecommendedPriceAsync(from.ToPoint(), to.ToPoint(), ct);

			return new Tuple<decimal, decimal>(result.Low, result.High);
		}
	}
}
