using WebApi.Extensions;

namespace WebApi.Services.Yandex
{
	public interface IRouteServiceConfig
	{
		string ApiKey { get; }
		TimeSpan Expiry { get; }
		TimeSpan FailExpiry { get; }
		bool IsDebug { get; }
		int RetryCount { get; }
		int MaxPoints { get; }
	}

	public class RouteServiceConfig : IBaseConfig, IRouteServiceConfig
	{
		public string Position => "Yandex:Route";

		public string ApiKey { get; set; } = default!;
		public TimeSpan Expiry { get; set; } = TimeSpan.FromDays(30);
		public TimeSpan FailExpiry { get; set; } = TimeSpan.FromMinutes(5);
		public int RetryCount { get; set; } = 2;
		public int MaxPoints { get; set; } = 10;
		public bool IsDebug { get; set; } = false;

		public IEnumerable<string> GetValidationErrors()
		{
			if (string.IsNullOrWhiteSpace(ApiKey))
				yield return $"{nameof(ApiKey)} is null or empty";

			if (Expiry <= TimeSpan.Zero)
				yield return $"{nameof(Expiry)} must be > 0";

			if (FailExpiry <= TimeSpan.Zero)
				yield return $"{nameof(FailExpiry)} must be > 0";

			if (RetryCount <= 0)
				yield return $"{nameof(RetryCount)} must be > 0";

			if (MaxPoints <= 1)
				yield return $"{nameof(MaxPoints)} must be >= 2";
		}

	}
}
