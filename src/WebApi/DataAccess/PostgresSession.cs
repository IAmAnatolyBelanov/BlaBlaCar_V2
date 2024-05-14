using Dapper;
using Npgsql;
using System.Data;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace WebApi.DataAccess;

public interface IPostgresSession : IDisposable
{
	IPostgresSession BeginTransaction();
	IPostgresSession StartTrace();

	Task<List<T>> QueryAsync<T>(string sql, CancellationToken ct,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "");

	Task<List<T>> QueryAsync<T>(string sql, object param, CancellationToken ct,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "");

	Task<List<T>> QueryAsync<T>(
		string sql,
		object? param = null,
		int? commandTimeout = null,
		CommandType? commandType = null,
		CancellationToken ct = default,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "");

	Task<List<T>> QueryAsync<T>(
		CommandDefinition command,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "");

	Task<int> ExecuteAsync<T>(string sql, CancellationToken ct,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "");

	Task<int> ExecuteAsync(string sql, object param, CancellationToken ct,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "");

	Task<int> ExecuteAsync(
		string sql,
		object? param = null,
		int? commandTimeout = null,
		CommandType? commandType = null,
		CancellationToken ct = default,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "");

	Task<int> ExecuteAsync(
		CommandDefinition command,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "");

	public Task<T?> QueryFirstOrDefaultAsync<T>(string sql, CancellationToken ct,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "");

	Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object param, CancellationToken ct,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "");

	Task<T?> QueryFirstOrDefaultAsync<T>(
		string sql,
		object? param = null,
		int? commandTimeout = null,
		CommandType? commandType = null,
		CancellationToken ct = default,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "");

	Task<T?> QueryFirstOrDefaultAsync<T>(
		CommandDefinition command,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "");

	Task CommitAsync(
		CancellationToken ct,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "");

	Task<IPostgresBinaryImporter> BeginBinaryImport(string copyFromCommand, CancellationToken ct);
}

public class PostgresSession : IDisposable, IPostgresSession
{
	private readonly Guid _id = Guid.NewGuid();
	private readonly ILogger _logger;
	private readonly Stopwatch _sessionTimer = Stopwatch.StartNew();

	private bool _trace;
	private readonly NpgsqlConnection _connection;
	private readonly NpgsqlDataSource _dataSource;
	private NpgsqlTransaction? _transaction;

	public PostgresSession(NpgsqlConnection connection, NpgsqlDataSource dataSource, bool beginTransaction = false, bool trace = false)
	{
		_connection = connection;
		_dataSource = dataSource;

		if (beginTransaction)
			BeginTransaction();

		if (trace)
			StartTrace();

		_logger = Log.ForContext<PostgresSession>().ForContext("PostgresConnectionId", _id);
		_logger.Information("Opened postgres connection {Id}", _id);
	}

	public IPostgresSession BeginTransaction()
	{
		if (_transaction is not null)
			return this;
		lock (this)
		{
			if (_transaction is not null)
				return this;

			_transaction = _connection.BeginTransaction();
			return this;
		}
	}

