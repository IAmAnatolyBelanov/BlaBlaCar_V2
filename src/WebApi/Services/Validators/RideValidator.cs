using FluentValidation;
using System.Data;

using WebApi.Models;
using WebApi.Services.Core;

namespace WebApi.Services.Validators;

public class RideValidationCodes : ValidationCodes
{
	public const string EmptyId = "Ride_EmptyId";
	public const string EmptyDriverId = "Ride_EmptyDriverId";

	public const string EmptyLegsCollection = "Ride_EmptyLegsCollection";
	public const string LegsCollectionContainsNull = "Ride_LegsCollectionContainsNull";
	public const string TooManyLegs = "Ride_TooManyLegs";
	public const string ContainsInvalidLeg = "Ride_ContainsInvalidLeg";
	public const string MismatchRideIdInLeg = "Ride_MismatchRideIdInLeg";
	public const string LegsChainIsInvalid = "Ride_LegsChainIsInvalid";

	public const string WrongAvailablePlacesCount = "Ride_WrongAvailablePlacesCount";

	public const string EmptyPricesCollection = "Ride_EmptyPricesCollection";
	public const string PricesCollectionContainsNull = "Ride_PricesCollectionContainsNull";
	public const string WrongPricesCount = "Ride_WrongPricesCount";
	public const string ContainsInvalidPrice = "Ride_ContainsInvalidPrice";
	public const string MismatchRideIdInPrice = "Ride_MismatchRideIdInPrice";

	public const string NotEveryLegPairHasPrice = "Ride_NotEveryLegPairHasPrice";


	public const string EmptyAuthorId = "RideValidator_EmptyAuthorId";

	public const string EmptyPaymentMethods = "RideValidator_EmptyPaymentMethods";
	public const string DoubledPaymentMethods = "RideValidator_DoubledPaymentMethods";
	public const string PaymentMethodsContainsUnknown = "RideValidator_PaymentMethodsContainsUnknown";

	public const string StatusOutOfEnum = "RideValidator_StatusOutOfEnum";
	public const string StatusUnknown = "RideValidator_StatusUnknown";
	public const string InvalidCreationStatus = "RideValidator_InvalidCreationStatus";

	public const string TooLittlePassengerSeats = "RideValidator_TooLittlePassengerSeats";

	public const string EmptyCarId = "RideValidator_EmptyCarId";

	public const string ValidationMethodOutOfEnum = "RideValidator_ValidationMethodOutOfEnum";
	public const string ValidationMethodUnknown = "RideValidator_ValidationMethodUnknown";

	public const string EmptyValidationTimeBeforeDeparture = "RideValidator_ValidationTimeBeforeDeparture";
	public const string TooSmallValidationTimeBeforeDeparture = "RideValidator_TooSmallValidationTimeBeforeDeparture";

	public const string EmptyAfterRideValidationTimeoutAction = "RideValidator_EmptyAfterRideValidationTimeoutAction";
	public const string UnknownAfterRideValidationTimeoutAction = "RideValidator_UnknownAfterRideValidationTimeoutAction";

	public const string EmptyWaypoints = "RideValidator_EmptyWaypoints";
	public const string EmptyLegs = "RideValidator_EmptyLegs";

	public const string MismatchWaypointsAndLegs = "RideValidator_MismatchWaypointsAndLegs";

	public const string DoubledWaypointsCoordinates = "RideValidator_DoubledWaypointsCoordinates";
	public const string IncorrectWaypointsTimesChain = "RideValidator_IncorrectWaypointsTimesChain";
	public const string DoubledLegs = "RideValidator_DoubledLegs";

	public const string WrongCountOfWaypointsWithoutDeparture = "RideValidator_WrongCountOfWaypointsWithoutDeparture";

	public const string ImpossibleToGoThroughEveryWaypoint = "RideValidator_ImpossibleToGoThroughEveryWaypoint";

	public const string IncorrectTimesChainInLegs = "RideValidator_IncorrectTimesChainInLegs";

	public const string LegWithoutPrice = "RideValidator_LegWithoutPrice";

	public const string WrongCountOfWaypoints = "RideValidator_WrongCountOfWaypoints";
}

public class RidePreparationValidator : AbstractValidator<RidePreparationDto_Obsolete>
{
	private readonly IRideServiceConfig _rideServiceConfig;

	private readonly IValidator<LegDto_Obsolete> _legValidator;

