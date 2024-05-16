using NpgsqlTypes;
using WebApi.DataAccess;
using WebApi.Models;
using WebApi.Models.ControllersModels.RideControllerModels;

namespace WebApi.Repositories;

public interface IRideRepository : IRepository
{
	Task<int> Insert(IPostgresSession session, Ride ride, CancellationToken ct);
	Task<Ride?> GetById(IPostgresSession session, Guid rideId, CancellationToken ct);
	Task<IReadOnlyList<SearchRideDbResponse>> GetByFilter(IPostgresSession session, RideDbFilter filter, CancellationToken ct);
	Task<string> GetCounts(IPostgresSession session, CancellationToken ct);
}

public class RideRepository : IRideRepository
{
	private const string _tableName = "\"Rides\"";
	private const string _waypointsTableName = "\"Waypoints\"";
	private const string _legsTableName = "\"Legs\"";
	private const string _reservationsTableName = "\"Reservations\"";


	private const string _defaultRideAlias = "ride";
	private const string _defaultWaypointDepartureAlias = "waypoint_departure";
	private const string _defaultWaypointArrivalAlias = "waypoint_arrival";
	private const string _defaultLegAlias = "leg";
	private const string _defaultTmpRideAlias = "tmp_ride";
	private const string _defaultTmpPairAlias = "tmp_pair";


	public async Task<int> Insert(IPostgresSession session, Ride ride, CancellationToken ct)
	{
		// BinaryImporter используется из-за нежелания делать модель, полностью повторяющую Ride, за исключением того, что енумы становятся интами.
		const string sql = $@"
			COPY {_tableName} ({_fullColumnsList})
			FROM STDIN (FORMAT BINARY);
		";

		using var importer = await session.BeginBinaryImport(sql, ct);

		await importer.StartRow(ct);

		await importer.Write(ride.Id, ct);
		await importer.Write(ride.AuthorId, ct);
		await importer.WriteValueOrNull(ride.DriverId, ct);
		await importer.WriteValueOrNull(ride.CarId, ct);
		await importer.Write(ride.Created, ct);
		await importer.Write((int)ride.Status, ct);
		await importer.Write(ride.AvailablePlacesCount, ct);
		await importer.WriteValueOrNull(ride.Comment, ct);
		await importer.Write(ride.IsCashPaymentMethodAvailable, ct);
		await importer.Write(ride.IsCashlessPaymentMethodAvailable, ct);
		await importer.Write((int)ride.ValidationMethod, ct);
		await importer.WriteValueOrNull(ride.ValidationTimeBeforeDeparture, NpgsqlDbType.Time, ct);
		await importer.WriteValueOrNull((int?)ride.AfterRideValidationTimeoutAction, ct);

		var result = await importer.Complete(ct);

		return (int)result;
	}

	public async Task<Ride?> GetById(IPostgresSession session, Guid rideId, CancellationToken ct)
	{
		var sql = $@"
			SELECT {_fullColumnsList} FROM {_tableName}
			WHERE ""{nameof(Ride.Id)}"" = '{rideId}'
			LIMIT 1;
		";

		var result = await session.QueryFirstOrDefaultAsync<Ride>(sql, ct);
		return result;
	}

	public async Task<string> GetCounts(IPostgresSession session, CancellationToken ct)
	{
		var sql = $"select count(*) from {_tableName};";
		var ridesCount = await session.QueryFirstOrDefaultAsync<long>(sql, ct);

		sql = $"select count(*) from {_waypointsTableName};";
		var waypointsCount = await session.QueryFirstOrDefaultAsync<long>(sql, ct);

		sql = $"select count(*) from {_legsTableName};";
		var legsCount = await session.QueryFirstOrDefaultAsync<long>(sql, ct);

		return $"rides: {ridesCount}, waypoints: {waypointsCount}, legs: {legsCount}.";
	}

