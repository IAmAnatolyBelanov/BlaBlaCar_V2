namespace WebApi.Models
{
	public class Reservation
	{
		private DateTimeOffset createDateTime;

		public Guid Id { get; set; }

		public Guid StartLegId { get; set; }
		public Leg_Obsolete StartLeg { get; set; } = default!;
		public Guid EndLegId { get; set; }
		public Leg_Obsolete EndLeg { get; set; } = default!;
		/// <summary>
		/// Айдишники Leg'ов, которые участвуют в резервации. Включая start и end. Нужен только для ускорения посика в postgres.
		/// </summary>
		/// <remarks>Array вместо IReadOnlyList из-за EF - он не справился с интерфейсом.</remarks>
		public Guid[] AffectedLegIds { get; set; } = default!;
		public ulong UserId { get; set; }
		public bool IsActive { get; set; }
		public DateTimeOffset CreateDateTime { get => createDateTime; set => createDateTime = value.ToUniversalTime(); }
		public int Count { get; set; }
	}
}
