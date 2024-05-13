using NetTopologySuite.Geometries;

namespace WebApi.Models
{
	public class Leg_Obsolete
	{
		private DateTimeOffset startTime;
		private DateTimeOffset endTime;

		public Guid Id { get; set; }
		public Ride_Obsolete Ride { get; set; } = default!;
		public Guid RideId { get; set; }

		public Point From { get; set; } = default!;
		public Point To { get; set; } = default!;
		public DateTimeOffset StartTime { get => startTime; set => startTime = value.ToUniversalTime(); }
		public DateTimeOffset EndTime { get => endTime; set => endTime = value.ToUniversalTime(); }
		public string Description { get; set; } = default!;

		public Guid? NextLegId { get; set; }
		public Leg_Obsolete? NextLeg { get; set; }
		public Guid? PreviousLegId { get; set; }
		public Leg_Obsolete? PreviousLeg { get; set; }
	}
}
