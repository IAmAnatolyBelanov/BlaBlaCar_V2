namespace WebApi.Models;

public class Ride
{
	private DateTimeOffset created;


	public Guid Id { get; set; }
	public Guid AuthorId { get; set; }
	public Guid DriverId { get; set; }
	public DateTimeOffset Created { get => created; set => created = value.ToUniversalTime(); }
	public int AvailablePlacesCount { get; set; }
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

	public bool IsDeleted { get; set; }

	// На поле не висит Foreign Key, так как в этом случае получится зацикленная зависимость. А их разруливать больно.
	public Guid StartWaypointId { get; set; }
}