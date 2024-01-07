namespace WebApi.Models
{
	public class Ride
	{
		public Guid Id { get; set; }
		public ulong DriverId { get; set; }

		public int AvailablePlacesCount { get; set; }
	}
}
