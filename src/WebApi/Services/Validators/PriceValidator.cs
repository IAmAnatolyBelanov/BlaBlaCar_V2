using FluentValidation;

using WebApi.Models;
using WebApi.Services.Core;

namespace WebApi.Services.Validators
{
	public class PriceValidationCodes : ValidationCodes
	{
		public const string EmptyId = "Price_EmptyId";
		public const string WrongPrice = "Price_WrongPrice";
		public const string WrongStartLegId = "Price_WrongStartLegId";
		public const string WrongEndLegId = "Price_WrongEndLegId";
	}

	public class PriceValidator : AbstractValidator<PriceDto>
	{
		private readonly IRideServiceConfig _rideServiceConfig;

		public PriceValidator(IRideServiceConfig rideServiceConfig)
		{
			_rideServiceConfig = rideServiceConfig;

			RuleFor(x => x.Id)
				.NotEmpty()
				.WithErrorCode(PriceValidationCodes.EmptyId);

			RuleFor(x => x.PriceInRub)
				.GreaterThanOrEqualTo(_rideServiceConfig.MinPriceInRub)
				.WithErrorCode(PriceValidationCodes.WrongPrice)
				.WithMessage($"Цена должна быть в диапазоне от {_rideServiceConfig.MinPriceInRub} до {_rideServiceConfig.MaxPriceInRub}");

			RuleFor(x => x.PriceInRub)
				.LessThanOrEqualTo(_rideServiceConfig.MaxPriceInRub)
				.WithErrorCode(PriceValidationCodes.WrongPrice)
				.WithMessage($"Цена должна быть в диапазоне от {_rideServiceConfig.MinPriceInRub} до {_rideServiceConfig.MaxPriceInRub}");

			RuleFor(x => x.StartLegId)
				.NotEmpty()
				.WithErrorCode(PriceValidationCodes.WrongStartLegId);
			RuleFor(x => x.EndLegId)
				.NotEmpty()
				.WithErrorCode(PriceValidationCodes.WrongEndLegId);

			RuleFor(x => x.StartLegId)
				.Must((price, _) => price.StartLegId == price.StartLeg?.Id)
				.WithErrorCode(PriceValidationCodes.WrongStartLegId)
				.WithMessage($"{nameof(PriceDto.StartLegId)} не совпадает с {nameof(PriceDto.StartLeg)}.{nameof(PriceDto.StartLeg.Id)}");
			RuleFor(x => x.EndLegId)
				.Must((price, _) => price.EndLegId == price.EndLeg?.Id)
				.WithErrorCode(PriceValidationCodes.WrongEndLegId)
				.WithMessage($"{nameof(PriceDto.EndLegId)} не совпадает с {nameof(PriceDto.EndLeg)}.{nameof(PriceDto.EndLeg.Id)}");
		}
	}
}
