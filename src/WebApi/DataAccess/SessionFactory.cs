using Npgsql;

namespace WebApi.DataAccess;

public class SessionFactory
{
	private readonly IPostgresConfig _postgresConfig;

	public SessionFactory(IPostgresConfig postgresConfig)
	{
		_postgresConfig = postgresConfig;
	}

	public PostgresSession OpenPostgresConnection(bool beginTransaction = false, bool trace = false)
	{
		var connection = new NpgsqlConnection(_postgresConfig.ConnectionString);
		connection.Open();
		var result = new PostgresSession(connection, beginTransaction, trace);
		return result;
	}
}
