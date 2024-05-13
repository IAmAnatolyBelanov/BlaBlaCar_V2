namespace WebApi;

public interface IDriverServiceConfig
{
	string ApiCloudBaseUrl { get; }
	string ApiCloudApiKey { get; }
	int RetryCount { get; }
	TimeSpan RetryDelay { get; }

	/// <summary>
	/// Минимальный возраст (в большинстве случаев равен возрасту, с которого можно получать паспорт).
	/// </summary>
	int MinAgesForPassport { get; }

	/// <summary>
	/// Период, в течение которого считается, что паспорт перепроверять не нужно.
	/// </summary>
	TimeSpan TrustPassportPeriod { get; }

	/// <summary>
	/// На момент написания кода сервис проверки паспортов работал только по архивным данным из-за проблем МВД. В таких условиях проверкой паспорта решено пренебречь, так как проверка на существование паспорта неявно осуществляется получением ИНН.
	/// </summary>
	bool NeedToCheckPassport { get; }

	int MinAgesForDrivingLicense { get; }

	TimeSpan CloudApiResponseSaverPollingDelay { get; }

	int CloudApiResponseSaverMaxBatchCount { get; }
}

public class DriverServiceClientConfig : IBaseConfig, IDriverServiceConfig
{
	public string Position => "DriverServiceClient";

	public string ApiCloudBaseUrl { get; set; } = "https://api-cloud.ru/api/";

	public string ApiCloudApiKey { get; set; } = "fake"; //= default!;

	public int RetryCount { get; set; } = 2;

	public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(50);

	public int MinAgesForPassport { get; set; } = 14;

	public TimeSpan TrustPassportPeriod { get; set; } = TimeSpan.FromDays(90);

	public bool NeedToCheckPassport { get; set; } = false;

	public int MinAgesForDrivingLicense { get; set; } = 16;

	public TimeSpan CloudApiResponseSaverPollingDelay { get; set; } = TimeSpan.FromSeconds(3);

	public int CloudApiResponseSaverMaxBatchCount { get; set; } = 5_000;

	public IEnumerable<string> GetValidationErrors()
	{
		if (ApiCloudBaseUrl.IsNullOrWhiteSpace())
			yield return $"{nameof(ApiCloudBaseUrl)} is null or white space";

		if (ApiCloudApiKey.IsNullOrWhiteSpace())
			yield return $"{nameof(ApiCloudApiKey)} is null or white space";

		if (RetryCount < 0)
			yield return $"{nameof(RetryCount)} must be >= 0";

		if (RetryDelay < TimeSpan.Zero)
			yield return $"{nameof(RetryDelay)} must be >= 0";

		if (MinAgesForPassport <= 0)
			yield return $"{nameof(MinAgesForPassport)} must be > 0";

		if (TrustPassportPeriod <= TimeSpan.Zero)
			yield return $"{nameof(TrustPassportPeriod)} must be > 0";

		if (MinAgesForDrivingLicense <= 0)
			yield return $"{nameof(MinAgesForDrivingLicense)} must be > 0";

		if (CloudApiResponseSaverPollingDelay <= TimeSpan.Zero)
			yield return $"{nameof(CloudApiResponseSaverPollingDelay)} must be > 0";

		if (CloudApiResponseSaverMaxBatchCount <= 0)
			yield return $"{nameof(CloudApiResponseSaverMaxBatchCount)} must be > 0";
	}
}
