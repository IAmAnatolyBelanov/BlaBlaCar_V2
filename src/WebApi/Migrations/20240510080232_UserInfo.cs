using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApi.Migrations
{
    /// <inheritdoc />
    public partial class UserInfo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CloudApiResponseInfos",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now() at time zone 'utc'"),
                    Request = table.Column<string>(type: "text", nullable: false),
                    RequestBasePath = table.Column<string>(type: "text", nullable: false),
                    Response = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloudApiResponseInfos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DriverDatas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    LicenseSeries = table.Column<int>(type: "integer", nullable: false),
                    LicenseNumber = table.Column<int>(type: "integer", nullable: false),
                    Issuance = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ValidTill = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Categories = table.Column<string>(type: "text", nullable: false),
                    BirthDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsValid = table.Column<bool>(type: "boolean", nullable: false),
                    LastCheckDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DriverDatas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DriverDatas_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PersonDatas",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    PassportSeries = table.Column<int>(type: "integer", nullable: false),
                    PassportNumber = table.Column<int>(type: "integer", nullable: false),
                    FirstName = table.Column<string>(type: "text", nullable: false),
                    LastName = table.Column<string>(type: "text", nullable: false),
                    SecondName = table.Column<string>(type: "text", nullable: true),
                    BirthDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Inn = table.Column<long>(type: "bigint", nullable: false),
                    IsPassportValid = table.Column<bool>(type: "boolean", nullable: false),
                    WasCheckedAtLeastOnce = table.Column<bool>(type: "boolean", nullable: false),
                    LastCheckPassportDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserId1 = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId2 = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonDatas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonDatas_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonDatas_Users_UserId1",
                        column: x => x.UserId1,
                        principalTable: "Users",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PersonDatas_Users_UserId2",
                        column: x => x.UserId2,
                        principalTable: "Users",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CloudApiResponseInfos_Created",
                table: "CloudApiResponseInfos",
                column: "Created");

            migrationBuilder.CreateIndex(
                name: "IX_CloudApiResponseInfos_RequestBasePath",
                table: "CloudApiResponseInfos",
                column: "RequestBasePath");

            migrationBuilder.CreateIndex(
                name: "IX_DriverDatas_LicenseSeries_LicenseNumber",
                table: "DriverDatas",
                columns: new[] { "LicenseSeries", "LicenseNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DriverDatas_UserId",
                table: "DriverDatas",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonDatas_Inn",
                table: "PersonDatas",
                column: "Inn");

            migrationBuilder.CreateIndex(
                name: "IX_PersonDatas_PassportSeries_PassportNumber",
                table: "PersonDatas",
                columns: new[] { "PassportSeries", "PassportNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonDatas_UserId",
                table: "PersonDatas",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonDatas_UserId1",
                table: "PersonDatas",
                column: "UserId1",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PersonDatas_UserId2",
                table: "PersonDatas",
                column: "UserId2");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CloudApiResponseInfos");

            migrationBuilder.DropTable(
                name: "DriverDatas");

            migrationBuilder.DropTable(
                name: "PersonDatas");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
