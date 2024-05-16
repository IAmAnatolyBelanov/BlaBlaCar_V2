using NetTopologySuite.Geometries;

namespace WebApi.Models;

public class SearchRideDbResponse
{
	// Ride
	private DateTimeOffset created;


	public Guid RideId { get; set; }
	public Guid AuthorId { get; set; }
	public Guid? DriverId { get; set; }
	public Guid? CarId { get; set; }
	public DateTimeOffset Created { get => created; set => created = value.ToUniversalTime(); }
	public RideStatus Status { get; set; }
	public int TotalAvailablePlacesCount { get; set; }
	public string? Comment { get; set; }
	public bool IsCashPaymentMethodAvailable { get; set; }
	public bool IsCashlessPaymentMethodAvailable { get; set; }
	public RideValidationMethod ValidationMethod { get; set; }

	/// <summary>
	/// Время, которое даётся водителю либо менеджеру на валидацию пассажиров. Например, если поездка назначена в 21:00, а <see cref="ValidationTimeBeforeDeparture"/> равен 1 часу, то в 20:00 все пассажиры должны быть отвалидированы.
	/// </summary>
	public TimeSpan? ValidationTimeBeforeDeparture { get; set; }

	/// <summary>
	/// Действия, что необходимо автоматически предпринять по окончанию <see cref="ValidationTimeBeforeDeparture"/>.
	/// </summary>
	public AfterRideValidationTimeoutAction? AfterRideValidationTimeoutAction { get; set; }




	// Waypoint from
	private DateTimeOffset fromArrival;
	private DateTimeOffset? fromDeparture;

	public Guid WaypointFromId { get; set; }

	public Point FromPoint { get; set; } = default!;

	public float FromDistanceKilometers { get; set; }

	public string FromFullName { get; set; } = default!;

	public string FromNameToCity { get; set; } = default!;

	public DateTimeOffset FromArrival { get => fromArrival; set => fromArrival = value.ToUniversalTime(); }

	public DateTimeOffset? FromDeparture { get => fromDeparture; set => fromDeparture = value?.ToUniversalTime(); }




	// Waypoint to
	private DateTimeOffset toArrival;
	private DateTimeOffset? toDeparture;

	public Guid WaypointToId { get; set; }

	public Point ToPoint { get; set; } = default!;

	public float ToDistanceKilometers { get; set; }

	public string ToFullName { get; set; } = default!;

	public string ToNameToCity { get; set; } = default!;

	public DateTimeOffset ToArrival { get => toArrival; set => toArrival = value.ToUniversalTime(); }

	public DateTimeOffset? ToDeparture { get => toDeparture; set => toDeparture = value?.ToUniversalTime(); }



	// Prices
	public int? LegPrice { get; set; }
	public int DefaultPrice { get; set; }
}