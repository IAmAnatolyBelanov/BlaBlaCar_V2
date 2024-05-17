using Dapper;
using FluentValidation;

using Microsoft.Extensions.DependencyInjection.Extensions;

using NetTopologySuite.Geometries;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

using Npgsql;

using NpgsqlTypes;
using System.Data;
using System.Reflection;
using WebApi.DataAccess;
using WebApi.Models;
using WebApi.Services.Core;
using WebApi.Services.Driver;
using WebApi.Services.Redis;
using WebApi.Services.User;
using WebApi.Services.Validators;
using WebApi.Services.Yandex;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Services.AddControllers(options =>
		{
			options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
		}).AddNewtonsoftJson(options =>
		{
			options.SerializerSettings.Converters.Add(new StringEnumConverter(new CamelCaseNamingStrategy()));
		});

		builder.Configuration
			.AddDefaultConfigs()
			.Build();

		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen(options =>
		{
			options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
		});
		builder.Services.AddSwaggerGenNewtonsoftSupport();
		Serilog.Debugging.SelfLog.Enable(Console.Error);

		Log.Logger = new LoggerConfiguration()
			.ReadFrom.Configuration(builder.Configuration)
			.CreateLogger();

		builder.Host.UseSerilog();

		builder.Logging.AddRinLogger();
		builder.Services.AddRin();

		SqlMapper.AddTypeHandler(new PointTypeMapper());

		Log.Debug("{Config}", builder.Configuration.GetDebugView());

		builder.Services.RegisterConfigs(builder.Configuration.Bind);
		builder.Services.RegisterMappers();
		builder.Services.AddValidatorsFromAssemblyContaining<RideMapper>(lifetime: ServiceLifetime.Singleton);

		builder.Services.AddPostgresMigrator();

		builder.Services.TryAddSingleton<IClock, Clock>();

		builder.Services.TryAddSingleton<ISessionFactory, SessionFactory>();

		builder.Services.RegisterRepositories();

		builder.Services.TryAddSingleton<IRedisCacheService, RedisCacheService>();
		builder.Services.TryAddSingleton<ISuggestService, SuggestService>();
		builder.Services.TryAddSingleton<IGeocodeService, GeocodeService>();
		builder.Services.TryAddSingleton<IRouteService, RouteService>();
		builder.Services.TryAddSingleton<IRideService, RideService>();
		builder.Services.TryAddSingleton<IDriverService, DriverService>();
		builder.Services.TryAddSingleton<IUserService, UserService>();

		builder.Services.AddHttpClient(Constants.DefaultHttpClientName)
			.SetHandlerLifetime(TimeSpan.FromHours(1));

		var app = builder.Build();

		app.Services.ValidateConfigs();

		app.UseMiddleware<ExceptionMiddleware>();

		// Configure the HTTP request pipeline.
		if (app.Environment.IsDevelopment())
		{
			app.UseRin();
			app.UseSwagger();
			app.UseSwaggerUI();
			app.UseRinDiagnosticsHandler();
		}

		app.UseAuthorization();

		app.MapControllers();

		app.Run();
	}

	public class PointTypeMapper : SqlMapper.TypeHandler<Point>
	{
		public override void SetValue(IDbDataParameter parameter, Point? value)
		{
			if (parameter is NpgsqlParameter npgsqlParameter)
			{
				npgsqlParameter.NpgsqlDbType = NpgsqlDbType.Geography;
				npgsqlParameter.NpgsqlValue = value;
			}
			else
			{
				throw new ArgumentException();
			}
		}

		public override Point Parse(object value)
		{
			if (value is Point geometry)
			{
				return geometry;
			}

			throw new ArgumentException();
		}
	}


}