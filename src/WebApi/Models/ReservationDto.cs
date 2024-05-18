namespace WebApi.Models;

public class ReservationDto
{
	private DateTimeOffset created;


	public Guid Id { get; set; }
	public Guid RideId { get; set; }
	public Guid PassengerId { get; set; }
	public int PeopleCount { get; set; }
	public LegDto Leg { get; set; } = default!;
	public DateTimeOffset Created { get => created; set => created = value.ToUniversalTime(); }
}