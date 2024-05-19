using FluentValidation;
using System.Data;

using WebApi.Models;
using WebApi.Services.Core;

namespace WebApi.Services.Validators;

public class RideValidationCodes : ValidationCodes
{
	public const string EmptyAuthorId = "RideValidator_EmptyAuthorId";
	public const string EmptyDriverId = "RideValidator_EmptyDriverId";

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

	public const string PointsAreTooClose = "RideValidator_PointsAreTooClose";

	public const string IncorrectTimeForCreatingRide = "RideValidator_IncorrectTimeForCreatingRide";
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
		RuleFor(x => x.DriverId)
			.NotEmpty()
			.WithErrorCode(RideValidationCodes.EmptyDriverId)
			.WithMessage("Не заполнен водитель");

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
				.Must((ride, legs) => AreAllLegsConnectedInRightTimeOrder(ride))
				.WithErrorCode(RideValidationCodes.IncorrectTimesChainInLegs)
				.WithMessage("Среди сегментов поездки есть хотя бы 1, у которого отправление наступает раньше прибытия");

			RuleFor(x => x.Legs)
				.Must((ride, legs) => legs.All(leg => leg.PriceInRub > 0))
				.WithErrorCode(RideValidationCodes.LegWithoutPrice)
				.WithMessage("Среди сегментов поездки есть хотя бы 1, у которого цена меньше 1 рубля");

			RuleFor(x => x.Waypoints)
				.Must(x => x.Count <= _rideServiceConfig.MaxWaypoints)
				.WithErrorCode(RideValidationCodes.WrongCountOfWaypoints)
				.WithMessage("Количество точек в поездке (включая начальную и конечную) не может быть больше максимального")
				.DependentRules(() =>
				{
					RuleFor(x => x.Waypoints)
						.Must(AreAllWaypointsHasMinimalDistanceBetweenEachOther)
						.WithErrorCode(RideValidationCodes.PointsAreTooClose)
						.WithMessage("Точки поездки находятся слишком близко между собой");
				});
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

	private bool AreAllLegsConnectedInRightTimeOrder(RideDto ride)
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

	private bool AreAllWaypointsHasMinimalDistanceBetweenEachOther(IReadOnlyList<WaypointDto> waypoints)
	{
		for (int i = 0; i < waypoints.Count; i++)
		{
			var pointFrom = waypoints[i];

			for (int j = i + 1; j < waypoints.Count; j++)
			{
				var pointTo = waypoints[j];

				var distance = Haversine.CalculateDistanceInKilometers(pointFrom.Point, pointTo.Point);

				if (distance < _rideServiceConfig.MinDistanceBetweenPointsInKilometers)
					return false;
			}
		}
		return true;
	}
}
