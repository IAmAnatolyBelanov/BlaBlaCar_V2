using NpgsqlTypes;
using WebApi.DataAccess;
using WebApi.Models;

namespace WebApi.Repositories;

public interface IRideRepository : IRepository
{
	Task<int> Insert(IPostgresSession session, Ride ride, CancellationToken ct);
	Task<Ride?> GetById(IPostgresSession session, Guid rideId, CancellationToken ct);
	Task<IReadOnlyList<SearchRideDbResponse>> GetByFilter(IPostgresSession session, RideDbFilter filter, CancellationToken ct);
	Task<string> GetCounts(IPostgresSession session, CancellationToken ct);
	Task<PriceRecommendation?> GetPriceRecommendation(IPostgresSession session, PriceRecommendationDbRequest request, CancellationToken ct);
	Task<RideDbCounts?> GetCounts(IPostgresSession session, RideDbCountsFilter filter, CancellationToken ct);
	Task<int> UpdateAvailablePlacesCount(IPostgresSession session, Guid rideId, int count, CancellationToken ct);
}

public class RideRepository : IRideRepository
{
	private const string _rideTableName = "\"Rides\"";
	private const string _waypointsTableName = "\"Waypoints\"";
	private const string _legsTableName = "\"Legs\"";
	private const string _reservationsTableName = "\"Reservations\"";
	private const string _affectedByReservationsLegsTableName = "\"AffectedByReservationsLegs\"";


	private const string _defaultRideAlias = "ride";
	private const string _defaultWaypointDepartureAlias = "waypoint_departure";
	private const string _defaultWaypointArrivalAlias = "waypoint_arrival";
	private const string _defaultLegAlias = "leg";
	private const string _defaultReservationAlias = "reservation";
	private const string _defaultAffectedLegAlias = "affected_leg";

	public async Task<int> Insert(IPostgresSession session, Ride ride, CancellationToken ct)
	{
		// BinaryImporter используется из-за нежелания делать модель, полностью повторяющую Ride, за исключением того, что енумы становятся интами.
		const string sql = $@"
			COPY {_rideTableName} ({_fullColumnsList})
			FROM STDIN (FORMAT BINARY);
		";

		using var importer = await session.BeginBinaryImport(sql, ct);

		await importer.StartRow(ct);

		await importer.Write(ride.Id, ct);
		await importer.Write(ride.AuthorId, ct);
		await importer.Write(ride.DriverId, ct);
		await importer.Write(ride.Created, ct);
		await importer.Write(ride.AvailablePlacesCount, ct);
		await importer.Write(ride.IsCashPaymentMethodAvailable, ct);
		await importer.Write(ride.IsCashlessPaymentMethodAvailable, ct);
		await importer.Write((int)ride.ValidationMethod, ct);
		await importer.WriteValueOrNull(ride.ValidationTimeBeforeDeparture, NpgsqlDbType.Time, ct);
		await importer.WriteValueOrNull((int?)ride.AfterRideValidationTimeoutAction, ct);
		await importer.Write(ride.IsDeleted, ct);

		var result = await importer.Complete(ct);

		return (int)result;
	}

	public async Task<Ride?> GetById(IPostgresSession session, Guid rideId, CancellationToken ct)
	{
		var sql = $@"
			SELECT {_fullColumnsList} FROM {_rideTableName}
			WHERE ""{nameof(Ride.Id)}"" = '{rideId}'
			LIMIT 1;
		";

		var result = await session.QueryFirstOrDefaultAsync<Ride>(sql, ct);
		return result;
	}

