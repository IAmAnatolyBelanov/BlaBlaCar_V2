using Dapper;
using FluentValidation;

using Microsoft.EntityFrameworkCore;

using NetTopologySuite.Geometries;

using Npgsql;

using NpgsqlTypes;
using System.Data;
using WebApi.DataAccess;
using WebApi.Repositories;
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

		builder.Services.AddControllers(options
			=> options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);

		builder.Configuration
			.AddDefaultConfigs()
			.Build();

		// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen();

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
		builder.Services.AddValidatorsFromAssemblyContaining<LegDtoValidator>(lifetime: ServiceLifetime.Singleton);

		builder.Services.AddPostgresMigrator();

		builder.Services.AddDbContext<ApplicationContext>((serviceProvider, options) =>
			options.UseNpgsql(
				serviceProvider.GetRequiredService<IPostgresConfig>().ConnectionString,
				x => x.UseNetTopologySuite())
			.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

		builder.Services.AddSingleton<IClock, Clock>();

		builder.Services.AddSingleton<ISessionFactory, SessionFactory>();

		builder.Services.RegisterRepositories();

		builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
		builder.Services.AddSingleton<ISuggestService, SuggestService>();
		builder.Services.AddSingleton<IGeocodeService, GeocodeService>();
		builder.Services.AddSingleton<IRouteService, RouteService>();
		builder.Services.AddSingleton<IRideService, RideService>();
		builder.Services.AddSingleton<IDriverService, DriverService>();
		builder.Services.AddSingleton<IUserService, UserService>();

		builder.Services.AddHttpClient(Constants.DefaultHttpClientName)
			.SetHandlerLifetime(TimeSpan.FromHours(1));

		var app = builder.Build();

		var scope = app.Services.CreateScope();
		var ser = scope.ServiceProvider.GetRequiredService<IDriverService>();

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