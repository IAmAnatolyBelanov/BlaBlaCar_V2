using FluentValidation;

using WebApi.Models;
using WebApi.Services.Core;

namespace WebApi.Services.Validators
{
	public static partial class ValidationCodes
	{
		public const string Leg_InvalidStartAndEndTime = "Leg_InvalidStartAndEndTime";
		public const string Leg_InvalidPrice = "Leg_InvalidPrice";

		public const string Leg_EmptyCollection = "Leg_EmptyCollection";
		public const string Leg_NullElementInCollection = "Leg_NullElementInCollection";
		public const string Leg_DefaultId = "Leg_DefaultId";
		public const string Leg_InvalidCount = "Leg_InvalidCount";
		public const string Leg_MissmatchRideId = "Leg_MissmatchRideId";
		public const string Leg_DifferentRideId = "Leg_DifferentRideId";
		public const string Leg_InvalidElementInCollection = "Leg_InvalidElementInCollection";
		public const string Leg_InvalidPointsChain = "Leg_InvalidPointsChain";
		public const string Leg_InvalidTimeChain = "Leg_InvalidTimeChain";
	}

	public class CustomLegDtoValidator_v3 : AbstractBaseValidator<LegDto>
	{
		public CustomLegDtoValidator_v3()
		{
			AddRule(x =>
			{
				if (x.To.DateTime > x.From.DateTime)
					return null;

				return new CustomValidationFailure
				{
					ErrorCode = ValidationCodes.Leg_InvalidStartAndEndTime,
					PropertyName = $"({nameof(LegDto.To)}.{nameof(LegDto.To.DateTime)},{nameof(LegDto.From)}.{nameof(LegDto.From.DateTime)})",
					ErrorMessage = $"{nameof(LegDto.To)}.{nameof(LegDto.To.DateTime)} должен быть больше {nameof(LegDto.From)}.{nameof(LegDto.From.DateTime)}",
					AttemptedObjectIdentifier = x.Id.ToString(),
				};
			});

			AddRule(x =>
			{
				if (x.PriceInRub > 0)
					return null;

				return new CustomValidationFailure
				{
					ErrorCode = ValidationCodes.Leg_InvalidPrice,
					ErrorMessage = $"{nameof(LegDto.PriceInRub)} должен быть больше 0",
					PropertyName = nameof(LegDto.PriceInRub),
					AttemptedObjectIdentifier = x.Id.ToString(),
				};
			});
		}

		public override void ValidateAndThrow(LegDto instance) => throw new NotImplementedException();
		public override ValueTask ValidateAndThrowAsync(LegDto instance) => throw new NotImplementedException();
	}

	//public class CustomLegDtoValidator_V2 : CustomAbstractValidator_V2<LegDto>
	//{
	//	public CustomLegDtoValidator_V2()
	//	{
	//		RuleFor(x => x.To)
	//			.Must((leg, to) => to.DateTime > leg.From.DateTime)
	//			.WithErrorCode(ValidationCodes.Leg_InvalidStartAndEndTime)
	//			.WithMessage($"{nameof(LegDto.To)}.{nameof(LegDto.To.DateTime)} должен быть больше {nameof(LegDto.From)}.{nameof(LegDto.From.DateTime)}");

	//		RuleFor(x => x.PriceInRub)
	//			.Must(x => x > 0)
	//			.WithErrorCode(ValidationCodes.Leg_InvalidPrice)
	//			.WithMessage($"{nameof(LegDto.PriceInRub)} должен быть больше 0");
	//	}
	//}

