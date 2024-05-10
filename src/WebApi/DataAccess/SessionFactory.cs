using Npgsql;

namespace WebApi.DataAccess;

public interface ISessionFactory
{
	IPostgresSession OpenPostgresConnection(bool beginTransaction = false, bool trace = false);
}

public class SessionFactory : ISessionFactory
{
	private readonly IPostgresConfig _postgresConfig;

	public SessionFactory(IPostgresConfig postgresConfig)
	{
		_postgresConfig = postgresConfig;
	}

	public IPostgresSession OpenPostgresConnection(bool beginTransaction = false, bool trace = false)
	{
		var connection = new NpgsqlConnection(_postgresConfig.ConnectionString);
		connection.Open();
		var result = new PostgresSession(connection, beginTransaction, trace);
		return result;
	}
}
