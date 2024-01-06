using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using NetTopologySuite.Geometries;

using WebApi.DataAccess;
using WebApi.Models;
using WebApi.Services.Core;
using WebApi.Services.Yandex;

namespace WebApi.Controllers
{
	[ApiController]
	[Route("api/[controller]/[action]")]
	public class SuggestionTest
	{
		private readonly ISuggestService _suggestService;
		private readonly IGeocodeService _geocodeService;
		private readonly RideService _rideService;
		private readonly ApplicationContext _context;
		private readonly ILegDtoMapper _legDtoMapper;

		public SuggestionTest(
			ISuggestService suggestService,
			IGeocodeService geocodeService,
			RideService rideService,
			ApplicationContext context,
			ILegDtoMapper legDtoMapper)
		{
			_suggestService = suggestService;
			_geocodeService = geocodeService;
			_rideService = rideService;
			_context = context;
			_legDtoMapper = legDtoMapper;
		}

		[HttpGet]
		public async ValueTask<YandexSuggestResponse?> GetSuggest(string input, CancellationToken ct)
		{
			return await _suggestService.GetSuggestion(input, ct);
		}

		[HttpGet]
		public async ValueTask<YandexGeocodeResponse?> GetGeocodeByAddress(string input, CancellationToken ct)
		{
			return await _geocodeService.AddressToGeoCode(input, ct);
		}

		[HttpGet]
		public async ValueTask<YandexGeocodeResponse?> GetGeocodeByUri(string input, CancellationToken ct)
		{
			return await _geocodeService.UriToGeoCode(input, ct);
		}

		[HttpPost]
		public async ValueTask<YandexGeocodeResponse?> GetGeocodeByPoint(FormattedPoint point, CancellationToken ct)
		{
			return await _geocodeService.PointToGeoCode(point, ct);
		}

		[HttpPost]
		public async ValueTask TestGettingPrice(Tuple<FormattedPoint, FormattedPoint> arg, CancellationToken ct)
		{
			(var from, var to) = arg;
			var f = from.ToPoint();
			var t = to.ToPoint();
			var result = await _rideService.GetRecommendedPriceAsync(f, t, ct);
		}

		[HttpPost]
		public async ValueTask<Leg?> GetLeg(Guid id)
		{
			var leg = await _context.Legs.FirstOrDefaultAsync(x => x.Id == id);
			return leg;
		}

		[HttpPost]
		public async ValueTask<Leg> GenerateRandomLeg(CancellationToken ct)
		{
			var ride = new Ride
			{
				DriverId = 1,
				Id = Guid.NewGuid(),
			};

			var from = new FormattedPoint { Latitude = 44.228393, Longitude = 42.048261 };
			var to = new FormattedPoint { Latitude = 55.755484, Longitude = 37.618237 };
			var fromGeocode = await _geocodeService.PointToGeoCode(from, ct);
			var toGeocode = await _geocodeService.PointToGeoCode(to, ct);
			var description = $"{fromGeocode!.Response.GeoObjectCollection.FeatureMember[0].GeoObject.MetaDataProperty.GeocoderMetaData.Address.Formatted}@{toGeocode!.Response.GeoObjectCollection.FeatureMember[0].GeoObject.MetaDataProperty.GeocoderMetaData.Address.Formatted}";

			var leg = new Leg
			{
				Id = Guid.NewGuid(),
				Ride = ride,
				RideId = ride.Id,
				PriceInRub = Random.Shared.Next(100, 10_000),
				EndTime = DateTime.UtcNow,
				StartTime = DateTime.UtcNow.AddHours(-4),
				From = from.ToPoint(),
				To = to.ToPoint(),
				Description = description,
			};

			_context.Legs.Add(leg);
			_context.Rides.Add(ride);
			await _context.SaveChangesAsync();

			return leg;
		}

		[HttpGet]
		public void TestMapper()
		{
			var leg = new LegDto
			{
				Ride = new RideDto { Id = Guid.NewGuid(), }
			};

			var res = _legDtoMapper.FromDto(leg);
			Console.WriteLine(res);
		}
	}
}
