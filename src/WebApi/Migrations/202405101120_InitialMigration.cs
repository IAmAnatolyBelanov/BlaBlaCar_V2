using FluentMigrator;

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
			.WithColumn("Id").AsGuid().PrimaryKey();

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
			.WithColumn("Created").AsDateTimeOffset();

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
			.WithColumn("LastCheckDate").AsDateTimeOffset();

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
	}
}
