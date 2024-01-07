using AutoFixture;
using AutoFixture.AutoNSubstitute;

using WebApi.Models;

namespace Tests
{
	public static class Shared
	{
		public static Fixture BuildDefaultFixture()
		{
			var fixture = new Fixture();
			fixture.Customize(new AutoNSubstituteCustomization());
			fixture.Register<FormattedPoint>(() => new FormattedPoint
			{
				Latitude = Random.Shared.Next(-88, 88) + Random.Shared.NextDouble(),
				Longitude = Random.Shared.Next(-178, 178) + Random.Shared.NextDouble(),
			});
			fixture.Register<PlaceAndTime>(() => new PlaceAndTime
			{
				Point = fixture.Create<FormattedPoint>(),
				DateTime = fixture.Create<DateTimeOffset>(),
			});

			return fixture;
		}
	}
}
