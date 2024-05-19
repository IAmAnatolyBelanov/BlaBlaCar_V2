using FluentValidation;
using WebApi.Models;
using WebApi.Models.ControllersModels.RideControllerModels;
using WebApi.Services.Core;

namespace WebApi.Services.Validators;

public class RideFilterValidationCodes : ValidationCodes
{
	public const string UnknownSortType = "RideFilterValidation_UnknownSortType";
	public const string UnknownSortDirection = "RideFilterValidation_UnknownSortDirection";

	public const string EmptyArrivalPoint = "RideFilterValidation_EmptyArrivalPoint";
	public const string EmptyDeparturePoint = "RideFilterValidation_EmptyDeparturePoint";

	public const string TooSmallArrivalSearchRadius = "RideFilterValidation_TooSmallArrivalSearchRadius";
	public const string TooSmallDepartureSearchRadius = "RideFilterValidation_TooSmallDepartureSearchRadius";
	public const string TooLargeArrivalSearchRadius = "RideFilterValidation_TooLargeArrivalSearchRadius";
	public const string TooLargeDepartureSearchRadius = "RideFilterValidation_TooLargeDepartureSearchRadius";

	public const string EmptyMaxArrivalTime = "RideFilterValidation_EmptyMaxArrivalTime";
	public const string EmptyMinDepartureTime = "RideFilterValidation_EmptyMinDepartureTime";

	public const string ImpossibleTimeCombination = "RideFilterValidation_ImpossibleTimeCombination";

	public const string TooLargeSearchPeriod = "RideFilterValidation_TooLargeSearchPeriod";

	public const string IncorrectPriceLimits = "RideFilterValidation_IncorrectPriceLimits";

	public const string IncorrectFreeSeatsCount = "RideFilterValidation_IncorrectFreeSeatsCount";

	public const string EmptyPaymentMethods = "RideFilterValidation_EmptyPaymentMethods";
	public const string UnknownPaymentMethod = "RideFilterValidation_UnknownPaymentMethod";

	public const string EmptyValidationMethods = "RideFilterValidation_ValidationMethods";
	public const string UnknownValidationMethod = "RideFilterValidation_UnknownValidationMethod";

	public const string InvalidOffset = "RideFilterValidation_InvalidOffset";
	public const string InvalidLimit = "RideFilterValidation_InvalidLimit";
}

public class RideFilterValidator : AbstractValidator<RideFilter>
{
	private readonly IRideServiceConfig _config;

