namespace WebApi.DataAccess
{
	public interface IPostgresConfig
	{
		string ConnectionString { get; }
		int RetriesCount { get; }
		TimeSpan RetryDelay { get; }
	}

	public class PostgresConfig : IBaseConfig, IPostgresConfig
	{
		public string Position => "PostgreSQL";

		public string ConnectionString { get; set; } = default!;

		public int RetriesCount { get; set; } = 3;

		public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);

		public IEnumerable<string> GetValidationErrors()
		{
			if (string.IsNullOrWhiteSpace(ConnectionString))
				yield return $"{nameof(ConnectionString)} is null or empty";

			if (RetriesCount <= 0)
				yield return $"{nameof(RetriesCount)} must be > 0";

			if (RetryDelay <= TimeSpan.Zero)
				yield return $"{nameof(RetryDelay)} must be > 0";
		}
	}
}
