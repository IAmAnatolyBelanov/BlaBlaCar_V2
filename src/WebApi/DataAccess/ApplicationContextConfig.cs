namespace WebApi.DataAccess
{
	public interface IApplicationContextConfig
	{
		string ConnectionString { get; }
	}

	public class ApplicationContextConfig : IBaseConfig, IApplicationContextConfig
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
