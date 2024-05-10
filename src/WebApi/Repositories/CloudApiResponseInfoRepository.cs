using WebApi.DataAccess;
using WebApi.Models;

namespace WebApi.Repositories;

public interface ICloudApiResponseInfoRepository : IRepository
{
	Task<IReadOnlyList<CloudApiResponseInfo>> Get(IPostgresSession session, int limit, int offset, CancellationToken ct);
	Task<int> Insert(IPostgresSession session, CloudApiResponseInfo info, CancellationToken ct);
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
			INSERT INTO {_tableName} (
				""{nameof(CloudApiResponseInfo.Id)}""
				, ""{nameof(CloudApiResponseInfo.Created)}""
				, ""{nameof(CloudApiResponseInfo.Request)}""
				, ""{nameof(CloudApiResponseInfo.RequestBasePath)}""
				, ""{nameof(CloudApiResponseInfo.Response)}""
			)
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
}
