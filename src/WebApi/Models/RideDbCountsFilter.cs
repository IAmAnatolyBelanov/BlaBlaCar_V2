namespace WebApi.Models;

public class RideDbCountsFilter : RideDbFilter
{
	public float CloseDistanceInKilometers { get; set; }
	public float MiddleDistanceInKilometers { get; set; }
	public float FarAwayDistanceInKilometers { get; set; }
}