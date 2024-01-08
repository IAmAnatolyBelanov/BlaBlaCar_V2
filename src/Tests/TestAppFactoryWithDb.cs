using DotNet.Testcontainers.Builders;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Testcontainers.PostgreSql;

using WebApi.DataAccess;

namespace Tests
{
	public class TestAppFactoryWithDb : WebApplicationFactory<Program>, IDisposable
	{
		private readonly PostgreSqlContainer _postgreSqlContainer = new PostgreSqlBuilder()
			.WithImage("postgis/postgis:16-3.4")
			.WithWaitStrategy(Wait.ForUnixContainer())
			.Build();

		public TestAppFactoryWithDb()
		{
			Task.Run(async () => await _postgreSqlContainer.StartAsync()).Wait();
		}

		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			base.ConfigureWebHost(builder);
			builder.ConfigureTestServices(services =>
			{
				services.RemoveAll<IApplicationContextConfig>();
				var conf = new ApplicationContextConfig
				{
					ConnectionString = _postgreSqlContainer.GetConnectionString()
				};
				services.AddSingleton<IApplicationContextConfig>(conf);
			});
		}

		public void Dispose()
		{
			Task.Run(async () => await _postgreSqlContainer.DisposeAsync()).Wait();
		}

		public void MigrateDb(int attemptionsCount = 10, int sleepPerionMs = 500)
		{
			// Хз, в чём причина, но на старте бд порой не успевает прийти в состояние готовности. В пределах тестов готов закрыть на это глаза.
			for (int i = 0; i < attemptionsCount + 1; i++)
			{
				try
				{
					using var scope = Services.CreateScope();
					using var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();
					context.Migrate();

					break;
				}
				catch
				{
					if (i == attemptionsCount)
						throw;

					Thread.Sleep(sleepPerionMs);
				}
			}
		}
	}
}