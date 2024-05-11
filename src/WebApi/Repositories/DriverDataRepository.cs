using WebApi.DataAccess;
using WebApi.Models;

namespace WebApi.Repositories;

public interface IDriverDataRepository : IRepository
{
	Task<int> Insert(IPostgresSession session, DriverData data, CancellationToken ct);

	Task<DriverData?> GetByDrivingLicense(
		IPostgresSession session,
		int licenseSeries,
		int licenseNumber,
		CancellationToken ct);

	Task<DriverData?> GetByUserId(
		IPostgresSession session,
		Guid userId,
		CancellationToken ct);
}

public class DriverDataRepository : IDriverDataRepository
{
	private const string _tableName = "\"DriverDatas\"";

	public async Task<int> Insert(IPostgresSession session, DriverData data, CancellationToken ct)
	{
		const string sql = @$"
			INSERT INTO {_tableName} ({_fullColumnsList})
			VALUES(
				@{nameof(data.Id)}
				, @{nameof(data.UserId)}
				, @{nameof(data.LicenseSeries)}
				, @{nameof(data.LicenseNumber)}
				, @{nameof(data.Issuance)}
				, @{nameof(data.ValidTill)}
				, @{nameof(data.Categories)}
				, @{nameof(data.BirthDate)}
				, @{nameof(data.Created)}
				, @{nameof(data.IsValid)}
				, @{nameof(data.LastCheckDate)}
			);
		";

		var result = await session.ExecuteAsync(sql, data, ct);
		return result;
	}

	public async Task<DriverData?> GetByDrivingLicense(
		IPostgresSession session,
		int licenseSeries,
		int licenseNumber,
		CancellationToken ct)
	{
		var sql = @$"
			SELECT {_fullColumnsList}
			FROM {_tableName}
			WHERE
				""{nameof(DriverData.LicenseSeries)}"" = {licenseSeries}
				AND ""{nameof(DriverData.LicenseNumber)}"" = {licenseNumber}
				AND ""{nameof(DriverData.IsValid)}"" = TRUE
			ORDER BY ""{nameof(DriverData.Issuance)}"" DESC
			LIMIT 1;
		";

		var result = await session.QueryFirstOrDefaultAsync<DriverData>(sql, ct);
		return result;
	}


	public async Task<DriverData?> GetByUserId(
		IPostgresSession session,
		Guid userId,
		CancellationToken ct)
	{
		var sql = @$"
			SELECT {_fullColumnsList}
			FROM {_tableName}
			WHERE
				""{nameof(DriverData.UserId)}"" = '{userId}'
				AND ""{nameof(DriverData.IsValid)}"" = TRUE
			ORDER BY ""{nameof(DriverData.Issuance)}"" DESC
			LIMIT 1;
		";

		var result = await session.QueryFirstOrDefaultAsync<DriverData>(sql, ct);
		return result;
	}

	private const string _fullColumnsList = @$"
		""{nameof(DriverData.Id)}""
			, ""{nameof(DriverData.UserId)}""
			, ""{nameof(DriverData.LicenseSeries)}""
			, ""{nameof(DriverData.LicenseNumber)}""
			, ""{nameof(DriverData.Issuance)}""
			, ""{nameof(DriverData.ValidTill)}""
			, ""{nameof(DriverData.Categories)}""
			, ""{nameof(DriverData.BirthDate)}""
			, ""{nameof(DriverData.Created)}""
			, ""{nameof(DriverData.IsValid)}""
			, ""{nameof(DriverData.LastCheckDate)}""
	";
}