	public RidePreparationValidator(
		IRideServiceConfig rideServiceConfig,
		IValidator<LegDto_Obsolete> legValidator)
	{
		_rideServiceConfig = rideServiceConfig;
		_legValidator = legValidator;

		RuleFor(x => x.Id)
			.NotEmpty()
			.WithErrorCode(RideValidationCodes.EmptyId);

		RuleFor(x => x.DriverId)
			.NotEmpty()
			.WithErrorCode(RideValidationCodes.EmptyDriverId);

		RuleFor(x => x.AvailablePlacesCount)
			.GreaterThanOrEqualTo(1)
			.WithErrorCode(RideValidationCodes.WrongAvailablePlacesCount);

		RuleFor(x => x.Legs)
			.NotEmpty()
			.WithErrorCode(RideValidationCodes.EmptyLegsCollection)
			.DependentRules(() =>
			{
				RuleFor(x => x.Legs)
					.Must((ride, _) => !ride.Legs!.ContainsNull())
					.WithErrorCode(RideValidationCodes.LegsCollectionContainsNull)
					.WithMessage($"{nameof(RideDto_Obsolete.Legs)} содержит NULL элемент")
					.DependentRules(() =>
					{
						RuleFor(x => x.Legs!.Count)
							.LessThanOrEqualTo(_rideServiceConfig.MaxWaypoints - 1)
							.WithErrorCode(RideValidationCodes.TooManyLegs);

						RuleFor(x => x.Legs).Custom(LegValidatorWrapper!);

						RuleForEach(x => x.Legs)
							.Must((ride, leg) => leg.RideId == ride.Id)
							.WithErrorCode(RideValidationCodes.MismatchRideIdInLeg)
							.WithMessage((ride, leg) => $"Leg {leg.Id} не связан с Ride {ride.Id}");

						RuleFor(x => x.Legs)
							.Must(IsLegsChainValid!)
							.WithErrorCode(RideValidationCodes.LegsChainIsInvalid)
							.WithMessage($"Коллекция {nameof(RideDto_Obsolete.Legs)} не создаёт неразрывной последовательности");
					});
			});
	}


	private void LegValidatorWrapper(IReadOnlyList<LegDto_Obsolete> legs, ValidationContext<RidePreparationDto_Obsolete> context)
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
				ErrorMessage = $"В коллекции {nameof(RidePreparationDto_Obsolete.Legs)} содержатся невалидные элементы",
				PropertyName = nameof(RidePreparationDto_Obsolete.Legs),
				AttemptedValue = legs
			});
		}
	}

	private bool IsLegsChainValid(IReadOnlyList<LegDto_Obsolete> legs)
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
}

public class RideObsoleteValidator : AbstractValidator<RideDto_Obsolete>
{
	private readonly IValidator<RidePreparationDto_Obsolete> _ridePreparationValidator;
	private readonly IValidator<LegDto_Obsolete> _legValidator;
	private readonly IValidator<PriceDto> _priceValidator;

	private readonly IRideServiceConfig _rideServiceConfig;

	private readonly IReadOnlyDictionary<int, int> _validPriceWaypointCounts;
	private readonly IReadOnlyDictionary<int, int> _validWaypointPriceCounts;

	public RideObsoleteValidator(
		IValidator<RidePreparationDto_Obsolete> ridePreparationValidator,
		IValidator<LegDto_Obsolete> legValidator,
		IRideServiceConfig rideServiceConfig,
		IValidator<PriceDto> priceValidator)
	{
		_ridePreparationValidator = ridePreparationValidator;
		_legValidator = legValidator;
		_priceValidator = priceValidator;

		_rideServiceConfig = rideServiceConfig;

		_validPriceWaypointCounts = Helpers.ValidPriceWaypointCounts(_rideServiceConfig.MaxWaypoints)
			.ToDictionary(x => x.PricesCount, x => x.WaypointsCount);
		_validWaypointPriceCounts = Helpers.ValidPriceWaypointCounts(_rideServiceConfig.MaxWaypoints)
			.ToDictionary(x => x.WaypointsCount, x => x.PricesCount);

		RuleFor(x => x)
			.Custom((_, context) => _ridePreparationValidator.Validate(context));

		RuleFor(x => x.Prices)
			.NotEmpty()
			.WithErrorCode(RideValidationCodes.EmptyPricesCollection)
			.DependentRules(() =>
			{
				RuleFor(x => x.Prices)
					.Must((ride, _) => !ride.Prices!.ContainsNull())
					.WithErrorCode(RideValidationCodes.PricesCollectionContainsNull)
					.WithMessage($"{nameof(RideDto_Obsolete.Prices)} содержит NULL элемент")
					.DependentRules(() =>
					{
						RuleFor(x => x.Prices)
							.Must((ride, _) => IsPricesCountValid(ride))
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
							.WithErrorCode(RideValidationCodes.MismatchRideIdInPrice)
							.WithMessage((ride, price) => $"Price {price.Id} не связан с {ride.Id}");
					});
			});


		RuleFor(x => x.Prices)
			.Must(DoesEveryLegPairHavePrice!)
			.WithErrorCode(RideValidationCodes.NotEveryLegPairHasPrice)
			.WithMessage("Не для каждой пары Legs удалось найти Price");
	}

