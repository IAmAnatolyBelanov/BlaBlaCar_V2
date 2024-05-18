using NetTopologySuite.Geometries;

namespace WebApi.Models;

public class PriceRecommendationRequest
{
	public float LowerPercentile { get; set; }
	public float MiddlePercentile { get; set; }
	public float HigherPercentile { get; set; }

	public Point PointFrom { get; set; } = default!;
	public Point PointTo { get; set; } = default!;

	public float RadiusInKilometers { get; set; }

	public DateTimeOffset ArrivalDateFrom { get; set; }
	public DateTimeOffset ArrivalDateTo { get; set; }
}