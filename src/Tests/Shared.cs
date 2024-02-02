using AutoFixture;
using AutoFixture.AutoNSubstitute;

using WebApi.Models;

namespace Tests
{
	public static class Shared
	{
		private static readonly object _locker = new();

		public static Fixture BuildDefaultFixture()
		{
			var fixture = new Fixture();
			fixture.Customize(new AutoNSubstituteCustomization());
			fixture.Register<FormattedPoint>(() =>
			{
				var randomCity = CityInfoManager.GetUnique();
				return new FormattedPoint
				{
					Latitude = randomCity.Latitude,
					Longitude = randomCity.Longitude,
				};
			});
			fixture.Register<PlaceAndTime>(() => new PlaceAndTime
			{
				Point = fixture.Create<FormattedPoint>(),
				DateTime = fixture.Create<DateTimeOffset>(),
			});

			fixture.Customize<RideDto>(x => x
				.Without(r => r.Legs)
				.Without(r => r.Prices)
				.With(x => x.DriverId, () => (ulong)Random.Shared.Next(1, int.MaxValue)));

			fixture.Customize<ReservationDto>(x => x
				.With(x => x.Count, () => Random.Shared.Next(1, 100))
				.With(x => x.UserId, () => (ulong)Random.Shared.Next(1, int.MaxValue)));

			fixture.Customize<LegDto>(x => x
				.Without(x => x.Ride)
				.Without(x => x.NextLeg)
				.Without(x => x.NextLegId)
				.Without(x => x.PreviousLeg)
				.Without(x => x.PreviousLegId));

			fixture.Customize<PriceDto>(x => x
				.Without(x => x.StartLeg)
				.Without(x => x.EndLeg));

			return fixture;
		}

		public static FormattedPoint GetNewPoint()
		{
			lock (_locker)
			{
				if (_allPointsEnumerator.MoveNext())
				{
					return _allPointsEnumerator.Current;
				}
				else
				{
					_allPointsEnumerator = BuildAllPoints().GetEnumerator();
					_allPointsEnumerator.MoveNext();
					return _allPointsEnumerator.Current;
				}
			}
		}

		private static IEnumerator<FormattedPoint> _allPointsEnumerator = BuildAllPoints().GetEnumerator();
		public static IEnumerable<FormattedPoint> BuildAllPoints()
		{
			var step = 100;

			for (int lat = -30 * 1000; lat < 30 * 1000; lat += step)
			{
				for (int lon = -179 * 1000; lon < 179 * 1000; lon += step)
				{
					yield return new FormattedPoint
					{
						Latitude = lat / 1000.0,
						Longitude = lon / 1000.0
					};
				}
			}
		}
	}
}
