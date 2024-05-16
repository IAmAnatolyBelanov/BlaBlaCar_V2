using NpgsqlTypes;
using WebApi.DataAccess;
using WebApi.Models;

namespace WebApi.Repositories;

public interface IWaypointRepository : IRepository
{
	Task<ulong> BulkInsert(IPostgresSession session, IReadOnlyList<Waypoint> waypoints, CancellationToken ct);
	Task<IReadOnlyList<Waypoint>> GetByRideId(IPostgresSession session, Guid rideId, CancellationToken ct);
}

public class WaypointRepository : IWaypointRepository
{
	private const string _tableName = "\"Waypoints\"";

	public async Task<ulong> BulkInsert(IPostgresSession session, IReadOnlyList<Waypoint> waypoints, CancellationToken ct)
	{
		const string sql = $@"
			COPY {_tableName} ({_fullColumnsList})
			FROM STDIN (FORMAT BINARY);
		";

		using var importer = await session.BeginBinaryImport(sql, ct);
		for (int i = 0; i < waypoints.Count; i++)
		{
			var waypoint = waypoints[i];

			await importer.StartRow(ct);

			await importer.Write(waypoint.Id, ct);
			await importer.Write(waypoint.RideId, ct);
			await importer.Write(waypoint.Point, NpgsqlDbType.Geography, ct);
			await importer.Write(waypoint.FullName, ct);
			await importer.Write(waypoint.NameToCity, ct);
			await importer.Write(waypoint.Arrival, ct);
			await importer.Write(waypoint.Departure, ct);
			await importer.WriteValueOrNull(waypoint.PreviousWaypointId, ct);
			await importer.WriteValueOrNull(waypoint.NextWaypointId, ct);
		}

		var result = await importer.Complete(ct);
		return result;
	}

	public async Task<IReadOnlyList<Waypoint>> GetByRideId(IPostgresSession session, Guid rideId, CancellationToken ct)
	{
		var sql = $@"
			SELECT {_fullColumnsList} FROM {_tableName}
			WHERE ""{nameof(Waypoint.RideId)}"" = '{rideId}'
			ORDER BY
				""{nameof(Waypoint.Arrival)}"" ASC
				, ""{nameof(Waypoint.Departure)}"" ASC NULLS LAST;
		";

		var result = await session.QueryAsync<Waypoint>(sql, ct);
		return result;
	}

	private const string _fullColumnsList = $@"
		""{nameof(Waypoint.Id)}""
		, ""{nameof(Waypoint.RideId)}""
		, ""{nameof(Waypoint.Point)}""
		, ""{nameof(Waypoint.FullName)}""
		, ""{nameof(Waypoint.NameToCity)}""
		, ""{nameof(Waypoint.Arrival)}""
		, ""{nameof(Waypoint.Departure)}""
		, ""{nameof(Waypoint.PreviousWaypointId)}""
		, ""{nameof(Waypoint.NextWaypointId)}""
	";
}
