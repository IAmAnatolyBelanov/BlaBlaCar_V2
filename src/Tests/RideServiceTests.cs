using AutoFixture;

using FluentAssertions;

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
		public async Task CreateRideCorrectFullyLeg()
		{
			using var scope = _provider.CreateScope();
			var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

			var ride = _fixture.Create<RideDto>();
			var legsCount = ValidLegWaypointCounts(5).Last().legsCount;
			var legs = _fixture
				.Build<LegDto>()
				.With(x => x.Ride, ride)
				.With(x => x.RideId, ride.Id)
				.CreateMany(legsCount)
				.ToArray();
			ride.Legs = legs;
			NormalizeFromTo(legs);

			var result = await _rideService.CreateRide(context, ride, CancellationToken.None);

			result.FullyLeg.Should().Be(result.Legs!.OrderByDescending(x => x.Duration).First());
		}

		private static IEnumerable<(int legsCount, int waypointsCount)> ValidLegWaypointCounts(int maxWaypointsCount)
		{
			var lastValidLegCount = 1;
			yield return (lastValidLegCount, 2);

			for (int waypointCount = 3; waypointCount <= maxWaypointsCount; waypointCount++)
			{
				lastValidLegCount += waypointCount - 1;
				yield return (lastValidLegCount, waypointCount);
			}
		}

		private void NormalizeFromTo(IReadOnlyList<LegDto> legs)
		{
			var waypointsCount = ValidLegWaypointCounts(10)
				.First(x => x.legsCount == legs.Count)
				.waypointsCount;

			var points = _fixture.CreateMany<PlaceAndTime>(waypointsCount).ToArray();

			var now = DateTimeOffset.UtcNow;
			for (int i = 0; i < points.Length; i++)
				points[i].DateTime = now.AddHours(i);

			var allPairs = GetAllPairs(points).ToArray();

			allPairs.Should().HaveSameCount(legs);

			for (int i = 0; i < allPairs.Length; i++)
			{
				legs[i].From = allPairs[i].from;
				legs[i].To = allPairs[i].to;
			}
		}

		private IEnumerable<(PlaceAndTime from, PlaceAndTime to)> GetAllPairs(IReadOnlyList<PlaceAndTime> points)
		{
			for (int i = 0; i < points.Count; i++)
			{
				for (int j = i + 1; j < points.Count; j++)
				{
					yield return (points[i], points[j]);
				}
			}
		}
	}
}
