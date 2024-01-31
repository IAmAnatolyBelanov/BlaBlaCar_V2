using Dapper;

using Riok.Mapperly.Abstractions;

namespace WebApi.Models
{
	public class RideDto : RidePreparationDto
	{
		public int WaypointsCount => Legs?.Count + 1 ?? 0;

		public IReadOnlyList<PriceDto>? Prices { get; set; }

		public RideStatus Status { get; set; }

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

	public class RidePreparationDto
	{
		public Guid Id { get; set; }
		public ulong DriverId { get; set; }

		public IReadOnlyList<LegDto>? Legs { get; set; }
		public int AvailablePlacesCount { get; set; }
	}

	public interface IRideDtoMapper : IBaseMapper<Ride, RideDto>
	{
	}

	public interface IRidePreparationDtoMapper : IBaseMapper<Ride, RidePreparationDto>
	{
	}

	[Mapper]
	public partial class RideDtoMapper : BaseMapper<Ride, RideDto>, IRideDtoMapper
	{
		public RideDtoMapper() : base(() => new(), () => new())
		{
		}


		[MapperIgnoreTarget(nameof(RideDto.Prices))]
		[MapperIgnoreTarget(nameof(RideDto.Legs))]
		private partial void ToDtoAuto(Ride ride, RideDto dto);

		private partial void FromDtoAuto(RideDto dto, Ride ride);

		private partial void BetweenDtosAuto(RideDto from, RideDto to);
		private partial void BetweenEntitiesAuto(Ride from, Ride to);


		protected override void BetweenDtos(RideDto from, RideDto to)
			=> BetweenDtosAuto(from, to);
		protected override void BetweenEntities(Ride from, Ride to)
			=> BetweenEntitiesAuto(from, to);
		protected override void FromDtoAbstract(RideDto dto, Ride entity, IDictionary<object, object> mappedObjects)
			=> FromDtoAuto(dto, entity);
		protected override void ToDtoAbstract(Ride entity, RideDto dto, IDictionary<object, object> mappedObjects)
			=> ToDtoAuto(entity, dto);
	}

	[Mapper]
	public partial class RidePreparationMapper : BaseMapper<Ride, RidePreparationDto>, IRidePreparationDtoMapper
	{
		public RidePreparationMapper() : base(() => new(), () => new())
		{
		}

		[MapperIgnoreTarget(nameof(RidePreparationDto.Legs))]
		private partial void ToDtoAuto(Ride ride, RidePreparationDto dto);

		private partial void FromDtoAuto(RidePreparationDto dto, Ride ride);

		private partial void BetweenDtosAuto(RidePreparationDto from, RidePreparationDto to);
		private partial void BetweenEntitiesAuto(Ride from, Ride to);


		protected override void BetweenDtos(RidePreparationDto from, RidePreparationDto to)
			=> BetweenDtosAuto(from, to);
		protected override void BetweenEntities(Ride from, Ride to)
			=> BetweenEntitiesAuto(from, to);
		protected override void FromDtoAbstract(RidePreparationDto dto, Ride entity, IDictionary<object, object> mappedObjects)
			=> FromDtoAuto(dto, entity);
		protected override void ToDtoAbstract(Ride entity, RidePreparationDto dto, IDictionary<object, object> mappedObjects)
			=> ToDtoAuto(entity, dto);
	}
}
