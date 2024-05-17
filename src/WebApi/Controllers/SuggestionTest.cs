using Microsoft.AspNetCore.Mvc;
using WebApi.Models;
using WebApi.Services.Yandex;

namespace WebApi.Controllers
{
	[ApiController]
	[Route("api/[controller]/[action]")]
	public class SuggestionTest
	{
		private readonly ISuggestService _suggestService;
		private readonly IGeocodeService _geocodeService;

		public SuggestionTest(
			ISuggestService suggestService,
			IGeocodeService geocodeService)
		{
			_suggestService = suggestService;
			_geocodeService = geocodeService;
		}

		[HttpGet]
		public async ValueTask<YandexSuggestResponseDto?> GetSuggest(string input, CancellationToken ct)
		{
			return await _suggestService.GetSuggestion(input, ct);
		}

		[HttpGet]
		public async ValueTask<YandexGeocodeResponseDto?> GetGeocodeByAddress(string input, CancellationToken ct)
		{
			return await _geocodeService.AddressToGeoCode(input, ct);
		}

		[HttpGet]
		public async ValueTask<YandexGeocodeResponseDto?> GetGeocodeByUri(string input, CancellationToken ct)
		{
			return await _geocodeService.UriToGeoCode(input, ct);
		}

		[HttpPost]
		public async ValueTask<YandexGeocodeResponseDto?> GetGeocodeByPoint(FormattedPoint point, CancellationToken ct)
		{
			return await _geocodeService.PointToGeoCode(point, ct);
		}
	}
}