	public RideFilterValidator(IRideServiceConfig config)
	{
		_config = config;

		RuleFor(x => x.Offset)
			.GreaterThanOrEqualTo(0)
			.WithErrorCode(RideFilterValidationCodes.InvalidOffset)
			.WithMessage("Offset не может быть меньше 0");

		RuleFor(x => x.Limit)
			.GreaterThanOrEqualTo(1)
			.WithErrorCode(RideFilterValidationCodes.InvalidLimit)
			.WithMessage("Limit не может быть меньше 1");
		RuleFor(x => x.Limit)
			.LessThanOrEqualTo(_config.MaxSqlLimit)
			.WithErrorCode(RideFilterValidationCodes.InvalidLimit)
			.WithMessage("Limit не может быть больше максимального");

		RuleFor(x => x.SortType)
			.IsInEnum()
			.WithErrorCode(RideFilterValidationCodes.UnknownSortType)
			.WithMessage(x => $"Сортировка {x.SortType} не поддерживается");
		RuleFor(x => x.SortType)
			.NotEqual(RideSortType.Unknown)
			.WithErrorCode(RideFilterValidationCodes.UnknownSortType)
			.WithMessage($"Сортировка {nameof(RideSortType.Unknown)} не поддерживается");

		RuleFor(x => x.SortDirection)
			.IsInEnum()
			.WithErrorCode(RideFilterValidationCodes.UnknownSortDirection)
			.WithMessage(x => $"Направление сортировки {x.SortDirection} не поддерживается");
		RuleFor(x => x.SortDirection)
			.NotEqual(SortDirection.Unknown)
			.WithErrorCode(RideFilterValidationCodes.UnknownSortDirection)
			.WithMessage($"Направление сортировки {nameof(SortDirection.Unknown)} не поддерживается");

		RuleFor(x => x.ArrivalPoint)
			.NotNull()
			.WithErrorCode(RideFilterValidationCodes.EmptyArrivalPoint)
			.WithMessage("Не задана точка прибытия");
		RuleFor(x => x.DeparturePoint)
			.NotNull()
			.WithErrorCode(RideFilterValidationCodes.EmptyDeparturePoint)
			.WithMessage("Не задана точка отправления");

		RuleFor(x => x.ArrivalPointSearchRadiusKilometers)
			.GreaterThanOrEqualTo(_config.MinRadiusForSearchKilometers)
			.WithErrorCode(RideFilterValidationCodes.TooSmallArrivalSearchRadius)
			.WithMessage("Слишком маленькая область поиска конечной точки");
		RuleFor(x => x.DeparturePointSearchRadiusKilometers)
			.GreaterThanOrEqualTo(_config.MinRadiusForSearchKilometers)
			.WithErrorCode(RideFilterValidationCodes.TooSmallDepartureSearchRadius)
			.WithMessage("Слишком маленькая область поиска начальной точки");

		RuleFor(x => x.ArrivalPointSearchRadiusKilometers)
			.LessThanOrEqualTo(_config.MaxRadiusForSearchKilometers)
			.WithErrorCode(RideFilterValidationCodes.TooLargeArrivalSearchRadius)
			.WithMessage("Слишком большая область поиска конечной точки");
		RuleFor(x => x.DeparturePointSearchRadiusKilometers)
			.LessThanOrEqualTo(_config.MaxRadiusForSearchKilometers)
			.WithErrorCode(RideFilterValidationCodes.TooLargeDepartureSearchRadius)
			.WithMessage("Слишком большая область поиска начальной точки");

		RuleFor(x => x.MaxArrivalTime)
			.NotEmpty()
			.WithErrorCode(RideFilterValidationCodes.EmptyMaxArrivalTime)
			.WithMessage("Не задано максимальное время прибытия");
		RuleFor(x => x.MinDepartureTime)
			.NotEmpty()
			.WithErrorCode(RideFilterValidationCodes.EmptyMinDepartureTime)
			.WithMessage("Не задано минимальное время отправления");

		RuleFor(x => x.MaxArrivalTime)
			.Must((filter, _) => filter.MaxArrivalTime >= filter.MinArrivalTime)
			.WithErrorCode(RideFilterValidationCodes.ImpossibleTimeCombination)
			.WithMessage($"{nameof(RideFilter.MaxArrivalTime)} не может быть меньше {nameof(RideFilter.MinArrivalTime)}");
		RuleFor(x => x.MaxDepartureTime)
			.Must((filter, _) => filter.MaxDepartureTime >= filter.MinDepartureTime)
			.WithErrorCode(RideFilterValidationCodes.ImpossibleTimeCombination)
			.WithMessage($"{nameof(RideFilter.MaxDepartureTime)} не может быть меньше {nameof(RideFilter.MinDepartureTime)}");

		RuleFor(x => x.MaxArrivalTime)
			.Must((filter, _) => (filter.MaxArrivalTime - filter.MinDepartureTime) <= _config.MaxSearchPeriod)
			.WithErrorCode(RideFilterValidationCodes.TooLargeSearchPeriod)
			.WithMessage("Период поиска не может быть больше максимального системного");

		When(x => x.MinPriceInRub.HasValue, () =>
		{
			RuleFor(x => x.MinPriceInRub)
				.GreaterThanOrEqualTo(_config.MinPriceInRub)
				.WithErrorCode(RideFilterValidationCodes.IncorrectPriceLimits)
				.WithMessage("Цена не может быть меньше минимальной");
			RuleFor(x => x.MinPriceInRub)
				.LessThanOrEqualTo(_config.MaxPriceInRub)
				.WithErrorCode(RideFilterValidationCodes.IncorrectPriceLimits)
				.WithMessage("Цена не может быть больше максимальной");
		});

		When(x => x.MaxPriceInRub.HasValue, () =>
		{
			RuleFor(x => x.MaxPriceInRub)
				.GreaterThanOrEqualTo(_config.MinPriceInRub)
				.WithErrorCode(RideFilterValidationCodes.IncorrectPriceLimits)
				.WithMessage("Цена не может быть меньше системной минимальной");
			RuleFor(x => x.MaxPriceInRub)
				.LessThanOrEqualTo(_config.MaxPriceInRub)
				.WithErrorCode(RideFilterValidationCodes.IncorrectPriceLimits)
				.WithMessage("Цена не может быть больше системной максимальной");
		});

		When(x => x.MinPriceInRub.HasValue && x.MaxPriceInRub.HasValue, () =>
		{
			RuleFor(x => x.MaxPriceInRub)
				.Must((filter, _) => filter.MaxPriceInRub >= filter.MinPriceInRub)
				.WithErrorCode(RideFilterValidationCodes.IncorrectPriceLimits)
				.WithMessage("Максимальная цена не может быть меньше минимальной");
		});

		RuleFor(x => x.FreeSeatsCount)
			.GreaterThanOrEqualTo(1)
			.WithErrorCode(RideFilterValidationCodes.IncorrectFreeSeatsCount)
			.WithMessage("Количество необходимых мест не может быть меньше 1");

		RuleFor(x => x.PaymentMethods)
			.NotEmpty()
			.WithErrorCode(RideFilterValidationCodes.EmptyPaymentMethods)
			.WithMessage("Не заполнены допустимые способы оплаты")
			.DependentRules(() =>
			{
				RuleFor(x => x.PaymentMethods)
					.Must(x => x.All(p => p != PaymentMethod.Unknown && Enum.IsDefined(p)))
					.WithErrorCode(RideFilterValidationCodes.UnknownPaymentMethod)
					.WithErrorCode("Список способов оплаты содержит неизвестные значения");
			});

		RuleFor(x => x.ValidationMethods)
			.NotEmpty()
			.WithErrorCode(RideFilterValidationCodes.EmptyValidationMethods)
			.WithMessage("Не заполнены допустимые способы валидации пассажиров")
			.DependentRules(() =>
			{
				RuleFor(x => x.ValidationMethods)
					.Must(x => x.All(p => p != RideValidationMethod.Unknown && Enum.IsDefined(p)))
					.WithErrorCode(RideFilterValidationCodes.UnknownValidationMethod)
					.WithErrorCode("Список способов валидации пассажиров содержит неизвестные значения");
			});
	}
}
