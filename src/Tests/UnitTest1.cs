using AutoFixture;

using DotNet.Testcontainers.Builders;

using FluentAssertions;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Testcontainers.PostgreSql;

using WebApi.DataAccess;
using WebApi.Extensions;
using WebApi.Models;
using WebApi.Services.Core;

namespace Tests
{
	public class TestApp : WebApplicationFactory<Program>, IDisposable
	{
		private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
			.WithImage("postgis/postgis:16-3.4")
			.WithWaitStrategy(Wait.ForUnixContainer())
			.Build();

		public TestApp()
		{
			Task.Run(async () => await _postgreSqlContainer.StartAsync()).Wait();
		}

		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			base.ConfigureWebHost(builder);
			builder.ConfigureTestServices(services =>
			{
				services.RemoveAll<IApplicationContextConfig>();
				var conf = new ApplicationContextConfig
				{
					ConnectionString = _postgreSqlContainer.GetConnectionString()
				};
				services.AddSingleton<IApplicationContextConfig>(conf);
			});
		}

		public void Dispose()
		{
			Task.Run(async () => await _postgreSqlContainer.DisposeAsync()).Wait();
		}
	}

	public class UnitTest1 : IClassFixture<TestApp>
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

		public UnitTest1(TestApp fixture)
		{
			_provider = fixture.Services;

			Exception? ex = null;

			// Хз, в чём дело, но без такой херни подключение к бд падает.
			for (int i = 0; i < 5; i++)
			{
				try
				{
					using var scope = _provider.CreateScope();
					using var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
					context.Database.Migrate();
					var rideServiceConfig = (RideServiceConfig)scope.ServiceProvider.GetRequiredService<IRideServiceConfig>();
					rideServiceConfig.PriceStatisticsRadiusMeters = 10;

					ex = null;
					break;
				}
				catch (Exception exc)
				{
					ex = exc;
					Thread.Sleep(500);
				}
			}

			if (ex != null)
				throw ex;
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
		public async Task Test2()
		{
			using var scope = _provider.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
			var rideService = scope.ServiceProvider.GetRequiredService<IRideService>();

			var fixture = new Fixture();

			var startTime = DateTimeOffset.UtcNow.AddDays(-1);
			var endTime = startTime.AddHours(6);
			var from = GetUniquePoint().ToPoint();
			var to = GetUniquePoint().ToPoint();

			var ride = fixture.Create<Ride>();

			var generateLegsFunc = (int count)
				=> fixture.Build<Leg>()
				.With(x => x.RideId, ride.Id)
				.With(x => x.Ride, ride)
				.With(x => x.StartTime, startTime)
				.With(x => x.EndTime, endTime)
				.With(x => x.From, from)
				.With(x => x.To, to)
				.CreateMany(count);

			var lowPriceLegs = generateLegsFunc(1000)
				.ForEach(x => x.PriceInRub = 1000)
				.ToArray();
			var highPriceLegs = generateLegsFunc(30)
				.ForEach(x => x.PriceInRub = 10_000)
				.ToArray();

			context.Rides.Add(ride);
			context.Legs.AddRange(lowPriceLegs);
			context.Legs.AddRange(highPriceLegs);
			context.SaveChanges();

			var avg = (decimal)lowPriceLegs.Concat(highPriceLegs)
				.Select(x => x.PriceInRub)
				.Average();

			var result = await rideService.GetRecommendedPriceAsync(from, to, CancellationToken.None);

			avg.Should().BeGreaterThan(result.high);
		}

		[Fact]
		public async Task Test3()
		{
			using var scope = _provider.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
			var rideService = scope.ServiceProvider.GetRequiredService<IRideService>();

			var fixture = new Fixture();

			var startTime = DateTimeOffset.UtcNow.AddDays(-1);
			var endTime = startTime.AddHours(6);
			var from = GetUniquePoint().ToPoint();
			var to = GetUniquePoint().ToPoint();

			var ride = fixture.Create<Ride>();

			var generateLegsFunc = (int count)
				=> fixture.Build<Leg>()
				.With(x => x.RideId, ride.Id)
				.With(x => x.Ride, ride)
				.With(x => x.StartTime, startTime)
				.With(x => x.EndTime, endTime)
				.With(x => x.From, from)
				.With(x => x.To, to)
				.CreateMany(count);

			var lowPriceLegs = generateLegsFunc(1000)
				.ToArray();
			for (var i = 0; i < lowPriceLegs.Length; i++)
				lowPriceLegs[i].PriceInRub = 1000 + i;

			var highPriceLegs = generateLegsFunc(1000)
				.ToArray();
			for (var i = 0; i < highPriceLegs.Length; i++)
				lowPriceLegs[i].PriceInRub = 10_000 + i;

			context.Rides.Add(ride);
			context.Legs.AddRange(lowPriceLegs);
			context.Legs.AddRange(highPriceLegs);
			context.SaveChanges();

			var result = await rideService.GetRecommendedPriceAsync(from, to, CancellationToken.None);

			Console.WriteLine(result);
		}

		[Fact]
		public async Task Test4()
		{
			using var scope = _provider.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
			var rideService = scope.ServiceProvider.GetRequiredService<IRideService>();

			var fixture = new Fixture();

			var startTime = DateTimeOffset.UtcNow.AddDays(-1);
			var endTime = startTime.AddHours(6);
			var from = GetUniquePoint().ToPoint();
			var to = GetUniquePoint().ToPoint();

			var ride = fixture.Create<Ride>();

			var generateLegsFunc = (int count)
				=> fixture.Build<Leg>()
				.With(x => x.RideId, ride.Id)
				.With(x => x.Ride, ride)
				.With(x => x.StartTime, startTime)
				.With(x => x.EndTime, endTime)
				.With(x => x.From, from)
				.With(x => x.To, to)
				.CreateMany(count);

			var legs = generateLegsFunc(3000)
				.ToArray();
			for (var i = 0; i < legs.Length; i++)
				legs[i].PriceInRub = 1000 + i;

			context.Rides.Add(ride);
			context.Legs.AddRange(legs);
			context.SaveChanges();

			var avg = (decimal)legs.Select(x => x.PriceInRub).Average();

			var result = await rideService.GetRecommendedPriceAsync(from, to, CancellationToken.None);

			avg.Should().BeLessThanOrEqualTo(result.high)
				.And.BeGreaterThanOrEqualTo(result.low);

			Console.WriteLine(result);
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