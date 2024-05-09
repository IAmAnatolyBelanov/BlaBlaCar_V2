namespace WebApi.Models.DriverServiceModels;

public class Driver
{
	private DateTimeOffset issuance;

	public int LicenseSeries { get; set; }
	public int LicenseNumber { get; set; }

	/// <summary>
	/// Дата выдачи водительского удостоверения.
	/// </summary>
	public DateTimeOffset Issuance { get => issuance; set => issuance = value.ToUniversalTime(); }
}
