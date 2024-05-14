namespace WebApi.Models
{
	public enum RideStatus
	{
		Unknown = 0,
		Draft,
		ActiveNotStarted,
		Canceled,
		StartedOrDone,
		Deleted,
	}
}
