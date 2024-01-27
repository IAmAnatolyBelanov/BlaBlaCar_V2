namespace WebApi.Services.Redis
{
	public interface IRedisCacheServiceConfig
	{
		string ConnectionString { get; }
		TimeSpan ConnectionLifetime { get; }
	}

	public class RedisCacheServiceConfig : IRedisCacheServiceConfig, IBaseConfig
	{
		public string Position => "Redis";

		public string ConnectionString { get; set; } = default!;

		public TimeSpan ConnectionLifetime { get; set; } = TimeSpan.FromMinutes(15);

		public IEnumerable<string> GetValidationErrors()
		{
			if (string.IsNullOrWhiteSpace(ConnectionString))
				yield return $"{nameof(ConnectionString)} is null or empty";

			if (ConnectionLifetime <= TimeSpan.Zero)
				yield return $"{nameof(ConnectionLifetime)} must be > 0";
		}
	}
}
