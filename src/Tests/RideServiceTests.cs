using System.Reflection;
using WebApi.DataAccess;
using WebApi.Extensions;
using WebApi.Models;
using WebApi.Services.Core;
using WebApi.Shared;

namespace Tests
{
	public class RideServiceTests : IClassFixture<TestAppFactoryFull>
	{
		private readonly IServiceProvider _provider;
		private readonly Fixture _fixture;
		private readonly IRideService _rideService;
		private readonly IServiceScope _scope;

		private readonly string _originalConfigJson;

		public RideServiceTests(TestAppFactoryFull factory)
		{
			// Используем только postgres (без redis), так как обращаемся к реальному api за реальной географией.
			// Кеш позволит иногда не делать запросы к внешнему сервису.
			factory.AddPostgres();
			_provider = factory.Services;
			_fixture = Shared.BuildDefaultFixture();
			_scope = _provider.CreateScope();
			_rideService = _scope.ServiceProvider.GetRequiredService<IRideService>();

			var config = _scope.ServiceProvider.GetRequiredService<IRideServiceConfig>();
			_originalConfigJson = JsonConvert.SerializeObject(config);
		}

		private void NormalizeFromTo(RideDto_Obsolete ride)
		{
			var now = DateTimeOffset.UtcNow;
			var start = now.AddYears(-1);
			var duration = TimeSpan.FromHours(4);
			NormalizeFromTo(ride, start, duration);
		}

		private void NormalizeFromTo(RideDto_Obsolete ride, DateTimeOffset start, TimeSpan duration)
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

		public IEnumerable<PriceDto> BuildPrices(RideDto_Obsolete ride, int defaultPrice = 1000)
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

		public IEnumerable<Price> BuildPrices(IReadOnlyList<Leg_Obsolete> legs, int defaultPrice = 1000)
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
