using System.Reflection;
using WebApi.DataAccess;
using WebApi.Extensions;
using WebApi.Models;
using WebApi.Services.Core;
using WebApi.Shared;

namespace Tests
{
	public class RideServiceTests : IClassFixture<TestAppFactoryFull>
	{
		private readonly IServiceProvider _provider;
		private readonly Fixture _fixture;
		private readonly IRideService _rideService;
		private readonly IServiceScope _scope;

		private readonly string _originalConfigJson;

		public RideServiceTests(TestAppFactoryFull factory)
		{
			// Используем только postgres (без redis), так как обращаемся к реальному api за реальной географией.
			// Кеш позволит иногда не делать запросы к внешнему сервису.
			factory.AddPostgres();
			_provider = factory.Services;
			_fixture = Shared.BuildDefaultFixture();
			_scope = _provider.CreateScope();
			_rideService = _scope.ServiceProvider.GetRequiredService<IRideService>();

			var config = _scope.ServiceProvider.GetRequiredService<IRideServiceConfig>();
			_originalConfigJson = JsonConvert.SerializeObject(config);
		}

		private IEnumerable<DateTimeOffset> BuildPeriods(DateTimeOffset start, TimeSpan duration)
		{
			yield return start;

			while (true)
			{
				start += duration;
				yield return start;
			}
		}

		private void RestoreConfig()
		{
			var currentConfig = _scope.ServiceProvider.GetRequiredService<RideServiceConfig>();
			var originalConfig = JsonConvert.DeserializeObject<RideServiceConfig>(_originalConfigJson);

			var type = currentConfig.GetType();

			var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty | BindingFlags.GetProperty)
				.Where(x => x.CanRead && x.CanWrite)
				.ToArray();

			foreach (var prop in properties)
			{
				var value = prop.GetValue(originalConfig);
				prop.SetValue(currentConfig, value);
			}
		}
	}
}
