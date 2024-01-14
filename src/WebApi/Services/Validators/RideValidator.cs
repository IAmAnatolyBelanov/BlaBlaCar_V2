using FluentValidation;

using WebApi.Models;
using WebApi.Services.Core;

namespace WebApi.Services.Validators
{
	public class RideValidationCodes : ValidationCodes
	{
		public const string EmptyId = "Ride_EmptyId";
		public const string EmptyDriverId = "Ride_EmptyDriverId";

		public const string EmptyLegsCollection = "Ride_EmptyLegsCollection";
		public const string LegsCollectionContainsNull = "Ride_LegsCollectionContainsNull";
		public const string TooManyLegs = "Ride_TooManyLegs";
		public const string ContainsInvalidLeg = "Ride_ContainsInvalidLeg";
		public const string MissmatchRideIdInLeg = "Ride_MissmatchRideIdInLeg";
		public const string LegsChainIsInvalid = "Ride_LegsChainIsInvalid";

		public const string WrongAvailablePlacesCount = "Ride_WrongAvailablePlacesCount";

		public const string EmptyPricesCollection = "Ride_EmptyPricesCollection";
		public const string PricesCollectionContansNull = "Ride_PricesCollectionContansNull";
		public const string WrongPricesCount = "Ride_WrongPricesCount";
		public const string ContainsInvalidPrice = "Ride_ContainsInvalidPrice";
		public const string MissmatchRideIdInPrice = "Ride_MissmatchRideIdInPrice";

		public const string NotEveryLegPairHasPrice = "Ride_NotEveryLegPairHasPrice";
	}

	public class RideValidator : AbstractValidator<RideDto>
	{
		private readonly IValidator<LegDto> _legValidator;
		private readonly IValidator<PriceDto> _priceValidator;

		private readonly IRideServiceConfig _rideServiceConfig;

		private readonly IReadOnlyDictionary<int, int> _validPriceWaypointCounts;
		private readonly IReadOnlyDictionary<int, int> _validWaypointPriceCounts;

		public RideValidator(
			IValidator<LegDto> legValidator,
			IRideServiceConfig rideServiceConfig,
			IValidator<PriceDto> priceValidator)
		{
			_legValidator = legValidator;
			_priceValidator = priceValidator;

			_rideServiceConfig = rideServiceConfig;

			_validPriceWaypointCounts = Helpers.ValidPriceWaypointCounts(_rideServiceConfig.MaxWaypoints)
				.ToDictionary(x => x.PricesCount, x => x.WaypointsCount);
			_validWaypointPriceCounts = Helpers.ValidPriceWaypointCounts(_rideServiceConfig.MaxWaypoints)
				.ToDictionary(x => x.WaypointsCount, x => x.PricesCount);

			RuleFor(x => x.Id)
				.NotEmpty()
				.WithErrorCode(RideValidationCodes.EmptyId);

			RuleFor(x => x.DriverId)
				.NotEmpty()
				.WithErrorCode(RideValidationCodes.EmptyDriverId);

			RuleFor(x => x.Legs)
				.NotEmpty()
				.WithErrorCode(RideValidationCodes.EmptyLegsCollection)
				.DependentRules(() =>
				{
					RuleFor(x => x.Legs)
						.Must((ride, _) => !ride.Legs!.ContainsNull())
						.WithErrorCode(RideValidationCodes.LegsCollectionContainsNull)
						.WithMessage($"{nameof(RideDto.Legs)} содержит NULL элемент")
						.DependentRules(() =>
						{
							RuleFor(x => x.Legs!.Count)
								.LessThanOrEqualTo(_rideServiceConfig.MaxWaypoints - 1)
								.WithErrorCode(RideValidationCodes.TooManyLegs);

							RuleFor(x => x.Legs).Custom(LegLavidatorWrapper!);

							RuleForEach(x => x.Legs)
								.Must((ride, leg) => leg.RideId == ride.Id)
								.WithErrorCode(RideValidationCodes.MissmatchRideIdInLeg)
								.WithMessage((ride, leg) => $"Leg {leg.Id} не связан с Ride {ride.Id}");

							RuleFor(x => x.Legs)
								.Must(IsLegsChainValid!)
								.WithErrorCode(RideValidationCodes.LegsChainIsInvalid)
								.WithMessage($"Коллекция {nameof(RideDto.Legs)} не создаёт неразрвыной последовательности");
						});
				});

			RuleFor(x => x.AvailablePlacesCount)
				.GreaterThanOrEqualTo(1)
				.WithErrorCode(RideValidationCodes.WrongAvailablePlacesCount);

			RuleFor(x => x.Prices)
				.NotEmpty()
				.WithErrorCode(RideValidationCodes.EmptyPricesCollection)
				.DependentRules(() =>
				{
					RuleFor(x => x.Prices)
						.Must((ride, _) => !ride.Prices!.ContainsNull())
						.WithErrorCode(RideValidationCodes.PricesCollectionContansNull)
						.WithMessage($"{nameof(RideDto.Prices)} содержит NULL элемент")
						.DependentRules(() =>
						{
							RuleFor(x => x.Prices)
								.Must((ride, prices) => IsPricesCountValid(ride))
								.WithMessage(x =>
								{
									if (_validWaypointPriceCounts.TryGetValue(x.WaypointsCount, out var validPricesCount))
										return $"Для {x.WaypointsCount} точек должно быть {validPricesCount} цен";
									else
										return $"Для {x.WaypointsCount} точек невозможно посчитать необходимое количество цен";
								})
								.WithErrorCode(RideValidationCodes.WrongPricesCount);

							RuleFor(x => x.Prices).Custom(PriceValidatorWrapper!);

							RuleForEach(x => x.Prices)
								.Must((ride, price) => price.StartLeg?.RideId == ride.Id && price.EndLeg?.RideId == ride.Id)
								.WithErrorCode(RideValidationCodes.MissmatchRideIdInPrice)
								.WithMessage((ride, price) => $"Price {price.Id} не связан с {ride.Id}");
						});
				});


			RuleFor(x => x.Prices)
				.Must(DoesEveryLegPairHavePrice!)
				.WithErrorCode(RideValidationCodes.NotEveryLegPairHasPrice)
				.WithMessage("Не для каждой пары Legs удалось найти Price");
		}