	//public class CustomLegDtoValidator : CustomAbstractValidator<LegDto>
	//{
	//	public CustomLegDtoValidator()
	//	{
	//		StartRule(x => x.To.DateTime > x.From.DateTime)
	//			.SetAttemptedObjectIdentifier(x => x.Id)
	//			.SetErrorCode(ValidationCodes.Leg_InvalidStartAndEndTime)
	//			.SetErrorMessage($"{nameof(LegDto.To)}.{nameof(LegDto.To.DateTime)} должен быть больше {nameof(LegDto.From)}.{nameof(LegDto.From.DateTime)}")
	//			.SetPropertyName($"({nameof(LegDto.To.DateTime)}.{nameof(LegDto.To.DateTime)},{nameof(LegDto.From)}.{nameof(LegDto.From.DateTime)})")
	//			.Apply();

	//		StartRule(x => x.PriceInRub > 0)
	//			.SetAttemptedObjectIdentifier(x => x.Id)
	//			.SetErrorCode(ValidationCodes.Leg_InvalidStartAndEndTime)
	//			.SetErrorMessage($"{nameof(LegDto.PriceInRub)} должен быть больше 0")
	//			.GetAndSetPropertyName(x => x.PriceInRub)
	//			.Apply();
	//	}
	//}

	public class LegDtoValidator : AbstractValidator<LegDto>
	{
		public LegDtoValidator()
		{
			RuleFor(x => x.To)
				.Must((leg, to) => to.DateTime > leg.From.DateTime)
				.WithErrorCode(ValidationCodes.Leg_InvalidStartAndEndTime)
				.WithMessage($"{nameof(LegDto.To)}.{nameof(LegDto.To.DateTime)} должен быть больше {nameof(LegDto.From)}.{nameof(LegDto.From.DateTime)}");

			RuleFor(x => x.PriceInRub)
				.Must(x => x > 0)
				.WithErrorCode(ValidationCodes.Leg_InvalidPrice)
				.WithMessage($"{nameof(LegDto.PriceInRub)} должен быть больше 0");
		}
	}

	public class CustomLegCollectionValidator_V3 : AbstractBaseCollectionValidator<LegDto>
	{
		private readonly ICustomValidator<LegDto> _legValidator;
		private readonly IRideServiceConfig _rideServiceConfig;

		private readonly IReadOnlyDictionary<int, int> _validLegWaypointCounts;
		private readonly IReadOnlyDictionary<int, int> _validWaypointLegCounts;


		public CustomLegCollectionValidator_V3(
			ICustomValidator<LegDto> legValidator,
			IRideServiceConfig rideServiceConfig)
		{
			_legValidator = legValidator;
			_rideServiceConfig = rideServiceConfig;
			_validLegWaypointCounts = ValidLegWaypointCounts()
				.ToDictionary(x => x.legsCount, x => x.waypointsCount);
			_validWaypointLegCounts = ValidLegWaypointCounts()
				.ToDictionary(x => x.waypointsCount, x => x.legsCount);


			var notEmpty = AddRule(legs =>
			{
				if (legs.Any())
					return null;

				return new CustomValidationFailure
				{
					ErrorCode = ValidationCodes.Leg_EmptyCollection,
					ErrorMessage = "Коллекция не содержит элементов",
				};
			});

			var allNotNull = AddRuleForeach((leg, _, _, index) =>
			{
				if (leg is not null)
					return null;

				return new CustomValidationFailure
				{
					ErrorCode = ValidationCodes.Leg_NullElementInCollection,
					ErrorMessage = "Коллекция содержит null элемент",
					PropertyName = $"collection[{index}]",
				};
			}, notEmpty);

			var noDefaultIds = AddRuleForeach((leg, _, _, index) =>
			{
				if (leg.Id != default)
					return null;

				return new CustomValidationFailure
				{
					ErrorCode = ValidationCodes.Leg_DefaultId,
					ErrorMessage = "Коллекция содержит элемент с неинициализированным Id",
					PropertyName = $"collection[{index}].{nameof(LegDto.Id)}",
				};
			}, notEmpty, allNotNull);

			var validCount = AddRule((legs) =>
			{
				if (_validLegWaypointCounts.ContainsKey(legs.Count()))
					return null;

				return new()
				{
					ErrorCode = ValidationCodes.Leg_InvalidCount,
					ErrorMessage = "Невозможное количество leg'ов",
				};
			}, notEmpty);

			var noDefaultRideId = AddRuleForeach((leg, _, _, index) => AllRideIdsAreMatched(leg, index!.Value), notEmpty, allNotNull);

			var allRideIdAreTheSame = AddRule(AllRideIdAreTheSame, notEmpty, allNotNull, noDefaultRideId);

			var allLegsAreValid = AddRuleForeach((leg, errors, failedRules, index) =>
			{
				var result = _legValidator.Validate(leg);
				if (result.IsValid)
					return null;

				return new CustomValidationFailure
				{
					ErrorCode = ValidationCodes.Leg_InvalidElementInCollection,
					ErrorMessage = "Коллеция содержит невалидные элементы",
					Internal = result.Errors,
					AttemptedObjectIdentifier = leg.Id.ToString(),
					PropertyName = $"collection[{index}]",
				};
			}, notEmpty, allNotNull, noDefaultIds, noDefaultRideId, allRideIdAreTheSame);

			AddRule(ValidChainOfLegs, notEmpty, allNotNull, noDefaultIds, validCount, allRideIdAreTheSame, allLegsAreValid);
		}

