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
			var result = await _rideService.Reserv(reserv, ct);
			return result;
		}
	}
}
