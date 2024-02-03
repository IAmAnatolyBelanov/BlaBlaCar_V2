using WebApi.Models;
using WebApi.Services.Validators;

namespace Tests
{
	public class YandexGeocodeResponseTests : IClassFixture<EmptyTestAppFactory>
	{
		private readonly Fixture _fixture;
		private readonly IValidator<(FormattedPoint Point, YandexGeocodeResponseDto GeocodeResponse)> _yaGeocodeResponseWithPointValidator;

		public YandexGeocodeResponseTests(EmptyTestAppFactory factory)
		{
			_fixture = Shared.BuildDefaultFixture();

			_yaGeocodeResponseWithPointValidator = factory.Services.GetRequiredService<IValidator<(FormattedPoint Point, YandexGeocodeResponseDto GeocodeResponse)>>();
		}


		[Fact]
		public void TestInternalValidator()
		{
			var geocode = new YandexGeocodeResponseDto
			{
				Success = false
			};

			var point = CityInfoManager.GetUnique().GetPoint();

			var errors = _yaGeocodeResponseWithPointValidator.Validate((point, geocode));

			errors.IsValid.Should().BeFalse();
			errors.Errors.Should().HaveCount(1);
			errors.Errors.Should().Contain(x => x.ErrorCode == YandexGeocodeResponseDtoValidationCodes.Fail);
			errors.Errors.Should().Contain(x => x.ErrorMessage.StartsWith(point.ToString() + " is invalid. "));
		}
	}
}
