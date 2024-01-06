using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApi.Migrations
{
    /// <inheritdoc />
    public partial class CompositeLegAdded : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CompositeLegs",
                columns: table => new
                {
                    MasterLegId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubLegId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompositeLegs", x => new { x.MasterLegId, x.SubLegId });
                    table.ForeignKey(
                        name: "FK_CompositeLegs_Legs_MasterLegId",
                        column: x => x.MasterLegId,
                        principalTable: "Legs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CompositeLegs_Legs_SubLegId",
                        column: x => x.SubLegId,
                        principalTable: "Legs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompositeLegs_MasterLegId",
                table: "CompositeLegs",
                column: "MasterLegId");

            migrationBuilder.CreateIndex(
                name: "IX_CompositeLegs_SubLegId",
                table: "CompositeLegs",
                column: "SubLegId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompositeLegs");
        }
    }
}
