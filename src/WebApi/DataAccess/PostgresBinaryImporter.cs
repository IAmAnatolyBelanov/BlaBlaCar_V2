using Npgsql;
using NpgsqlTypes;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace WebApi.DataAccess;

public interface IPostgresBinaryImporter : IDisposable
{
	Task StartRow(CancellationToken ct);

	Task WriteValueOrNull<T>(T? value, CancellationToken ct);

	Task WriteValueOrNull<T>(T? value, NpgsqlDbType npgsqlDbType, CancellationToken ct);

	Task WriteValueOrNull<T>(T? value, string dataTypeName, CancellationToken ct);

	Task Write<T>(T value, CancellationToken ct);

	Task Write<T>(T value, NpgsqlDbType npgsqlDbType, CancellationToken ct);

	Task Write<T>(T value, string dataTypeName, CancellationToken ct);

	ValueTask<ulong> Complete(CancellationToken ct, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "");
}

public class PostgresBinaryImporter : IDisposable, IPostgresBinaryImporter
{
	private readonly Guid _id = Guid.NewGuid();
	private readonly ILogger _logger;
	private readonly bool _trace;

	private readonly Stopwatch _importerTimer = Stopwatch.StartNew();
	private readonly NpgsqlBinaryImporter _importer;

	public PostgresBinaryImporter(NpgsqlBinaryImporter importer, bool trace)
	{
		_importer = importer;
		_trace = trace;

		_logger = Log.ForContext<PostgresBinaryImporter>().ForContext("PostgresBinaryImporterId", _id);
		_logger.Information("Opened postgres binary importer {Id}", _id);
	}

	public async Task StartRow(CancellationToken ct)
	{
		await _importer.StartRowAsync(ct);
	}

	public async Task WriteValueOrNull<T>(T? value, CancellationToken ct)
	{
		if (value is null)
			await _importer.WriteNullAsync(ct);
		else
			await _importer.WriteAsync(value, ct);
	}

	public async Task WriteValueOrNull<T>(T? value, NpgsqlDbType npgsqlDbType, CancellationToken ct)
	{
		if (value is null)
			await _importer.WriteNullAsync(ct);
		else
			await _importer.WriteAsync(value, npgsqlDbType, ct);
	}

	public async Task WriteValueOrNull<T>(T? value, string dataTypeName, CancellationToken ct)
	{
		if (value is null)
			await _importer.WriteNullAsync(ct);
		else
			await _importer.WriteAsync(value, dataTypeName, ct);
	}

	public async Task Write<T>(T value, CancellationToken ct)
	{
		await _importer.WriteAsync(value, ct);
	}

	public async Task Write<T>(T value, NpgsqlDbType npgsqlDbType, CancellationToken ct)
	{
		await _importer.WriteAsync(value, npgsqlDbType, ct);
	}

	public async Task Write<T>(T value, string dataTypeName, CancellationToken ct)
	{
		await _importer.WriteAsync(value, dataTypeName, ct);
	}

	public async ValueTask<ulong> Complete(CancellationToken ct, [CallerFilePath] string sourceFilePath = "", [CallerMemberName] string memberName = "")
	{
		var operationId = Guid.NewGuid();

		Stopwatch? timer = _trace ? Stopwatch.StartNew() : null;

		if (timer is not null)
		{
			_logger.Information(
				"Start to complete {OperationId} binary importer from {File} from member {Member}",
				operationId,
				sourceFilePath,
				memberName);
		}

		try
		{
			var result = await _importer.CompleteAsync(ct);
			return result;
		}
		catch (Exception ex)
		{
			_logger.Error(
				ex,
				"Failed to complete {OperationId} binary importer from {File} from member {Member}",
				operationId,
				sourceFilePath,
				memberName);
			throw;
		}
		finally
		{
			if (timer is not null)
			{
				_logger.Information(
					"Binary importer operation {OperationId} from {File} from member {Member} executed in {Elapsed}",
					operationId,
					sourceFilePath,
					memberName,
					timer.Elapsed);
			}
		}
	}

	public void Dispose()
	{
		_importer.Dispose();
		_importerTimer.Stop();


		if (_importerTimer.IsRunning)
		{
			_importerTimer.Stop();
			_logger.Information("Disposed postgres binary importer {Id} after {LifeTime}", _id, _importerTimer.Elapsed);
		}
	}

	~PostgresBinaryImporter()
	{
		Dispose();
	}
}