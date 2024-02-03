using WebApi.DataAccess;
using WebApi.Services.Core;

namespace Tests
{
	public class UnitTest1 : IClassFixture<TestAppFactoryWithDb>
	{
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
			var from = Shared.GetNewPoint().ToPoint();
			var to = Shared.GetNewPoint().ToPoint();
			var kek = await lol.GetRecommendedPriceAsync(from, to, CancellationToken.None);
		}

		[Fact]
		public async Task Test5()
		{
			using var scope = _provider.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
			var rideService = scope.ServiceProvider.GetRequiredService<IRideService>();

			var point = Shared.GetNewPoint().ToPoint();

			var result = await rideService.GetRecommendedPriceAsync(point, point, CancellationToken.None);

			result.Low.Should().Be(-1);
			result.High.Should().Be(-1);
		}
	}
}