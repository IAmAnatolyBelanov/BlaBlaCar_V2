namespace WebApi.Models
{
	public enum RideStatus
	{
		Unknown = 0,
		Draft,
		Active,
		Canceled,
		StartedOrDone,
		Deleted,
	}
}
