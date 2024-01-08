using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApi.Migrations
{
	/// <inheritdoc />
	public partial class ReservationTriggerAdded : Migration
	{
		public const string UpCommand = @"
CREATE OR REPLACE FUNCTION ""Custom_DbFunctionName_RideHasEnoughFreePlaces_OnReservation""()
RETURNS TRIGGER AS $$
DECLARE
	total_reserved_seats INT;
	max_available_places INT;
BEGIN
	IF NEW.""IsActive"" IS NULL OR NEW.""IsActive"" IS FALSE THEN
		RETURN NEW;
	END IF;

	SELECT SUM(reserv.""Count"")
	INTO total_reserved_seats
	FROM ""Reservations"" AS reserv
	JOIN ""Legs"" AS leg ON reserv.""LegId"" = leg.""Id""
	JOIN ""Legs"" AS reserving_leg ON reserving_leg.""Id"" = NEW.""Id""
	WHERE leg.""StartTime"" <= reserving_leg.""EndTime""
		AND leg.""EndTime"" >= reserving_leg.""StartTime""
		AND reserv.""IsActive""
		AND reserv.""Id"" != NEW.""Id"";

	SELECT (MIN(ride.""AvailablePlacesCount"") - COALESCE(total_reserved_seats, 0))
	INTO max_available_places
	FROM ""Rides"" AS ride
	JOIN ""Legs"" AS leg ON leg.""RideId"" = ride.""Id""
	WHERE leg.""Id"" = NEW.""LegId"";

	IF (max_available_places < NEW.""Count"") THEN
		RAISE EXCEPTION 'Custom_DbFunctionName_RideHasEnoughFreePlaces_OnReservation_Error_NotEnoughFreePlaces';
	END IF;

	RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER ""Custom_DbTriggerName_RideHasEnoughFreePlaces_OnReservation""
BEFORE INSERT OR UPDATE ON ""Reservations""
FOR EACH ROW
EXECUTE FUNCTION ""Custom_DbFunctionName_RideHasEnoughFreePlaces_OnReservation""();
";

		public const string DownCommand = @"
DROP TRIGGER ""Custom_DbTriggerName_RideHasEnoughFreePlaces_OnReservation"" ON ""Reservations"";
DROP FUNCTION ""Custom_DbFunctionName_RideHasEnoughFreePlaces_OnReservation""();
";

		/// <inheritdoc />
		protected override void Up(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql(UpCommand);
		}

		/// <inheritdoc />
		protected override void Down(MigrationBuilder migrationBuilder)
		{
			migrationBuilder.Sql(DownCommand);
		}
	}
}
