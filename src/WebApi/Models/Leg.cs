using NetTopologySuite.Geometries;

namespace WebApi.Models
{
	public class Leg
	{
		public Guid Id { get; set; }
		public Ride Ride { get; set; } = default!;
		public Guid RideId { get; set; }

		public Point From { get; set; } = default!;
		public Point To { get; set; } = default!;
		public DateTimeOffset StartTime { get; set; }
		public DateTimeOffset EndTime { get; set; }
		public int PriceInRub { get; set; }
		public string Description { get; set; } = default!;

		public ulong[] Passangers { get; set; } = default!;
	}
}
