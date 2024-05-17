namespace WebApi.Models;

/// <summary>
/// По сути, табличка многие-ко-многим между Leg и Reservation.
/// Задумка в том, что Reservation хранит инфу о конкретном Leg'е, на который юзер сделал бронь.
/// Однако же один Reservation может заблокировать возможность брони для нескольких других Leg'ов.
/// Например, если в маршруте ABCD бронь была AC, то это аффектит AB, BC (так как бронь включает их в себя), AD (так как этот сегмент включает в себя бронь), а также BD (так как есть пересечение).
/// Эта табличка как раз хранит все зааффекченные Leg'и.
/// </summary>
public class AffectedByReservationLeg
{
	public Guid ReservationId { get; set; }
	public Guid LegId { get; set; }
}