using Dapper;
using Npgsql;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace WebApi.DataAccess;

public class PostgresSession : IDisposable
{
	private static readonly ILogger _logger = Log.ForContext<PostgresSession>();

	private bool _trace;

	public NpgsqlConnection Connection { get; private init; }
	public NpgsqlTransaction? Transaction { get; private set; }

	public PostgresSession(NpgsqlConnection connection, bool beginTransaction = false, bool trace = false)
	{
		Connection = connection;

		if (beginTransaction)
			BeginTransaction();

		if (trace)
			StartTrace();
	}

	public PostgresSession BeginTransaction()
	{
		if (Transaction is not null)
			return this;
		lock (this)
		{
			if (Transaction is not null)
				return this;

			Transaction = Connection.BeginTransaction();
			return this;
		}
	}

	public PostgresSession StartTrace()
	{
		_trace = true;
		return this;
	}


	public Task<List<T>> QueryAsync<T>(string sql, CancellationToken ct,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "")
		=> QueryAsync<T>(
			sql: sql,
			ct: ct,
			param: null,
			commandTimeout: null,
			commandType: null,
			sourceFilePath: sourceFilePath,
			memberName: memberName);

	public Task<List<T>> QueryAsync<T>(string sql, object param, CancellationToken ct,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "")
		=> QueryAsync<T>(
			sql: sql,
			param: param,
			ct: ct,
			commandTimeout: null,
			commandType: null,
			sourceFilePath: sourceFilePath,
			memberName: memberName);

	public Task<List<T>> QueryAsync<T>(
		string sql,
		object? param = null,
		int? commandTimeout = null,
		CommandType? commandType = null,
		CancellationToken ct = default,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "")
	{
		var commandDefinition = new CommandDefinition(
			commandText: sql,
			parameters: param,
			transaction: Transaction,
			commandTimeout: commandTimeout,
			commandType: commandType,
			cancellationToken: ct
		);

		var result = QueryAsync<T>(commandDefinition, sourceFilePath, memberName);
		return result;
	}

	public async Task<List<T>> QueryAsync<T>(
		CommandDefinition command,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "")
	{
		var queryId = Guid.NewGuid();

		Stopwatch? timer = _trace ? Stopwatch.StartNew() : null;

		if (timer is not null)
		{
			_logger.Information(
				"Start to execute sql query {QueryId} from {File} from member {Member}",
				queryId,
				sourceFilePath,
				memberName);
		}

		try
		{
			var result = await Connection.QueryAsync<T>(command);
			return result.AsList();
		}
		catch (Exception ex)
		{
			_logger.Error(ex, "Failed to execute query");
			throw;
		}
		finally
		{
			if (timer is not null)
			{
				_logger.Information(
					"Sql query {QueryId} from {File} from member {Member} executed in {Elapsed}",
					queryId,
					sourceFilePath,
					memberName,
					timer.Elapsed);
			}
		}
	}

	public void Dispose()
	{
		Transaction?.Dispose();
		Connection.Dispose();
	}

	~PostgresSession()
	{
		Dispose();
	}
}
