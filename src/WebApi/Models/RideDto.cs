namespace WebApi.Models;

public class RideDto
{
	public Guid Id { get; set; }
	public Guid AuthorId { get; set; }
	public Guid? DriverId { get; set; }
	public Guid? CarId { get; set; }
	public DateTimeOffset Created { get; set; }
	public RideStatus Status { get; set; }
	public int AvailablePlacesCount { get; set; }
	public string? Comment { get; set; }
	public IReadOnlyList<PaymentMethod> PaymentMethods { get; set; } = default!;
	public RideValidationMethod ValidationMethod { get; set; }

	/// <summary>
	/// Время, которое даётся водителю либо менеджеру на валидацию пассажиров. Например, если поездка назначена в 21:00, а <see cref="ValidationTimeBeforeDeparture"/> равен 1 часу, то в 20:00 все пассажиры должны быть отвалидированы.
	/// </summary>
	public TimeSpan? ValidationTimeBeforeDeparture { get; set; }

	/// <summary>
	/// Действия, что необходимо автоматически предпринять по окончанию <see cref="ValidationTimeBeforeDeparture"/>.
	/// </summary>
	public AfterRideValidationAction AfterRideValidationAction { get; set; }

	public IReadOnlyList<WaypointDto> Waypoints { get; set; } = default!;

	public IReadOnlyList<LegDto> Legs { get; set; } = default!;
}
