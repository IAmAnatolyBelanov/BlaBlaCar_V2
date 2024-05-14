namespace WebApi.Models;

public class ReservationDbFilter
{
	public int Offset { get; set; }
	public int Limit { get; set; }

	public Guid? ReservationId { get; set; }
	public Guid? RideId { get; set; }
	public Guid? PassengerId { get; set; }
	public bool HideDeleted { get; set; } = true;
}