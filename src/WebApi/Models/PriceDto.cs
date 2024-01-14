
using Riok.Mapperly.Abstractions;

namespace WebApi.Models
{
	public class PriceDto
	{
		public Guid Id { get; set; }

		public int PriceInRub { get; set; }

		public Guid StartLegId { get; set; }
		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		public LegDto StartLeg { get; set; } = default!;

		public Guid EndLegId { get; set; }
		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		public LegDto EndLeg { get; set; } = default!;
	}

	public interface IPriceDtoMapper : IBaseMapper<Price, PriceDto>
	{
	}

	[Mapper]
	public partial class PriceDtoMapper : BaseMapper<Price, PriceDto>, IPriceDtoMapper
	{
		private readonly Lazy<ILegDtoMapper> _legMapper;

		public PriceDtoMapper(Lazy<ILegDtoMapper> legMapper)
			: base(() => new(), () => new())
		{
			_legMapper = legMapper;
		}

		[MapperIgnoreTarget(nameof(PriceDto.StartLeg))]
		[MapperIgnoreTarget(nameof(PriceDto.EndLeg))]
		private partial void ToDtoAuto(Price entity, PriceDto dto);

		[MapperIgnoreTarget(nameof(Price.StartLeg))]
		[MapperIgnoreTarget(nameof(Price.EndLeg))]
		private partial void FromDtoAuto(PriceDto entity, Price leg);

		private partial void BetweenDtosAuto(PriceDto from, PriceDto to);
		private partial void BetweenEntitiesAuto(Price from, Price to);


		protected override void BetweenDtos(PriceDto from, PriceDto to)
			=> BetweenDtosAuto(from, to);
		protected override void BetweenEntities(Price from, Price to)
			=> BetweenEntitiesAuto(from, to);
		protected override void FromDtoAbstract(PriceDto dto, Price entity, IDictionary<object, object> mappedObjects)
		{
			FromDtoAuto(dto, entity);

			entity.StartLeg = dto.StartLeg is null
				? default!
				: _legMapper.Value.FromDto(dto.StartLeg, mappedObjects);
			entity.EndLeg = dto.EndLeg is null
				? default!
				: _legMapper.Value.FromDto(dto.EndLeg, mappedObjects);
		}

		protected override void ToDtoAbstract(Price entity, PriceDto dto, IDictionary<object, object> mappedObjects)
		{
			ToDtoAuto(entity, dto);

			dto.StartLeg = entity.StartLeg is null
				? default!
				: _legMapper.Value.ToDto(entity.StartLeg, mappedObjects);
			dto.EndLeg = entity.EndLeg is null
				? default!
				: _legMapper.Value.ToDto(entity.EndLeg, mappedObjects);
		}
	}
}