	private void PriceValidatorWrapper(IReadOnlyList<PriceDto> prices, ValidationContext<RideDto_Obsolete> context)
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
				ErrorMessage = $"В коллекции {nameof(RideDto_Obsolete.Prices)} содержатся невалидные элементы",
				PropertyName = nameof(RideDto_Obsolete.Prices),
				AttemptedValue = prices
			});
		}
	}

	private bool IsPricesCountValid(RideDto_Obsolete ride)
	{
		if (!_validPriceWaypointCounts.TryGetValue(ride.Prices!.Count, out var validWaypointsCount))
			return false;

		return validWaypointsCount == ride.WaypointsCount;
	}

	private bool DoesEveryLegPairHavePrice(RideDto_Obsolete ride, IReadOnlyList<PriceDto> _)
	{
		// Эти проверки уже выполнены ранее
		if (ride.Legs is null || ride.Legs.Count == 0)
			return true;
		if (ride.Prices is null || ride.Prices.Count == 0)
			return true;

		for (int i = 0; i < ride.Legs.Count; i++)
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

	private bool ContainsPriceForLegs(IReadOnlyList<PriceDto> prices, LegDto_Obsolete start, LegDto_Obsolete end)
	{
		for (int i = 0; i < prices.Count; i++)
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

public class RideDtoValidator : AbstractValidator<RideDto>
{
	public const string CreateRuleSet = "CreateRuleSet";

	private readonly IRideServiceConfig _rideServiceConfig;

	public RideDtoValidator(IRideServiceConfig rideServiceConfig)
	{
		_rideServiceConfig = rideServiceConfig;

		RuleFor(x => x.AuthorId)
			.NotEmpty()
			.WithErrorCode(RideValidationCodes.EmptyAuthorId)
			.WithMessage("Не заполнен автор");

		RuleFor(x => x.Status)
			.IsInEnum()
			.WithErrorCode(RideValidationCodes.StatusOutOfEnum)
			.WithMessage(x => $"Статус {x.Status} не поддерживается");

		RuleFor(x => x.Status)
			.NotEqual(RideStatus.Unknown)
			.WithErrorCode(RideValidationCodes.StatusUnknown)
			.WithMessage($"Статус {nameof(RideStatus.Unknown)} не поддерживается");

		RuleFor(x => x.PaymentMethods)
			.NotEmpty()
			.WithErrorCode(RideValidationCodes.EmptyPaymentMethods)
			.WithMessage($"Не заполнены способы оплаты поездки")
			.DependentRules(() =>
			{
				RuleFor(x => x.PaymentMethods)
					.Must(PaymentMethodsAreUnique)
					.WithErrorCode(RideValidationCodes.DoubledPaymentMethods)
					.WithMessage("Способы оплаты не должны дублироваться");

				RuleFor(x => x.PaymentMethods)
					.Must(x => !PaymentMethodsContainsUnknown(x))
					.WithErrorCode(RideValidationCodes.PaymentMethodsContainsUnknown)
					.WithMessage($"Способ оплаты {nameof(PaymentMethod.Unknown)} не поддерживается");
			});

		RuleFor(x => x.AvailablePlacesCount)
			.GreaterThanOrEqualTo(1)
			.WithErrorCode(RideValidationCodes.TooLittlePassengerSeats)
			.WithMessage("Количество мест для пассажиров должно быть минимум 1");

		RuleFor(x => x.ValidationMethod)
			.IsInEnum()
			.WithErrorCode(RideValidationCodes.ValidationMethodOutOfEnum)
			.WithMessage(x => $"Способ проверки пассажира {x.ValidationMethod} не поддерживается");

		RuleFor(x => x.ValidationMethod)
			.NotEqual(RideValidationMethod.Unknown)
			.WithErrorCode(RideValidationCodes.ValidationMethodUnknown)
			.WithMessage($"Способ проверки пассажира {nameof(RideValidationMethod.Unknown)} не поддерживается");

		When(x => x.ValidationMethod == RideValidationMethod.ValidationBeforeAccessPassenger, () =>
		{
			RuleFor(x => x.ValidationTimeBeforeDeparture)
				.NotNull()
				.WithErrorCode(RideValidationCodes.EmptyValidationTimeBeforeDeparture)
				.WithMessage($"При способе валидации пассажира {nameof(RideValidationMethod.ValidationBeforeAccessPassenger)} должно быть заполнено поле {nameof(RideDto.ValidationTimeBeforeDeparture)}")
				.DependentRules(() =>
				{
					RuleFor(x => x.ValidationTimeBeforeDeparture)
						.GreaterThanOrEqualTo(_rideServiceConfig.MinTimeForValidationPassengerBeforeDeparture)
						.WithErrorCode(RideValidationCodes.TooSmallValidationTimeBeforeDeparture)
						.WithMessage("Время на валидацию пассажиров не может быть меньше минимального");
				});

			RuleFor(x => x.AfterRideValidationTimeoutAction)
				.NotNull()
				.WithErrorCode(RideValidationCodes.EmptyAfterRideValidationTimeoutAction)
				.WithMessage($"При способе валидации пассажира {nameof(RideValidationMethod.ValidationBeforeAccessPassenger)} должно быть заполнено поле {nameof(RideDto.AfterRideValidationTimeoutAction)}")
				.DependentRules(() =>
				{
					RuleFor(x => x.AfterRideValidationTimeoutAction)
						.NotEqual(AfterRideValidationTimeoutAction.Unknown)
						.WithErrorCode(RideValidationCodes.UnknownAfterRideValidationTimeoutAction)
						.WithMessage($"Действие по завершению таймаута на валидацию пассажиров {nameof(AfterRideValidationTimeoutAction.Unknown)} не поддерживается");
				});
		});

		When(x => x.Status != RideStatus.Draft && x.Status != RideStatus.Deleted, () =>
		{
			RuleFor(x => x.Waypoints)
				.NotNull()
				.WithErrorCode(RideValidationCodes.EmptyWaypoints)
				.WithMessage("Не заполнены пункты назначения и промежуточные точки поездки")
				.DependentRules(() =>
				{
					RuleFor(x => x.Waypoints)
						.Must(x => x.Count >= 2)
						.WithErrorCode(RideValidationCodes.EmptyWaypoints)
						.WithMessage("Не заполнены пункты назначения и промежуточные точки поездки");
				});

			RuleFor(x => x.Legs)
				.NotEmpty()
				.WithErrorCode(RideValidationCodes.EmptyLegs)
				.WithMessage("Не заполнены сегменты маршрута между пунктами назначения и/или промежуточными точками поездки");
		});

		When(x => x.Waypoints?.Count >= 2 && x.Legs?.Count >= 1, () =>
		{
			RuleFor(x => x.Legs)
				.Must((ride, legs) => EveryLegIsConnectedWithWaypoint(ride))
				.WithErrorCode(RideValidationCodes.MismatchWaypointsAndLegs)
				.WithMessage("Не для каждого указанного сегмента маршрута задана точка");

			RuleFor(x => x.Waypoints)
				.Must(x => x.Select(waypoint => waypoint.Point).ToHashSet().Count == x.Count)
				.WithErrorCode(RideValidationCodes.DoubledWaypointsCoordinates)
				.WithMessage("Среди точек маршрута есть дубли по координатам");

			RuleFor(x => x.Waypoints)
				.Must((ride, waypoints) => IsTimesChainValid(ride))
				.WithErrorCode(RideValidationCodes.IncorrectWaypointsTimesChain)
				.WithMessage("Невозможная комбинация времён прибытия-отбытия");

			RuleFor(x => x.Legs)
				.Must(x => x.Select(leg => (leg.WaypointFrom, leg.WaypointTo)).ToHashSet().Count == x.Count)
				.WithErrorCode(RideValidationCodes.DoubledLegs)
				.WithMessage("Среди сегментов маршрута есть дубли");

			RuleFor(x => x.Waypoints)
				.Must(x => x.Count(waypoint => waypoint.Departure is null) == 1)
				.WithErrorCode(RideValidationCodes.WrongCountOfWaypointsWithoutDeparture)
				.WithMessage("В поездке должна быть ровно 1 точка, у которой не будет времени отправления - конечная");

			RuleFor(x => x.Legs)
				.Must((ride, legs) => IsItPossibleToGoThroughEveryWaypoint(ride))
				.WithErrorCode(RideValidationCodes.ImpossibleToGoThroughEveryWaypoint)
				.WithMessage("Для каждой точки кроме конечной должен существовать сегмент, позволяющий проехать к следующей точке");

			RuleFor(x => x.Legs)
				.Must((ride, legs) => IsAllLegsConnectedInRightTimeOrder(ride))
				.WithErrorCode(RideValidationCodes.IncorrectTimesChainInLegs)
				.WithMessage("Среди сегментов поездки есть хотя бы 1, у которого отправление наступает раньше прибытия");

			RuleFor(x => x.Legs)
				.Must((ride, legs) => legs.All(leg => leg.PriceInRub > 0))
				.WithErrorCode(RideValidationCodes.IncorrectTimesChainInLegs)
				.WithMessage("Среди сегментов поездки есть хотя бы 1, у которого цена меньше 1 рубля");

			RuleFor(x => x.Waypoints)
				.Must(x => x.Count <= _rideServiceConfig.MaxWaypoints)
				.WithErrorCode(RideValidationCodes.WrongCountOfWaypoints)
				.WithMessage("Количество точек в поездке (включая начальную и конечную) не может быть больше максимального");
		});
	}

	private bool PaymentMethodsAreUnique(IReadOnlyList<PaymentMethod> methods)
	{
		var hashSet = methods.ToHashSet();
		return methods.Count == hashSet.Count;
	}

	private bool PaymentMethodsContainsUnknown(IReadOnlyList<PaymentMethod> methods)
	{
		for (int i = 0; i < methods.Count; i++)
		{
			if (methods[i] == PaymentMethod.Unknown)
			{
				return true;
			}
		}
		return false;
	}

	private bool EveryLegIsConnectedWithWaypoint(RideDto ride)
	{
		var points = ride.Waypoints.Select(x => x.Point).ToHashSet();
		for (int i = 0; i < ride.Legs.Count; i++)
		{
			var leg = ride.Legs[i];
			if (!points.Contains(leg.WaypointFrom) || !points.Contains(leg.WaypointTo))
			{
				return false;
			}
		}
		return true;
	}

	private bool IsTimesChainValid(RideDto ride)
	{
		var orderedWaypoints = ride.Waypoints
			.OrderBy(x => x.Arrival)
			.ThenBy(x => x.Departure ?? DateTimeOffset.MaxValue)
			.ToArray();

		for (int i = 1; i < orderedWaypoints.Length; i++)
		{
			var currentPoint = orderedWaypoints[i];
			var previousPoint = orderedWaypoints[i - 1];

			if (previousPoint.Departure.HasValue && previousPoint.Departure.Value < previousPoint.Arrival)
				return false;
			if (currentPoint.Departure.HasValue && currentPoint.Departure.Value < currentPoint.Arrival)
				return false;

			if (previousPoint.Departure.HasValue && currentPoint.Arrival <= previousPoint.Departure.Value)
				return false;
		}

		return true;
	}

	private bool IsItPossibleToGoThroughEveryWaypoint(RideDto ride)
	{
		var orderedWaypoints = ride.Waypoints
			.OrderBy(x => x.Arrival)
			.ThenBy(x => x.Departure ?? DateTimeOffset.MaxValue)
			.ToArray();

		var legs = ride.Legs
			.Select(x => (x.WaypointFrom, x.WaypointTo))
			.ToHashSet();

		for (int i = 1; i < orderedWaypoints.Length; i++)
		{
			var currentPoint = orderedWaypoints[i];
			var previousPoint = orderedWaypoints[i - 1];

			if (!legs.Contains((previousPoint.Point, currentPoint.Point)))
				return false;
		}

		return true;
	}

	private bool IsAllLegsConnectedInRightTimeOrder(RideDto ride)
	{
		var waypoints = ride.Waypoints
			.DistinctBy(x => x.Point)
			.ToDictionary(x => x.Point);

		for (int i = 0; i < ride.Legs.Count; i++)
		{
			var leg = ride.Legs[i];

			if (!waypoints.TryGetValue(leg.WaypointFrom, out var pointFrom))
				continue;
			if (!waypoints.TryGetValue(leg.WaypointTo, out var pointTo))
				continue;

			if (pointTo.Arrival < (pointFrom.Departure ?? DateTimeOffset.MaxValue))
				return false;
		}

		return true;
	}
}
