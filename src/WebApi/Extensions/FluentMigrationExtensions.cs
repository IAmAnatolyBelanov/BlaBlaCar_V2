using FluentMigrator.Builders.Create.Table;

namespace WebApi.Extensions;

public static class FluentMigrationExtensions
{
	public static ICreateTableWithColumnSyntax WithTechnicalCommentColumn(this ICreateTableWithColumnSyntax createTable)
		=> createTable.WithColumn("TechnicalComment")
			.AsString()
			.Nullable()
			.WithColumnDescription("Comment for support. Does not used in service logic.");

	public static ICreateTableColumnOptionOrWithColumnSyntax AsPoint(this ICreateTableColumnAsTypeSyntax createTableWithColumn)
		=> createTableWithColumn.AsCustom("geography (point)");

	public static ICreateTableColumnOptionOrWithColumnSyntax AsInt32Array(this ICreateTableColumnAsTypeSyntax createTableWithColumn)
		=> createTableWithColumn.AsCustom("integer[]");
}