		private void LegLavidatorWrapper(IReadOnlyList<LegDto> legs, ValidationContext<RideDto> context)
		{
			bool anyError = false;

			for (int legIndex = 0; legIndex < legs.Count; legIndex++)
			{
				var leg = legs[legIndex];

				var internalErrors = _legValidator.Validate(leg);

				if (!internalErrors.IsValid)
				{
					anyError = true;

					for (int errorIndex = 0; errorIndex < internalErrors.Errors.Count; errorIndex++)
					{
						var error = internalErrors.Errors[errorIndex];
						error.ErrorMessage = $"Legs[{legIndex}] - Leg {leg.Id} - is invalid. {error.ErrorMessage}";
						context.AddFailure(error);
					}
				}
			}

			if (anyError)
			{
				context.AddFailure(new ValidationFailure()
				{
					Severity = Severity.Error,
					ErrorCode = RideValidationCodes.ContainsInvalidLeg,
					ErrorMessage = $"В коллекции {nameof(RideDto.Legs)} содержатся невалидные элементы",
					PropertyName = nameof(RideDto.Legs),
					AttemptedValue = legs
				});
			}
		}

		private void PriceValidatorWrapper(IReadOnlyList<PriceDto> prices, ValidationContext<RideDto> context)
		{
			bool anyError = false;

			for (int priceIndex = 0; priceIndex < prices.Count; priceIndex++)
			{
				var price = prices[priceIndex];

				var internalErrors = _priceValidator.Validate(price);

				if (!internalErrors.IsValid)
				{
					for (int errorIndex = 0; errorIndex < internalErrors.Errors.Count; errorIndex++)
					{
						var error = internalErrors.Errors[errorIndex];
						error.ErrorMessage = $"Prices[{priceIndex}] - Price {price.Id} - is invalid. {error.ErrorMessage}";
						context.AddFailure(error);
					}
				}
			}

			if (anyError)
			{
				context.AddFailure(new ValidationFailure()
				{
					Severity = Severity.Error,
					ErrorCode = RideValidationCodes.ContainsInvalidPrice,
					ErrorMessage = $"В коллекции {nameof(RideDto.Prices)} содержатся невалидные элементы",
					PropertyName = nameof(RideDto.Prices),
					AttemptedValue = prices
				});
			}
		}

		private bool IsLegsChainValid(IReadOnlyList<LegDto> legs)
		{
			if (legs.Count == 0)
				return false;

			if (legs[0].PreviousLeg is not null || legs[0].PreviousLegId is not null)
				return false;

			if (legs[^1].NextLeg is not null || legs[^1].NextLegId is not null)
				return false;

			if (legs.Count == 1)
				return true;

			for (int i = 1; i < legs.Count - 1; i++)
			{
				var leg = legs[i];

				if (leg.PreviousLeg is null || leg.PreviousLegId != leg.PreviousLeg.Id)
					return false;
				if (leg.NextLeg is null || leg.NextLegId != leg.NextLeg.Id)
					return false;

				if (leg.PreviousLeg != legs[i - 1] || leg.PreviousLegId != legs[i - 1].Id)
					return false;
				if (leg.NextLeg != legs[i + 1] || leg.NextLegId != legs[i + 1].Id)
					return false;

				var previous = leg.PreviousLeg;
				var next = leg.NextLeg;

				if (leg.From != previous.To)
					return false;
				if (leg.To != next.From)
					return false;
			}

			return true;
		}

		private bool IsPricesCountValid(RideDto ride)
		{
			if (!_validPriceWaypointCounts.TryGetValue(ride.Prices!.Count, out var validWaypointsCount))
				return false;

			return validWaypointsCount == ride.WaypointsCount;
		}

		private bool DoesEveryLegPairHavePrice(RideDto ride, IReadOnlyList<PriceDto> _)
		{
			// Эти проверки уже выполнены ранее
			if (ride.Legs is null || ride.Legs.Count == 0)
				return true;
			if(ride.Prices is null || ride.Prices.Count == 0)
				return true;

			for (int i = 0;  i < ride.Legs.Count;  i++)
			{
				for (int j = i; j < ride.Legs.Count; j++)
				{
					var start = ride.Legs[i];
					var end = ride.Legs[j];

					if (start is null || end is null)
						return false;

					if (!ContainsPriceForLegs(ride.Prices, start, end))
						return false;
				}
			}

			return true;
		}

		private bool ContainsPriceForLegs(IReadOnlyList<PriceDto> prices, LegDto start, LegDto end)
		{
			for (int i = 0; i < prices.Count;i++)
			{
				var price = prices[i];

				if (price is null)
					return false;

				if (price.StartLeg == start && price.EndLeg == end)
					return true;
			}
			return false;
		}
	}
}