		private IEnumerable<(int legsCount, int waypointsCount)> ValidLegWaypointCounts()
		{
			var lastValidLegCount = 1;
			yield return (lastValidLegCount, 2);

			for (int waypointCount = 3; waypointCount <= _rideServiceConfig.MaxWaypoints; waypointCount++)
			{
				lastValidLegCount += waypointCount - 1;
				yield return (lastValidLegCount, waypointCount);
			}
		}

		private CustomValidationFailure? AllRideIdsAreMatched(LegDto leg, int index)
		{
			if (leg.Ride is not null && leg.RideId == leg.Ride.Id)
				return null;

			return new()
			{
				ErrorCode = ValidationCodes.Leg_MissmatchRideId,
				ErrorMessage = $"Не совпадает {nameof(LegDto.RideId)} с {nameof(LegDto.Ride)}.{nameof(LegDto.Ride.Id)}",
				AttemptedObjectIdentifier = leg.Id.ToString(),
				PropertyName = $"(collection[{index}].{nameof(LegDto.RideId)},collection[{index}].{nameof(LegDto.Ride)}.{nameof(LegDto.Ride.Id)})",
			};
		}

		private CustomValidationFailure? AllRideIdAreTheSame(IEnumerable<LegDto> legs)
		{
			bool areTheSame = true;

			if (legs is IReadOnlyList<LegDto> list)
			{
				var mainId = list[0].RideId;

				for (int i = 1; i < list.Count; i++)
				{
					if (list[i].RideId != mainId)
					{
						areTheSame = false;
						break;
					}
				}
			}
			else
			{
				var mainId = legs.First().RideId;
				areTheSame = legs.All(x => x.RideId == mainId);
			}

			if (areTheSame)
				return null;

			return new()
			{
				ErrorCode = ValidationCodes.Leg_MissmatchRideId,
				ErrorMessage = $"Не во всех leg'ах {nameof(LegDto.RideId)} совпадает с {nameof(LegDto.Ride)}.{nameof(LegDto.Ride.Id)}",
				PropertyName = "collection",
			};
		}

		private CustomValidationFailure? ValidChainOfLegs(IEnumerable<LegDto> legs)
		{
			if (legs is not IReadOnlyList<LegDto> list)
				list = legs.ToArray();

			if (ValidChainOfLegs_internal(list))
				return null;

			return new()
			{
				ErrorCode = ValidationCodes.Leg_InvalidPointsChain,
				ErrorMessage = "Невалидная цепочка leg'ов. Нет возможности построить маршрут из любой точки до конечных",
				PropertyName = "collection",
			};
		}

