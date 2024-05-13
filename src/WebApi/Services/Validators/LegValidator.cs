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
		public const string MismatchRideId = "Leg_MismatchRideId";
		public const string DifferentRideId = "Leg_DifferentRideId";
		public const string InvalidElementInCollection = "Leg_InvalidElementInCollection";
		public const string InvalidPointsChain = "Leg_InvalidPointsChain";
		public const string InvalidTimeChain = "Leg_InvalidTimeChain";
		public const string EmptyId = "Leg_EmptyId";
		public const string InvalidCoordinates = "Point_InvalidCoordinates";
	}

	public class LegDtoValidator : AbstractValidator<LegDto_Obsolete>
	{
		public LegDtoValidator()
		{
			RuleFor(x => x.Id)
				.NotEmpty()
				.WithErrorCode(LegValidationCodes.EmptyId);

			RuleFor(x => x.To)
				.Must((leg, to) => to.DateTime > leg.From.DateTime)
				.WithErrorCode(LegValidationCodes.InvalidStartAndEndTime)
				.WithMessage($"{nameof(LegDto_Obsolete.To)}.{nameof(LegDto_Obsolete.To.DateTime)} должен быть больше {nameof(LegDto_Obsolete.From)}.{nameof(LegDto_Obsolete.From.DateTime)}");

			RuleFor(x => x.RideId)
				.Must((leg, _) => leg.RideId == leg.Ride.Id)
				.WithErrorCode(LegValidationCodes.MismatchRideId)
				.WithMessage($"{nameof(LegDto_Obsolete.RideId)} не совпадает с {nameof(LegDto_Obsolete.Ride)}.{nameof(LegDto_Obsolete.Ride.Id)}");

			RuleFor(x => x.From.Point.Longitude)
				.GreaterThanOrEqualTo(-180)
				.WithErrorCode(LegValidationCodes.InvalidCoordinates)
				.LessThanOrEqualTo(180)
				.WithErrorCode(LegValidationCodes.InvalidCoordinates);

			RuleFor(x => x.To.Point.Longitude)
				.GreaterThanOrEqualTo(-180)
				.WithErrorCode(LegValidationCodes.InvalidCoordinates)
				.LessThanOrEqualTo(180)
				.WithErrorCode(LegValidationCodes.InvalidCoordinates);

			RuleFor(x => x.From.Point.Latitude)
				.GreaterThanOrEqualTo(-90)
				.WithErrorCode(LegValidationCodes.InvalidCoordinates)
				.LessThanOrEqualTo(90)
				.WithErrorCode(LegValidationCodes.InvalidCoordinates);

			RuleFor(x => x.To.Point.Latitude)
				.GreaterThanOrEqualTo(-90)
				.WithErrorCode(LegValidationCodes.InvalidCoordinates)
				.LessThanOrEqualTo(90)
				.WithErrorCode(LegValidationCodes.InvalidCoordinates);
		}
	}
}
