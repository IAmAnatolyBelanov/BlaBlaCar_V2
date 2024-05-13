using Npgsql;

namespace WebApi.DataAccess;

public interface ISessionFactory
{
	IPostgresSession OpenPostgresConnection(bool beginTransaction = false, bool trace = false);
}

public class SessionFactory : ISessionFactory
{
	private readonly IPostgresConfig _postgresConfig;
	private readonly IClock _clock;

	public SessionFactory(IPostgresConfig postgresConfig, IClock clock)
	{
		_postgresConfig = postgresConfig;
		_clock = clock;

	}

	public IPostgresSession OpenPostgresConnection(bool beginTransaction = false, bool trace = false)
	{
		var connection = new NpgsqlConnection(_postgresConfig.ConnectionString);
		connection.Open();
		var result = new PostgresSession(connection, _clock, beginTransaction, trace);
		return result;
	}
}
