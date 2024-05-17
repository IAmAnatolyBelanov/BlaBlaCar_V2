using FluentAssertions.Extensions;
using NetTopologySuite.Geometries;
using WebApi.Models;

namespace Tests
{
	public static class Shared
	{
		private static readonly object _locker = new();
		private static IFixture _dateTimeFixture = new Fixture();

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

			fixture.Register<DateTimeOffset>(() =>
			{
				var dateTime = _dateTimeFixture.Create<DateTimeOffset>();
				var result = dateTime.AddNanoseconds(-dateTime.Nanosecond);
				return result;
			});

			fixture.Register<DateTime>(() =>
			{
				var dateTime = fixture.Create<DateTimeOffset>();
				var result = dateTime.DateTime;
				return result;
			});

			fixture.Register<TimeSpan>(() =>
			{
				var seconds = Random.Shared.Next(1, 59);
				var minutes = Random.Shared.Next(0, 59);
				var hours = Random.Shared.Next(0, 23);

				var result = new TimeSpan(hours: hours, minutes: minutes, seconds: seconds);
				return result;
			});

			fixture.Register<Point>(() =>
			{
				var formattedPoint = fixture.Create<FormattedPoint>();
				var result = formattedPoint.ToPoint();
				return result;
			});

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
