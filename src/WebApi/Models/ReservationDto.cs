using Riok.Mapperly.Abstractions;

namespace WebApi.Models
{
	public class ReservationDto
	{
		public Guid Id { get; set; }
		public Guid StartLegId { get; set; }
		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		public LegDto_Obsolete StartLeg { get; set; } = default!;
		public Guid EndLegId { get; set; }
		[System.Text.Json.Serialization.JsonIgnore]
		[Newtonsoft.Json.JsonIgnore]
		public LegDto_Obsolete EndLeg { get; set; } = default!;
		public ulong UserId { get; set; }
		public bool IsActive { get; set; }
		public DateTimeOffset CreateDateTime { get; set; }
		public int Count { get; set; }
	}

	public interface IReservationDtoMapper : IBaseMapper<Reservation_Obsolete, ReservationDto>
	{
	}

	[Mapper]
	public partial class ReservationDtoMapper : BaseMapper<Reservation_Obsolete, ReservationDto>, IReservationDtoMapper
	{
		private readonly Lazy<ILegDtoMapper> _legMapper;

		public ReservationDtoMapper(Lazy<ILegDtoMapper> legMapper)
			: base(() => new(), () => new())
		{
			_legMapper = legMapper;
		}


		[MapperIgnoreTarget(nameof(ReservationDto.StartLeg))]
		[MapperIgnoreTarget(nameof(ReservationDto.EndLeg))]
		private partial void ToDtoAuto(Reservation_Obsolete entity, ReservationDto dto);

		[MapperIgnoreTarget(nameof(Reservation_Obsolete.StartLeg))]
		[MapperIgnoreTarget(nameof(Reservation_Obsolete.EndLeg))]
		[MapperIgnoreTarget(nameof(Reservation_Obsolete.AffectedLegIds))]
		private partial void FromDtoAuto(ReservationDto dto, Reservation_Obsolete entity);

		private partial void BetweenDtosAuto(ReservationDto from, ReservationDto to);
		private partial void BetweenEntitiesAuto(Reservation_Obsolete from, Reservation_Obsolete to);


		protected override void BetweenDtos(ReservationDto from, ReservationDto to)
			=> BetweenDtosAuto(from, to);
		protected override void BetweenEntities(Reservation_Obsolete from, Reservation_Obsolete to)
			=> BetweenEntities(from, to);
		protected override void FromDtoAbstract(ReservationDto dto, Reservation_Obsolete entity, IDictionary<object, object> mappedObjects)
		{
			FromDtoAuto(dto, entity);

			entity.StartLeg = dto.StartLeg is null
				? default!
				: _legMapper.Value.FromDto(dto.StartLeg, mappedObjects);
			entity.EndLeg = dto.EndLeg is null
				? default!
				: _legMapper.Value.FromDto(dto.EndLeg, mappedObjects);
		}

		protected override void ToDtoAbstract(Reservation_Obsolete entity, ReservationDto dto, IDictionary<object, object> mappedObjects)
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
