namespace WebApi.Services.Redis
{
	public interface IRedisCacheServiceConfig
	{
		string ConnectionString { get; }
		int RetriesCount { get; }
		TimeSpan RetriesDelay { get; }
		int ConcurrentConnections { get; }
		TimeSpan PingPeriod { get; }
		TimeSpan ReconnectDelay { get; }
		int CircuitBreakerAllowedExceptionsCount { get; }
		TimeSpan CircuitBreakerOpenPeriod { get; }
		int CircuitBreakerRetriesCount { get; }
	}

	public class RedisCacheServiceConfig : IRedisCacheServiceConfig, IBaseConfig
	{
		public string Position => "Redis";

		public string ConnectionString { get; set; } = default!;

		public int RetriesCount { get; set; } = 2;

		public TimeSpan RetriesDelay { get; set; } = TimeSpan.FromMilliseconds(150);

		public int ConcurrentConnections { get; set; } = 1000;

		public TimeSpan PingPeriod { get; set; } = TimeSpan.FromSeconds(5);

		public TimeSpan ReconnectDelay { get; set; } = TimeSpan.FromMilliseconds(50);

		public int CircuitBreakerAllowedExceptionsCount { get; set; } = 2;

		public TimeSpan CircuitBreakerOpenPeriod { get; set; } = TimeSpan.FromSeconds(2);

		public int CircuitBreakerRetriesCount { get; set; } = 20;

		public IEnumerable<string> GetValidationErrors()
		{
			if (string.IsNullOrWhiteSpace(ConnectionString))
				yield return $"{nameof(ConnectionString)} is null or empty";

			if (RetriesCount <= 0)
				yield return $"{nameof(RetriesCount)} must be > 0";

			if (RetriesDelay <= TimeSpan.Zero)
				yield return $"{nameof(RetriesDelay)} must be > 0";

			if (ConcurrentConnections <= 0)
				yield return $"{nameof(ConcurrentConnections)} must be > 0";

			if (PingPeriod <= TimeSpan.Zero)
				yield return $"{nameof(PingPeriod)} must be > 0";

			if (ReconnectDelay < TimeSpan.Zero)
				yield return $"{nameof(ReconnectDelay)} must be >= 0";

			if (CircuitBreakerAllowedExceptionsCount < 0)
				yield return $"{nameof(CircuitBreakerAllowedExceptionsCount)} must be >= 0";

			if (CircuitBreakerOpenPeriod <= TimeSpan.Zero)
				yield return $"{nameof(CircuitBreakerOpenPeriod)} must be > 0";

			if (CircuitBreakerRetriesCount <= 0)
				yield return $"{nameof(CircuitBreakerRetriesCount)} must be > 0";
		}
	}
}
