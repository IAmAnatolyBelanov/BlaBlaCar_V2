﻿using FluentMigrator.Runner;
using FluentMigrator.Runner.Initialization;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Newtonsoft.Json;

using Riok.Mapperly.Abstractions;

using System.Reflection;
using WebApi.DataAccess;
using WebApi.Migrations;
using WebApi.Repositories;

namespace WebApi.Extensions
{
	public static class ServiceCollectionExtensions
	{
		private static readonly ILogger _logger = Log.Logger;

		// Я так и не понял, почему не удаётся протащить нормальный Configuration.Bind без извращений.
		public static void RegisterConfigs(this IServiceCollection services, Action<string, object> bind)
			=> RegisterConfigs(services, bind, Assembly.GetExecutingAssembly());
		public static void RegisterConfigs(this IServiceCollection services, Action<string, object> bind, Assembly assembly)
		{
			var configTypes = GetAllConfigTypes(assembly);

			foreach (var configType in configTypes)
			{
				var configInterfaces = configType.GetInterfaces()
					.Where(i => i != typeof(IBaseConfig) && i != typeof(IValidatableConfig))
					.ToArray();

				services.AddSingleton(configType);
				services.AddSingleton(typeof(IBaseConfig), provider =>
				{
					var config = provider.GetRequiredService(configType);
					var baseConf = config as IBaseConfig;
					bind(baseConf!.Position, config);
					var errors = baseConf.GetValidationErrors().ToArray();

					if (errors.Length != 0)
						throw new Exception($"Fail on validation {configType.FullName}.{Environment.NewLine}{RenderErrors(errors)}");

					_logger.Debug(
						"For interface {Interface} registered implementation {Implementation}: {Json}",
						typeof(IBaseConfig).FullName,
						configType.FullName,
						JsonConvert.SerializeObject(config));
					return config;
				});
				services.AddSingleton(typeof(IValidatableConfig), provider =>
				{
					var config = provider.GetRequiredService<IEnumerable<IBaseConfig>>()
						.First(x => x.GetType() == configType);
					_logger.Debug(
						"For interface {Interface} registered implementation {Implementation}: {Json}",
						typeof(IValidatableConfig).FullName,
						configType.FullName,
						JsonConvert.SerializeObject(config));
					return config;
				});

				foreach (var configInterface in configInterfaces)
				{
					services.AddSingleton(configInterface, provider =>
					{
						var config = provider.GetRequiredService(configType);
						_logger.Debug(
							"For interface {Interface} registered implementation {Implementation}: {Json}",
							configInterface.FullName,
							configType.FullName,
							JsonConvert.SerializeObject(config));
						return config;
					});
				}
			}
		}

		public static void ValidateConfigs(this IServiceProvider serviceProvider)
		{
			var configs = serviceProvider.GetRequiredService<IEnumerable<IValidatableConfig>>().ToArray();

			foreach (var config in configs)
			{
				var errors = config.GetValidationErrors().ToArray();

				if (errors.Length != 0)
					throw new Exception($"Fail on validation {config.GetType().FullName}.{Environment.NewLine}{RenderErrors(errors)}");
			}
		}

		private static Type[] GetAllConfigTypes(Assembly assembly)
		{
			return assembly.GetTypes()
				.Where(t => typeof(IBaseConfig).IsAssignableFrom(t) && t != typeof(IBaseConfig) && t != typeof(IValidatableConfig) && t.IsClass)
				.ToArray();
		}

		private static string RenderErrors(string[] errors)
		{
			return string.Join(Environment.NewLine, errors.Select(x => $"{x.Trim('.', ' ')}."));
		}

		public static void RegisterMappers(this IServiceCollection services)
		{
			services.TryAddSingleton(typeof(Lazy<>), typeof(Lazier<>));

			var mappers = Assembly.GetExecutingAssembly().GetTypes()
				.Where(x => x.GetCustomAttribute<MapperAttribute>() is not null)
				.ToArray();

			foreach (var mapper in mappers)
			{
				var mapperInterfaces = mapper.GetInterfaces();
				foreach (var mapperInterface in mapperInterfaces)
				{
					services.TryAddSingleton(mapperInterface, mapper);
				}
			}
		}

		public static IConfigurationBuilder AddDefaultConfigs(this IConfigurationBuilder configuration)
		{
			var result = configuration
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
				.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable(variable: "ASPNETCORE_ENVIRONMENT") ?? "Production"}.json", optional: true, reloadOnChange: false)
				.AddEnvironmentVariables("BBC_");

			return result;
		}

		private class Lazier<T> : Lazy<T> where T : class
		{
			public Lazier(IServiceProvider provider)
				: base(() => provider.GetRequiredService<T>())
			{
			}
		}

		private static IHost? _postgresMigratorHost;
		public static void AddPostgresMigrator(this IServiceCollection services)
		{
			services.AddKeyedTransient<IMigrationRunner>(Constants.PostgresMigratorKey, (services, _) =>
			{
				var connectionString = services.GetRequiredService<IPostgresConfig>().ConnectionString;
				var tempPgBuilder = Host.CreateDefaultBuilder();

				tempPgBuilder.ConfigureServices(tmpServices =>
				{
					tmpServices.AddFluentMigratorCore()
						.ConfigureRunner(rb =>
							rb.AddPostgres()
							.WithGlobalConnectionString(connectionString)
							.ScanIn(typeof(InitialMigration).Assembly).For.Migrations())
						.Configure<RunnerOptions>(x => x.Tags = [Constants.PostgresMigrationTag])
						.AddLogging(lb => lb.AddSerilog());
				});

				_postgresMigratorHost = tempPgBuilder.Build();

				var scope = _postgresMigratorHost.Services.CreateScope();
				var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
				return runner;
			});
		}

		public static void RegisterRepositories(this IServiceCollection services)
			=> RegisterRepositories(services, Assembly.GetExecutingAssembly());

		public static void RegisterRepositories(this IServiceCollection services, Assembly assembly)
		{
			var repositoryTypes = GetAllRepositoryTypes(assembly);

			foreach (var repoType in repositoryTypes)
			{
				var repoInterfaces = repoType.GetInterfaces()
					.Where(i => i != typeof(IRepository))
					.ToArray();

				services.AddSingleton(repoType);

				foreach (var repoInterface in repoInterfaces)
				{
					services.AddSingleton(repoInterface, provider => provider.GetRequiredService(repoType));
				}
			}
		}

		private static Type[] GetAllRepositoryTypes(Assembly assembly)
		{
			return assembly.GetTypes()
				.Where(t => typeof(IRepository).IsAssignableFrom(t) && t != typeof(IRepository) && t.IsClass)
				.ToArray();
		}
	}

	// Необходим для автоматического поиска и регистрации конфигов в системе
	public interface IBaseConfig : IValidatableConfig
	{
		string Position { get; }
	}

	public interface IValidatableConfig
	{
		IEnumerable<string> GetValidationErrors();
	}
}
