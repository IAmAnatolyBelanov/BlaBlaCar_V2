using NetTopologySuite.Geometries;

namespace WebApi.Models;

public class RideDbFilter
{
	private DateTimeOffset? minDepartureTime;
	private DateTimeOffset? maxDepartureTime;
	private DateTimeOffset? minArrivalTime;
	private DateTimeOffset? maxArrivalTime;

	public Guid[]? RideIds { get; set; }
	public bool HideDeleted { get; set; } = true;
	public bool HideStarted { get; set; } = true;

	public int Offset { get; set; }
	public int Limit { get; set; }

	public RideSortType SortType { get; set; } = RideSortType.ByEndTime;
	public SortDirection SortDirection { get; set; } = SortDirection.Desc;

	public Point? DeparturePoint { get; set; }
	public float DeparturePointSearchRadiusKilometers { get; set; }

	public Point? ArrivalPoint { get; set; }
	public float ArrivalPointSearchRadiusKilometers { get; set; }


	public DateTimeOffset? MinDepartureTime { get => minDepartureTime; set => minDepartureTime = value?.ToUniversalTime(); }
	public DateTimeOffset? MaxDepartureTime { get => maxDepartureTime; set => maxDepartureTime = value?.ToUniversalTime(); }
	public DateTimeOffset? MinArrivalTime { get => minArrivalTime; set => minArrivalTime = value?.ToUniversalTime(); }
	public DateTimeOffset? MaxArrivalTime { get => maxArrivalTime; set => maxArrivalTime = value?.ToUniversalTime(); }

	public int? MinPriceInRub { get; set; }
	public int? MaxPriceInRub { get; set; }

	public int? FreeSeatsCount { get; set; }

	public PaymentMethod[]? PaymentMethods { get; set; }
	public int[]? ValidationMethods { get; set; }
}