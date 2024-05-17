using WebApi.DataAccess;
using WebApi.Models;

namespace WebApi.Repositories;

public interface IUserRepository : IRepository
{
	Task<IReadOnlyList<User>> GetByIds(IPostgresSession session, IReadOnlyCollection<Guid> ids, CancellationToken ct);
	Task<User?> GetById(IPostgresSession session, Guid id, CancellationToken ct);
	Task<int> Insert(IPostgresSession session, User user, CancellationToken ct);
}

public class UserRepository : IUserRepository
{
	private const string _tableName = "\"Users\"";

	public async Task<IReadOnlyList<User>> GetByIds(IPostgresSession session, IReadOnlyCollection<Guid> ids, CancellationToken ct)
	{
		var args = new { ids };
		const string sql = @$"
			SELECT ""{nameof(User.Id)}"" FROM {_tableName}
			WHERE ""{nameof(User.Id)}"" = ANY(@{nameof(args.ids)});
		";

		var result = await session.QueryAsync<User>(sql, args, ct);
		return result;
	}

	public async Task<User?> GetById(IPostgresSession session, Guid id, CancellationToken ct)
	{
		var sql = @$"
			SELECT ""{nameof(User.Id)}"" FROM {_tableName}
			WHERE ""{nameof(User.Id)}"" = '{id}'
			LIMIT 1;
		";

		var result = await session.QueryFirstOrDefaultAsync<User>(sql, ct);
		return result;
	}

	public async Task<int> Insert(IPostgresSession session, User user, CancellationToken ct)
	{
		const string sql = @$"
			INSERT INTO {_tableName} (
				""{nameof(User.Id)}""
			)
			VALUES(
				@{nameof(user.Id)}
			);
		";

		var result = await session.ExecuteAsync(sql, user, ct);
		return result;
	}
}
