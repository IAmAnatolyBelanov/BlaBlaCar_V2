namespace WebApi;

public class PersonData
{
	private DateTimeOffset birthDate;
	private DateTimeOffset lastCheckDate;
	private DateTimeOffset created;

	public Guid Id { get; set; }
	public Guid? UserId { get; set; }

	public int PassportSeries { get; set; }
	public int PassportNumber { get; set; }

	public string FirstName { get; set; } = default!;
	public string LastName { get; set; } = default!;

	/// <summary>
	/// Отчество.
	/// </summary>
	public string? SecondName { get; set; }
	public DateTimeOffset BirthDate { get => birthDate; set => birthDate = value.ToUniversalTime(); }
	public long Inn { get; set; }
	public bool IsPassportValid { get; set; } = false;
	public bool WasCheckedAtLeastOnce { get; set; } = false;
	public DateTimeOffset LastCheckPassportDate { get => lastCheckDate; set => lastCheckDate = value.ToUniversalTime(); }

	public DateTimeOffset Created { get => created; set => created = value.ToUniversalTime(); }
}
