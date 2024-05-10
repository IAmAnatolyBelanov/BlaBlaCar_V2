using WebApi.DataAccess;
using WebApi.Models;

namespace WebApi.Repositories;

public class CloudApiResponseInfoRepository
{
	private const string _tableName = "\"CloudApiResponseInfos\"";

	public async Task<IReadOnlyList<CloudApiResponseInfo>> Select(PostgresSession session, int limit, int offset, CancellationToken ct)
	{
		var sql = @$"
			SELECT * FROM {_tableName}
			ORDER BY ""{nameof(CloudApiResponseInfo.Created)}"" DESC
			LIMIT {limit} OFFSET {offset};
		";

		var result = await session.QueryAsync<CloudApiResponseInfo>(sql, ct);

		return result;
	}
}