		private bool ValidChainOfLegs_internal(IReadOnlyList<LegDto> legs)
		{
			var fullLeg = legs[0];
			for (var i = 0; i < legs.Count; i++)
			{
				var leg = legs[i];
				if (leg.From.DateTime <= fullLeg.From.DateTime && leg.To.DateTime >= fullLeg.To.DateTime)
					fullLeg = leg;
			}

			for (var i = 0; i < legs.Count; i++)
			{
				var leg = legs[i];
				if (ReferenceEquals(leg, fullLeg))
					continue;

				if (leg.From == fullLeg.From && leg.To == fullLeg.To)
					return false;

				if (leg.From.DateTime < fullLeg.From.DateTime || leg.To.DateTime > fullLeg.To.DateTime)
					return false;
			}

			if (legs.DistinctBy(x => (x.From.DateTime, x.To.DateTime)).Count() != legs.Count)
				return false;
			if (legs.DistinctBy(x => (x.From.Point, x.To.Point)).Count() != legs.Count)
				return false;

			var waypointsCount = legs.Select(x => x.From.Point)
				.Concat(legs.Select(l => l.To.Point))
				.Distinct()
				.Count();

			if (_validLegWaypointCounts[legs.Count] != waypointsCount)
				return false;

			for (int i = 0; i < legs.Count; i++)
				if (!CanComeToStartAndEnd(legs[i], fullLeg, legs))
					return false;

			return true;
		}

		private bool CanComeToStartAndEnd(LegDto leg, LegDto fullLeg, IReadOnlyList<LegDto> legs)
		{
			return CanComeToStart(leg, fullLeg, legs) && CanComeToEnd(leg, fullLeg, legs);
		}
		private bool CanComeToStart(LegDto leg, LegDto fullLeg, IReadOnlyList<LegDto> legs)
		{
			if (leg.From == fullLeg.From)
				return true;

			LegDto? localLeg = leg;

			while (localLeg.From != fullLeg.From)
			{
				localLeg = Previous(localLeg, legs);
				if (localLeg is null)
					return false;
			}

			return true;
		}
		private LegDto? Previous(LegDto leg, IReadOnlyList<LegDto> legs)
		{
			for (var i = 0; i < legs.Count; i++)
			{
				var potencialLeg = legs[i];
				if (potencialLeg.To == leg.From)
					return potencialLeg;
			}
			return null;
		}
		private bool CanComeToEnd(LegDto leg, LegDto fullLeg, IReadOnlyList<LegDto> legs)
		{
			if (leg.To == fullLeg.To)
				return true;

			LegDto? localLeg = leg;

			while (localLeg.To != fullLeg.To)
			{
				localLeg = Next(localLeg, legs);
				if (localLeg is null)
					return false;
			}

			return true;
		}
		private LegDto? Next(LegDto leg, IReadOnlyList<LegDto> legs)
		{
			for (var i = 0; i < legs.Count; i++)
			{
				var potencialLeg = legs[i];
				if (potencialLeg.From == leg.To)
					return potencialLeg;
			}
			return null;
		}


		public override void ValidateAndThrow(IEnumerable<LegDto> instance) => throw new NotImplementedException();
		public override ValueTask ValidateAndThrowAsync(IEnumerable<LegDto> instance) => throw new NotImplementedException();
	}

	//public class CustomLegCollectionValidator : CustomAbstractValidator<IReadOnlyList<LegDto>>
	//{
	//	private readonly ICustomValidator<LegDto> _sigleLegValidator;
	//	private readonly IRideServiceConfig _rideServiceConfig;

	//	private readonly IReadOnlyDictionary<int, int> _validLegWaypointCounts;
	//	private readonly IReadOnlyDictionary<int, int> _validWaypointLegCounts;

