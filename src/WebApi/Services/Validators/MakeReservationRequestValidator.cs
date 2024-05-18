using FluentValidation;
using WebApi.Models.ControllersModels.RideControllerModels;

namespace WebApi.Services.Validators;

public class MakeReservationRequestValidationCodes : ValidationCodes
{
	public const string EmptyRideId = "MakeReservationRequestValidation_EmptyRideId";
	public const string EmptyPassengerId = "MakeReservationRequestValidation_EmptyPassengerId";
	public const string EmptyPassengersCount = "MakeReservationRequestValidation_EmptyPassengersCount";
	public const string TooLittlePassengersCount = "MakeReservationRequestValidation_TooLittlePassengersCount";
	public const string EmptyCoordinates = "MakeReservationRequestValidation_EmptyCoordinates";
}

public class MakeReservationRequestValidator : AbstractValidator<MakeReservationRequest>
{
	public MakeReservationRequestValidator()
	{
		RuleFor(x => x.RideId)
			.NotEmpty()
			.WithErrorCode(MakeReservationRequestValidationCodes.EmptyRideId)
			.WithMessage("Не заполнена поездка");

		RuleFor(x => x.PassengerId)
			.NotNull()
			.WithErrorCode(MakeReservationRequestValidationCodes.EmptyPassengerId)
			.WithMessage("Не заполнен пассажир");

		RuleFor(x => x.PassengersCount)
			.NotNull()
			.WithErrorCode(MakeReservationRequestValidationCodes.EmptyPassengersCount)
			.WithMessage("Не указано число пассажиров")
			.DependentRules(() =>
			{
				RuleFor(x => x.PassengersCount)
					.GreaterThanOrEqualTo(1)
					.WithErrorCode(MakeReservationRequestValidationCodes.TooLittlePassengersCount)
					.WithMessage("Количество пассажиров не может быть меньше 1");
			});

		RuleFor(x => x.WaypointFrom)
			.NotEmpty()
			.WithErrorCode(MakeReservationRequestValidationCodes.EmptyCoordinates)
			.WithMessage("Не указана точка отправления");
		RuleFor(x => x.WaypointTo)
			.NotEmpty()
			.WithErrorCode(MakeReservationRequestValidationCodes.EmptyCoordinates)
			.WithMessage("Не указана точка прибытия");
	}
}