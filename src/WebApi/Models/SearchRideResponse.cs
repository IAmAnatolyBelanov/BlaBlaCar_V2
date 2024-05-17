namespace WebApi.Models;

public class SearchRideResponse
{
	public float DepartureDistanceKilometers { get; set; }
	public WaypointDto WaypointDeparture { get; set; } = default!;
	public float ArrivalDistanceKilometers { get; set; }
	public WaypointDto WaypointArrival { get; set; } = default!;
	public RideDto Ride { get; set; } = default!;
	public int PriceInRub { get; set; }
	public int FreePlaces { get; set; }
}
