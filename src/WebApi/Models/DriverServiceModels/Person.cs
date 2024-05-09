namespace WebApi.Models.DriverServiceModels;

public class Person
{
	private DateTimeOffset birthDate;

	/// <summary>
	/// Серия паспорта.
	/// </summary>
	public int PassportSeries { get; set; }

	/// <summary>
	/// Номер паспорта.
	/// </summary>
	public int PassportNumber { get; set; }

	/// <summary>
	/// Имя.
	/// </summary>
	public string FirstName { get; set; } = default!;

	/// <summary>
	/// Фамилия.
	/// </summary>
	public string LastName { get; set; } = default!;

	/// <summary>
	/// Отчество.
	/// </summary>
	public string? SecondName { get; set; }

	/// <summary>
	/// Дата рождения.
	/// </summary>
	public DateTimeOffset BirthDate { get => birthDate; set => birthDate = value.ToUniversalTime(); }

	public void Normalize()
	{
		FirstName = FirstName?.Trim().ReplaceApostrophes().StartWithUpperCase()!;
		LastName = LastName?.Trim().ReplaceApostrophes().StartWithUpperCase()!;
		SecondName = SecondName?.Trim().ReplaceApostrophes().StartWithUpperCase();
	}
}
