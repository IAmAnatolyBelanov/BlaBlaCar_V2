namespace WebApi.DataAccess
{
	public interface IPostgresConfig
	{
		string ConnectionString { get; }
	}

	public class PostgresConfig : IBaseConfig, IPostgresConfig
	{
		public string Position => "PostgreSQL";

		public string ConnectionString { get; set; } = default!;

		public IEnumerable<string> GetValidationErrors()
		{
			if (string.IsNullOrWhiteSpace(ConnectionString))
				yield return $"{nameof(ConnectionString)} is null or empty";
		}
	}
}
