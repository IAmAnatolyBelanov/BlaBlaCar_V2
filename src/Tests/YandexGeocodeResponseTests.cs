using AutoFixture;

using FluentAssertions;

using FluentValidation;

using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

using WebApi.Models;
using WebApi.Services.Validators;

namespace Tests
{
	public class YandexGeocodeResponseTests : IClassFixture<WebApplicationFactory<Program>>
	{
		private readonly Fixture _fixture;
		private readonly IValidator<(FormattedPoint Point, YandexGeocodeResponseDto GeocodeResponse)> _yaGeocodeResponseWithPointValidator;

		public YandexGeocodeResponseTests(WebApplicationFactory<Program> factory)
		{
			_fixture = Shared.BuildDefaultFixture();

			_yaGeocodeResponseWithPointValidator = factory.Services.GetRequiredService<IValidator<(FormattedPoint Point, YandexGeocodeResponseDto GeocodeResponse)>>();
		}


		[Fact]
		public void TestInternalValidator()
		{
			var geoocode = new YandexGeocodeResponseDto
			{
				Success = false
			};

			var point = CityInfoManager.GetUnique().GetPoint();

			var errors = _yaGeocodeResponseWithPointValidator.Validate((point, geoocode));

			errors.IsValid.Should().BeFalse();
			errors.Errors.Should().HaveCount(1);
			errors.Errors.Should().Contain(x => x.ErrorCode == YandexGeocodeResponseDtoValidationCodes.Fail);
			errors.Errors.Should().Contain(x => x.ErrorMessage.StartsWith(point.ToString() + " is invalid. "));
		}
	}
}
