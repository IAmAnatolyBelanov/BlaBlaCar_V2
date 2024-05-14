using NpgsqlTypes;
using WebApi.DataAccess;
using WebApi.Models;

namespace WebApi.Repositories;

public interface IRideRepository : IRepository
{
	Task<int> Insert(IPostgresSession session, Ride ride, CancellationToken ct);
	Task<Ride?> GetById(IPostgresSession session, Guid rideId, CancellationToken ct);
}

public class RideRepository : IRideRepository
{
	private const string _tableName = "\"Rides\"";

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