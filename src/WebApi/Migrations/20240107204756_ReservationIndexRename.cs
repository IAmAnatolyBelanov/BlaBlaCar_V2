using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApi.Migrations
{
    /// <inheritdoc />
    public partial class ReservationIndexRename : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Reservations_LegId_UserId",
                table: "Reservations",
                newName: "Custom_DbIndexName_Reservations_UserId_LegId_UniqueIfActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "Custom_DbIndexName_Reservations_UserId_LegId_UniqueIfActive",
                table: "Reservations",
                newName: "IX_Reservations_LegId_UserId");
        }
    }
}
