using DotNet.Testcontainers.Builders;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

using WebApi.DataAccess;
using WebApi.Services.Redis;

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

		public void MigrateDb(int attemptsCount = 10, int sleepPeriodMs = 500)
		{
			// Хз, в чём причина, но на старте бд порой не успевает прийти в состояние готовности. В пределах тестов готов закрыть на это глаза.
			for (int i = 0; i < attemptsCount + 1; i++)
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
					if (i == attemptsCount)
						throw;

					Thread.Sleep(sleepPeriodMs);
				}
			}
		}
	}

	public class TestAppFactoryWithRedis : WebApplicationFactory<Program>, IDisposable
	{
		private readonly string _mountPath = Path.Combine(Path.GetTempPath(), nameof(TestAppFactoryWithRedis), Guid.NewGuid().ToString());
		private readonly RedisBuilder _redisBuilder;
		private RedisContainer _redisContainer;

		public TestAppFactoryWithRedis()
		{
			Directory.CreateDirectory(_mountPath);

			_redisBuilder = new RedisBuilder()
				.WithBindMount(_mountPath, "/data")
				.WithImage("redis:7.2.3")
				.WithAutoRemove(true);

			_redisContainer = _redisBuilder.Build();
			Task.Run(async () => await _redisContainer.StartAsync()).Wait();
		}

		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			base.ConfigureWebHost(builder);

			builder.ConfigureTestServices(services =>
			{
				services.RemoveAll<IRedisCacheServiceConfig>();
				var redisConfig = new RedisCacheServiceConfig()
				{
					ConnectionString = _redisContainer.GetConnectionString()
				};
				services.AddSingleton<IRedisCacheServiceConfig>(redisConfig);
			});
		}

		public void RestartContainer(TimeSpan extraDelay)
		{
			lock (this)
			{
				// Чтобы всё нужное сдампилось.
				Thread.Sleep(1500);

				var port = _redisContainer.GetMappedPublicPort(6379);

				Task.Run(async () =>
				{
					await _redisContainer.StopAsync();
					await _redisContainer.DisposeAsync();
				}).Wait();

				if (extraDelay > TimeSpan.Zero)
					Thread.Sleep(extraDelay);

				_redisContainer = _redisBuilder
					.WithPortBinding(port, 6379)
					.Build();

				Task.Run(async () => await _redisContainer.StartAsync()).Wait();
			}
		}

		protected override void Dispose(bool disposing)
		{
			lock (this)
			{
				if (Directory.Exists(_mountPath))
					Directory.Delete(_mountPath, recursive: true);
			}
			base.Dispose(disposing);
		}
	}
}