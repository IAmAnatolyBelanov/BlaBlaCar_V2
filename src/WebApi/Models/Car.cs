namespace WebApi.Models;

public class Car
{
	private DateTimeOffset created;


	public Guid Id { get; set; }
	public DateTimeOffset Created { get => created; set => created = value.ToUniversalTime(); }
	public string Vin { get; set; } = default!;
	public string RegistrationNumber { get; set; } = default!;
	public bool DoesVinAndRegistrationNumberMatches { get; set; } = false;
	public string Name { get; set; } = default!;

	/// <summary>
	/// Количество мест в авто, включая водительское.
	/// </summary>
	public int SeatsCount { get; set; }

	/// <summary>
	/// Например, владелец продал авто, новый владелец повторно зарегистрировал его, но с новым номером. Старый номер таким образом перестаёт быть активным.
	/// </summary>
	public bool IsDeleted { get; set; }
}