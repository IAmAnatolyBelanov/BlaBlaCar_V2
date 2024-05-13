using NpgsqlTypes;
using WebApi.DataAccess;
using WebApi.Models;

namespace WebApi.Repositories;

public interface ICloudApiResponseInfoRepository : IRepository
{
	Task<IReadOnlyList<CloudApiResponseInfo>> Get(IPostgresSession session, int limit, int offset, CancellationToken ct);
	Task<int> Insert(IPostgresSession session, CloudApiResponseInfo info, CancellationToken ct);
	Task<ulong> BulkInsert(IPostgresSession session, IReadOnlyList<CloudApiResponseInfo> infos, CancellationToken ct);
}

public class CloudApiResponseInfoRepository : ICloudApiResponseInfoRepository
{
	private const string _tableName = "\"CloudApiResponseInfos\"";

	public async Task<IReadOnlyList<CloudApiResponseInfo>> Get(IPostgresSession session, int limit, int offset, CancellationToken ct)
	{
		var sql = @$"
			SELECT * FROM {_tableName}
			ORDER BY ""{nameof(CloudApiResponseInfo.Created)}"" DESC
			LIMIT {limit} OFFSET {offset};
		";

		var result = await session.QueryAsync<CloudApiResponseInfo>(sql, ct);

		return result;
	}

	public async Task<int> Insert(IPostgresSession session, CloudApiResponseInfo info, CancellationToken ct)
	{
		const string sql = @$"
			INSERT INTO {_tableName} ({_fullColumnsList})
			VALUES (
				@{nameof(info.Id)}
				, @{nameof(info.Created)}
				, @{nameof(info.Request)}
				, @{nameof(info.RequestBasePath)}
				, @{nameof(info.Response)}::jsonb
			);
		";

		var result = await session.ExecuteAsync(sql, info, ct);
		return result;
	}

	public async Task<ulong> BulkInsert(IPostgresSession session, IReadOnlyList<CloudApiResponseInfo> infos, CancellationToken ct)
	{
		const string sql = $@"
			COPY {_tableName} ({_fullColumnsList})
			FROM STDIN (FORMAT BINARY);
		";

		using var importer = await session.BeginBinaryImport(sql, ct);
		for (int i = 0; i < infos.Count; i++)
		{
			var info = infos[i];

			await importer.StartRow(ct);

			await importer.Write(info.Id, ct);
			await importer.Write(info.Created, ct);
			await importer.Write(info.Request, ct);
			await importer.Write(info.RequestBasePath, ct);
			await importer.Write(info.Response, NpgsqlDbType.Jsonb, ct);
		}

		var result = await importer.Complete(ct);
		return result;
	}

	private const string _fullColumnsList = $@"
		""{nameof(CloudApiResponseInfo.Id)}""
		, ""{nameof(CloudApiResponseInfo.Created)}""
		, ""{nameof(CloudApiResponseInfo.Request)}""
		, ""{nameof(CloudApiResponseInfo.RequestBasePath)}""
		, ""{nameof(CloudApiResponseInfo.Response)}""
	";
}
