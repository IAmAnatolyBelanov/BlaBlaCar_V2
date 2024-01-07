using AutoFixture;

using FluentAssertions;

using FluentValidation;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using WebApi.Models;
using WebApi.Services.Validators;

namespace Tests
{
	public class LegsValidationTests : IClassFixture<WebApplicationFactory<Program>>
	{
		private readonly Fixture _fixture;
		private readonly IValidator<LegDto> _validatorSingleLeg;
		private readonly IValidator<IReadOnlyList<LegDto>> _validatorCollectionOfLegs;

		public LegsValidationTests(WebApplicationFactory<Program> factory)
		{
			_fixture = Shared.BuildDefaultFixture();

			_validatorSingleLeg = factory.Services.GetRequiredService<IValidator<LegDto>>();
			_validatorCollectionOfLegs = factory.Services.GetRequiredService<IValidator<IReadOnlyList<LegDto>>>();
		}

		[Fact]
		public void TestRandomLeg()
		{
			(var legCount, var waypointCount)
				= ValidLegWaypointCounts(maxWaypoints: 10).Last();

			var ride = _fixture.Create<RideDto>();
			var legs = _fixture.Build<LegDto>()
				.With(x => x.Ride, ride)
				.With(x => x.RideId, ride.Id)
				.CreateMany(legCount)
				.ToArray();

			var result = _validatorCollectionOfLegs.Validate(legs);

			var lol = result.ToString();
			var kek = result.Errors.Select(x => x.ToString()).ToArray();

			Exception eee = null!;
			try { _validatorCollectionOfLegs.ValidateAndThrow(legs); }
			catch(Exception ex) { eee = ex; }

			var lolkek = eee.ToString();

			result.IsValid.Should().BeFalse();
		}

		[Fact]
		public void TestDoubledPoint()
		{
			var legs = BuildMinimalValidLegsCollection();

			legs[1].From = legs[2].From;
			legs[1].To = legs[2].To;

			var result = _validatorCollectionOfLegs.Validate(legs);

			result.IsValid.Should().BeFalse();
			result.Errors.Should().Contain(x => x.ErrorCode == ValidationCodes.Leg_InvalidPointsChain);
		}

		[Fact]
		public void ValidLegsTest()
		{
			var legs = BuildMinimalValidLegsCollection();

			var result = _validatorCollectionOfLegs.Validate(legs);

			result.IsValid.Should().BeTrue();
		}

		[Fact]
		public void WrongTimeChainTest()
		{
			var legs = BuildMinimalValidLegsCollection();

			var from = legs[1].From;
			from.DateTime = legs[0].To.DateTime.AddMinutes(1);
			legs[1].From = from;

			var result = _validatorCollectionOfLegs.Validate(legs);

			result.IsValid.Should().BeFalse();
		}

		private IEnumerable<(int legsCount, int waypointsCount)> ValidLegWaypointCounts(int maxWaypoints)
		{
			var lastValidLegCount = 1;
			yield return (lastValidLegCount, 2);

			for (int waypointCount = 3; waypointCount <= maxWaypoints; waypointCount++)
			{
				lastValidLegCount += waypointCount - 1;
				yield return (lastValidLegCount, waypointCount);
			}
		}

		private IReadOnlyList<LegDto> BuildMinimalValidLegsCollection()
		{
			var ride = _fixture.Create<RideDto>();
			return BuildMinimalValidLegsCollection(ride);
		}

		private IReadOnlyList<LegDto> BuildMinimalValidLegsCollection(RideDto ride)
		{
			var legs = _fixture.Build<LegDto>()
				.With(x => x.Ride, ride)
				.With(x => x.RideId, ride.Id)
				.CreateMany(3)
				.ToArray();

			var now = DateTimeOffset.UtcNow;
			var temp = legs[0].To;

			temp.DateTime = now;
			legs[0].To = temp;
			temp.DateTime = now.AddHours(-2);
			legs[0].From = temp;

			legs[1].From = legs[0].From;
			temp = legs[1].From;
			temp.DateTime = temp.DateTime.AddHours(1);
			legs[1].To = temp;

			legs[2].From = legs[1].To;
			legs[2].To = legs[0].To;

			return legs;
		}
	}
}