	public IPostgresSession StartTrace()
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
			transaction: _transaction,
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
			var result = await _connection.QueryAsync<T>(command);
			return result.AsList();
		}
		catch (Exception ex)
		{
			_logger.Error(
				ex,
				"Failed to execute query {QueryId} from {File} from member {Member}",
				queryId,
				sourceFilePath,
				memberName);
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

	public Task<int> ExecuteAsync<T>(string sql, CancellationToken ct,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "")
		=> ExecuteAsync(
			sql: sql,
			ct: ct,
			param: null,
			commandTimeout: null,
			commandType: null,
			sourceFilePath: sourceFilePath,
			memberName: memberName);

	public Task<int> ExecuteAsync(string sql, object param, CancellationToken ct,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "")
		=> ExecuteAsync(
			sql: sql,
			param: param,
			ct: ct,
			commandTimeout: null,
			commandType: null,
			sourceFilePath: sourceFilePath,
			memberName: memberName);

	public Task<int> ExecuteAsync(
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
			transaction: _transaction,
			commandTimeout: commandTimeout,
			commandType: commandType,
			cancellationToken: ct
		);

		var result = ExecuteAsync(commandDefinition, sourceFilePath, memberName);
		return result;
	}

	public async Task<int> ExecuteAsync(
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
			var result = await _connection.ExecuteAsync(command);
			return result;
		}
		catch (Exception ex)
		{
			_logger.Error(
				ex,
				"Failed to execute query {QueryId} from {File} from member {Member}",
				queryId,
				sourceFilePath,
				memberName);
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

	public Task<T?> QueryFirstOrDefaultAsync<T>(string sql, CancellationToken ct,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "")
	=> QueryFirstOrDefaultAsync<T>(
		sql: sql,
		ct: ct,
		param: null,
		commandTimeout: null,
		commandType: null,
		sourceFilePath: sourceFilePath,
		memberName: memberName);

	public Task<T?> QueryFirstOrDefaultAsync<T>(string sql, object param, CancellationToken ct,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "")
		=> QueryFirstOrDefaultAsync<T>(
			sql: sql,
			param: param,
			ct: ct,
			commandTimeout: null,
			commandType: null,
			sourceFilePath: sourceFilePath,
			memberName: memberName);

	public Task<T?> QueryFirstOrDefaultAsync<T>(
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
			transaction: _transaction,
			commandTimeout: commandTimeout,
			commandType: commandType,
			cancellationToken: ct
		);

		var result = QueryFirstOrDefaultAsync<T>(commandDefinition, sourceFilePath, memberName);
		return result;
	}

	public async Task<T?> QueryFirstOrDefaultAsync<T>(
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
			var result = await _connection.QueryFirstOrDefaultAsync<T>(command);
			return result;
		}
		catch (Exception ex)
		{
			_logger.Error(
				ex,
				"Failed to execute query {QueryId} from {File} from member {Member}",
				queryId,
				sourceFilePath,
				memberName);
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

	public async Task CommitAsync(
		CancellationToken ct,
		[CallerFilePath] string sourceFilePath = "",
		[CallerMemberName] string memberName = "")
	{
		var commitId = Guid.NewGuid();

		Stopwatch? timer = _trace ? Stopwatch.StartNew() : null;

		if (timer is not null)
		{
			_logger.Information(
				"Start to commit {CommitId} from {File} from member {Member}",
				commitId,
				sourceFilePath,
				memberName);
		}

		try
		{
			// Может быть null. Должно управляться слоем бизнес-логики.
			await _transaction!.CommitAsync(ct);
		}
		catch (Exception ex)
		{
			_logger.Error(
				ex,
				"Failed to commit {CommitId} from {File} from member {Member}",
				commitId,
				sourceFilePath,
				memberName);
			throw;
		}
		finally
		{
			if (timer is not null)
			{
				_logger.Information(
					"Commit {CommitId} from {File} from member {Member} executed in {Elapsed}",
					commitId,
					sourceFilePath,
					memberName,
					timer.Elapsed);
			}
		}
	}

	public async Task<IPostgresBinaryImporter> BeginBinaryImport(string copyFromCommand, CancellationToken ct)
	{
		var importer = await _connection.BeginBinaryImportAsync(copyFromCommand, ct);
		var result = new PostgresBinaryImporter(importer, _trace);
		return result;
	}

	public void Dispose()
	{
		_transaction?.Dispose();
		_connection.Dispose();
		_dataSource.Dispose();

		if (_sessionTimer.IsRunning)
		{
			_sessionTimer.Stop();
			_logger.Information("Disposed postgres connection {Id} after {LifeTime}", _id, _sessionTimer.Elapsed);
		}
	}

	~PostgresSession()
	{
		Dispose();
	}
}
