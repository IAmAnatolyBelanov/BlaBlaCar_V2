using Newtonsoft.Json;

using System.Reflection;

namespace WebApi.Extensions
{
	public static class ServiceCollectionExtensions
	{
		private static readonly ILogger _logger = Log.Logger;

		// Я так и не понял, почему не удаётся протащить нормальный Configuration.Bind без извращений.
		public static void RegisterConfigs(this IServiceCollection services, Action<string, object> bind)
		{
			var configTypes = Assembly.GetExecutingAssembly().GetTypes()
				.Where(t => typeof(IBaseConfig).IsAssignableFrom(t) && !t.IsInterface)
				.ToArray();

			foreach (var configType in configTypes)
			{
				var interfaces = configType.GetInterfaces();
				var configInterface = interfaces
					.Except(interfaces.SelectMany(i => i.GetInterfaces()))
					.Where(i => i != typeof(IBaseConfig))
					.Single();

				var config = Activator.CreateInstance(configType);
				bind(((IBaseConfig)config!).Position, config);

				var errors = ((IBaseConfig)config).GetValidationErrors().ToArray();
				if (errors.Length != 0)
					throw new Exception($"Fail on validation {configType.FullName}.{Environment.NewLine}{RenderErrors(errors)}");

				services.AddSingleton(configInterface, config);

				_logger.Debug(
					"For interface {Interface} registered implementation {Implementation}: {Json}",
					configInterface.FullName,
					configType.FullName,
					JsonConvert.SerializeObject(config));
			}
		}

		private static string RenderErrors(string[] errors)
		{
			return string.Join(Environment.NewLine, errors.Select(x => $"{x.Trim('.', ' ')}."));
		}
	}

	// Необходим для автоматического поиска и регистрации конфигов в системе
	public interface IBaseConfig
	{
		string Position { get; }

		IEnumerable<string> GetValidationErrors();
	}
}
