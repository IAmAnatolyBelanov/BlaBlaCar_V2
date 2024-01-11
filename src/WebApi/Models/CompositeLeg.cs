namespace WebApi.Models
{
	public class CompositeLeg
	{
		public Guid MasterLegId { get; set; }
		public Leg MasterLeg { get; set; } = default!;

		public Guid SubLegId { get; set; }
		public Leg SubLeg { get; set; } = default!;

		public override int GetHashCode() => HashCode.Combine(MasterLegId, SubLegId);
		public override bool Equals(object? obj)
			=> ReferenceEquals(this, obj) || obj is CompositeLeg other
				&& other.MasterLegId == MasterLegId
				&& other.SubLegId == SubLegId;
	}
}
