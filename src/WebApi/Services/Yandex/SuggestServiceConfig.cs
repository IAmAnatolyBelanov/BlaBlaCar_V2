namespace WebApi.Services.Yandex
{
	public interface ISuggestServiceConfig
	{
		string ApiKey { get; }
		TimeSpan DistributedCacheExpiry { get; }
		int MinInput { get; }
		int RetryCount { get; }
		TimeSpan FailExpiry { get; }
		bool IsDebug { get; }
		int InMemoryCacheMaxObjects { get; }
		TimeSpan InMemoryCacheObjectLifetime { get; }
	}

	public class SuggestServiceConfig : IBaseConfig, ISuggestServiceConfig
	{
		public string Position => "Yandex:Suggest";

		public string ApiKey { get; set; } = default!;
		public TimeSpan DistributedCacheExpiry { get; set; } = TimeSpan.FromDays(30);
		public TimeSpan FailExpiry { get; set; } = TimeSpan.FromMinutes(5);
		public int MinInput { get; set; } = 2; // Например, посёлок Яя.
		public int RetryCount { get; set; } = 2;
		public bool IsDebug { get; set; } = false;
		public int InMemoryCacheMaxObjects { get; set; } = 100_000;
		public TimeSpan InMemoryCacheObjectLifetime { get; set; } = TimeSpan.FromHours(8);

		public IEnumerable<string> GetValidationErrors()
		{
			if (string.IsNullOrWhiteSpace(ApiKey))
				yield return $"{nameof(ApiKey)} is null or empty";

			if (DistributedCacheExpiry <= TimeSpan.Zero)
				yield return $"{nameof(DistributedCacheExpiry)} must be > 0";

			if (FailExpiry <= TimeSpan.Zero)
				yield return $"{nameof(FailExpiry)} must be > 0";

			if (MinInput <= 0)
				yield return $"{nameof(MinInput)} must be > 0";

			if (RetryCount <= 0)
				yield return $"{nameof(RetryCount)} must be > 0";

			if (InMemoryCacheMaxObjects <= 0)
				yield return $"{nameof(InMemoryCacheMaxObjects)} must be > 0";

			if (InMemoryCacheObjectLifetime <= TimeSpan.Zero)
				yield return $"{nameof(InMemoryCacheObjectLifetime)} must be > 0";
		}
	}
}
