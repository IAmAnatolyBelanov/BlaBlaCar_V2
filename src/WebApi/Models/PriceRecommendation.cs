namespace WebApi.Models;

public class PriceRecommendation
{
	public float LowerRecommendedPrice { get; set; }
	public float MiddleRecommendedPrice { get; set; }
	public float HigherRecommendedPrice { get; set; }
	public long RowsCount { get; set; }
}
