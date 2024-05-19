using Microsoft.AspNetCore.Mvc;

using WebApi.Models;
using WebApi.Models.ControllersModels;
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
		public async Task<BaseResponse<PriceRecommendation?>> GetRecommendedPrice([FromBody] GetPriceRecommendationRequest request, CancellationToken ct)
		{
			var result = await _rideService.GetPriceRecommendation(request, ct);

			return result;
		}

		[HttpPost]
		public async Task<BaseResponse<RideDto>> CreateRide([FromBody] RideDto ride, CancellationToken ct)
		{
			var result = await _rideService.CreateRide(ride, ct);
			return result;
		}

		[HttpPost]
		public async Task<BaseResponse<RideDto?>> GetRideById([FromBody] RequestWithId request, CancellationToken ct)
		{
			var result = await _rideService.GetRideById(request.Id, ct);
			return result;
		}

		[HttpPost]
		public async Task<BaseResponse<IReadOnlyList<SearchRideResponse>>> SearchRides([FromBody] RideFilter filter, CancellationToken ct)
		{
			var result = await _rideService.SearchRides(filter, ct);
			return BaseResponse.From(result);
		}

		[HttpPost]
		public async Task<BaseResponse<ReservationDto>> MakeReservation([FromBody] MakeReservationRequest request, CancellationToken ct)
		{
			var result = await _rideService.MakeReservation(request, ct);
			return result;
		}

		[HttpPost]
		public async Task<BaseResponse<RideCounts?>> GetCounts([FromBody] RideFilter filter, CancellationToken ct)
		{
			// Для каунтеров эти поля не важны, но на эти поля будет ругаться валидатор, если фронт их не заполнил.
			filter.SortDirection = SortDirection.Asc;
			filter.SortType = RideSortType.ByEndTime;
			filter.Offset = 0;
			filter.Limit = 20;

			var result = await _rideService.GetCounts(filter, ct);
			return result;
		}

		[HttpPost]
		public async Task<StringResponse> UpdateRideAvailablePlacesCount([FromBody] UpdateRideAvailablePlacesCountRequest request, CancellationToken ct)
		{
			await _rideService.UpdateRideAvailablePlacesCount(request.RideId, request.Count, ct);
			return StringResponse.Empty;
		}

		[HttpPost]
		public async Task<StringResponse> DeleteRide([FromBody] RequestWithId request, CancellationToken ct)
		{
			await _rideService.DeleteRide(request.Id, ct);
			return StringResponse.Empty;
		}

		[HttpPost]
		public async Task<StringResponse> CancelReservation([FromBody] RequestWithId request, CancellationToken ct)
		{
			await _rideService.CancelReservation(request.Id, ct);
			return StringResponse.Empty;
		}
	}
}
