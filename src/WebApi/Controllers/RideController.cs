using Microsoft.AspNetCore.Mvc;

using WebApi.Models;
using WebApi.Models.ControllersModels.RideControllerModels;
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
		public async ValueTask<BaseResponse<Tuple<decimal, decimal>>> GetRecommendedPrice(Tuple<FormattedPoint, FormattedPoint> coordinates, CancellationToken ct)
		{
			(var from, var to) = coordinates;
			var result = await _rideService.GetRecommendedPriceAsync(from.ToPoint(), to.ToPoint(), ct);

			return new Tuple<decimal, decimal>(result.Low, result.High);
		}

		[HttpPost]
		public async Task<BaseResponse<RideDto>> CreateRide(RideDto ride, CancellationToken ct)
		{
			var result = await _rideService.CreateRide(ride, ct);
			return result;
		}

		[HttpPost]
		public async Task<BaseResponse<GetRideResponse?>> GetRideById([FromBody] GetRideByIdRequest request, CancellationToken ct)
		{
			var result = await _rideService.GetRideById(request.RideId, ct);
			return result;
		}

		[HttpPost]
		public async Task<IReadOnlyList<SearchRideResponse>> SearchRides([FromBody] RideFilter filter, CancellationToken ct)
		{
			var result = await _rideService.SearchRides(filter, ct);
			return result;
		}
	}
}
