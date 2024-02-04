using DotNet.Testcontainers.Builders;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Serilog;
using Testcontainers.PostgreSql;
using Testcontainers.Redis;

using WebApi.DataAccess;
using WebApi.Extensions;

namespace Tests
{
	public class TestAppFactoryFull : EmptyTestAppFactory, IDisposable
	{
		private readonly Dictionary<Type, EmptyTestAppFactory> _factories = [];

		/// <summary>
		/// Этот и прочие *Add* методы должны вызываться перед обращением к factory.Services.
		/// </summary>
		public TestAppFactoryFull AddAll()
		{
			var functions = new Func<TestAppFactoryFull>[]
			{
				AddPostgres,
				AddRedis,
			};

			foreach (var func in functions)
				func.Invoke();

			return this;
		}

		public TestAppFactoryFull AddPostgres()
		{
			Add<TestAppFactoryWithDb>();
			var factory = _factories[typeof(TestAppFactoryWithDb)] as TestAppFactoryWithDb;
			factory!.MigrateDb();
			return this;
		}

		public TestAppFactoryFull AddRedis()
		{
			Add<TestAppFactoryWithRedis>();
			return this;
		}

		private void Add<T>() where T : EmptyTestAppFactory
		{
			Add(typeof(T));
		}

		private void Add(params Type[] types)
		{
			foreach (var type in types)
			{
				if (_factories.ContainsKey(type))
					continue;

				var factory = Activator.CreateInstance(type) as EmptyTestAppFactory;
				_factories[type] = factory!;
				continue;
			}
		}

		public void RestartRedisContainer(TimeSpan? extraDelay = null)
		{
			var factory = _factories[typeof(TestAppFactoryWithRedis)] as TestAppFactoryWithRedis;
			factory!.RestartContainer(extraDelay);
		}

		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			base.ConfigureWebHost(builder);

			builder.ConfigureAppConfiguration((webBuilder, confBuilder) =>
			{
				foreach (var factory in _factories.Values)
				{
					confBuilder = factory.PrepareTestConfigs(confBuilder);
				}
				confBuilder.Build();

				Log.Logger = new LoggerConfiguration()
					.WriteTo.Console()
					.MinimumLevel.Debug()
					.CreateLogger();
			});
		}

		protected override void Dispose(bool disposing)
		{
			foreach (var factory in _factories.Values)
				factory.Dispose();

			base.Dispose(disposing);
		}
	}


	public class TestAppFactoryWithDb : EmptyTestAppFactory, IDisposable
	{
		public PostgreSqlBuilder PostgreSqlBuilder { get; init; }

		private readonly PostgreSqlContainer _postgreSqlContainer;

		public TestAppFactoryWithDb()
		{
			PostgreSqlBuilder = new PostgreSqlBuilder()
				.WithImage("postgis/postgis:16-3.4")
				.WithWaitStrategy(Wait.ForUnixContainer())
				.WithAutoRemove(true);

			_postgreSqlContainer = PostgreSqlBuilder.Build();

			Task.Run(async () => await _postgreSqlContainer.StartAsync()).Wait();
		}

		protected override void Dispose(bool disposing)
		{
			Task.Run(async () => await _postgreSqlContainer.DisposeAsync()).Wait();
			base.Dispose(disposing);
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

		public override IConfigurationBuilder PrepareTestConfigs(IConfigurationBuilder configuration)
		{
			var result = base.PrepareTestConfigs(configuration);

			var replace = new Dictionary<string, string?>
			{
				["PostgreSQL:ConnectionString"] = _postgreSqlContainer.GetConnectionString()
			};

			result = result.AddInMemoryCollection(replace);

			return result;
		}
	}

	public class TestAppFactoryWithRedis : EmptyTestAppFactory, IDisposable
	{
		public string MountPath { get; } = Path.Combine(Path.GetTempPath(), nameof(TestAppFactoryWithRedis), Guid.NewGuid().ToString());
		public RedisBuilder RedisBuilder { get; private init; }
		private RedisContainer _redisContainer;

		public TestAppFactoryWithRedis()
		{
			Directory.CreateDirectory(MountPath);

			RedisBuilder = new RedisBuilder()
				.WithBindMount(MountPath, "/data")
				.WithImage("redis:7.2.3")
				.WithAutoRemove(true);

			_redisContainer = RedisBuilder.Build();
			Task.Run(async () => await _redisContainer.StartAsync()).Wait();
		}

		public void RestartContainer(TimeSpan? extraDelay = null)
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

				if (extraDelay.HasValue && extraDelay.Value > TimeSpan.Zero)
					Thread.Sleep(extraDelay.Value);

				_redisContainer = RedisBuilder
					.WithPortBinding(port, 6379)
					.Build();

				Task.Run(async () => await _redisContainer.StartAsync()).Wait();
			}
		}

		public override IConfigurationBuilder PrepareTestConfigs(IConfigurationBuilder configuration)
		{
			var result = base.PrepareTestConfigs(configuration);

			var replace = new Dictionary<string, string?>
			{
				["Redis:ConnectionString"] = _redisContainer.GetConnectionString()
			};

			result = result.AddInMemoryCollection(replace);

			return result;
		}

		protected override void Dispose(bool disposing)
		{
			lock (this)
			{
				if (Directory.Exists(MountPath))
					Directory.Delete(MountPath, recursive: true);
			}
			base.Dispose(disposing);
		}
	}

	public class EmptyTestAppFactory : WebApplicationFactory<Program>
	{
		protected override void ConfigureWebHost(IWebHostBuilder builder)
		{
			base.ConfigureWebHost(builder);

			builder.ConfigureAppConfiguration((webBuilder, confBuilder) =>
			{
				var conf = PrepareTestConfigs(confBuilder).Build();

				Log.Logger = new LoggerConfiguration()
					.WriteTo.Console()
					.MinimumLevel.Debug()
					.CreateLogger();
			});
		}

		public virtual IConfigurationBuilder PrepareTestConfigs(IConfigurationBuilder configuration)
		{
			configuration.AddDefaultConfigs()
				.AddJsonFile("appsettings.Test.json");

			return configuration;
		}
	}
}