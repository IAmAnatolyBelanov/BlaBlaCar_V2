using Npgsql;
using Polly;

namespace WebApi.DataAccess;

public interface ISessionFactory
{
	IPostgresSession OpenPostgresConnection(bool beginTransaction = false, bool trace = false);
}

public class SessionFactory : ISessionFactory
{
	private readonly IPostgresConfig _postgresConfig;
	private readonly NpgsqlDataSourceBuilder _builder;
	private readonly NpgsqlDataSource _dataSource;
	private readonly Policy _policy;
	private readonly ILogger _logger = Log.ForContext<SessionFactory>();

	public SessionFactory(IPostgresConfig postgresConfig)
	{
		_postgresConfig = postgresConfig;
		_builder = new NpgsqlDataSourceBuilder(_postgresConfig.ConnectionString);
		_builder.UseNetTopologySuite();
		_dataSource = _builder.Build();

		_policy = Policy.Handle<Exception>()
			.WaitAndRetry(
				retryCount: _postgresConfig.RetriesCount,
				sleepDurationProvider: _ => _postgresConfig.RetryDelay,
				onRetry: (ex, delay, attempt, context) =>
				{
					_logger.Error(ex, "Failed to work with sessions");
				});
	}

	public IPostgresSession OpenPostgresConnection(bool beginTransaction = false, bool trace = false)
	{
		var result = _policy.Execute(() =>
		{
			var connection = _dataSource.OpenConnection();

			var session = new PostgresSession(connection, beginTransaction, trace);

			return session;
		});
		return result;
	}
}
