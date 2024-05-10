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
	}
}
