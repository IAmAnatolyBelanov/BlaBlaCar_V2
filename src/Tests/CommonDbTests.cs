using FluentAssertions;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using WebApi.DataAccess;
using WebApi.Migrations;
using WebApi.Models;

namespace Tests
{
	public class CommonDbTests : IClassFixture<TestAppFactoryWithDb>
	{
		private readonly IServiceProvider _provider;
		public CommonDbTests(TestAppFactoryWithDb fixture)
		{
			_provider = fixture.Services;

			fixture.MigrateDb();
		}

		[Fact]
		public void DbContainsFunctions()
		{
			var requredFunctions = DbConstants.FunctionNames.AllConstantValues;

			using var scope = _provider.CreateScope();
			using var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

			var allFunctions = context.Database.SqlQuery<string>($@"
SELECT routine_name
FROM  information_schema.routines
WHERE routine_type = 'FUNCTION'
AND routine_schema = 'public'")
				.ToHashSet();

			allFunctions.Should().Contain(requredFunctions);
		}

		[Fact]
		public void DbContainsTriggers()
		{
			var requredTriggerNames = GetAllTriggers();

			using var scope = _provider.CreateScope();
			using var context = scope.ServiceProvider.GetRequiredService<ApplicationContext>();

			var allTriggers = context.Database.SqlQuery<string>($@"
SELECT CONCAT(event_object_table, '|', trigger_name)
FROM information_schema.triggers")
				.ToArray()
				.Except(["layer|layer_integrity_checks"]) // Служебный триггер. Есть только в тестах.
				.ToHashSet();

			allTriggers.Should().BeEquivalentTo(requredTriggerNames);
		}

		/// <summary>
		/// Есть вероятность забыть какой-нибудь триггер.
		/// Так как триггеры зависят ещё и от таблицы, то просто по имени сравнивать нельзя.
		/// Поэтому принято решение закостылять так. Однако дополнительно надо проверить,
		/// а не забыт ли триггер в самих тестах. Поэтому их имена генерятся здесь на ходу.
		/// </summary>
		[Fact]
		public void AllTriggersAreUsed()
		{
			var allTriggers = GetAllTriggers();

			allTriggers.Should().HaveSameCount(DbConstants.TriggerNames.AllConstantValues);
		}

		/// <summary>
		/// Кастомные объекты, такие как триггеры и функции, не отслеживаются ef.
		/// Так как вклиниваться в механизм миграций мне лень,
		/// то просто буду тестом сравнивать, что команды верны.
		/// </summary>
		[Fact]
		public void CreateCustomObjectsCommandsAreCorrect()
		{
			var upReservationTriggerCommand = $@"
CREATE OR REPLACE FUNCTION ""{DbConstants.FunctionNames.TriggerReservation_EnoughFreePlaces}""()
RETURNS TRIGGER AS $$
DECLARE
	total_reserved_seats INT;
	max_available_places INT;
BEGIN
	IF NEW.""{nameof(Reservation.IsActive)}"" IS NULL OR NEW.""{nameof(Reservation.IsActive)}"" IS FALSE THEN
		RETURN NEW;
	END IF;

	SELECT SUM(reserv.""{nameof(Reservation.Count)}"")
	INTO total_reserved_seats
	FROM ""{nameof(ApplicationContext.Reservations)}"" AS reserv
	JOIN ""{nameof(ApplicationContext.Legs)}"" AS leg ON reserv.""{nameof(Reservation.LegId)}"" = leg.""{nameof(Leg.Id)}""
	JOIN ""{nameof(ApplicationContext.Legs)}"" AS reserving_leg ON reserving_leg.""{nameof(Leg.Id)}"" = NEW.""{nameof(Leg.Id)}""
	WHERE leg.""{nameof(Leg.StartTime)}"" <= reserving_leg.""{nameof(Leg.EndTime)}""
		AND leg.""{nameof(Leg.EndTime)}"" >= reserving_leg.""{nameof(Leg.StartTime)}""
		AND reserv.""{nameof(Reservation.IsActive)}""
		AND reserv.""{nameof(Reservation.Id)}"" != NEW.""{nameof(Reservation.Id)}"";

	SELECT (MIN(ride.""{nameof(Ride.AvailablePlacesCount)}"") - COALESCE(total_reserved_seats, 0))
	INTO max_available_places
	FROM ""{nameof(ApplicationContext.Rides)}"" AS ride
	JOIN ""{nameof(ApplicationContext.Legs)}"" AS leg ON leg.""{nameof(Leg.RideId)}"" = ride.""{nameof(Ride.Id)}""
	WHERE leg.""{nameof(Leg.Id)}"" = NEW.""{nameof(Reservation.LegId)}"";

	IF (max_available_places < NEW.""{nameof(Reservation.Count)}"") THEN
		RAISE EXCEPTION '{DbConstants.FunctionErrors.TriggerReservation_EnoughFreePlaces__NotEnougFreePlaces}';
	END IF;

	RETURN NEW;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE TRIGGER ""{DbConstants.TriggerNames.Reservation_EnoughFreePlaces}""
BEFORE INSERT OR UPDATE ON ""{nameof(ApplicationContext.Reservations)}""
FOR EACH ROW
EXECUTE FUNCTION ""{DbConstants.FunctionNames.TriggerReservation_EnoughFreePlaces}""();
";
			var downReservationTriggerCommand = $@"
DROP TRIGGER ""{DbConstants.TriggerNames.Reservation_EnoughFreePlaces}"" ON ""{nameof(ApplicationContext.Reservations)}"";
DROP FUNCTION ""{DbConstants.FunctionNames.TriggerReservation_EnoughFreePlaces}""();
";

			upReservationTriggerCommand.Trim()
				.Should().Be(ReservationTriggerAdded.UpCommand.Trim());
			downReservationTriggerCommand.Trim()
				.Should().Be(ReservationTriggerAdded.DownCommand.Trim());
		}

		private string[] GetAllTriggers() 
			=> new string[]
			{
				nameof(ApplicationContext.Reservations) + '|' + DbConstants.TriggerNames.Reservation_EnoughFreePlaces,
				//nameof(ApplicationContext.Reservations) + '|' + DbConstants.TriggerNames.Ride_EnoughFreePlaces,
			};
	}
}