	public async Task<string> GetCounts(IPostgresSession session, CancellationToken ct)
	{
		var sql = $"select count(*) from {_rideTableName};";
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
			WITH {BuildLegReservedSeatsCte()}

			SELECT
				{_defaultRideAlias}.""{nameof(Ride.Id)}"" AS ""{nameof(SearchRideDbResponse.RideId)}""
				, {_defaultRideAlias}.""{nameof(Ride.AuthorId)}"" AS ""{nameof(SearchRideDbResponse.AuthorId)}""
				, {_defaultRideAlias}.""{nameof(Ride.DriverId)}"" AS ""{nameof(SearchRideDbResponse.DriverId)}""
				, {_defaultRideAlias}.""{nameof(Ride.Created)}"" AS ""{nameof(SearchRideDbResponse.Created)}""
				, {_defaultRideAlias}.""{nameof(Ride.AvailablePlacesCount)}"" AS ""{nameof(SearchRideDbResponse.TotalAvailablePlacesCount)}""
				, {_defaultRideAlias}.""{nameof(Ride.IsCashPaymentMethodAvailable)}"" AS ""{nameof(SearchRideDbResponse.IsCashPaymentMethodAvailable)}""
				, {_defaultRideAlias}.""{nameof(Ride.IsCashlessPaymentMethodAvailable)}"" AS ""{nameof(SearchRideDbResponse.IsCashlessPaymentMethodAvailable)}""
				, {_defaultRideAlias}.""{nameof(Ride.ValidationMethod)}"" AS ""{nameof(SearchRideDbResponse.ValidationMethod)}""
				, {_defaultRideAlias}.""{nameof(Ride.ValidationTimeBeforeDeparture)}"" AS ""{nameof(SearchRideDbResponse.ValidationTimeBeforeDeparture)}""
				, {_defaultRideAlias}.""{nameof(Ride.AfterRideValidationTimeoutAction)}"" AS ""{nameof(SearchRideDbResponse.AfterRideValidationTimeoutAction)}""
				, {_defaultRideAlias}.""{nameof(Ride.IsDeleted)}"" AS ""{nameof(SearchRideDbResponse.IsDeleted)}""

				, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Id)}"" AS ""{nameof(SearchRideDbResponse.WaypointFromId)}""
				, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Point)}"" AS ""{nameof(SearchRideDbResponse.FromPoint)}""
				, (ST_DISTANCE({_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Point)}"", COALESCE(@{nameof(RideDbFilter.DeparturePoint)}, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Point)}"")) / 1000) AS ""{nameof(SearchRideDbResponse.FromDistanceKilometers)}""
				, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.FullName)}"" AS ""{nameof(SearchRideDbResponse.FromFullName)}""
				, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.NameToCity)}"" AS ""{nameof(SearchRideDbResponse.FromNameToCity)}""
				, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Arrival)}"" AS ""{nameof(SearchRideDbResponse.FromArrival)}""
				, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Departure)}"" AS ""{nameof(SearchRideDbResponse.FromDeparture)}""

				, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Id)}"" AS ""{nameof(SearchRideDbResponse.WaypointToId)}""
				, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Point)}"" AS ""{nameof(SearchRideDbResponse.ToPoint)}""
				, (ST_DISTANCE({_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Point)}"", COALESCE(@{nameof(RideDbFilter.ArrivalPoint)}, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Point)}"")) / 1000) AS ""{nameof(SearchRideDbResponse.ToDistanceKilometers)}""
				, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.FullName)}"" AS ""{nameof(SearchRideDbResponse.ToFullName)}""
				, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.NameToCity)}"" AS ""{nameof(SearchRideDbResponse.ToNameToCity)}""
				, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Arrival)}"" AS ""{nameof(SearchRideDbResponse.ToArrival)}""
				, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Departure)}"" AS ""{nameof(SearchRideDbResponse.ToDeparture)}""

				, {_defaultLegAlias}.""{nameof(Leg.PriceInRub)}"" AS ""{nameof(SearchRideDbResponse.Price)}""
				, {_defaultLegAlias}.""{nameof(Leg.IsManual)}"" AS ""{nameof(SearchRideDbResponse.IsPriceManual)}""

				, leg_reserved_seat.""AlreadyReservedSeatsCount"" AS {nameof(SearchRideDbResponse.AlreadyReservedSeatsCount)}
			FROM {_rideTableName} {_defaultRideAlias}
			INNER JOIN {_legsTableName} {_defaultLegAlias}
				ON {_defaultLegAlias}.""{nameof(Leg.RideId)}"" = {_defaultRideAlias}.""{nameof(Ride.Id)}""
			INNER JOIN {_waypointsTableName} {_defaultWaypointDepartureAlias}
				ON {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.RideId)}"" = {_defaultRideAlias}.""{nameof(Ride.Id)}""
				AND {_defaultLegAlias}.""{nameof(Leg.WaypointFromId)}"" = {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Id)}""
			INNER JOIN {_waypointsTableName} {_defaultWaypointArrivalAlias}
				ON {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.RideId)}"" = {_defaultRideAlias}.""{nameof(Ride.Id)}""
				AND {_defaultLegAlias}.""{nameof(Leg.WaypointToId)}"" = {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Id)}""
			LEFT JOIN leg_reserved_seats leg_reserved_seat
				ON leg_reserved_seat.""LegId"" = {_defaultLegAlias}.""{nameof(Leg.Id)}""
			{BuildWhereSection(filter)}
			ORDER BY
				{GetSqlSortType(filter.SortType)} {filter.SortDirection}
				, {_defaultRideAlias}.""{nameof(Ride.Created)}"" DESC
				, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Departure)}"" ASC
				, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Arrival)}"" ASC NULLS LAST
			OFFSET @{nameof(filter.Offset)}
			LIMIT @{nameof(filter.Limit)};
		";

		var result = await session.QueryAsync<SearchRideDbResponse>(sql, filter, ct);
		return result;
	}

	public async Task<RideDbCounts?> GetCounts(IPostgresSession session, RideDbCountsFilter filter, CancellationToken ct)
	{
		var sql = $@"
			WITH {BuildLegReservedSeatsCte()}

			SELECT
				COUNT(*) AS ""{nameof(RideDbCounts.TotalCount)}""

				, COUNT(*) FILTER (WHERE ({_defaultRideAlias}.""{nameof(Ride.IsCashPaymentMethodAvailable)}"" = TRUE)) AS ""{nameof(RideDbCounts.CashAvailableCount)}""
				, COUNT(*) FILTER (WHERE ({_defaultRideAlias}.""{nameof(Ride.IsCashlessPaymentMethodAvailable)}"" = TRUE)) AS ""{nameof(RideDbCounts.CashlessAvailableCount)}""

				, COUNT(*) FILTER (WHERE ({_defaultRideAlias}.""{nameof(Ride.ValidationMethod)}"" = {(int)RideValidationMethod.ValidationBeforeAccessPassenger})) AS ""{nameof(RideDbCounts.WithValidationCount)}""
				, COUNT(*) FILTER (WHERE ({_defaultRideAlias}.""{nameof(Ride.ValidationMethod)}"" = {(int)RideValidationMethod.WithoutValidation})) AS ""{nameof(RideDbCounts.WithoutValidationCount)}""

				, COUNT(*) FILTER (WHERE ((ST_DISTANCE({_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Point)}"", COALESCE(@{nameof(RideDbFilter.DeparturePoint)}, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Point)}"")) / 1000) <= @{nameof(RideDbCountsFilter.CloseDistanceInKilometers)})) AS ""{nameof(RideDbCounts.CloseDepartureDistanceCount)}""
				, COUNT(*) FILTER (WHERE ((ST_DISTANCE({_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Point)}"", COALESCE(@{nameof(RideDbFilter.DeparturePoint)}, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Point)}"")) / 1000) <= @{nameof(RideDbCountsFilter.MiddleDistanceInKilometers)})) AS ""{nameof(RideDbCounts.MiddleDepartureDistanceCount)}""
				, COUNT(*) FILTER (WHERE ((ST_DISTANCE({_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Point)}"", COALESCE(@{nameof(RideDbFilter.DeparturePoint)}, {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Point)}"")) / 1000) <= @{nameof(RideDbCountsFilter.FarAwayDistanceInKilometers)})) AS ""{nameof(RideDbCounts.FarAwayDepartureDistanceCount)}""

				, COUNT(*) FILTER (WHERE ((ST_DISTANCE({_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Point)}"", COALESCE(@{nameof(RideDbFilter.ArrivalPoint)}, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Point)}"")) / 1000) <= @{nameof(RideDbCountsFilter.CloseDistanceInKilometers)})) AS ""{nameof(RideDbCounts.CloseArrivalDistanceCount)}""
				, COUNT(*) FILTER (WHERE ((ST_DISTANCE({_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Point)}"", COALESCE(@{nameof(RideDbFilter.ArrivalPoint)}, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Point)}"")) / 1000) <= @{nameof(RideDbCountsFilter.MiddleDistanceInKilometers)})) AS ""{nameof(RideDbCounts.MiddleArrivalDistanceCount)}""
				, COUNT(*) FILTER (WHERE ((ST_DISTANCE({_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Point)}"", COALESCE(@{nameof(RideDbFilter.ArrivalPoint)}, {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Point)}"")) / 1000) <= @{nameof(RideDbCountsFilter.FarAwayDistanceInKilometers)})) AS ""{nameof(RideDbCounts.FarAwayArrivalDistanceCount)}""
			FROM {_rideTableName} {_defaultRideAlias}
			INNER JOIN {_legsTableName} {_defaultLegAlias}
				ON {_defaultLegAlias}.""{nameof(Leg.RideId)}"" = {_defaultRideAlias}.""{nameof(Ride.Id)}""
			INNER JOIN {_waypointsTableName} {_defaultWaypointDepartureAlias}
				ON {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.RideId)}"" = {_defaultRideAlias}.""{nameof(Ride.Id)}""
				AND {_defaultLegAlias}.""{nameof(Leg.WaypointFromId)}"" = {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Id)}""
			INNER JOIN {_waypointsTableName} {_defaultWaypointArrivalAlias}
				ON {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.RideId)}"" = {_defaultRideAlias}.""{nameof(Ride.Id)}""
				AND {_defaultLegAlias}.""{nameof(Leg.WaypointToId)}"" = {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Id)}""
			LEFT JOIN leg_reserved_seats leg_reserved_seat
				ON leg_reserved_seat.""LegId"" = {_defaultLegAlias}.""{nameof(Leg.Id)}""
			{BuildWhereSection(filter)};
		";

		var result = await session.QueryFirstOrDefaultAsync<RideDbCounts>(sql, filter, ct);
		return result;
	}

	public async Task<PriceRecommendation?> GetPriceRecommendation(IPostgresSession session, PriceRecommendationDbRequest request, CancellationToken ct)
	{
		var sql = $@"
			SELECT
				PERCENTILE_CONT(@{nameof(request.LowerPercentile)}) WITHIN GROUP (ORDER BY {_defaultLegAlias}.""{nameof(Leg.PriceInRub)}"" ASC) AS {nameof(PriceRecommendation.LowerRecommendedPrice)}
				, PERCENTILE_CONT(@{nameof(request.MiddlePercentile)}) WITHIN GROUP (ORDER BY {_defaultLegAlias}.""{nameof(Leg.PriceInRub)}"" ASC) AS {nameof(PriceRecommendation.MiddleRecommendedPrice)}
				, PERCENTILE_CONT(@{nameof(request.HigherPercentile)}) WITHIN GROUP (ORDER BY {_defaultLegAlias}.""{nameof(Leg.PriceInRub)}"" ASC) AS {nameof(PriceRecommendation.HigherRecommendedPrice)}
				, COUNT(*) AS {nameof(PriceRecommendation.RowsCount)}
			FROM
				{_affectedByReservationsLegsTableName} {_defaultAffectedLegAlias}
			INNER JOIN {_reservationsTableName} {_defaultReservationAlias}
				ON {_defaultReservationAlias}.""{nameof(Reservation.Id)}"" = {_defaultAffectedLegAlias}.""{nameof(AffectedByReservationLeg.ReservationId)}""
			INNER JOIN {_legsTableName} {_defaultLegAlias}
				ON {_defaultLegAlias}.""{nameof(Leg.Id)}"" = {_defaultAffectedLegAlias}.""{nameof(AffectedByReservationLeg.LegId)}""
			INNER JOIN {_rideTableName} {_defaultRideAlias}
				ON {_defaultRideAlias}.""{nameof(Ride.Id)}"" = {_defaultLegAlias}.""{nameof(Leg.RideId)}""
			INNER JOIN {_waypointsTableName} {_defaultWaypointDepartureAlias}
				ON {_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Id)}"" = {_defaultLegAlias}.""{nameof(Leg.WaypointFromId)}""
			INNER JOIN {_waypointsTableName} {_defaultWaypointArrivalAlias}
				ON {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Id)}"" = {_defaultLegAlias}.""{nameof(Leg.WaypointToId)}""
			WHERE
				{_defaultRideAlias}.""{nameof(Ride.IsDeleted)}"" = FALSE
				AND {_defaultReservationAlias}.""{nameof(Reservation.IsDeleted)}"" = FALSE
				AND {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Arrival)}"" <= @{nameof(request.ArrivalDateTo)}
				AND {_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Arrival)}"" >= @{nameof(request.ArrivalDateFrom)}
				AND (ST_DISTANCE({_defaultWaypointDepartureAlias}.""{nameof(Waypoint.Point)}"", @{nameof(request.PointFrom)}) / 1000) <= @{nameof(request.RadiusInKilometers)}
				AND (ST_DISTANCE({_defaultWaypointArrivalAlias}.""{nameof(Waypoint.Point)}"", @{nameof(request.PointTo)}) / 1000) <= @{nameof(request.RadiusInKilometers)}
		";

		var result = await session.QueryFirstOrDefaultAsync<PriceRecommendation>(sql, request, ct);
		return result;
	}

	public async Task<int> UpdateAvailablePlacesCount(IPostgresSession session, Guid rideId, int count, CancellationToken ct)
	{
		var sql = $@"
			UPDATE
				{_rideTableName}
			SET
				""{nameof(Ride.AvailablePlacesCount)}"" = {count}
			WHERE
				""{nameof(Ride.Id)}"" = '{rideId}';
			";

		var result = await session.ExecuteAsync(sql, ct);
		return result;
	}

	private string BuildLegReservedSeatsCte(
		string cteName = "leg_reserved_seats",
		string affectedLegAlias = _defaultAffectedLegAlias,
		string reservationAlias = _defaultReservationAlias
		)
	{
		if (!affectedLegAlias.IsNullOrEmpty() && affectedLegAlias.EndsWith('.'))
			affectedLegAlias = affectedLegAlias.TrimEnd('.');
		if (!reservationAlias.IsNullOrEmpty() && reservationAlias.EndsWith('.'))
			reservationAlias = reservationAlias.TrimEnd('.');

		var result = $@"
			{cteName} AS(
				SELECT
					{affectedLegAlias}.""{nameof(AffectedByReservationLeg.LegId)}"" AS ""LegId""
					, SUM({reservationAlias}.""{nameof(Reservation.PeopleCount)}"") AS ""AlreadyReservedSeatsCount""
				FROM {_affectedByReservationsLegsTableName} {affectedLegAlias}
				INNER JOIN {_reservationsTableName} {reservationAlias}
					ON {reservationAlias}.""{nameof(Reservation.Id)}"" = {affectedLegAlias}.""{nameof(AffectedByReservationLeg.ReservationId)}""
				WHERE {reservationAlias}.""{nameof(Reservation.IsDeleted)}"" = FALSE
				GROUP BY {affectedLegAlias}.""{nameof(AffectedByReservationLeg.LegId)}""
			)
		";
		return result;
	}

	private string BuildWhereSection(
		RideDbFilter filter,
		string legAlias = _defaultLegAlias + ".",
		string rideAlias = _defaultRideAlias + ".",
		string waypointDepartureAlias = _defaultWaypointDepartureAlias + ".",
		string waypointArrivalAlias = _defaultWaypointArrivalAlias + "."
		)
	{
		if (!rideAlias.IsNullOrEmpty() && !rideAlias.EndsWith('.'))
			rideAlias += '.';
		if (!legAlias.IsNullOrEmpty() && !legAlias.EndsWith('.'))
			legAlias += '.';
		if (!waypointDepartureAlias.IsNullOrEmpty() && !waypointDepartureAlias.EndsWith('.'))
			waypointDepartureAlias += '.';
		if (!waypointArrivalAlias.IsNullOrEmpty() && !waypointArrivalAlias.EndsWith('.'))
			waypointArrivalAlias += '.';

		var clauses = BuildClauses(
			filter: filter,
			rideAliasWithDot: rideAlias,
			legAliasWithDot: legAlias,
			waypointDepartureAliasWithDot: waypointDepartureAlias,
			waypointArrivalAliasWithDot: waypointArrivalAlias);

		var clausesArray = clauses
			.Select(x => $"({x})")
			.ToArray();

		if (clausesArray.Length == 0)
			return string.Empty;

		var result = $"WHERE\n\t{string.Join("\n\tAND ", clausesArray)}";
		return result;
	}

	private IEnumerable<string> BuildClauses(
		RideDbFilter filter,
		string rideAliasWithDot,
		string legAliasWithDot,
		string waypointDepartureAliasWithDot,
		string waypointArrivalAliasWithDot
		)
	{
		if (filter.RideIds is not null)
			yield return $"{rideAliasWithDot}\"{nameof(Ride.Id)}\" = ANY(@{nameof(RideDbFilter.RideIds)})";

		if (filter.HideDeleted)
			yield return $"{rideAliasWithDot}\"{nameof(Ride.IsDeleted)}\" = FALSE";

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

		if (filter.MinPriceInRub.HasValue)
			yield return $"{legAliasWithDot}\"{nameof(Leg.PriceInRub)}\" >= @{nameof(RideDbFilter.MinPriceInRub)}";

		if (filter.MaxPriceInRub.HasValue)
			yield return $"{legAliasWithDot}\"{nameof(Leg.PriceInRub)}\" <= @{nameof(RideDbFilter.MaxPriceInRub)}";

		if (filter.FreeSeatsCount.HasValue)
			yield return $"({rideAliasWithDot}\"{nameof(Ride.AvailablePlacesCount)}\" - COALESCE(leg_reserved_seat.\"AlreadyReservedSeatsCount\", 0)) >= @{nameof(RideDbFilter.FreeSeatsCount)}";
	}

	private string GetSqlSortType(
		RideSortType sortType,
		string rideAlias = _defaultRideAlias + ".",
		string legAlias = _defaultLegAlias + ".",
		string waypointDepartureAlias = _defaultWaypointDepartureAlias + ".",
		string waypointArrivalAlias = _defaultWaypointArrivalAlias + ".")
	{
		if (!legAlias.IsNullOrEmpty() && !legAlias.EndsWith('.'))
			legAlias += '.';
		if (!waypointDepartureAlias.IsNullOrEmpty() && !waypointDepartureAlias.EndsWith('.'))
			waypointDepartureAlias += '.';
		if (!waypointArrivalAlias.IsNullOrEmpty() && !waypointArrivalAlias.EndsWith('.'))
			waypointArrivalAlias += '.';

		var result = sortType switch
		{
			RideSortType.ByPrice => $"{legAlias}\"{nameof(Leg.PriceInRub)}\"",
			RideSortType.ByStartPointDistance => $"(ST_DISTANCE({waypointDepartureAlias}\"{nameof(Waypoint.Point)}\", COALESCE(@{nameof(RideDbFilter.DeparturePoint)}, {waypointDepartureAlias}\"{nameof(Waypoint.Point)}\")))",
			RideSortType.ByEndPointDistance => $"(ST_DISTANCE({waypointArrivalAlias}\"{nameof(Waypoint.Point)}\", COALESCE(@{nameof(RideDbFilter.ArrivalPoint)}, {waypointArrivalAlias}\"{nameof(Waypoint.Point)}\")))",
			RideSortType.ByStartTime => $"{waypointDepartureAlias}\"{nameof(Waypoint.Arrival)}\"",
			RideSortType.ByEndTime => $"{waypointArrivalAlias}\"{nameof(Waypoint.Departure)}\"",
			RideSortType.ByFreeSeatsCount => $"({rideAlias}\"{nameof(Ride.AvailablePlacesCount)}\" - leg_reserved_seat.\"AlreadyReservedSeatsCount\")",
			_ => throw new ArgumentOutOfRangeException(nameof(sortType)),
		};
		return result;
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

	private const string _fullColumnsList = $@"
		""{nameof(Ride.Id)}""
		, ""{nameof(Ride.AuthorId)}""
		, ""{nameof(Ride.DriverId)}""
		, ""{nameof(Ride.Created)}""
		, ""{nameof(Ride.AvailablePlacesCount)}""
		, ""{nameof(Ride.IsCashPaymentMethodAvailable)}""
		, ""{nameof(Ride.IsCashlessPaymentMethodAvailable)}""
		, ""{nameof(Ride.ValidationMethod)}""
		, ""{nameof(Ride.ValidationTimeBeforeDeparture)}""
		, ""{nameof(Ride.AfterRideValidationTimeoutAction)}""
		, ""{nameof(Ride.IsDeleted)}""
	";
}