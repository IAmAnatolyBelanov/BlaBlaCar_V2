using NetTopologySuite.Geometries;

namespace WebApi.Models
{
	public class Leg
	{
		private DateTimeOffset startTime;
		private DateTimeOffset endTime;

		public Guid Id { get; set; }
		public Ride Ride { get; set; } = default!;
		public Guid RideId { get; set; }

		public Point From { get; set; } = default!;
		public Point To { get; set; } = default!;
		public DateTimeOffset StartTime { get => startTime; set => startTime = value.ToUniversalTime(); }
		public DateTimeOffset EndTime { get => endTime; set => endTime = value.ToUniversalTime(); }
		public int PriceInRub { get; set; }
		public string Description { get; set; } = default!;
	}
}
