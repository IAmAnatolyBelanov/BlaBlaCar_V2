﻿using AutoFixture;

using FluentAssertions;

using Microsoft.Extensions.DependencyInjection;

using Newtonsoft.Json;

using Serilog;

using WebApi.DataAccess;
using WebApi.Models;
using WebApi.Services.Core;
using WebApi.Services.Yandex;

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

		[Fact]
		public void TestFormatOfPoint()
		{
			var point = _fixture.Create<PlaceAndTime>();

			var pointAsJson = JsonConvert.SerializeObject(point);
			var pointAsStr = $"{point}";

			var pointFromJson = JsonConvert.DeserializeObject<PlaceAndTime>(pointAsJson);
			var pointFromStr = JsonConvert.DeserializeObject<PlaceAndTime>(pointAsStr);

			pointAsStr.Should().Be(pointAsJson);

			pointFromStr.Should().BeEquivalentTo(point);
			pointFromStr.Should().BeEquivalentTo(pointFromJson);

			pointFromJson.Should().BeEquivalentTo(point);
			pointFromJson.Should().BeEquivalentTo(pointFromStr);
		}

		[Fact]
		public async Task CollectPoints()
		{
			var points = Enumerable.Range(0, 90)
				.Select(x=> new FormattedPoint
				{
					Latitude = Random.Shared.Next(41, 81) + Random.Shared.NextDouble(),
					Longitude = Random.Shared.Next(82, 189) + Random.Shared.NextDouble()
				})
				.ToArray();

			using var scope = _provider.CreateScope();
			var geocodeService = scope.ServiceProvider.GetRequiredService<IGeocodeService>();

			var geocodeTasks = points.Select(x => geocodeService.PointToGeoCode(x, CancellationToken.None).AsTask())
				.ToArray();
			await Task.WhenAll(geocodeTasks);
			var geocodes = geocodeTasks.Select(x => x.Result).ToArray();

			Log.Information("Take geocodes: {Geocodes}", JsonConvert.SerializeObject(geocodes));

			await Task.Delay(10_000);

			var strings = geocodes.Select(x => $"{x!.Geoobjects[0].Point.Longitude},{x.Geoobjects[0].Point.Latitude},\"{x.Geoobjects[0].FormattedAddress}\"").ToArray();
			Log.Information("geocodes: {Geocodes}", strings);

			var russiaGeocodes = geocodes.Where(x => x.Geoobjects[0].FormattedAddress.StartsWith("Россия,"))
				.Select(x => $"{x!.Geoobjects[0].Point.Longitude},{x.Geoobjects[0].Point.Latitude},\"{x.Geoobjects[0].FormattedAddress}\"").ToArray();
			Log.Information("russiaGeocodes: {RussiaGeocodes}", russiaGeocodes);

			await Task.Delay(10_000);
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
