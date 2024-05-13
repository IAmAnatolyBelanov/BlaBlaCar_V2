namespace WebApi.Models
{
	public class Ride_Obsolete
	{
		public Guid Id { get; set; }
		public ulong DriverId { get; set; }

		public int AvailablePlacesCount { get; set; }

		public RideStatus Status { get; set; }
	}
}