	public async Task<IReadOnlyList<SearchRideDbResponse>> GetByFilter(IPostgresSession session, RideDbFilter filter, CancellationToken ct)
	{
		var sql = $@"
			WITH RECURSIVE
				tmp_rides AS (
					SELECT
						ride.""{nameof(Ride.Id)}"" AS ""RideId""
						, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Id)}"" AS ""WaypointDepartureId""
						, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.NextWaypointId)}"" AS ""WaypointDepartureNextPointId""
						, (ST_DISTANCE({_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Point)}"", COALESCE(@{nameof(RideDbFilter.DeparturePoint)}, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Point)}"")) / 1000) AS ""WaypointDepartureDistanceKilometers""
						, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Id)}"" AS ""WaypointArrivalId""
						, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.NextWaypointId)}"" AS ""WaypointArrivalNextPointId""
						, (ST_DISTANCE({_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Point)}"", COALESCE(@{nameof(RideDbFilter.ArrivalPoint)}, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Point)}"")) / 1000) AS ""WaypointArrivalDistanceKilometers""
					FROM {_tableName} {_defaultRideAlias}
					INNER JOIN {_waypointsTableName} {_defaultWaypointDepartureAlias}
						ON {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.RideId)}"" = {_defaultRideAlias}.""{nameof(Ride.Id)}""
					INNER JOIN {_waypointsTableName} {_defaultWaypointArrivalAlias}
						ON {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.RideId)}"" = {_defaultRideAlias}.""{nameof(Ride.Id)}""
					{BuildMainCteWhereSection(
							filter: filter,
							rideAlias: _defaultRideAlias,
							waypointDepartureAlias: _defaultWaypointDepartureAlias,
							waypointArrivalAlias: _defaultWaypointArrivalAlias)}
				)

				, tmp_pairs AS (
				SELECT
					ride.""RideId"" AS ""RideId""
					, ride.""WaypointDepartureId"" AS ""StartId""
					, ride.""WaypointDepartureId"" AS ""CurrentId""
					, ride.""WaypointDepartureNextPointId"" AS ""EndId""
					, {_defaultLegAlias}.""{nameof(Leg.PriceInRub)}"" AS ""TotalPrice""
				FROM tmp_rides {_defaultTmpRideAlias}
				INNER JOIN {_legsTableName} {_defaultLegAlias}
					ON {_defaultLegAlias}.""{nameof(Leg.WaypointFromId)}"" = {_defaultTmpRideAlias}.""WaypointDepartureId""
					AND {_defaultLegAlias}.""{nameof(Leg.WaypointToId)}"" = {_defaultTmpRideAlias}.""WaypointDepartureNextPointId""
				UNION
				SELECT
					{_defaultTmpPairAlias}.""RideId"" AS ""RideId""
					, {_defaultTmpPairAlias}.""StartId"" AS ""StartId""
					, {_defaultTmpPairAlias}.""EndId"" AS ""CurrentId""
					, end_point.""{nameof(Waypoint.NextWaypointId)}"" AS ""EndId""
					, {_defaultTmpPairAlias}.""TotalPrice"" + {_defaultLegAlias}.""{nameof(Leg.PriceInRub)}"" AS ""TotalPrice""
				FROM tmp_pairs {_defaultTmpPairAlias}
				INNER JOIN {_waypointsTableName} end_point
					ON end_point.""{nameof(Waypoint.Id)}"" = {_defaultTmpPairAlias}.""EndId""
				INNER JOIN {_legsTableName} {_defaultLegAlias}
					ON {_defaultLegAlias}.""{nameof(Leg.WaypointFromId)}"" = {_defaultTmpPairAlias}.""EndId""
					AND {_defaultLegAlias}.""{nameof(Leg.WaypointToId)}"" = end_point.""{nameof(Waypoint.NextWaypointId)}""
				{BuildRecursiveWhereSection(filter)}
				)

			SELECT
				{_defaultRideAlias}.""{nameof(Ride.Id)}"" AS ""{nameof(SearchRideDbResponse.RideId)}""
				, {_defaultRideAlias}.""{nameof(Ride.AuthorId)}"" AS ""{nameof(SearchRideDbResponse.AuthorId)}""
				, {_defaultRideAlias}.""{nameof(Ride.DriverId)}"" AS ""{nameof(SearchRideDbResponse.DriverId)}""
				, {_defaultRideAlias}.""{nameof(Ride.CarId)}"" AS ""{nameof(SearchRideDbResponse.CarId)}""
				, {_defaultRideAlias}.""{nameof(Ride.Created)}"" AS ""{nameof(SearchRideDbResponse.Created)}""
				, {_defaultRideAlias}.""{nameof(Ride.Status)}"" AS ""{nameof(SearchRideDbResponse.Status)}""
				, {_defaultRideAlias}.""{nameof(Ride.AvailablePlacesCount)}"" AS ""{nameof(SearchRideDbResponse.TotalAvailablePlacesCount)}""
				, {_defaultRideAlias}.""{nameof(Ride.Comment)}"" AS ""{nameof(SearchRideDbResponse.Comment)}""
				, {_defaultRideAlias}.""{nameof(Ride.IsCashPaymentMethodAvailable)}"" AS ""{nameof(SearchRideDbResponse.IsCashPaymentMethodAvailable)}""
				, {_defaultRideAlias}.""{nameof(Ride.IsCashlessPaymentMethodAvailable)}"" AS ""{nameof(SearchRideDbResponse.IsCashlessPaymentMethodAvailable)}""
				, {_defaultRideAlias}.""{nameof(Ride.ValidationMethod)}"" AS ""{nameof(SearchRideDbResponse.ValidationMethod)}""
				, {_defaultRideAlias}.""{nameof(Ride.ValidationTimeBeforeDeparture)}"" AS ""{nameof(SearchRideDbResponse.ValidationTimeBeforeDeparture)}""
				, {_defaultRideAlias}.""{nameof(Ride.AfterRideValidationTimeoutAction)}"" AS ""{nameof(SearchRideDbResponse.AfterRideValidationTimeoutAction)}""

				, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Id)}"" AS ""{nameof(SearchRideDbResponse.WaypointFromId)}""
				, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Point)}"" AS ""{nameof(SearchRideDbResponse.FromPoint)}""
				, {_defaultTmpRideAlias}.""WaypointDepartureDistanceKilometers"" AS ""{nameof(SearchRideDbResponse.FromDistanceKilometers)}""
				, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.FullName)}"" AS ""{nameof(SearchRideDbResponse.FromFullName)}""
				, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.NameToCity)}"" AS ""{nameof(SearchRideDbResponse.FromNameToCity)}""
				, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Arrival)}"" AS ""{nameof(SearchRideDbResponse.FromArrival)}""
				, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Departure)}"" AS ""{nameof(SearchRideDbResponse.FromDeparture)}""

				, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Id)}"" AS ""{nameof(SearchRideDbResponse.WaypointToId)}""
				, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Point)}"" AS ""{nameof(SearchRideDbResponse.ToPoint)}""
				, {_defaultTmpRideAlias}.""WaypointArrivalDistanceKilometers"" AS ""{nameof(SearchRideDbResponse.ToDistanceKilometers)}""
				, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.FullName)}"" AS ""{nameof(SearchRideDbResponse.ToFullName)}""
				, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.NameToCity)}"" AS ""{nameof(SearchRideDbResponse.ToNameToCity)}""
				, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Arrival)}"" AS ""{nameof(SearchRideDbResponse.ToArrival)}""
				, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Departure)}"" AS ""{nameof(SearchRideDbResponse.ToDeparture)}""

				, {_defaultLegAlias}.""{nameof(Leg.PriceInRub)}"" AS ""{nameof(SearchRideDbResponse.LegPrice)}""
				, {_defaultTmpPairAlias}.""TotalPrice"" AS ""{nameof(SearchRideDbResponse.DefaultPrice)}""

			FROM {_tableName} {_defaultRideAlias}
			INNER JOIN tmp_rides {_defaultTmpRideAlias}
				ON {_defaultTmpRideAlias}.""RideId"" = {_defaultRideAlias}.""{nameof(Ride.Id)}""
			INNER JOIN tmp_pairs {_defaultTmpPairAlias}
				ON {_defaultTmpPairAlias}.""RideId"" = {_defaultRideAlias}.""{nameof(Ride.Id)}""
				AND {_defaultTmpRideAlias}.""WaypointDepartureId"" = {_defaultTmpPairAlias}.""StartId""
				AND {_defaultTmpRideAlias}.""WaypointArrivalId"" = {_defaultTmpPairAlias}.""EndId""
			INNER JOIN {_waypointsTableName} {_defaultWaypointDepartureAlias}
				ON {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Id)}"" = {_defaultTmpPairAlias}.""StartId""
			INNER JOIN {_waypointsTableName} {_defaultWaypointArrivalAlias}
				ON {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Id)}"" = {_defaultTmpPairAlias}.""EndId""
			LEFT JOIN {_legsTableName} {_defaultLegAlias}
				ON {_defaultLegAlias}.""{nameof(Leg.WaypointFromId)}"" = {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Id)}""
				AND {_defaultLegAlias}.""{nameof(Leg.WaypointToId)}"" = {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Id)}""
			{BuildWhereSection(filter)}
			ORDER BY
				{GetSqlSortType(filter.SortType)} {filter.SortDirection}
				, {_defaultRideAlias}.""{nameof(Ride.Created)}"" DESC
			OFFSET @{nameof(filter.Offset)}
			LIMIT @{nameof(filter.Limit)};
		";

		var result = await session.QueryAsync<SearchRideDbResponse>(sql, filter, ct);
		return result;
	}


	private string GetSqlSortType(
		RideSortType sortType,
		string legAliasWithDot = _defaultLegAlias + ".",
		string tmpPairAliasWithDot = _defaultTmpPairAlias + ".",
		string tmpRideAliasWithDot = _defaultTmpRideAlias + ".",
		string waypointDepartureAlias = _defaultWaypointDepartureAlias + ".",
		string waypointArrivalAlias = _defaultWaypointArrivalAlias + ".")
	{
		var result = sortType switch
		{
			RideSortType.ByPrice => $"COALESCE({legAliasWithDot}\"{nameof(Leg.PriceInRub)}\", {tmpPairAliasWithDot}\"TotalPrice\")",
			RideSortType.ByStartPointDistance => $"{tmpRideAliasWithDot}\"WaypointDepartureDistanceKilometers\"",
			RideSortType.ByEndPointDistance => $"{tmpRideAliasWithDot}\"WaypointArrivalDistanceKilometers\"",
			RideSortType.ByStartTime => $"{waypointDepartureAlias}\"{nameof(Waypoint.Arrival)}\"",
			RideSortType.ByEndTime => $"{waypointArrivalAlias}\"{nameof(Waypoint.Departure)}\"",
			_ => throw new ArgumentOutOfRangeException(nameof(sortType)),
		};
		return result;
	}


	private string BuildMainCteWhereSection(
		RideDbFilter filter,
		string rideAlias = _defaultRideAlias + ".",
		string waypointDepartureAlias = _defaultWaypointDepartureAlias + ".",
		string waypointArrivalAlias = _defaultWaypointArrivalAlias + "."
		)
	{
		if (!rideAlias.IsNullOrEmpty() && !rideAlias.EndsWith('.'))
			rideAlias += '.';
		if (!waypointDepartureAlias.IsNullOrEmpty() && !waypointDepartureAlias.EndsWith('.'))
			waypointDepartureAlias += '.';
		if (!waypointArrivalAlias.IsNullOrEmpty() && !waypointArrivalAlias.EndsWith('.'))
			waypointArrivalAlias += '.';

		var clauses = BuildMainCteClauses(filter, rideAlias, waypointDepartureAlias, waypointArrivalAlias)
			.Select(x => $"({x})")
			.ToArray();

		if (clauses.Length == 0)
			return string.Empty;

		var result = $"WHERE\n\t{string.Join("\n\tAND ", clauses)}";
		return result;
	}

	private IEnumerable<string> BuildMainCteClauses(
		RideDbFilter filter,
		string rideAliasWithDot,
		string waypointDepartureAliasWithDot,
		string waypointArrivalAliasWithDot
		)
	{
		if (filter.RideIds is not null)
			yield return $"{rideAliasWithDot}\"{nameof(Ride.Id)}\" = ANY(@{nameof(RideDbFilter.RideIds)})";

		if (filter.HideDeleted)
			yield return $"{rideAliasWithDot}\"{nameof(Ride.Status)}\" != {(int)RideStatus.Deleted}";

		if (filter.DeparturePoint is not null)
			yield return $"(ST_DISTANCE({waypointDepartureAliasWithDot}\"{nameof(Waypoint.Point)}\", @{nameof(RideDbFilter.DeparturePoint)}) / 1000) <= @{nameof(RideDbFilter.DeparturePointSearchRadiusKilometers)}";

		if (filter.ArrivalPoint is not null)
			yield return $"(ST_DISTANCE({waypointArrivalAliasWithDot}\"{nameof(Waypoint.Point)}\", @{nameof(RideDbFilter.ArrivalPoint)}) / 1000) <= @{nameof(RideDbFilter.ArrivalPointSearchRadiusKilometers)}";

		if (filter.MinDepartureTime.HasValue)
			yield return $"{waypointDepartureAliasWithDot}\"{nameof(Waypoint.Departure)}\" >= @{nameof(RideDbFilter.MinDepartureTime)}";
		if (filter.MaxDepartureTime.HasValue)
			yield return $"{waypointDepartureAliasWithDot}\"{nameof(Waypoint.Departure)}\" <= @{nameof(RideDbFilter.MaxDepartureTime)}";

		if (filter.MinArrivalTime.HasValue)
			yield return $"{waypointArrivalAliasWithDot}\"{nameof(Waypoint.Arrival)}\" >= @{nameof(RideDbFilter.MinArrivalTime)}";
		if (filter.MaxArrivalTime.HasValue)
			yield return $"{waypointArrivalAliasWithDot}\"{nameof(Waypoint.Arrival)}\" <= @{nameof(RideDbFilter.MaxArrivalTime)}";

		if (filter.PaymentMethods is not null)
		{
			var paymentsClauses = BuildPaymentMethodClauses(filter, rideAliasWithDot)
				.Select(x => $"({x})");
			var clause = string.Join(" OR ", paymentsClauses);
			yield return clause;
		}

		if (filter.ValidationMethods is not null)
			yield return $"{rideAliasWithDot}\"{nameof(Ride.ValidationMethod)}\" = ANY(@{nameof(RideDbFilter.ValidationMethods)})";

		if (filter.AvailableStatuses is not null)
			yield return $"{rideAliasWithDot}\"{nameof(Ride.Status)}\" = ANY(@{nameof(RideDbFilter.AvailableStatuses)})";
	}

	private IEnumerable<string> BuildPaymentMethodClauses(RideDbFilter filter, string rideAliasWithDot)
	{
		bool cashFilterUsed = false;
		bool cashlessFilterUsed = false;

		foreach (var method in filter.PaymentMethods!)
		{
			if (method == PaymentMethod.Cash && !cashFilterUsed)
			{
				cashFilterUsed = true;
				yield return $"{rideAliasWithDot}\"{nameof(Ride.IsCashPaymentMethodAvailable)}\" = TRUE";
			}

			if (method == PaymentMethod.Cashless && !cashlessFilterUsed)
			{
				cashlessFilterUsed = true;
				yield return $"{rideAliasWithDot}\"{nameof(Ride.IsCashlessPaymentMethodAvailable)}\" = TRUE";
			}
		}
	}

	private string BuildRecursiveWhereSection(
		RideDbFilter filter,
		string tmpPairAlias = _defaultTmpPairAlias + ".",
		string legAlias = _defaultLegAlias + "."
		)
	{
		if (!tmpPairAlias.IsNullOrEmpty() && !tmpPairAlias.EndsWith('.'))
			tmpPairAlias += '.';
		if (!legAlias.IsNullOrEmpty() && !legAlias.EndsWith('.'))
			legAlias += '.';

		var clauses = BuildRecursiveClauses(filter: filter, tmpPairAliasWithDot: tmpPairAlias, legAliasWithDot: legAlias)
			.Select(x => $"({x})")
			.ToArray();

		if (clauses.Length == 0)
			return string.Empty;

		var result = $"WHERE\n\t{string.Join("\n\tAND ", clauses)}";
		return result;
	}

	private IEnumerable<string> BuildRecursiveClauses(
		RideDbFilter filter,
		string tmpPairAliasWithDot,
		string legAliasWithDot
		)
	{
		if (filter.MaxPriceInRub is not null)
			yield return $"{tmpPairAliasWithDot}\"TotalPrice\" + {legAliasWithDot}\"{nameof(Leg.PriceInRub)}\" <= @{nameof(RideDbFilter.MaxPriceInRub)}";
	}

	private string BuildWhereSection(
		RideDbFilter filter,
		string tmpPairAlias = _defaultTmpPairAlias + ".",
		string legAlias = _defaultLegAlias + "."
		)
	{
		if (!tmpPairAlias.IsNullOrEmpty() && !tmpPairAlias.EndsWith('.'))
			tmpPairAlias += '.';
		if (!legAlias.IsNullOrEmpty() && !legAlias.EndsWith('.'))
			legAlias += '.';

		var clauses = BuildClauses(filter: filter, tmpPairAliasWithDot: tmpPairAlias, legAliasWithDot: legAlias)
			.Select(x => $"({x})")
			.ToArray();

		if (clauses.Length == 0)
			return string.Empty;

		var result = $"WHERE\n\t{string.Join("\n\tAND ", clauses)}";
		return result;
	}

	private IEnumerable<string> BuildClauses(
		RideDbFilter filter,
		string tmpPairAliasWithDot,
		string legAliasWithDot
		)
	{
		if (filter.MinPriceInRub.HasValue)
			yield return $"COALESCE({legAliasWithDot}\"{nameof(Leg.PriceInRub)}\", {tmpPairAliasWithDot}\"TotalPrice\") >= @{nameof(RideDbFilter.MinPriceInRub)}";

		if (filter.MaxPriceInRub.HasValue)
			yield return $"COALESCE({legAliasWithDot}\"{nameof(Leg.PriceInRub)}\", {tmpPairAliasWithDot}\"TotalPrice\") <= @{nameof(RideDbFilter.MaxPriceInRub)}";
	}

	private const string _fullColumnsList = $@"
		""{nameof(Ride.Id)}""
		, ""{nameof(Ride.AuthorId)}""
		, ""{nameof(Ride.DriverId)}""
		, ""{nameof(Ride.CarId)}""
		, ""{nameof(Ride.Created)}""
		, ""{nameof(Ride.Status)}""
		, ""{nameof(Ride.AvailablePlacesCount)}""
		, ""{nameof(Ride.Comment)}""
		, ""{nameof(Ride.IsCashPaymentMethodAvailable)}""
		, ""{nameof(Ride.IsCashlessPaymentMethodAvailable)}""
		, ""{nameof(Ride.ValidationMethod)}""
		, ""{nameof(Ride.ValidationTimeBeforeDeparture)}""
		, ""{nameof(Ride.AfterRideValidationTimeoutAction)}""
	";
}