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

	public interface IReservationDtoMapper : IBaseMapper<Reservation, ReservationDto>
	{
	}

	[Mapper]
	public partial class ReservationDtoMapper : BaseMapper<Reservation, ReservationDto>, IReservationDtoMapper
	{
		private readonly Lazy<ILegDtoMapper> _legMapper;

		public ReservationDtoMapper(Lazy<ILegDtoMapper> legMapper)
			: base(() => new(), () => new())
		{
			_legMapper = legMapper;
		}


		[MapperIgnoreTarget(nameof(ReservationDto.StartLeg))]
		[MapperIgnoreTarget(nameof(ReservationDto.EndLeg))]
		private partial void ToDtoAuto(Reservation entity, ReservationDto dto);

		[MapperIgnoreTarget(nameof(Reservation.StartLeg))]
		[MapperIgnoreTarget(nameof(Reservation.EndLeg))]
		[MapperIgnoreTarget(nameof(Reservation.AffectedLegIds))]
		private partial void FromDtoAuto(ReservationDto dto, Reservation entity);

		private partial void BetweenDtosAuto(ReservationDto from, ReservationDto to);
		private partial void BetweenEntitiesAuto(Reservation from, Reservation to);


		protected override void BetweenDtos(ReservationDto from, ReservationDto to)
			=> BetweenDtosAuto(from, to);
		protected override void BetweenEntities(Reservation from, Reservation to)
			=> BetweenEntities(from, to);
		protected override void FromDtoAbstract(ReservationDto dto, Reservation entity, IDictionary<object, object> mappedObjects)
		{
			FromDtoAuto(dto, entity);

			entity.StartLeg = dto.StartLeg is null
				? default!
				: _legMapper.Value.FromDto(dto.StartLeg, mappedObjects);
			entity.EndLeg = dto.EndLeg is null
				? default!
				: _legMapper.Value.FromDto(dto.EndLeg, mappedObjects);
		}

		protected override void ToDtoAbstract(Reservation entity, ReservationDto dto, IDictionary<object, object> mappedObjects)
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
