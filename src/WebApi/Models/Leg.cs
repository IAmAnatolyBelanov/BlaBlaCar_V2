namespace WebApi.Models;

public class Leg
{
	public Guid Id { get; set; }
	public Guid RideId { get; set; }
	public Guid WaypointFromId { get; set; }
	public Guid WaypointToId { get; set; }
	public int PriceInRub { get; set; }
	public bool IsManual { get; set; }
	public bool IsBetweenNeighborPoints { get; set; }
}