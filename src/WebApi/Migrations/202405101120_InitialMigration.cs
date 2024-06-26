﻿using FluentMigrator;
using FluentMigrator.Postgres;

namespace WebApi.Migrations;

[Migration(2024_05_10_1120)]
public class InitialMigration : PostgresMigrator
{
	public override void Up()
	{
		Execute.Sql("CREATE EXTENSION IF NOT EXISTS postgis;");

		Create.Table("CloudApiResponseInfos")
			.WithColumn("Id").AsGuid().PrimaryKey()
			.WithColumn("Created").AsDateTimeOffset()
			.WithColumn("Request").AsString()
			.WithColumn("RequestBasePath").AsString()
			.WithColumn("Response").AsCustom("jsonb");

		Create.Index()
			.OnTable("CloudApiResponseInfos")
			.OnColumn("RequestBasePath");

		Create.Table("Users")
			.WithColumn("Id").AsGuid().PrimaryKey()
			.WithTechnicalCommentColumn();

		Create.Table("PersonDatas")
			.WithColumn("Id").AsGuid().PrimaryKey()
			.WithColumn("UserId").AsGuid().Nullable()
			.WithColumn("PassportSeries").AsInt32()
			.WithColumn("PassportNumber").AsInt32()
			.WithColumn("FirstName").AsString()
			.WithColumn("LastName").AsString()
			.WithColumn("SecondName").AsString().Nullable()
			.WithColumn("BirthDate").AsDateTimeOffset()
			.WithColumn("Inn").AsInt64()
			.WithColumn("IsPassportValid").AsBoolean()
			.WithColumn("WasCheckedAtLeastOnce").AsBoolean()
			.WithColumn("LastCheckPassportDate").AsDateTimeOffset()
			.WithColumn("Created").AsDateTimeOffset()
			.WithTechnicalCommentColumn();

		Create.ForeignKey()
			.FromTable("PersonDatas").ForeignColumn("UserId")
			.ToTable("Users").PrimaryColumn("Id");
		Create.Index()
			.OnTable("PersonDatas")
			.OnColumn("UserId");

		Create.Index()
			.OnTable("PersonDatas")
			.OnColumn("PassportSeries")
			.Ascending()
			.OnColumn("PassportNumber")
			.Ascending();

		Create.Table("DriverDatas")
			.WithColumn("Id").AsGuid().PrimaryKey()
			.WithColumn("UserId").AsGuid().Nullable()
			.WithColumn("LicenseSeries").AsInt32()
			.WithColumn("LicenseNumber").AsInt32()
			.WithColumn("Issuance").AsDateTimeOffset()
			.WithColumn("ValidTill").AsDateTimeOffset()
			.WithColumn("Categories").AsString()
			.WithColumn("BirthDate").AsDateTimeOffset()
			.WithColumn("Created").AsDateTimeOffset()
			.WithColumn("IsValid").AsBoolean()
			.WithColumn("LastCheckDate").AsDateTimeOffset()
			.WithTechnicalCommentColumn();

		Create.ForeignKey()
			.FromTable("DriverDatas").ForeignColumn("UserId")
			.ToTable("Users").PrimaryColumn("Id");
		Create.Index()
			.OnTable("DriverDatas")
			.OnColumn("UserId");

		Create.Index()
			.OnTable("DriverDatas")
			.OnColumn("LicenseSeries")
			.Ascending()
			.OnColumn("LicenseNumber")
			.Ascending();

		Create.Table("Cars")
			.WithColumn("Id").AsGuid().PrimaryKey()
			.WithColumn("Created").AsDateTimeOffset()
			.WithColumn("Vin").AsString()
			.WithColumn("RegistrationNumber").AsString().Nullable()
			.WithColumn("DoesVinAndRegistrationNumberMatches").AsBoolean()
			.WithColumn("Name").AsString().Nullable()
			.WithColumn("SeatsCount").AsInt32().WithColumnDescription("Count of seats in the car including driver's one")
			.WithColumn("IsDeleted").AsBoolean()
			.WithColumn("IsVinValid").AsBoolean()
			.WithTechnicalCommentColumn();

		Create.Index()
			.OnTable("Cars")
			.OnColumn("Vin");
		Create.Index()
			.OnTable("Cars")
			.OnColumn("RegistrationNumber");
		Create.Index()
			.OnTable("Cars")
			.OnColumn("Vin")
			.Ascending()
			.OnColumn("RegistrationNumber")
			.Ascending();

		Create.Table("Cars_Users")
			.WithColumn("UserId").AsGuid()
			.WithColumn("CarId").AsGuid();

		Create.PrimaryKey()
			.OnTable("Cars_Users")
			.Columns("UserId", "CarId");

		Create.Table("Rides")
			.WithColumn("Id").AsGuid().PrimaryKey()
			.WithColumn("AuthorId").AsGuid()
			.WithColumn("DriverId").AsGuid()
			.WithColumn("Created").AsDateTimeOffset()
			.WithColumn("AvailablePlacesCount").AsInt32()
			.WithColumn("Comment").AsString().Nullable()
			.WithColumn("IsCashPaymentMethodAvailable").AsBoolean()
			.WithColumn("IsCashlessPaymentMethodAvailable").AsBoolean()
			.WithColumn("ValidationMethod").AsInt32()
			.WithColumn("ValidationTimeBeforeDeparture").AsTime().Nullable()
			.WithColumn("AfterRideValidationTimeoutAction").AsInt32().Nullable()
			.WithColumn("IsDeleted").AsBoolean()
			.WithColumn("StartWaypointId").AsGuid().WithColumnDescription("Column does not have FK because it would be cycled loop. And cycled loop is painful")
			.WithTechnicalCommentColumn();

		Create.ForeignKey()
			.FromTable("Rides").ForeignColumn("AuthorId")
			.ToTable("Users").PrimaryColumn("Id");
		Create.Index()
			.OnTable("Rides")
			.OnColumn("AuthorId");

		Create.ForeignKey()
			.FromTable("Rides").ForeignColumn("DriverId")
			.ToTable("Users").PrimaryColumn("Id");
		Create.Index()
			.OnTable("Rides")
			.OnColumn("DriverId");

		Create.Index()
			.OnTable("Rides")
			.OnColumn("IsCashPaymentMethodAvailable");
		Create.Index()
			.OnTable("Rides")
			.OnColumn("IsCashlessPaymentMethodAvailable");

		Create.Index()
			.OnTable("Rides")
			.OnColumn("ValidationMethod");

		Create.Table("Waypoints")
			.WithColumn("Id").AsGuid().PrimaryKey()
			.WithColumn("RideId").AsGuid()
			.WithColumn("Point").AsPoint()
			.WithColumn("FullName").AsString()
			.WithColumn("NameToCity").AsString()
			.WithColumn("Arrival").AsDateTimeOffset()
			.WithColumn("Departure").AsDateTimeOffset().Nullable()
			.WithColumn("PreviousWaypointId").AsGuid().Nullable()
			.WithColumn("NextWaypointId").AsGuid().Nullable();

		Create.ForeignKey()
			.FromTable("Waypoints").ForeignColumn("RideId")
			.ToTable("Rides").PrimaryColumn("Id");
		Create.Index()
			.OnTable("Waypoints")
			.OnColumn("RideId");

		Create.Index()
			.OnTable("Waypoints")
			.OnColumn("Arrival");

		Create.Index()
			.OnTable("Waypoints")
			.OnColumn("Departure");

		Create.Index()
			.OnTable("Waypoints")
			.OnColumn("Point")
			.Ascending()
			.WithOptions()
			.UsingGist();

		Create.ForeignKey()
			.FromTable("Waypoints").ForeignColumn("PreviousWaypointId")
			.ToTable("Waypoints").PrimaryColumn("Id");
		Create.ForeignKey()
			.FromTable("Waypoints").ForeignColumn("NextWaypointId")
			.ToTable("Waypoints").PrimaryColumn("Id");

		Create.Table("Legs")
			.WithColumn("Id").AsGuid().PrimaryKey()
			.WithColumn("RideId").AsGuid()
			.WithColumn("WaypointFromId").AsGuid()
			.WithColumn("WaypointToId").AsGuid()
			.WithColumn("PriceInRub").AsInt32()
			.WithColumn("IsManual").AsBoolean()
			.WithColumn("IsBetweenNeighborPoints").AsBoolean();

		Create.ForeignKey()
			.FromTable("Legs").ForeignColumn("RideId")
			.ToTable("Rides").PrimaryColumn("Id");
		Create.Index()
			.OnTable("Legs")
			.OnColumn("RideId");

		Create.ForeignKey()
			.FromTable("Legs").ForeignColumn("WaypointFromId")
			.ToTable("Waypoints").PrimaryColumn("Id");
		Create.Index()
			.OnTable("Legs")
			.OnColumn("WaypointFromId");

		Create.ForeignKey()
			.FromTable("Legs").ForeignColumn("WaypointToId")
			.ToTable("Waypoints").PrimaryColumn("Id");
		Create.Index()
			.OnTable("Legs")
			.OnColumn("WaypointToId");

		Create.Index()
			.OnTable("Legs")
			.OnColumn("PriceInRub");

		Create.Table("Reservations")
			.WithColumn("Id").AsGuid().PrimaryKey()
			.WithColumn("RideId").AsGuid()
			.WithColumn("PassengerId").AsGuid()
			.WithColumn("PeopleCount").AsInt32()
			.WithColumn("LegId").AsGuid()
			.WithColumn("IsDeleted").AsBoolean()
			.WithColumn("Created").AsDateTimeOffset()
			.WithTechnicalCommentColumn();

		Create.ForeignKey()
			.FromTable("Reservations").ForeignColumn("RideId")
			.ToTable("Rides").PrimaryColumn("Id");
		Create.Index()
			.OnTable("Reservations")
			.OnColumn("RideId");

		Create.ForeignKey()
			.FromTable("Reservations").ForeignColumn("PassengerId")
			.ToTable("Users").PrimaryColumn("Id");
		Create.Index()
			.OnTable("Reservations")
			.OnColumn("PassengerId");

		Create.ForeignKey()
			.FromTable("Reservations").ForeignColumn("LegId")
			.ToTable("Legs").PrimaryColumn("Id");
		Create.Index()
			.OnTable("Reservations")
			.OnColumn("LegId");

		Create.Table("AffectedByReservationsLegs")
			.WithColumn("ReservationId").AsGuid()
			.WithColumn("LegId").AsGuid();

		Create.PrimaryKey()
			.OnTable("AffectedByReservationsLegs")
			.Columns("ReservationId", "LegId");

		Create.ForeignKey()
			.FromTable("AffectedByReservationsLegs").ForeignColumn("ReservationId")
			.ToTable("Reservations").PrimaryColumn("Id");

		Create.ForeignKey()
			.FromTable("AffectedByReservationsLegs").ForeignColumn("LegId")
			.ToTable("Legs").PrimaryColumn("Id");
	}
}
