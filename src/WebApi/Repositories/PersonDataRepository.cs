using WebApi.DataAccess;
using WebApi.Models;

namespace WebApi.Repositories;

public interface IPersonDataRepository : IRepository
{
	Task<int> Insert(IPostgresSession session, PersonData data, CancellationToken ct);

	Task<PersonData?> GetByPassport(
		IPostgresSession session,
		int passportSeries,
		int passportNumber,
		CancellationToken ct);

	Task<PersonData?> GetByUserId(
		IPostgresSession session,
		Guid userId,
		CancellationToken ct);

	Task<PersonData?> GetById(
		IPostgresSession session,
		Guid id,
		CancellationToken ct);

	Task<int> UpdateUserId(
		IPostgresSession session,
		Guid personDataId,
		Guid userId,
		CancellationToken ct);

	Task<int> DisablePersonData(
		IPostgresSession session,
		Guid personDataId,
		CancellationToken ct);
}

public class PersonDataRepository : IPersonDataRepository
{
	private const string _tableName = "\"PersonDatas\"";

	public async Task<int> Insert(IPostgresSession session, PersonData data, CancellationToken ct)
	{
		const string sql = @$"
			INSERT INTO {_tableName} ({_fullColumnsList})
			VALUES (
				@{nameof(data.Id)}
				, @{nameof(data.UserId)}
				, @{nameof(data.PassportSeries)}
				, @{nameof(data.PassportNumber)}
				, @{nameof(data.FirstName)}
				, @{nameof(data.LastName)}
				, @{nameof(data.SecondName)}
				, @{nameof(data.BirthDate)}
				, @{nameof(data.Inn)}
				, @{nameof(data.IsPassportValid)}
				, @{nameof(data.WasCheckedAtLeastOnce)}
				, @{nameof(data.LastCheckPassportDate)}
				, @{nameof(data.Created)}
			);
		";

		var result = await session.ExecuteAsync(sql, data, ct);
		return result;
	}

	public async Task<PersonData?> GetByPassport(
		IPostgresSession session,
		int passportSeries,
		int passportNumber,
		CancellationToken ct)
	{
		var sql = @$"
			SELECT {_fullColumnsList}
			FROM {_tableName}
			WHERE
				""{nameof(PersonData.PassportSeries)}"" = {passportSeries}
				AND ""{nameof(PersonData.PassportNumber)}"" = {passportNumber}
				AND ""{nameof(PersonData.IsPassportValid)}"" = TRUE
			ORDER BY ""{nameof(PersonData.Created)}"" DESC
			LIMIT 1;
		";

		var result = await session.QueryFirstOrDefaultAsync<PersonData>(sql, ct);
		return result;
	}

	public async Task<PersonData?> GetByUserId(
		IPostgresSession session,
		Guid userId,
		CancellationToken ct)
	{
		var sql = @$"
			SELECT {_fullColumnsList}
			FROM {_tableName}
			WHERE
				""{nameof(PersonData.UserId)}"" = '{userId}'
				AND ""{nameof(PersonData.IsPassportValid)}"" = TRUE
			ORDER BY ""{nameof(PersonData.Created)}"" DESC
			LIMIT 1;
		";

		var result = await session.QueryFirstOrDefaultAsync<PersonData>(sql, ct);
		return result;
	}

	public async Task<PersonData?> GetById(
		IPostgresSession session,
		Guid id,
		CancellationToken ct)
	{
		var sql = @$"
			SELECT {_fullColumnsList}
			FROM {_tableName}
			WHERE
				""{nameof(PersonData.Id)}"" = '{id}'
				AND ""{nameof(PersonData.IsPassportValid)}"" = TRUE
			LIMIT 1;
		";

		var result = await session.QueryFirstOrDefaultAsync<PersonData>(sql, ct);
		return result;
	}

	public async Task<int> UpdateUserId(
		IPostgresSession session,
		Guid personDataId,
		Guid userId,
		CancellationToken ct)
	{
		var sql = @$"
			UPDATE {_tableName}
			SET ""{nameof(PersonData.UserId)}"" = '{userId}'
			WHERE ""{nameof(PersonData.Id)}"" = '{personDataId}';
		";

		var result = await session.ExecuteAsync(sql, ct);
		return result;
	}

	public async Task<int> DisablePersonData(
		IPostgresSession session,
		Guid personDataId,
		CancellationToken ct)
	{
		var sql = @$"
			UPDATE {_tableName}
			SET ""{nameof(PersonData.IsPassportValid)}"" = FALSE
			WHERE ""{nameof(PersonData.Id)}"" = '{personDataId}';
		";

		var result = await session.ExecuteAsync(sql, ct);
		return result;
	}

	private const string _fullColumnsList = @$"
		""{nameof(PersonData.Id)}""
		, ""{nameof(PersonData.UserId)}""
		, ""{nameof(PersonData.PassportSeries)}""
		, ""{nameof(PersonData.PassportNumber)}""
		, ""{nameof(PersonData.FirstName)}""
		, ""{nameof(PersonData.LastName)}""
		, ""{nameof(PersonData.SecondName)}""
		, ""{nameof(PersonData.BirthDate)}""
		, ""{nameof(PersonData.Inn)}""
		, ""{nameof(PersonData.IsPassportValid)}""
		, ""{nameof(PersonData.WasCheckedAtLeastOnce)}""
		, ""{nameof(PersonData.LastCheckPassportDate)}""
		, ""{nameof(PersonData.Created)}""";
}
