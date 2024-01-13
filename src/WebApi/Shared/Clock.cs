namespace WebApi.Shared
{
	public interface IClock
	{
		DateTimeOffset Now { get; }
	}

	public class Clock : IClock
	{
		public DateTimeOffset Now => DateTimeOffset.UtcNow;
	}
}
