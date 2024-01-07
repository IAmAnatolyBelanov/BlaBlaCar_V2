using Riok.Mapperly.Abstractions;

namespace WebApi.Models
{
	public class LegDto
	{
		public Guid Id { get; set; }
		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		public RideDto Ride { get; set; } = default!;
		public Guid RideId { get; set; }

		public FormattedPoint From { get; set; }
		public FormattedPoint To { get; set; }
		public DateTimeOffset StartTime { get; set; }
		public DateTimeOffset EndTime { get; set; }
		public int PriceInRub { get; set; }
		public string Description { get; set; } = default!;
	}

	public interface ILegDtoMapper : IBaseMapper<Leg, LegDto>
	{
	}

	[Mapper]
	public partial class LegDtoMapper : BaseMapper<Leg, LegDto>, ILegDtoMapper
	{
		private readonly Lazy<IRideDtoMapper> _rideDtoMpper;

		public LegDtoMapper(Lazy<IRideDtoMapper> rideDtoMpper)
			: base(() => new(), () => new())
		{
			_rideDtoMpper = rideDtoMpper;
		}

		[MapperIgnoreTarget(nameof(LegDto.Ride))]
		[MapperIgnoreTarget(nameof(LegDto.From))]
		[MapperIgnoreTarget(nameof(LegDto.To))]
		private partial void ToDtoAuto(Leg leg, LegDto dto);

		[MapperIgnoreTarget(nameof(Leg.Ride))]
		[MapperIgnoreTarget(nameof(Leg.From))]
		[MapperIgnoreTarget(nameof(Leg.To))]
		private partial void FromDtoAuto(LegDto legDto, Leg leg);

		private partial void BetweenDtosAuto(LegDto from, LegDto to);
		private partial void BetweenEntitiesAuto(Leg from, Leg to);


		protected override void BetweenDtos(LegDto from, LegDto to)
			=> BetweenDtosAuto(from, to);

		protected override void BetweenEntities(Leg from, Leg to)
			=> BetweenEntitiesAuto(from, to);
		protected override void FromDtoAbstract(LegDto dto, Leg entity, IDictionary<object, object> mappedObjects)
		{
			FromDtoAuto(dto, entity);

			entity.From = dto.From.ToPoint();
			entity.To = dto.To.ToPoint();

			if (dto.Ride is not null)
				entity.Ride = _rideDtoMpper.Value.FromDto(dto.Ride, mappedObjects);
		}

		protected override void ToDtoAbstract(Leg entity, LegDto dto, IDictionary<object, object> mappedObjects)
		{
			ToDtoAuto(entity, dto);

			dto.From = FormattedPoint.FromPoint(entity.From);
			dto.To = FormattedPoint.FromPoint(entity.To);

			if (entity.Ride is not null)
				dto.Ride = _rideDtoMpper.Value.ToDto(entity.Ride, mappedObjects);
		}
	}
}