	//	public CustomLegCollectionValidator(
	//		ICustomValidator<LegDto> sigleLegValidator,
	//		IRideServiceConfig rideServiceConfig)
	//	{
	//		_sigleLegValidator = sigleLegValidator;
	//		_rideServiceConfig = rideServiceConfig;

	//		_sigleLegValidator = sigleLegValidator;
	//		_rideServiceConfig = rideServiceConfig;
	//		_validLegWaypointCounts = ValidLegWaypointCounts()
	//			.ToDictionary(x => x.legsCount, x => x.waypointsCount);
	//		_validWaypointLegCounts = ValidLegWaypointCounts()
	//			.ToDictionary(x => x.waypointsCount, x => x.legsCount);

	//		var notEmpty = StartRule(x => x.Count > 0)
	//			.SetErrorCode(ValidationCodes.Leg_EmptyCollection)
	//			.Apply();

	//		//var allNotNull = StartRule(x =>
	//		//{
	//		//	for (int i = 0; i < x.Count; i++)

	//		//})
	//	}

	//	private IEnumerable<(int legsCount, int waypointsCount)> ValidLegWaypointCounts()
	//	{
	//		var lastValidLegCount = 1;
	//		yield return (lastValidLegCount, 2);

	//		for (int waypointCount = 3; waypointCount <= _rideServiceConfig.MaxWaypoints; waypointCount++)
	//		{
	//			lastValidLegCount += waypointCount - 1;
	//			yield return (lastValidLegCount, waypointCount);
	//		}
	//	}

	//	private bool RideIdEqualsRide(IReadOnlyList<LegDto> legs)
	//	{
	//		if (legs is null || legs.Count == 0)
	//			return true;

	//		for (var i = 0; i < legs.Count; i++)
	//		{
	//			var leg = legs[i];
	//			if (leg.Ride is null || leg.RideId != leg.Ride.Id)
	//				return false;
	//		}

	//		return true;
	//	}

	//	private bool SameRide(IReadOnlyList<LegDto> legs)
	//	{
	//		if (legs is null || legs.Count < 2)
	//			return true;

	//		var ride = legs[0].Ride;
	//		if (ride is null || ride.Id == default)
	//			return false;

	//		for (var i = 0; i < legs.Count; i++)
	//		{
	//			var leg = legs[i];
	//			if (leg.RideId != ride.Id || leg.Ride?.Id != ride.Id)
	//				return false;
	//		}

	//		return true;
	//	}

	//	private bool ValidChainOfLegs(IReadOnlyList<LegDto> legs)
	//	{
	//		if (legs is null || legs.Count < 2 || !_validLegWaypointCounts.ContainsKey(legs.Count))
	//			return true;

	//		for (var i = 0; i < legs.Count; i++)
	//			if (legs[i] is null)
	//				return false;

	//		var fullLeg = legs[0];
	//		for (var i = 0; i < legs.Count; i++)
	//		{
	//			var leg = legs[i];
	//			if (leg.From.DateTime <= fullLeg.From.DateTime && leg.To.DateTime >= fullLeg.To.DateTime)
	//				fullLeg = leg;
	//		}

	//		for (var i = 0; i < legs.Count; i++)
	//		{
	//			var leg = legs[i];
	//			if (ReferenceEquals(leg, fullLeg))
	//				continue;

	//			if (leg.From == fullLeg.From && leg.To == fullLeg.To)
	//				return false;

	//			if (leg.From.DateTime < fullLeg.From.DateTime || leg.To.DateTime > fullLeg.To.DateTime)
	//				return false;
	//		}

	//		if (legs.DistinctBy(x => (x.From.DateTime, x.To.DateTime)).Count() != legs.Count)
	//			return false;
	//		if (legs.DistinctBy(x => (x.From.Point, x.To.Point)).Count() != legs.Count)
	//			return false;

	//		var waypointsCount = legs.Select(x => x.From.Point)
	//			.Concat(legs.Select(l => l.To.Point))
	//			.Distinct()
	//			.Count();

