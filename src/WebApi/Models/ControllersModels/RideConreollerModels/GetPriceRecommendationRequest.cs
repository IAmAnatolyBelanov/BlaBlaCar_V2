namespace WebApi.Models.ControllersModels.RideControllerModels;

public class GetPriceRecommendationRequest
{
	public FormattedPoint PointFrom { get; set; }
	public FormattedPoint PointTo { get; set; }
}