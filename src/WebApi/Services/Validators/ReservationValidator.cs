using FluentValidation;

using WebApi.Models;

namespace WebApi.Services.Validators
{
	public class ReservationValidationCodes : ValidationCodes
	{
		public const string EmptyUserId = "Reserv_EmptyUserId";
		public const string TooLessCount = "Reserv_TooLessCount";
		public const string EmptyStartLegId = "Reserv_EmptyStartLegId";
		public const string EmptyEndLegId = "Reserv_EmptyEndLegId";
		public const string MissmatchLegId = "Reserv_MissmatchLegId";
		public const string WrongCreateDateTime = "Reserv_WrongCreateDateTime";
	}

	public class ReservationValidator : AbstractValidator<ReservationDto>
	{
		private readonly IClock _clock;

		public ReservationValidator(IClock clock)
		{
			_clock = clock;

			RuleFor(x => x.UserId)
				.NotEmpty()
				.WithErrorCode(ReservationValidationCodes.EmptyUserId);

			RuleFor(x => x.Count)
				.GreaterThanOrEqualTo(1)
				.WithErrorCode(ReservationValidationCodes.TooLessCount);

			RuleFor(x => x.StartLegId)
				.NotEmpty()
				.WithErrorCode(ReservationValidationCodes.EmptyStartLegId);
			RuleFor(x => x.EndLegId)
				.NotEmpty()
				.WithErrorCode(ReservationValidationCodes.EmptyEndLegId);

			RuleFor(x => x.StartLegId)
				.Equal(x => x.StartLeg!.Id)
				.When(x => x.StartLeg is not null)
				.WithErrorCode(ReservationValidationCodes.MissmatchLegId);
			RuleFor(x => x.EndLegId)
				.Equal(x => x.EndLeg!.Id)
				.When(x => x.EndLeg is not null)
				.WithErrorCode(ReservationValidationCodes.MissmatchLegId);

			RuleFor(x => x.CreateDateTime)
				.LessThanOrEqualTo(x => _clock.Now)
				.WithErrorCode(ReservationValidationCodes.WrongCreateDateTime);
		}
	}
}
