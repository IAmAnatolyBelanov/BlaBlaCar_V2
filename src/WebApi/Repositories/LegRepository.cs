using WebApi.DataAccess;
using WebApi.Models;

namespace WebApi.Repositories;

public interface ILegRepository : IRepository
{
	Task<ulong> BulkInsert(IPostgresSession session, IReadOnlyList<Leg> legs, CancellationToken ct);
	Task<IReadOnlyList<Leg>> GetByRideId(IPostgresSession session, Guid rideId, CancellationToken ct);
}

public class LegRepository : ILegRepository
{
	private const string _tableName = "\"Legs\"";
	private const string _waypointsTableName = "\"Waypoints\"";

	public async Task<ulong> BulkInsert(IPostgresSession session, IReadOnlyList<Leg> legs, CancellationToken ct)
	{
		const string sql = $@"
			COPY {_tableName} ({_fullColumnsList})
			FROM STDIN (FORMAT BINARY);
		";

		using var importer = await session.BeginBinaryImport(sql, ct);
		for (int i = 0; i < legs.Count; i++)
		{
			var leg = legs[i];

			await importer.StartRow(ct);

			await importer.Write(leg.Id, ct);
			await importer.Write(leg.RideId, ct);
			await importer.Write(leg.WaypointFromId, ct);
			await importer.Write(leg.WaypointToId, ct);
			await importer.Write(leg.PriceInRub, ct);
		}

		var result = await importer.Complete(ct);
		return result;
	}

	public async Task<IReadOnlyList<Leg>> GetByRideId(IPostgresSession session, Guid rideId, CancellationToken ct)
	{
		var sql = $@"
			SELECT
				leg.""{nameof(Leg.Id)}""
				, leg.""{nameof(Leg.RideId)}""
				, leg.""{nameof(Leg.WaypointFromId)}""
				, leg.""{nameof(Leg.WaypointToId)}""
				, leg.""{nameof(Leg.PriceInRub)}""
			FROM {_tableName} leg
			INNER JOIN {_waypointsTableName} waypoint_from ON waypoint_from.""{nameof(Waypoint.Id)}"" = leg.""{nameof(Leg.WaypointFromId)}""
			INNER JOIN {_waypointsTableName} waypoint_to ON waypoint_to.""{nameof(Waypoint.Id)}"" = leg.""{nameof(Leg.WaypointToId)}""
			WHERE leg.""{nameof(Leg.RideId)}"" = '{rideId}'
			ORDER BY
				waypoint_from.""{nameof(Waypoint.Arrival)}"" ASC
				, waypoint_to.""{nameof(Waypoint.Departure)}"" ASC;
		";

		var result = await session.QueryAsync<Leg>(sql, ct);
		return result;
	}

	private const string _fullColumnsList = $@"
		""{nameof(Leg.Id)}""
		, ""{nameof(Leg.RideId)}""
		, ""{nameof(Leg.WaypointFromId)}""
		, ""{nameof(Leg.WaypointToId)}""
		, ""{nameof(Leg.PriceInRub)}""
	";
}
