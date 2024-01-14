using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApi.Migrations
{
    /// <inheritdoc />
    public partial class ChangeConceptOfLegs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Legs_LegId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "Custom_DbIndexName_Reservations_UserId_LegId_UniqueIfActive",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "PriceInRub",
                table: "Legs");

            migrationBuilder.RenameColumn(
                name: "LegId",
                table: "Reservations",
                newName: "StartLegId");

            migrationBuilder.AddColumn<Guid[]>(
                name: "AffectedLegIds",
                table: "Reservations",
                type: "uuid[]",
                nullable: false,
                defaultValue: new Guid[0]);

            migrationBuilder.AddColumn<Guid>(
                name: "EndLegId",
                table: "Reservations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "NextLegId",
                table: "Legs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "PreviousLegId",
                table: "Legs",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Prices",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PriceInRub = table.Column<int>(type: "integer", nullable: false),
                    StartLegId = table.Column<Guid>(type: "uuid", nullable: false),
                    EndLegId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prices", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prices_Legs_EndLegId",
                        column: x => x.EndLegId,
                        principalTable: "Legs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Prices_Legs_StartLegId",
                        column: x => x.StartLegId,
                        principalTable: "Legs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "Custom_DbIndexName_Reservations_UserId_StartLegId_EndLegId_UniqueIfActive",
                table: "Reservations",
                columns: new[] { "StartLegId", "EndLegId", "UserId" },
                unique: true,
                filter: "\"IsActive\" IS TRUE");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_CreateDateTime",
                table: "Reservations",
                column: "CreateDateTime");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_EndLegId",
                table: "Reservations",
                column: "EndLegId");

            migrationBuilder.CreateIndex(
                name: "IX_Reservations_UserId",
                table: "Reservations",
                column: "UserId");

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

            migrationBuilder.CreateIndex(
                name: "IX_Prices_EndLegId",
                table: "Prices",
                column: "EndLegId");

            migrationBuilder.CreateIndex(
                name: "IX_Prices_StartLegId",
                table: "Prices",
                column: "StartLegId");

            migrationBuilder.CreateIndex(
                name: "IX_Prices_StartLegId_EndLegId",
                table: "Prices",
                columns: new[] { "StartLegId", "EndLegId" },
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

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Legs_EndLegId",
                table: "Reservations",
                column: "EndLegId",
                principalTable: "Legs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Legs_StartLegId",
                table: "Reservations",
                column: "StartLegId",
                principalTable: "Legs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Legs_Legs_NextLegId",
                table: "Legs");

            migrationBuilder.DropForeignKey(
                name: "FK_Legs_Legs_PreviousLegId",
                table: "Legs");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Legs_EndLegId",
                table: "Reservations");

            migrationBuilder.DropForeignKey(
                name: "FK_Reservations_Legs_StartLegId",
                table: "Reservations");

            migrationBuilder.DropTable(
                name: "Prices");

            migrationBuilder.DropIndex(
                name: "Custom_DbIndexName_Reservations_UserId_StartLegId_EndLegId_UniqueIfActive",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_CreateDateTime",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_EndLegId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Reservations_UserId",
                table: "Reservations");

            migrationBuilder.DropIndex(
                name: "IX_Legs_NextLegId",
                table: "Legs");

            migrationBuilder.DropIndex(
                name: "IX_Legs_PreviousLegId",
                table: "Legs");

            migrationBuilder.DropColumn(
                name: "AffectedLegIds",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "EndLegId",
                table: "Reservations");

            migrationBuilder.DropColumn(
                name: "NextLegId",
                table: "Legs");

            migrationBuilder.DropColumn(
                name: "PreviousLegId",
                table: "Legs");

            migrationBuilder.RenameColumn(
                name: "StartLegId",
                table: "Reservations",
                newName: "LegId");

            migrationBuilder.AddColumn<int>(
                name: "PriceInRub",
                table: "Legs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "Custom_DbIndexName_Reservations_UserId_LegId_UniqueIfActive",
                table: "Reservations",
                columns: new[] { "LegId", "UserId" },
                unique: true,
                filter: "\"IsActive\" IS TRUE");

            migrationBuilder.AddForeignKey(
                name: "FK_Reservations_Legs_LegId",
                table: "Reservations",
                column: "LegId",
                principalTable: "Legs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
