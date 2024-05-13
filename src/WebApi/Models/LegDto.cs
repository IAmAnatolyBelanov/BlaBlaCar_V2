namespace WebApi.Models;

public class LegDto
{
	public Guid Id { get; set; }
	public Guid WaypointFromId { get; set; }
	public Guid WaypointToId { get; set; }
	public int PriceInRub { get; set; }
	public int FreePlaces { get; set; }
}
