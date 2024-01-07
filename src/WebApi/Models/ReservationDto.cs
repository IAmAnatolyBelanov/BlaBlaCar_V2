using Riok.Mapperly.Abstractions;

namespace WebApi.Models
{
	public class ReservationDto
	{
		public Guid Id { get; set; }
		public Guid LegId { get; set; }
		public LegDto Leg { get; set; } = default!;
		public ulong UserId { get; set; }
		public bool IsActive { get; set; }
		public DateTimeOffset CreateDateTime { get; set; }
	}

	public interface IReservationDtoMapper : IBaseMapper<Reservation, ReservationDto>
	{
	}

	[Mapper]
	public partial class ReservationDtoMapper : BaseMapper<Reservation, ReservationDto>, IReservationDtoMapper
	{
		private readonly ILegDtoMapper _legMapper;

		public ReservationDtoMapper(ILegDtoMapper legMapper)
			: base(() => new(), () => new())
		{
			_legMapper = legMapper;
		}


		[MapperIgnoreTarget(nameof(ReservationDto.Leg))]
		private partial void ToDtoAuto(Reservation entity, ReservationDto dto);

		[MapperIgnoreTarget(nameof(Reservation.Leg))]
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

			entity.Leg = _legMapper.FromDto(dto.Leg, mappedObjects);
		}

		protected override void ToDtoAbstract(Reservation entity, ReservationDto dto, IDictionary<object, object> mappedObjects)
		{
			ToDtoAuto(entity, dto);

			dto.Leg = _legMapper.ToDto(entity.Leg, mappedObjects);
		}
	}
}
