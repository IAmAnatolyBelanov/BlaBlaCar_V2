namespace WebApi.Models;

public class LegDto
{
	public FormattedPoint WaypointFrom { get; set; }
	public FormattedPoint WaypointTo { get; set; }
	public int PriceInRub { get; set; }
	public int FreePlaces { get; set; }
}
