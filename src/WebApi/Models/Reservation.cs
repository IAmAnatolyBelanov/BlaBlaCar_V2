namespace WebApi.Models;

public class Reservation
{
	private DateTimeOffset created;


	public Guid Id { get; set; }
	public Guid RideId { get; set; }
	public Guid PassengerId { get; set; }
	public int PeopleCount { get; set; }
	public Guid LegId { get; set; }
	public bool IsDeleted { get; set; }
	public DateTimeOffset Created { get => created; set => created = value.ToUniversalTime(); }
}
