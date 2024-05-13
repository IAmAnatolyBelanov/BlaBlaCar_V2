using FluentMigrator.Builders.Create.Table;

namespace WebApi.Extensions;

public static class FluentMigrationExtensions
{
	public static ICreateTableWithColumnSyntax WithTechnicalCommentColumn(this ICreateTableWithColumnSyntax createTable)
		=> createTable.WithColumn("TechnicalComment")
			.AsString()
			.Nullable()
			.WithColumnDescription("Comment for support. Does not used in service logic.");
}