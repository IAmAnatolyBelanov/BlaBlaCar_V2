namespace WebApi.Models.ControllersModels.RideControllerModels;

public class MakeReservationRequest
{
	public Guid? PassengerId { get; set; }
	public Guid? RideId { get; set; }
	public int? PassengersCount { get; set; }
	public FormattedPoint? WaypointFrom { get; set; }
	public FormattedPoint? WaypointTo { get; set; }
}