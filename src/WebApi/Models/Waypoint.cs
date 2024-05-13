using NetTopologySuite.Geometries;

namespace WebApi.Models;

public class Waypoint
{
	private DateTimeOffset arrival;
	private DateTimeOffset? departure;

	public Guid Id { get; set; }
	public Guid RideId { get; set; }

	public Point Point { get; set; } = default!;

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