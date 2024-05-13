namespace WebApi.Models
{
	public class Price
	{
		public Guid Id { get; set; }

		public int PriceInRub { get; set; }

		public Guid StartLegId { get; set; }
		public Leg_Obsolete StartLeg { get; set; } = default!;

		public Guid EndLegId { get; set; }
		public Leg_Obsolete EndLeg { get; set; } = default!;
	}
}
