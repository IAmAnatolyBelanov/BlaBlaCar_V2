namespace WebApi.Models.ControllersModels.RideControllerModels;

public class UpdateRideAvailablePlacesCountRequest
{
	public Guid RideId { get; set; }
	public int Count { get; set; }
}