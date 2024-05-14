using WebApi.DataAccess;
using WebApi.Models;

namespace WebApi.Repositories;

public interface IReservationRepository : IRepository
{
	Task<int> Insert(IPostgresSession session, Reservation reservation, CancellationToken ct);
	Task<IReadOnlyList<Reservation>> GetByFilter(IPostgresSession session, ReservationDbFilter filter, CancellationToken ct);
}

public class ReservationRepository : IReservationRepository
{
	private const string _tableName = "\"Reservations\"";

	public async Task<int> Insert(IPostgresSession session, Reservation reservation, CancellationToken ct)
	{
		const string sql = $@"
			INSERT INTO {_tableName} ({_fullColumnsList})
			VALUES (
				@{nameof(reservation.Id)}
				, @{nameof(reservation.RideId)}
				, @{nameof(reservation.PassengerId)}
				, @{nameof(reservation.PeopleCount)}
				, @{nameof(reservation.WaypointFromId)}
				, @{nameof(reservation.WaypointToId)}
				, @{nameof(reservation.IsDeleted)}
				, @{nameof(reservation.Created)}
			);
		";

		var result = await session.ExecuteAsync(sql, reservation, ct);
		return result;
	}

	public async Task<IReadOnlyList<Reservation>> GetByFilter(IPostgresSession session, ReservationDbFilter filter, CancellationToken ct)
	{
		var sql = $@"
			SELECT {_fullColumnsList} FROM {_tableName}
			{BuildWhereSection(filter)}
			ORDER BY ""{nameof(Reservation.Created)}"" DESC
			OFFSET @{nameof(filter.Offset)}
			LIMIT @{nameof(filter.Limit)};
		";

		var result = await session.QueryAsync<Reservation>(sql, filter, ct);
		return result;
	}

	private string BuildWhereSection(ReservationDbFilter filter, string? alias = null)
	{
		if (!alias.IsNullOrEmpty() && !alias.EndsWith('.'))
			alias += '.';

		var clauses = BuildClauses(filter, alias)
			.Select(x => $"({x})")
			.ToArray();

		if (clauses.Length == 0)
			return string.Empty;

		var result = $"WHERE\n\t{string.Join("\n\tAND ", clauses)}";
		return result;
	}

	private IEnumerable<string> BuildClauses(ReservationDbFilter filter, string? aliasWithDot)
	{
		if (filter.ReservationId.HasValue)
			yield return $"{aliasWithDot}\"{nameof(Reservation.Id)}\" = @{nameof(ReservationDbFilter.ReservationId)}";

		if (filter.RideId.HasValue)
			yield return $"{aliasWithDot}\"{nameof(Reservation.RideId)}\" = @{nameof(ReservationDbFilter.RideId)}";

		if (filter.PassengerId.HasValue)
			yield return $"{aliasWithDot}\"{nameof(Reservation.PassengerId)}\" = @{nameof(ReservationDbFilter.PassengerId)}";

		if (filter.HideDeleted)
			yield return $"{aliasWithDot}\"{nameof(Reservation.IsDeleted)}\" = FALSE";
	}

	private const string _fullColumnsList = $@"
		""{nameof(Reservation.Id)}""
		, ""{nameof(Reservation.RideId)}""
		, ""{nameof(Reservation.PassengerId)}""
		, ""{nameof(Reservation.PeopleCount)}""
		, ""{nameof(Reservation.WaypointFromId)}""
		, ""{nameof(Reservation.WaypointToId)}""
		, ""{nameof(Reservation.IsDeleted)}""
		, ""{nameof(Reservation.Created)}""
	";
}
