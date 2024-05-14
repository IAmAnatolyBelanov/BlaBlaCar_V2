using Npgsql;

namespace WebApi.DataAccess;

public interface ISessionFactory
{
	IPostgresSession OpenPostgresConnection(bool beginTransaction = false, bool trace = false);
}

public class SessionFactory : ISessionFactory
{
	private readonly IPostgresConfig _postgresConfig;
	private readonly NpgsqlDataSourceBuilder _builder;

	public SessionFactory(IPostgresConfig postgresConfig)
	{
		_postgresConfig = postgresConfig;
		_builder = new NpgsqlDataSourceBuilder(_postgresConfig.ConnectionString);
		_builder.UseNetTopologySuite();
	}

	public IPostgresSession OpenPostgresConnection(bool beginTransaction = false, bool trace = false)
	{
		var dataSource = _builder.Build();
		var connection = dataSource.OpenConnection();

		var result = new PostgresSession(connection, dataSource, beginTransaction, trace);
		return result;
	}
}
