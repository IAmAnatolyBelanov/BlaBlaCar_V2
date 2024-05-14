namespace WebApi.Models;

public class Reservation
{
	private DateTimeOffset created;


	public Guid Id { get; set; }
	public Guid RideId { get; set; }
	public Guid PassengerId { get; set; }
	public int PeopleCount { get; set; }
	public Guid WaypointFromId { get; set; }
	public Guid WaypointToId { get; set; }
	public bool IsDeleted { get; set; }
	public DateTimeOffset Created { get => created; set => created = value.ToUniversalTime(); }
}
