namespace WebApi.Models;

public class GetRideResponse
{
	public RideDto Ride { get; set; } = default!;
	public CarDto? Car { get; set; }
}