	//		if (_validLegWaypointCounts[legs.Count] != waypointsCount)
	//			return false;

	//		for (int i = 0; i < legs.Count; i++)
	//			if (!CanComeToStartAndEnd(legs[i], fullLeg, legs))
	//				return false;

	//		return true;
	//	}

	//	private bool CanComeToStartAndEnd(LegDto leg, LegDto fullLeg, IReadOnlyList<LegDto> legs)
	//	{
	//		return CanComeToStart(leg, fullLeg, legs) && CanComeToEnd(leg, fullLeg, legs);
	//	}
	//	private bool CanComeToStart(LegDto leg, LegDto fullLeg, IReadOnlyList<LegDto> legs)
	//	{
	//		if (leg.From == fullLeg.From)
	//			return true;

	//		LegDto? localLeg = leg;

	//		while (localLeg.From != fullLeg.From)
	//		{
	//			localLeg = Previous(localLeg, legs);
	//			if (localLeg is null)
	//				return false;
	//		}

	//		return true;
	//	}
	//	private LegDto? Previous(LegDto leg, IReadOnlyList<LegDto> legs)
	//	{
	//		for (var i = 0; i < legs.Count; i++)
	//		{
	//			var potencialLeg = legs[i];
	//			if (potencialLeg.To == leg.From)
	//				return potencialLeg;
	//		}
	//		return null;
	//	}
	//	private bool CanComeToEnd(LegDto leg, LegDto fullLeg, IReadOnlyList<LegDto> legs)
	//	{
	//		if (leg.To == fullLeg.To)
	//			return true;

	//		LegDto? localLeg = leg;

	//		while (localLeg.To != fullLeg.To)
	//		{
	//			localLeg = Next(localLeg, legs);
	//			if (localLeg is null)
	//				return false;
	//		}

	//		return true;
	//	}
	//	private LegDto? Next(LegDto leg, IReadOnlyList<LegDto> legs)
	//	{
	//		for (var i = 0; i < legs.Count; i++)
	//		{
	//			var potencialLeg = legs[i];
	//			if (potencialLeg.From == leg.To)
	//				return potencialLeg;
	//		}
	//		return null;
	//	}

	//	private bool ValidTimeChain(IReadOnlyList<LegDto> legs)
	//	{
	//		if (legs is null || legs.Count < 2)
	//			return true;

	//		for (int i = 0; i < legs.Count - 1; i++)
	//		{
	//			for (int j = i + 1; j < legs.Count; j++)
	//			{
	//				var leg1 = legs[i];
	//				var leg2 = legs[j];

	//				if (leg1.To.Point == leg2.From.Point)
	//				{
	//					if (leg1.To.DateTime > leg2.From.DateTime)
	//						return false;
	//				}

	//				if (leg1.From.Point == leg2.To.Point)
	//				{
	//					if (leg1.From.DateTime < leg2.To.DateTime)
	//						return false;
	//				}
	//			}
	//		}

	//		return true;
	//	}

	//}

	public class LegCollectionValidator : AbstractValidator<IReadOnlyList<LegDto>>
	{
		private readonly IValidator<LegDto> _sigleLegValidator;
		private readonly IRideServiceConfig _rideServiceConfig;

		private readonly IReadOnlyDictionary<int, int> _validLegWaypointCounts;
		private readonly IReadOnlyDictionary<int, int> _validWaypointLegCounts;

