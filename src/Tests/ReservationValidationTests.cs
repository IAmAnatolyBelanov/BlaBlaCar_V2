using AutoFixture;

using FluentAssertions;

using FluentValidation;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using WebApi.Models;
using WebApi.Services.Validators;

namespace Tests
{
	public class ReservationValidationTests : IClassFixture<WebApplicationFactory<Program>>
	{
		private readonly Fixture _fixture;
		private readonly IValidator<ReservationDto> _validator;

		public ReservationValidationTests(WebApplicationFactory<Program> factory)
		{
			_fixture = Shared.BuildDefaultFixture();
			_validator = factory.Services.GetRequiredService<IValidator<ReservationDto>>();
		}

		[Fact]
		public void TestCreateDateTime()
		{
			var reserve = _fixture.Build<ReservationDto>()
				.Without(x => x.StartLeg)
				.Without(x => x.EndLeg)
				.With(x => x.CreateDateTime, () => DateTimeOffset.UtcNow.AddHours(1))
				.Create();

			var result = _validator.Validate(reserve);

			result.Errors.Should().Contain(x => x.ErrorCode == ReservationValidationCodes.WrongCreateDateTime);
		}

		[Fact]
		public void TestEmptyLeg()
		{
			var reserve = _fixture.Build<ReservationDto>()
				.Without(x => x.EndLeg)
				.With(x => x.CreateDateTime, () => DateTimeOffset.UtcNow)
				.Create();

			var result = _validator.Validate(reserve);

			result.IsValid.Should().BeTrue();
		}

		[Fact]
		public void TestMismatchLeg()
		{
			var reserve = _fixture.Build<ReservationDto>()
				.With(x => x.CreateDateTime, () => DateTimeOffset.UtcNow)
				.Create();

			var result = _validator.Validate(reserve);

			result.Errors.Should().Contain(x => x.ErrorCode == ReservationValidationCodes.MismatchLegId);
		}
	}
}
