namespace WebApi.Models;

public class RideDto
{
	/// <summary>
	/// Id. При создании поездки это поле игнорируется - сервис самостоятельно присваивает Id.
	/// </summary>
	public Guid Id { get; set; }
	public Guid AuthorId { get; set; }
	public Guid? DriverId { get; set; }
	public Guid? CarId { get; set; }

	/// <summary>
	/// Время создания поездки. Поле заполняется самим сервисом. Таким образом, даже если при создании или редактировании поездки передать поле в запросе, сервис никак не отреагирует на него.
	/// </summary>
	public DateTimeOffset Created { get; set; }
	public RideStatus Status { get; set; }

	/// <summary>
	/// Количество свободных мест в авто.
	/// Если выбрана машина, то количество свободных мест не может быть больше, чем кол-во сидений в авто минус 1 (водительское). Если авто не выбрано, можно выставить любое число, однако при последующем выборе авто количество свободных мест будет уменьшено до количества сидений в авто минус 1. Увеличиться количество свободных мест при выборе авто автоматически не может.
	/// </summary>
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
	public AfterRideValidationTimeoutAction? AfterRideValidationTimeoutAction { get; set; }

	public IReadOnlyList<WaypointDto> Waypoints { get; set; } = default!;

	public IReadOnlyList<LegDto> Legs { get; set; } = default!;
}
