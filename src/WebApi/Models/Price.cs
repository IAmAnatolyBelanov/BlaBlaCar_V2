namespace WebApi.Models
{
	public class Price
	{
		public Guid Id { get; set; }

		public int PriceInRub {  get; set; }

		public Guid StartLegId { get; set; }
		public Leg StartLeg { get; set; } = default!;

		public Guid EndLegId { get; set;}
		public Leg EndLeg { get; set; } = default!;
	}
}
