using NetTopologySuite.Geometries;
using WebApi.Models.ControllersModels.RideControllerModels;

namespace WebApi.Models;

public class RideDbFilter
{
	private DateTimeOffset? minDepartureTime;
	private DateTimeOffset? maxDepartureTime;
	private DateTimeOffset? minArrivalTime;
	private DateTimeOffset? maxArrivalTime;

	public Guid[]? RideIds { get; set; }
	public bool HideDeleted { get; set; } = true;

	public int Offset { get; set; }
	public int Limit { get; set; }

	public RideSortType SortType { get; set; }
	public SortDirection SortDirection { get; set; }

	public Point? DeparturePoint { get; set; }
	public int DeparturePointSearchRadiusKilometers { get; set; }

	public Point? ArrivalPoint { get; set; }
	public int ArrivalPointSearchRadiusKilometers { get; set; }


	public DateTimeOffset? MinDepartureTime { get => minDepartureTime; set => minDepartureTime = value?.ToUniversalTime(); }
	public DateTimeOffset? MaxDepartureTime { get => maxDepartureTime; set => maxDepartureTime = value?.ToUniversalTime(); }
	public DateTimeOffset? MinArrivalTime { get => minArrivalTime; set => minArrivalTime = value?.ToUniversalTime(); }
	public DateTimeOffset? MaxArrivalTime { get => maxArrivalTime; set => maxArrivalTime = value?.ToUniversalTime(); }

	public int? MinPriceInRub { get; set; }
	public int? MaxPriceInRub { get; set; }

	public int? FreeSeatsCount { get; set; }

	public IReadOnlyList<PaymentMethod>? PaymentMethods { get; set; }
	public int[]? ValidationMethods { get; set; }

	public int[]? AvailableStatuses { get; set; }
}