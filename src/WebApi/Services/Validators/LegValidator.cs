using FluentValidation;

using WebApi.Models;

namespace WebApi.Services.Validators
{
	public class LegValidationCodes : ValidationCodes
	{
		public const string InvalidStartAndEndTime = "Leg_InvalidStartAndEndTime";
		public const string InvalidPrice = "Leg_InvalidPrice";

		public const string EmptyCollection = "Leg_EmptyCollection";
		public const string NullElementInCollection = "Leg_NullElementInCollection";
		public const string DefaultId = "Leg_DefaultId";
		public const string InvalidCount = "Leg_InvalidCount";
		public const string MissmatchRideId = "Leg_MissmatchRideId";
		public const string DifferentRideId = "Leg_DifferentRideId";
		public const string InvalidElementInCollection = "Leg_InvalidElementInCollection";
		public const string InvalidPointsChain = "Leg_InvalidPointsChain";
		public const string InvalidTimeChain = "Leg_InvalidTimeChain";
		public const string EmptyId = "Leg_EmptyId";
	}

	public class LegDtoValidator : AbstractValidator<LegDto>
	{
		public LegDtoValidator()
		{
			RuleFor(x => x.Id)
				.NotEmpty()
				.WithErrorCode(LegValidationCodes.EmptyId);

			RuleFor(x => x.To)
				.Must((leg, to) => to.DateTime > leg.From.DateTime)
				.WithErrorCode(LegValidationCodes.InvalidStartAndEndTime)
				.WithMessage($"{nameof(LegDto.To)}.{nameof(LegDto.To.DateTime)} должен быть больше {nameof(LegDto.From)}.{nameof(LegDto.From.DateTime)}");

			RuleFor(x => x.RideId)
				.Must((leg, _) => leg.RideId == leg.Ride.Id)
				.WithErrorCode(LegValidationCodes.MissmatchRideId)
				.WithMessage($"{nameof(LegDto.RideId)} не совпадает с {nameof(LegDto.Ride)}.{nameof(LegDto.Ride.Id)}");
		}
	}
}
