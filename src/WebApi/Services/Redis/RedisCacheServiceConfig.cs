using WebApi.Extensions;

namespace WebApi.Services.Redis
{
	public interface IRedisCacheServiceConfig
	{
		string ConnectionString { get; }
	}

	public class RedisCacheServiceConfig : IRedisCacheServiceConfig, IBaseConfig
	{
		public string Position => "Redis";

		public string ConnectionString { get; set; } = default!;

		public IEnumerable<string> GetValidationErrors()
		{
			if (string.IsNullOrWhiteSpace(ConnectionString))
				yield return $"{nameof(ConnectionString)} is null or empty";
		}
	}
}