		public LegCollectionValidator(
			IValidator<LegDto> sigleLegValidator,
			IRideServiceConfig rideServiceConfig)
		{
			_sigleLegValidator = sigleLegValidator;
			_rideServiceConfig = rideServiceConfig;
			_validLegWaypointCounts = ValidLegWaypointCounts()
				.ToDictionary(x => x.legsCount, x => x.waypointsCount);
			_validWaypointLegCounts = ValidLegWaypointCounts()
				.ToDictionary(x => x.waypointsCount, x => x.legsCount);

			RuleFor(x => x)
				.NotEmpty()
				.WithErrorCode(ValidationCodes.Leg_EmptyCollection)
				.DependentRules(() =>
				{
					RuleForEach(x => x)
						.NotNull()
						.WithMessage("Leg {CollectionIndex} is null")
						.WithErrorCode(ValidationCodes.Leg_NullElementInCollection)
						.Must(x => x is null || x.Id != default)
						.WithMessage("Leg {CollectionIndex} has default Id")
						.WithErrorCode(ValidationCodes.Leg_DefaultId)
						.DependentRules(() =>
						{
							RuleFor(x => x)
								.Must(x => _validLegWaypointCounts.ContainsKey(x.Count))
								.WithErrorCode(ValidationCodes.Leg_InvalidCount)
								.WithMessage("Невозможное количество leg'ов");

							RuleFor(x => x)
								.Must(RideIdEqualsRide)
								.WithErrorCode(ValidationCodes.Leg_MissmatchRideId)
								.WithMessage($"Не во всех leg'ах {nameof(LegDto.RideId)} совпадает с {nameof(LegDto.Ride)}.{nameof(LegDto.Ride.Id)}");

							RuleFor(x => x)
								.Must(SameRide)
								.WithErrorCode(ValidationCodes.Leg_DifferentRideId)
								.WithMessage($"Не все leg'и имеют одинаковый {nameof(LegDto.RideId)}");

							RuleForEach(x => x)
								.SetValidator(_sigleLegValidator)
								.WithErrorCode(ValidationCodes.Leg_InvalidElementInCollection)
								.WithMessage((legs, leg) => $"Leg {leg.Id} is invalid");

							RuleFor(x => x)
								.Must(ValidChainOfLegs)
								.WithErrorCode(ValidationCodes.Leg_InvalidPointsChain)
								.WithMessage("Невалидная цепочка leg'ов. Нет возможности построить маршрут из любой точки до конечных");
							//.DependentRules(() =>
							//{
							//	RuleFor(x => x)
							//		.Must(ValidTimeChain)
							//		.WithErrorCode(ValidationCodes.Leg_InvalidTimeChain)
							//		.WithMessage($"Невалидная цепочка leg'ов. Некоторые из leg'ов имеют {nameof(LegDto.From)}.{nameof(LegDto.From.DateTime)} меньший, чем {nameof(LegDto.To)}.{nameof(LegDto.To.DateTime)} предыдущего leg'a");
							//});

						});
				});
		}

		private IEnumerable<(int legsCount, int waypointsCount)> ValidLegWaypointCounts()
		{
			var lastValidLegCount = 1;
			yield return (lastValidLegCount, 2);

			for (int waypointCount = 3; waypointCount <= _rideServiceConfig.MaxWaypoints; waypointCount++)
			{
				lastValidLegCount += waypointCount - 1;
				yield return (lastValidLegCount, waypointCount);
			}
		}

		private bool RideIdEqualsRide(IReadOnlyList<LegDto> legs)
		{
			if (legs is null || legs.Count == 0)
				return true;

			for (var i = 0; i < legs.Count; i++)
			{
				var leg = legs[i];
				if (leg.Ride is null || leg.RideId != leg.Ride.Id)
					return false;
			}

			return true;
		}

		private bool SameRide(IReadOnlyList<LegDto> legs)
		{
			if (legs is null || legs.Count < 2)
				return true;

			var ride = legs[0].Ride;
			if (ride is null || ride.Id == default)
				return false;

			for (var i = 0; i < legs.Count; i++)
			{
				var leg = legs[i];
				if (leg.RideId != ride.Id || leg.Ride?.Id != ride.Id)
					return false;
			}

			return true;
		}

