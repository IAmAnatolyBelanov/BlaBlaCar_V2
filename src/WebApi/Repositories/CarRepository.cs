using WebApi.DataAccess;
using WebApi.Models;

namespace WebApi.Repositories;

public interface ICarRepository : IRepository
{
	Task<int> Insert(IPostgresSession session, Car car, CancellationToken ct);
	Task<IReadOnlyList<Car>> GetByUserId(IPostgresSession session, Guid userId, CancellationToken ct);
}

public class CarRepository : ICarRepository
{
	private const string _tableName = "\"Cars\"";
	private const string _carsUsersTableName = "\"Cars_Users\"";

	public async Task<int> Insert(IPostgresSession session, Car car, CancellationToken ct)
	{
		const string sql = $@"
			INSERT INTO {_tableName} ({_fullColumnsList})
			VALUES (
				@{nameof(car.Id)}
				, @{nameof(car.Created)}
				, @{nameof(car.Vin)}
				, @{nameof(car.RegistrationNumber)}
				, @{nameof(car.DoesVinAndRegistrationNumberMatches)}
				, @{nameof(car.Name)}
				, @{nameof(car.SeatsCount)}
				, @{nameof(car.IsDeleted)}
			);
		";

		var result = await session.ExecuteAsync(sql, car, ct);
		return result;
	}

	public async Task<IReadOnlyList<Car>> GetByUserId(IPostgresSession session, Guid userId, CancellationToken ct)
	{
		var sql = $@"
			SELECT car.""{nameof(Car.Id)}""
				, car.""{nameof(Car.Created)}""
				, car.""{nameof(Car.Vin)}""
				, car.""{nameof(Car.RegistrationNumber)}""
				, car.""{nameof(Car.DoesVinAndRegistrationNumberMatches)}""
				, car.""{nameof(Car.Name)}""
				, car.""{nameof(Car.SeatsCount)}""
				, car.""{nameof(Car.IsDeleted)}""
			FROM {_tableName} car
			INNER JOIN {_carsUsersTableName} car_user ON car_user.""CarId"" = car.""{nameof(Car.Id)}""
			WHERE
				car_user.""UserId"" = '{userId}'
				AND car.""{nameof(Car.IsDeleted)}"" = FALSE
			ORDER BY
				car.""{nameof(Car.Name)}"" ASC
				, car.""{nameof(Car.RegistrationNumber)}"" ASC;
		";

		var result = await session.QueryAsync<Car>(sql, ct);
		return result;
	}

	private const string _fullColumnsList = $@"
		""{nameof(Car.Id)}""
		, ""{nameof(Car.Created)}""
		, ""{nameof(Car.Vin)}""
		, ""{nameof(Car.RegistrationNumber)}""
		, ""{nameof(Car.DoesVinAndRegistrationNumberMatches)}""
		, ""{nameof(Car.Name)}""
		, ""{nameof(Car.SeatsCount)}""
		, ""{nameof(Car.IsDeleted)}""
	";
}
