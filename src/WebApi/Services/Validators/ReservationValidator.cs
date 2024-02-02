using FluentValidation;

using WebApi.Models;

namespace WebApi.Services.Validators
{
	public class ReservationValidationCodes : ValidationCodes
	{
		public const string EmptyUserId = "Reserve_EmptyUserId";
		public const string TooLessCount = "Reserve_TooLessCount";
		public const string EmptyStartLegId = "Reserve_EmptyStartLegId";
		public const string EmptyEndLegId = "Reserve_EmptyEndLegId";
		public const string MismatchLegId = "Reserve_MismatchLegId";
		public const string WrongCreateDateTime = "Reserve_WrongCreateDateTime";
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
				.WithErrorCode(ReservationValidationCodes.MismatchLegId);
			RuleFor(x => x.EndLegId)
				.Equal(x => x.EndLeg!.Id)
				.When(x => x.EndLeg is not null)
				.WithErrorCode(ReservationValidationCodes.MismatchLegId);

			RuleFor(x => x.CreateDateTime)
				.LessThanOrEqualTo(x => _clock.Now)
				.WithErrorCode(ReservationValidationCodes.WrongCreateDateTime);
		}
	}
}
