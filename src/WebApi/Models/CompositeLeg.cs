namespace WebApi.Models
{
	public class CompositeLeg
	{
		public Guid MasterLegId { get; set; }
		public Leg_Obsolete MasterLeg { get; set; } = default!;

		public Guid SubLegId { get; set; }
		public Leg_Obsolete SubLeg { get; set; } = default!;
	}
}
