namespace WebApi.Models
{
	public class CompositeLeg
	{
		public Guid MasterLegId { get; set; }
		public Leg MasterLeg { get; set; } = default!;

		public Guid SubLegId { get; set; }
		public Leg SubLeg { get; set; } = default!;
	}
}
