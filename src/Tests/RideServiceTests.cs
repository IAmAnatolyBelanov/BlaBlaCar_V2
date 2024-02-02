using AutoFixture;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using System.Reflection;
using WebApi.DataAccess;
using WebApi.Extensions;
using WebApi.Models;
using WebApi.Services.Core;
using WebApi.Shared;

namespace Tests
{
	public class RideServiceTests : IClassFixture<TestAppFactoryWithDb>
	{
		private readonly IServiceProvider _provider;
		private readonly Fixture _fixture;
		private readonly IRideService _rideService;
		private readonly IServiceScope _scope;
		private readonly ApplicationContext _applicationContext;

		private readonly string _originalConfigJson;

		public RideServiceTests(TestAppFactoryWithDb factory)
		{
			_provider = factory.Services;
			factory.MigrateDb();
			_fixture = Shared.BuildDefaultFixture();
			_scope = _provider.CreateScope();
			_rideService = _scope.ServiceProvider.GetRequiredService<IRideService>();
			_applicationContext = _scope.ServiceProvider.GetRequiredService<ApplicationContext>();

			var config = _scope.ServiceProvider.GetRequiredService<IRideServiceConfig>();
			_originalConfigJson = JsonConvert.SerializeObject(config);
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

			var result = await _rideService.CreateRide(context, ride, CancellationToken.None);

			result.Should().BeEquivalentTo(ride, x => x.Excluding(r => r.Prices).Excluding(r => r.Legs));
			result.Legs.Should().BeEquivalentTo(ride.Legs, x => x.Excluding(l => l.Ride).Excluding(l => l.NextLeg).Excluding(l => l.PreviousLeg));
			result.Prices.Should().BeEquivalentTo(ride.Prices, x => x.Excluding(p => p.StartLeg).Excluding(p => p.EndLeg));
		}

		[Fact]
		public async Task GettingPercentile()
		{
			RestoreConfig();
			var config = _scope.ServiceProvider.GetRequiredService<RideServiceConfig>();
			config.PriceStatisticsRadiusMeters = 1;

			try
			{
				var limits = Helpers.ValidPriceWaypointCounts(config.MaxWaypoints)
					.Last();
				var now = DateTimeOffset.UtcNow;

				var point = Shared.GetNewPoint().ToPoint();

				var rides = new List<Ride>();
				var legs = new List<Leg>();
				var prices = new List<Price>();

				for (int i = 0; i < 1000; i++)
				{
					var ride = _fixture.Build<Ride>()
						.With(x => x.Status, RideStatus.StartedOrDone)
						.Create();
					rides.Add(ride);

					var legsForOneRide = _fixture.Build<Leg>()
						.Without(x => x.NextLeg)
						.Without(x => x.NextLegId)
						.Without(x => x.PreviousLeg)
						.Without(x => x.PreviousLegId)
						.With(x => x.Ride, ride)
						.With(x => x.RideId, ride.Id)
						.With(x => x.From, point)
						.With(x => x.To, point)
						.With(x => x.StartTime, now)
						.With(x => x.EndTime, now)
						.CreateMany(limits.WaypointsCount - 1)
						.ToArray();

					legs.AddRange(legsForOneRide);

					var pricesForOneRide = BuildPrices(legsForOneRide).ToArray();

					prices.AddRange(pricesForOneRide);
				}

				var popular = prices.Count * 95 / 100;
				prices[0].PriceInRub = 1000;
				for (int i = 1; i < popular; i++)
					prices[i].PriceInRub = prices[i - 1].PriceInRub + 1;

				prices[popular].PriceInRub = 100_000;
				for (int i = popular + 1; i < prices.Count; i++)
					prices[i].PriceInRub = prices[i - 1].PriceInRub + 1;

				_applicationContext.Rides.AddRange(rides);
				_applicationContext.Legs.AddRange(legs);
				_applicationContext.Prices.AddRange(prices);
				_applicationContext.SaveChanges();

				var percentiles = await _rideService.GetRecommendedPriceAsync(point, point, CancellationToken.None);

				percentiles.Low.Should().BeGreaterThan(prices[0].PriceInRub)
					.And.BeLessThan(prices[popular - 1].PriceInRub);
				percentiles.High.Should().BeGreaterThan(prices[0].PriceInRub)
					.And.BeLessThan(prices[popular - 1].PriceInRub);
			}
			finally
			{
				RestoreConfig();
			}
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

		public IEnumerable<Price> BuildPrices(IReadOnlyList<Leg> legs, int defaultPrice = 1000)
		{
			for (int i = 0; i < legs!.Count; i++)
			{
				for (int j = i; j < legs.Count; j++)
				{
					yield return new Price
					{
						Id = _fixture.Create<Guid>(),
						PriceInRub = defaultPrice,
						StartLeg = legs[i],
						StartLegId = legs[i].Id,
						EndLeg = legs[j],
						EndLegId = legs[j].Id,
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

		private void RestoreConfig()
		{
			var currentConfig = _scope.ServiceProvider.GetRequiredService<RideServiceConfig>();
			var originalConfig = JsonConvert.DeserializeObject<RideServiceConfig>(_originalConfigJson);

			var type = currentConfig.GetType();

			var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty | BindingFlags.GetProperty)
				.Where(x => x.CanRead && x.CanWrite)
				.ToArray();

			foreach (var prop in properties)
			{
				var value = prop.GetValue(originalConfig);
				prop.SetValue(currentConfig, value);
			}
		}
	}
}
