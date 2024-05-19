using WebApi.DataAccess;
using WebApi.Models;

namespace WebApi.Repositories;

public interface IReservationRepository : IRepository
{
	Task<int> InsertReservation(IPostgresSession session, Reservation reservation, CancellationToken ct);
	Task<IReadOnlyList<Reservation>> GetReservationsByFilter(IPostgresSession session, ReservationDbFilter filter, CancellationToken ct);
	Task<ulong> BulkInsertAffectedLegs(IPostgresSession session, Guid reservationId, IReadOnlyList<Guid> legIds, CancellationToken ct);
	Task<int> CancelReservation(IPostgresSession session, Guid reservationId, CancellationToken ct);
}

public class ReservationRepository : IReservationRepository
{
	private const string _reservationTableName = "\"Reservations\"";
	private const string _affectedLegsTableName = "\"AffectedByReservationsLegs\"";

	public async Task<int> InsertReservation(IPostgresSession session, Reservation reservation, CancellationToken ct)
	{
		const string sql = $@"
			INSERT INTO {_reservationTableName} ({_fullColumnsList})
			VALUES (
				@{nameof(reservation.Id)}
				, @{nameof(reservation.RideId)}
				, @{nameof(reservation.PassengerId)}
				, @{nameof(reservation.PeopleCount)}
				, @{nameof(reservation.LegId)}
				, @{nameof(reservation.IsDeleted)}
				, @{nameof(reservation.Created)}
			);
		";

		var result = await session.ExecuteAsync(sql, reservation, ct);
		return result;
	}

	public async Task<IReadOnlyList<Reservation>> GetReservationsByFilter(IPostgresSession session, ReservationDbFilter filter, CancellationToken ct)
	{
		var sql = $@"
			SELECT {_fullColumnsList} FROM {_reservationTableName}
			{BuildWhereSection(filter)}
			ORDER BY ""{nameof(Reservation.Created)}"" DESC
			OFFSET @{nameof(filter.Offset)}
			LIMIT @{nameof(filter.Limit)};
		";

		var result = await session.QueryAsync<Reservation>(sql, filter, ct);
		return result;
	}

	public async Task<ulong> BulkInsertAffectedLegs(IPostgresSession session, Guid reservationId, IReadOnlyList<Guid> legIds, CancellationToken ct)
	{
		const string sql = $@"
			COPY {_affectedLegsTableName} (
				""{nameof(AffectedByReservationLeg.ReservationId)}""
				, ""{nameof(AffectedByReservationLeg.LegId)}""
			)
			FROM STDIN (FORMAT BINARY);
		";

		using var importer = await session.BeginBinaryImport(sql, ct);
		for (int i = 0; i < legIds.Count; i++)
		{
			var legId = legIds[i];

			await importer.StartRow(ct);

			await importer.Write(reservationId, ct);
			await importer.Write(legId, ct);
		}

		var result = await importer.Complete(ct);
		return result;
	}

	public async Task<int> CancelReservation(IPostgresSession session, Guid reservationId, CancellationToken ct)
	{
		var sql = $@"
			UPDATE {_reservationTableName}
			SET ""{nameof(Reservation.IsDeleted)}"" = TRUE
			WHERE ""{nameof(Reservation.Id)}"" = '{reservationId}';
		";

		var result = await session.ExecuteAsync(sql, ct);
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
		, ""{nameof(Reservation.LegId)}""
		, ""{nameof(Reservation.IsDeleted)}""
		, ""{nameof(Reservation.Created)}""
	";
}
