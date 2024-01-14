using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApi.Migrations
{
    /// <inheritdoc />
    public partial class TryToLiveWithoutForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Legs_Legs_NextLegId",
                table: "Legs");

            migrationBuilder.DropForeignKey(
                name: "FK_Legs_Legs_PreviousLegId",
                table: "Legs");

            migrationBuilder.DropIndex(
                name: "IX_Legs_NextLegId",
                table: "Legs");

            migrationBuilder.DropIndex(
                name: "IX_Legs_PreviousLegId",
                table: "Legs");

            migrationBuilder.CreateIndex(
                name: "IX_Legs_NextLegId",
                table: "Legs",
                column: "NextLegId");

            migrationBuilder.CreateIndex(
                name: "IX_Legs_PreviousLegId",
                table: "Legs",
                column: "PreviousLegId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Legs_NextLegId",
                table: "Legs");

            migrationBuilder.DropIndex(
                name: "IX_Legs_PreviousLegId",
                table: "Legs");

            migrationBuilder.CreateIndex(
                name: "IX_Legs_NextLegId",
                table: "Legs",
                column: "NextLegId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Legs_PreviousLegId",
                table: "Legs",
                column: "PreviousLegId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Legs_Legs_NextLegId",
                table: "Legs",
                column: "NextLegId",
                principalTable: "Legs",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Legs_Legs_PreviousLegId",
                table: "Legs",
                column: "PreviousLegId",
                principalTable: "Legs",
                principalColumn: "Id");
        }
    }
}
