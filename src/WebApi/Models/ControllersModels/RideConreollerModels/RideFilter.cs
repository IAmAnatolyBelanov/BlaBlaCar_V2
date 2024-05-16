namespace WebApi.Models.ControllersModels.RideControllerModels;

public class RideFilter
{
	public int Offset { get; set; }
	public int Limit { get; set; }

	public RideSortType SortType { get; set; }
	public SortDirection SortDirection { get; set; }

	/// <summary>
	/// Точка окончания. Nullable сделан для возможности проверить, что параметр передан.
	/// </summary>
	public FormattedPoint? ArrivalPoint { get; set; }
	public int ArrivalPointSearchRadiusKilometers { get; set; }

	/// <summary>
	/// Точка старта. Nullable сделан для возможности проверить, что параметр передан.
	/// </summary>
	public FormattedPoint? DeparturePoint { get; set; }
	public int DeparturePointSearchRadiusKilometers { get; set; }

	public DateTimeOffset? MinArrivalTime { get; set; }

	/// <summary>
	/// Максимальное время прибытия. Nullable сделан для возможности проверить, что параметр передан.
	/// </summary>
	public DateTimeOffset? MaxArrivalTime { get; set; }

	/// <summary>
	/// Минимальное время отправления. Nullable сделан для возможности проверить, что параметр передан.
	/// </summary>
	public DateTimeOffset? MinDepartureTime { get; set; }
	public DateTimeOffset? MaxDepartureTime { get; set; }

	public int? MinPriceInRub { get; set; }
	public int? MaxPriceInRub { get; set; }

	public int FreeSeatsCount { get; set; }

	public IReadOnlyList<PaymentMethod> PaymentMethods { get; set; } = default!;

	public IReadOnlyList<RideValidationMethod> ValidationMethods { get; set; } = default!;

	public IReadOnlyList<RideStatus>? AvailableStatuses { get; set; }
}
