using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using WebApi.Models.ControllersModels;
using WebApi.Services.Yandex;

namespace WebApi.Controllers
{
	[ApiController]
	[Route("api/[controller]/[action]")]
	public class GoecodeController
	{
		private readonly ISuggestService _suggestService;
		private readonly IGeocodeService _geocodeService;

		public GoecodeController(
			ISuggestService suggestService,
			IGeocodeService geocodeService)
		{
			_suggestService = suggestService;
			_geocodeService = geocodeService;
		}

		[HttpPost]
		public async Task<BaseResponse<YandexSuggestResponseDto?>> GetSuggest([FromBody] GeocodeStringRequest request, CancellationToken ct)
		{
			return await _suggestService.GetSuggestion(request.Input, ct);
		}

		[HttpPost]
		public async Task<BaseResponse<YandexGeocodeResponseDto?>> GetGeocodeByAddress([FromBody] GeocodeStringRequest request, CancellationToken ct)
		{
			return await _geocodeService.AddressToGeoCode(request.Input, ct);
		}

		[HttpPost]
		public async Task<BaseResponse<YandexGeocodeResponseDto?>> GetGeocodeByUri([FromBody] GeocodeStringRequest request, CancellationToken ct)
		{
			return await _geocodeService.UriToGeoCode(request.Input, ct);
		}

		[HttpPost]
		public async Task<BaseResponse<YandexGeocodeResponseDto?>> GetGeocodeByPoint([FromBody] FormattedPoint point, CancellationToken ct)
		{
			return await _geocodeService.PointToGeoCode(point, ct);
		}
	}
}
