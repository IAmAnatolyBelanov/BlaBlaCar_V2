using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace WebApi.Migrations
{
    /// <inheritdoc />
    public partial class LegIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Point>(
                name: "To",
                table: "Legs",
                type: "geography (point)",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geometry");

            migrationBuilder.AlterColumn<Point>(
                name: "From",
                table: "Legs",
                type: "geography (point)",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geometry");

            migrationBuilder.CreateIndex(
                name: "IX_Legs_EndTime",
                table: "Legs",
                column: "EndTime");

            migrationBuilder.CreateIndex(
                name: "IX_Legs_From",
                table: "Legs",
                column: "From")
                .Annotation("Npgsql:IndexMethod", "GIST");

            migrationBuilder.CreateIndex(
                name: "IX_Legs_StartTime",
                table: "Legs",
                column: "StartTime");

            migrationBuilder.CreateIndex(
                name: "IX_Legs_To",
                table: "Legs",
                column: "To")
                .Annotation("Npgsql:IndexMethod", "GIST");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Legs_EndTime",
                table: "Legs");

            migrationBuilder.DropIndex(
                name: "IX_Legs_From",
                table: "Legs");

            migrationBuilder.DropIndex(
                name: "IX_Legs_StartTime",
                table: "Legs");

            migrationBuilder.DropIndex(
                name: "IX_Legs_To",
                table: "Legs");

            migrationBuilder.AlterColumn<Point>(
                name: "To",
                table: "Legs",
                type: "geometry",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geography (point)");

            migrationBuilder.AlterColumn<Point>(
                name: "From",
                table: "Legs",
                type: "geometry",
                nullable: false,
                oldClrType: typeof(Point),
                oldType: "geography (point)");
        }
    }
}
