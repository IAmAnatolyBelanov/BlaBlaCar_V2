using FluentValidation;

namespace WebApi.Services.Validators;

public class DriverValidatorCodes : ValidationCodes
{
	public const string IncorrectDriverLicenseData = "DriverValidator_IncorrectDriverLicenseData";
	public const string DrivingLicenseWasUsedForAnotherUser = "DriverValidator_DrivingLicenseWasUsedForAnotherUser";

	public const string IncorrectDriverLicenseSeries = "DriverValidator_IncorrectDriverLicenseSeries";
	public const string IncorrectDriverLicenseNumber = "DriverValidator_IncorrectDriverLicenseNumber";

	public const string IncorrectDriverLicenseIssuance = "DriverValidator_IncorrectDriverLicenseIssuance";

	public const string EmptyPassport = "DriverValidator_EmptyPassport";
}

public class DriverValidator : AbstractValidator<WebApi.Models.DriverServiceModels.Driver>
{
	public DriverValidator()
	{
		RuleFor(x => x.LicenseSeries)
			.GreaterThanOrEqualTo(1)
			.WithErrorCode(DriverValidatorCodes.IncorrectDriverLicenseSeries)
			.WithMessage("Серия водительского удостоверения должна находиться в диапазоне от 0001 до 9999");

		RuleFor(x => x.LicenseSeries)
			.LessThanOrEqualTo(9_999)
			.WithErrorCode(DriverValidatorCodes.IncorrectDriverLicenseSeries)
			.WithMessage("Серия водительского удостоверения должна находиться в диапазоне от 0001 до 9999");

		RuleFor(x => x.LicenseNumber)
			.GreaterThanOrEqualTo(1)
			.WithErrorCode(DriverValidatorCodes.IncorrectDriverLicenseNumber)
			.WithMessage("Номер водительского удостоверения должен находиться в диапазоне от 000001 до 999999");

		RuleFor(x => x.LicenseNumber)
			.LessThanOrEqualTo(valueToCompare: 999_999)
			.WithErrorCode(DriverValidatorCodes.IncorrectDriverLicenseNumber)
			.WithMessage("Номер водительского удостоверения должен находиться в диапазоне от 000001 до 999999");

		RuleFor(x => x.Issuance)
			.NotEmpty()
			.WithErrorCode(DriverValidatorCodes.IncorrectDriverLicenseIssuance)
			.WithMessage("Не заполнена дата выдачи водительского удостоверения");
	}
}
