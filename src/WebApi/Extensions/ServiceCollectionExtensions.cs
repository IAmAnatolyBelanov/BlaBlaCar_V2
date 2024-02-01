using Newtonsoft.Json;

using Riok.Mapperly.Abstractions;

using System.Reflection;

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
					.Where(i => i != typeof(IBaseConfig))
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
			var configs = serviceProvider.GetRequiredService<IEnumerable<IBaseConfig>>().ToArray();

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
				.Where(t => typeof(IBaseConfig).IsAssignableFrom(t) && t != typeof(IBaseConfig) && t.IsClass)
				.ToArray();
		}

		private static string RenderErrors(string[] errors)
		{
			return string.Join(Environment.NewLine, errors.Select(x => $"{x.Trim('.', ' ')}."));
		}

		public static void RegisterMappers(this IServiceCollection services)
		{
			services.AddSingleton(typeof(Lazy<>), typeof(Lazier<>));

			var mappers = Assembly.GetExecutingAssembly().GetTypes()
				.Where(x => x.GetCustomAttribute<MapperAttribute>() is not null)
				.ToArray();

			foreach (var mapper in mappers)
			{
				var mapperInterface = mapper.GetInterfaces()
					.Single(x => !x.IsGenericType);
				services.AddSingleton(mapperInterface, mapper);
			}
		}

		private class Lazier<T> : Lazy<T> where T : class
		{
			public Lazier(IServiceProvider provider)
				: base(() => provider.GetRequiredService<T>())
			{
			}
		}
	}

	// Необходим для автоматического поиска и регистрации конфигов в системе
	public interface IBaseConfig
	{
		string Position { get; }

		IEnumerable<string> GetValidationErrors();
	}
}
