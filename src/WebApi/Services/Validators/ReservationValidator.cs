using FluentValidation;

using WebApi.Models;

namespace WebApi.Services.Validators
{
	public static partial class ValidationCodes
	{
		public const string Reserv_EmptyUserId = "Reserv_EmptyUserId";
		public const string Reserv_TooLessCount = "Reserv_TooLessCount";
		public const string Reserv_EmptyLegId = "Reserv_EmptyLegId";
		public const string Reserv_MissmatchLegId = "Reserv_MissmatchLegId";
		public const string Reserv_WrongCreateDateTime = "Reserv_WrongCreateDateTime";
	}

	public class ReservationValidator : AbstractValidator<ReservationDto>
	{
		public ReservationValidator()
		{
			RuleFor(x => x.UserId)
				.NotEmpty()
				.WithErrorCode(ValidationCodes.Reserv_EmptyUserId);

			RuleFor(x => x.Count)
				.GreaterThanOrEqualTo(1)
				.WithErrorCode(ValidationCodes.Reserv_TooLessCount);

			RuleFor(x => x.LegId)
				.NotEmpty()
				.WithErrorCode(ValidationCodes.Reserv_EmptyLegId);

			RuleFor(x => x.LegId)
				.Equal(x => x.Leg!.Id)
				.When(x => x.Leg is not null)
				.WithErrorCode(ValidationCodes.Reserv_MissmatchLegId);

			RuleFor(x => x.CreateDateTime)
				.LessThanOrEqualTo(x => DateTimeOffset.UtcNow)
				.WithErrorCode(ValidationCodes.Reserv_WrongCreateDateTime);
		}
	}
}
