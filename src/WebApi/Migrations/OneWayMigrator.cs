using FluentMigrator;

namespace WebApi.Migrations;

public abstract class OneWayMigrator : Migration
{
	public override void Down()
	{
		throw new NotSupportedException();
	}
}
