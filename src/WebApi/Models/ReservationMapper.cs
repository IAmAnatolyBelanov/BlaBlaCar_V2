using Riok.Mapperly.Abstractions;

namespace WebApi.Models;

public interface IReservationMapper
{
	ReservationDto ToDto(Reservation reservation);
}

[Mapper]
public partial class ReservationMapper : IReservationMapper
{
	[MapperIgnoreTarget(nameof(ReservationDto.Leg))]
	public partial ReservationDto ToDto(Reservation reservation);
}
