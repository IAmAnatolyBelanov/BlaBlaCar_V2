namespace WebApi.Models
{
	public class Ride
	{
		public Guid Id { get; set; }
		public ulong DriverId { get; set; }

		public int AvailablePlacesCount { get; set; }

		public RideStatus Status { get; set; }
	}

	public enum RideStatus
	{
		Unknown = 0,
		Preparation,
		Active,
		Canceled,
		StartedOrDone,
	}
}
