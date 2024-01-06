using Dapper;

using Microsoft.EntityFrameworkCore;

using NetTopologySuite.Geometries;

using Npgsql;

using NpgsqlTypes;

using System.Data;

using WebApi.DataAccess;
using WebApi.Extensions;
using WebApi.Services.Core;
using WebApi.Services.Redis;
using WebApi.Services.Yandex;

public class Program
{
	public static void Main(string[] args)
	{
		var builder = WebApplication.CreateBuilder(args);

		builder.Services.AddControllers();

		// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
		builder.Services.AddEndpointsApiExplorer();
		builder.Services.AddSwaggerGen();

		Serilog.Debugging.SelfLog.Enable(Console.Error);

		Log.Logger = new LoggerConfiguration()
			.MinimumLevel.Debug()
			.Enrich.FromLogContext()
			.Enrich.WithThreadId()
			.WriteTo.Console()
			.WriteTo.Seq("http://host.docker.internal:5341", restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug)
			.CreateLogger();

		builder.Host.UseSerilog();

		SqlMapper.AddTypeHandler(new PointTypeMapper());

		Log.Debug(builder.Configuration.GetDebugView());

		builder.Services.RegisterConfigs(builder.Configuration.Bind);

		builder.Services.AddDbContext<ApplicationContext>((serviceProvider, options) =>
			options.UseNpgsql(
				serviceProvider.GetRequiredService<IApplicationContextConfig>().ConnectionString,
				x => x.UseNetTopologySuite())
			.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking));

		builder.Services.AddSingleton<IRedisCacheService, RedisCacheService>();
		builder.Services.AddSingleton<ISuggestService, SuggestService>();
		builder.Services.AddSingleton<IGeocodeService, GeocodeService>();
		builder.Services.AddSingleton<IRouteService, RouteService>();
		builder.Services.AddSingleton<RideService>();

		builder.Services.AddHttpClient(Constants.DefaultHttpClientName)
			.SetHandlerLifetime(TimeSpan.FromHours(1));

		var app = builder.Build();

		// Configure the HTTP request pipeline.
		if (app.Environment.IsDevelopment())
		{
			app.UseSwagger();
			app.UseSwaggerUI();
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