using Dapper;

using Riok.Mapperly.Abstractions;

namespace WebApi.Models
{
	public class RideDto_Obsolete : RidePreparationDto_Obsolete
	{
		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		public int WaypointsCount => Legs?.Count + 1 ?? 0;

		public IReadOnlyList<PriceDto>? Prices { get; set; }

		public RideStatus Status { get; set; }
	}

	public class RidePreparationDto_Obsolete
	{
		public Guid Id { get; set; }
		public ulong DriverId { get; set; }

		public IReadOnlyList<LegDto_Obsolete>? Legs { get; set; }
		public int AvailablePlacesCount { get; set; }

		public void NormalizeLegs()
		{
			if (Legs is null || Legs.Count == 0)
				return;

			if (Legs.Count == 1)
			{
				var leg = Legs[0];
				leg.NextLeg = null;
				leg.NextLegId = null;
				leg.PreviousLeg = null;
				leg.PreviousLegId = null;
				return;
			}

			var sorted = Legs.AsList();

			sorted.Sort(LegDtoTimeFromComparer.Instance);

			Legs = sorted;

			Legs[0].NextLeg = Legs[1];
			Legs[0].NextLegId = Legs[1].Id;

			Legs[^1].PreviousLeg = Legs[^2];
			Legs[^1].PreviousLegId = Legs[^2].Id;

			for (int i = 1; i < Legs.Count - 1; i++)
			{
				var leg = Legs[i];

				leg.PreviousLeg = Legs[i - 1];
				leg.PreviousLegId = Legs[i - 1].Id;

				leg.NextLeg = Legs[i + 1];
				leg.NextLegId = Legs[i + 1].Id;
			}
		}
	}

	public interface IRideDtoMapper : IBaseMapper<Ride_Obsolete, RideDto_Obsolete>
	{
	}

	public interface IRidePreparationDtoMapper : IBaseMapper<Ride_Obsolete, RidePreparationDto_Obsolete>
	{
	}

	[Mapper]
	public partial class RideDtoMapper : BaseMapper<Ride_Obsolete, RideDto_Obsolete>, IRideDtoMapper
	{
		public RideDtoMapper() : base(() => new(), () => new())
		{
		}


		[MapperIgnoreTarget(nameof(RideDto_Obsolete.Prices))]
		[MapperIgnoreTarget(nameof(RideDto_Obsolete.Legs))]
		private partial void ToDtoAuto(Ride_Obsolete ride, RideDto_Obsolete dto);

		private partial void FromDtoAuto(RideDto_Obsolete dto, Ride_Obsolete ride);

		private partial void BetweenDtosAuto(RideDto_Obsolete from, RideDto_Obsolete to);
		private partial void BetweenEntitiesAuto(Ride_Obsolete from, Ride_Obsolete to);


		protected override void BetweenDtos(RideDto_Obsolete from, RideDto_Obsolete to)
			=> BetweenDtosAuto(from, to);
		protected override void BetweenEntities(Ride_Obsolete from, Ride_Obsolete to)
			=> BetweenEntitiesAuto(from, to);
		protected override void FromDtoAbstract(RideDto_Obsolete dto, Ride_Obsolete entity, IDictionary<object, object> mappedObjects)
			=> FromDtoAuto(dto, entity);
		protected override void ToDtoAbstract(Ride_Obsolete entity, RideDto_Obsolete dto, IDictionary<object, object> mappedObjects)
			=> ToDtoAuto(entity, dto);
	}

	[Mapper]
	public partial class RidePreparationMapper : BaseMapper<Ride_Obsolete, RidePreparationDto_Obsolete>, IRidePreparationDtoMapper
	{
		public RidePreparationMapper() : base(() => new(), () => new())
		{
		}

		[MapperIgnoreTarget(nameof(RidePreparationDto_Obsolete.Legs))]
		private partial void ToDtoAuto(Ride_Obsolete ride, RidePreparationDto_Obsolete dto);

		private partial void FromDtoAuto(RidePreparationDto_Obsolete dto, Ride_Obsolete ride);

		private partial void BetweenDtosAuto(RidePreparationDto_Obsolete from, RidePreparationDto_Obsolete to);
		private partial void BetweenEntitiesAuto(Ride_Obsolete from, Ride_Obsolete to);


		protected override void BetweenDtos(RidePreparationDto_Obsolete from, RidePreparationDto_Obsolete to)
			=> BetweenDtosAuto(from, to);
		protected override void BetweenEntities(Ride_Obsolete from, Ride_Obsolete to)
			=> BetweenEntitiesAuto(from, to);
		protected override void FromDtoAbstract(RidePreparationDto_Obsolete dto, Ride_Obsolete entity, IDictionary<object, object> mappedObjects)
			=> FromDtoAuto(dto, entity);
		protected override void ToDtoAbstract(Ride_Obsolete entity, RidePreparationDto_Obsolete dto, IDictionary<object, object> mappedObjects)
			=> ToDtoAuto(entity, dto);
	}
}
