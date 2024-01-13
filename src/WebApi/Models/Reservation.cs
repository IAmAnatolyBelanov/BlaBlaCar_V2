namespace WebApi.Models
{
	public class Reservation
	{
		private DateTimeOffset createDateTime;

		public Guid Id { get; set; }

		public Guid LegId { get; set; }
		public Leg Leg { get; set; } = default!;
		public ulong UserId { get; set; }
		public bool IsActive { get; set; }
		public DateTimeOffset CreateDateTime { get => createDateTime; set => createDateTime = value.ToUniversalTime(); }
		public int Count { get; set; }
	}
}
