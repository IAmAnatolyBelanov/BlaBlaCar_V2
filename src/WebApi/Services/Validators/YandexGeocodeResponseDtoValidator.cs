using FluentValidation;

using WebApi.Models;

namespace WebApi.Services.Validators
{
	public static partial class ValidationCodes
	{
		public const string YaGeocodeResponse_Fail = "YandexGeocodeResponse_Fail";
		public const string YaGeocodeResponse_EmptyGeoobjects = "YandexGeocodeResponse_EmptyGeoobjects";
		public const string YaGeocodeResponse_EmptyGeoobject = "YandexGeocodeResponse_EmptyGeoobject";
		public const string YaGeocodeResponse_GeoobjectAddressIsEmpty = "YandexGeocodeResponse_GeoobjectAddressIsEmpty";
	}

	public class YandexGeocodeResponseDtoWithPointValidator : AbstractValidator<(FormattedPoint Point, YandexGeocodeResponseDto GeocodeResponse)>
	{
		private readonly IValidator<YandexGeocodeResponseDto> _validator;

		public YandexGeocodeResponseDtoWithPointValidator(IValidator<YandexGeocodeResponseDto> validator)
		{
			_validator = validator;

			RuleFor(x => x)
				.Custom((full, context) =>
				{
					var internalErrors = _validator.Validate(full.GeocodeResponse);

					if (!internalErrors.IsValid)
					{
						for (int i = 0; i < internalErrors.Errors.Count; i++)
						{
							var error = internalErrors.Errors[i];
							error.ErrorMessage = $"{full.Point} is invalid. {error.ErrorMessage}";
							context.AddFailure(error);
						}
					}
				});
		}
	}

	public class YandexGeocodeResponseDtoValidator : AbstractValidator<YandexGeocodeResponseDto>
	{
		public YandexGeocodeResponseDtoValidator()
		{
			RuleFor(x => x.Success)
				.Equal(true)
				.WithErrorCode(ValidationCodes.YaGeocodeResponse_Fail)
				.WithMessage("Не удалось получить гео код")
				.DependentRules(() =>
				{
					RuleFor(x => x.Geoobjects)
						.NotEmpty()
						.WithErrorCode(ValidationCodes.YaGeocodeResponse_EmptyGeoobjects)
						.DependentRules(() =>
						{
							RuleForEach(x => x.Geoobjects)
								.NotEmpty()
								.WithErrorCode(ValidationCodes.YaGeocodeResponse_EmptyGeoobject);

							RuleForEach(x => x.Geoobjects)
								.Must(x => !string.IsNullOrWhiteSpace(x?.FormattedAddress))
								.WithErrorCode(ValidationCodes.YaGeocodeResponse_GeoobjectAddressIsEmpty)
								.WithMessage("Адрес в гео коде пуст");
						});
				});
		}
	}
}
