using AutoFixture;

using Microsoft.Extensions.DependencyInjection;

using WebApi.DataAccess;
using WebApi.Models;
using WebApi.Services.Core;

namespace Tests
{
	public class RideServiceTests : IClassFixture<TestAppFactoryWithDb>
	{
		private readonly IServiceProvider _provider;
		private readonly Fixture _fixture;
		private readonly IRideService _rideService;

		public RideServiceTests(TestAppFactoryWithDb factory)
		{
			_provider = factory.Services;
			factory.MigrateDb();
			_fixture = Shared.BuildDefaultFixture();
			_rideService = _provider.GetRequiredService<IRideService>();
		}

		[Fact]
		public async Task CreateRideTest()
		{
			var ride = _fixture.Create<RideDto>();
			var legs = _fixture.CreateMany<LegDto>(3).ToArray();
			ride.Legs = legs;
			NormalizeFromTo(ride);
			ride.Prices = BuildPrices(ride).ToArray();

			using var scope = _provider.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

			await _rideService.CreateRide(context, ride, CancellationToken.None);


		}

		private void NormalizeFromTo(RideDto ride)
		{
			var now = DateTimeOffset.UtcNow;
			var start = now.AddYears(-1);
			var duration = TimeSpan.FromHours(4);
			NormalizeFromTo(ride, start, duration);
		}

		private void NormalizeFromTo(RideDto ride, DateTimeOffset start, TimeSpan duration)
		{
			var places = BuildPlaces(start, duration)
				.Take(ride.WaypointsCount)
				.ToArray();

			for (int i = 0; i < ride.Legs!.Count; i++)
			{
				ride.Legs[i].From = places[i];
				ride.Legs[i].To = places[i + 1];
				ride.Legs[i].Ride = ride;
				ride.Legs[i].RideId = ride.Id;
			}
		}

		public IEnumerable<PriceDto> BuildPrices(RideDto ride, int defaultPrice = 1000)
		{
			for (int i = 0; i < ride.Legs!.Count; i++)
			{
				for (int j = i; j < ride.Legs.Count; j++)
				{
					yield return new PriceDto
					{
						Id = _fixture.Create<Guid>(),
						PriceInRub = defaultPrice,
						StartLeg = ride.Legs[i],
						StartLegId = ride.Legs[i].Id,
						EndLeg = ride.Legs[j],
						EndLegId = ride.Legs[j].Id,
					};
				}
			}
		}

		private IEnumerable<PlaceAndTime> BuildPlaces(DateTimeOffset start, TimeSpan duration)
		{
			var place = new PlaceAndTime
			{
				DateTime = start,
				Point = CityInfoManager.GetUnique().GetPoint(),
			};
			yield return place;

			while (true)
			{
				place = new PlaceAndTime
				{
					DateTime = place.DateTime + duration,
					Point = CityInfoManager.GetUnique().GetPoint(),
				};
				yield return place;
			}
		}

		private IEnumerable<DateTimeOffset> BuildPeriods(DateTimeOffset start, TimeSpan duration)
		{
			yield return start;

			while (true)
			{
				start += duration;
				yield return start;
			}
		}
	}
}
