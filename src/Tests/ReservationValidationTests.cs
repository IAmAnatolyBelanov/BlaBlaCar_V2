using WebApi.Models;
using WebApi.Services.Validators;

namespace Tests
{
	public class ReservationValidationTests : IClassFixture<EmptyTestAppFactory>
	{
		private readonly Fixture _fixture;
		private readonly IValidator<ReservationDto> _validator;

		public ReservationValidationTests(EmptyTestAppFactory factory)
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
