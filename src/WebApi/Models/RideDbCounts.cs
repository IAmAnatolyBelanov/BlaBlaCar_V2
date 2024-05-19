namespace WebApi.Models;

public class RideDbCounts
{
	public long TotalCount { get; set; }
	public long CashAvailableCount { get; set; }
	public long CashlessAvailableCount { get; set; }
	public long WithValidationCount { get; set; }
	public long WithoutValidationCount { get; set; }
	public long CloseDepartureDistanceCount { get; set; }
	public long MiddleDepartureDistanceCount { get; set; }
	public long FarAwayDepartureDistanceCount { get; set; }
	public long CloseArrivalDistanceCount { get; set; }
	public long MiddleArrivalDistanceCount { get; set; }
	public long FarAwayArrivalDistanceCount { get; set; }
}
