namespace WebApi.Models;

public class WaypointDto
{
	private DateTimeOffset arrival;
	private DateTimeOffset? departure;

	public Guid Id { get; set; }

	public FormattedPoint Point { get; set; }

	/// <summary>
	/// Полное имя (например, включая номер дома).
	/// </summary>
	public string FullName { get; set; } = default!;

	/// <summary>
	/// Название с точностью до населённого пункта.
	/// </summary>
	public string NameToCity { get; set; } = default!;

	/// <summary>
	/// Время прибытия в точку.
	/// </summary>
	public DateTimeOffset Arrival { get => arrival; set => arrival = value.ToUniversalTime(); }

	/// <summary>
	/// Время отправления из точки. Может быть <see langword="null"/>, если это конечная точка.
	/// </summary>
	public DateTimeOffset? Departure { get => departure; set => departure = value?.ToUniversalTime(); }
}
