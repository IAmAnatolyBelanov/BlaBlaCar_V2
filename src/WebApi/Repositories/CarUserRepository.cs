using WebApi.DataAccess;

namespace WebApi.Repositories;

public interface ICarUserRepository : IRepository
{
	Task<int> Insert(IPostgresSession session, Guid carId, Guid userId, CancellationToken ct);
}

public class CarUserRepository : ICarUserRepository
{
	private const string _tableName = "\"Cars_Users\"";

	public async Task<int> Insert(IPostgresSession session, Guid carId, Guid userId, CancellationToken ct)
	{
		var sql = $@"
			INSERT INTO {_tableName} (
				""CarId""
				, ""UserId""
			)
			VALUES (
				'{carId}'
				, '{userId}'
			);
		";

		var result = await session.ExecuteAsync(sql, ct);
		return result;
	}
}