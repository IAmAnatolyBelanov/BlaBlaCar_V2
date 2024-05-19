namespace WebApi.Models;

public class RideCounts
{
	public long TotalCount { get; set; }
	public IReadOnlyDictionary<PaymentMethod, long> PaymentMethodCounts { get; set; } = default!;
	public IReadOnlyDictionary<RideValidationMethod, long> ValidationMethodCounts { get; set; } = default!;
	public IReadOnlyDictionary<float, long> DepartureDistanceInKilometersCounts { get; set; } = default!;
	public IReadOnlyDictionary<float, long> ArrivalDistanceInKilometersCounts { get; set; } = default!;
}
