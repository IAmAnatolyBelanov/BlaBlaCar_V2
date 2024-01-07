namespace WebApi.Models
{
	public class Reservation
	{
		public Guid Id { get; set; }

		public Guid LegId { get; set; }
		public Leg Leg { get; set; } = default!;
		public ulong UserId { get; set; }
		public bool IsActive { get; set; }
		public DateTimeOffset CreateDateTime { get; set; }
		public int Count { get; set; }
	}
}