		private bool ValidChainOfLegs(IReadOnlyList<LegDto> legs)
		{
			if (legs is null || legs.Count < 2 || !_validLegWaypointCounts.ContainsKey(legs.Count))
				return true;

			for (var i = 0; i < legs.Count; i++)
				if (legs[i] is null)
					return false;

			var fullLeg = legs[0];
			for (var i = 0; i < legs.Count; i++)
			{
				var leg = legs[i];
				if (leg.From.DateTime <= fullLeg.From.DateTime && leg.To.DateTime >= fullLeg.To.DateTime)
					fullLeg = leg;
			}

			for (var i = 0; i < legs.Count; i++)
			{
				var leg = legs[i];
				if (ReferenceEquals(leg, fullLeg))
					continue;

				if (leg.From == fullLeg.From && leg.To == fullLeg.To)
					return false;

				if (leg.From.DateTime < fullLeg.From.DateTime || leg.To.DateTime > fullLeg.To.DateTime)
					return false;
			}

			if (legs.DistinctBy(x => (x.From.DateTime, x.To.DateTime)).Count() != legs.Count)
				return false;
			if (legs.DistinctBy(x => (x.From.Point, x.To.Point)).Count() != legs.Count)
				return false;

			var waypointsCount = legs.Select(x => x.From.Point)
				.Concat(legs.Select(l => l.To.Point))
				.Distinct()
				.Count();

			if (_validLegWaypointCounts[legs.Count] != waypointsCount)
				return false;

			for (int i = 0; i < legs.Count; i++)
				if (!CanComeToStartAndEnd(legs[i], fullLeg, legs))
					return false;

			return true;
		}

		private bool CanComeToStartAndEnd(LegDto leg, LegDto fullLeg, IReadOnlyList<LegDto> legs)
		{
			return CanComeToStart(leg, fullLeg, legs) && CanComeToEnd(leg, fullLeg, legs);
		}
		private bool CanComeToStart(LegDto leg, LegDto fullLeg, IReadOnlyList<LegDto> legs)
		{
			if (leg.From == fullLeg.From)
				return true;

			LegDto? localLeg = leg;

			while (localLeg.From != fullLeg.From)
			{
				localLeg = Previous(localLeg, legs);
				if (localLeg is null)
					return false;
			}

			return true;
		}
		private LegDto? Previous(LegDto leg, IReadOnlyList<LegDto> legs)
		{
			for (var i = 0; i < legs.Count; i++)
			{
				var potencialLeg = legs[i];
				if (potencialLeg.To == leg.From)
					return potencialLeg;
			}
			return null;
		}
		private bool CanComeToEnd(LegDto leg, LegDto fullLeg, IReadOnlyList<LegDto> legs)
		{
			if (leg.To == fullLeg.To)
				return true;

			LegDto? localLeg = leg;

			while (localLeg.To != fullLeg.To)
			{
				localLeg = Next(localLeg, legs);
				if (localLeg is null)
					return false;
			}

			return true;
		}
		private LegDto? Next(LegDto leg, IReadOnlyList<LegDto> legs)
		{
			for (var i = 0; i < legs.Count; i++)
			{
				var potencialLeg = legs[i];
				if (potencialLeg.From == leg.To)
					return potencialLeg;
			}
			return null;
		}

		private bool ValidTimeChain(IReadOnlyList<LegDto> legs)
		{
			if (legs is null || legs.Count < 2)
				return true;

			for (int i = 0; i < legs.Count - 1; i++)
			{
				for (int j = i + 1; j < legs.Count; j++)
				{
					var leg1 = legs[i];
					var leg2 = legs[j];

					if (leg1.To.Point == leg2.From.Point)
					{
						if (leg1.To.DateTime > leg2.From.DateTime)
							return false;
					}

					if (leg1.From.Point == leg2.To.Point)
					{
						if (leg1.From.DateTime < leg2.To.DateTime)
							return false;
					}
				}
			}

			return true;
		}
	}
}
