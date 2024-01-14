using AutoFixture;

using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using WebApi.DataAccess;
using WebApi.Extensions;
using WebApi.Models;
using WebApi.Services.Core;

namespace Tests
{
	public class UnitTest1 : IClassFixture<TestAppFactoryWithDb>
	{
		private static IEnumerable<FormattedPoint> _formattedPoints = BuildFormattedPoints();
		private static IEnumerable<FormattedPoint> BuildFormattedPoints()
		{
			for (double lat = -89; lat < 89; lat += 0.1)
			{
				for (double lon = -179; lon < 179; lon += 0.1)
				{
					yield return new FormattedPoint
					{
						Latitude = lat,
						Longitude = lon,
					};
				}
			}
		}
		private static object _locker = new();
		private static FormattedPoint GetUniquePoint()
		{
			lock (_locker)
			{
				var uniquePoint = _formattedPoints.First();
				_formattedPoints = _formattedPoints.Skip(1);
				return uniquePoint;
			}
		}

		private readonly IServiceProvider _provider;

		public UnitTest1(TestAppFactoryWithDb fixture)
		{
			_provider = fixture.Services;

			fixture.MigrateDb();

			using var scope = _provider.CreateScope();
			var rideServiceConfig = (RideServiceConfig)scope.ServiceProvider.GetRequiredService<IRideServiceConfig>();
			rideServiceConfig.PriceStatisticsRadiusMeters = 10;
		}

		[Fact]
		public async Task Test1()
		{
			using var scope = _provider.CreateScope();
			var lol = scope.ServiceProvider.GetRequiredService<IRideService>();
			var from = GetUniquePoint().ToPoint();
			var to = GetUniquePoint().ToPoint();
			var kek = await lol.GetRecommendedPriceAsync(from, to, CancellationToken.None);
		}

		[Fact]
		public async Task Test5()
		{
			using var scope = _provider.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
			var rideService = scope.ServiceProvider.GetRequiredService<IRideService>();

			var point = GetUniquePoint().ToPoint();

			var result = await rideService.GetRecommendedPriceAsync(point, point, CancellationToken.None);

			result.low.Should().Be(-1);
			result.high.Should().Be(-1);
		}
	}
}