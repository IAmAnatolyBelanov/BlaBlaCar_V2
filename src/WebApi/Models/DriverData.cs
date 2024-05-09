namespace WebApi.Models;

public class DriverData
{
	private DateTimeOffset issuance;
	private DateTimeOffset lastCheckDate;
	private DateTimeOffset created;
	private DateTimeOffset validTill;
	private DateTimeOffset birthDate;

	public Guid Id { get; set; }
	public Guid? UserId { get; set; }

	public int LicenseSeries { get; set; }
	public int LicenseNumber { get; set; }

	/// <summary>
	/// Дата выдачи водительского удостоверения.
	/// </summary>
	public DateTimeOffset Issuance { get => issuance; set => issuance = value.ToUniversalTime(); }

	public DateTimeOffset ValidTill { get => validTill; set => validTill = value.ToUniversalTime(); }

	/// <summary>
	/// Категории через запятую.
	/// </summary>
	/// <remarks>
	/// При занесении в БД все русские символы должны заменяться на английские.
	/// </remarks>
	public string Categories { get; set; } = default!;

	/// <summary>
	/// Дата рождения владельца прав. На момент написания кода это единственное поле, которым можно сопоставить владельца прав и владельца паспорта.
	/// </summary>
	public DateTimeOffset BirthDate { get => birthDate; set => birthDate = value.ToUniversalTime(); }
	public DateTimeOffset Created { get => created; set => created = value.ToUniversalTime(); }


	public bool IsValid { get; set; } = false;
	public DateTimeOffset LastCheckDate { get => lastCheckDate; set => lastCheckDate = value.ToUniversalTime(); }
}
