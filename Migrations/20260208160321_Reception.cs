using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class Reception : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DoctorReceptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    ReceptionName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ReceptionTel1 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ReceptionTel2 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ReceptionAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    LoginUsername = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LoginPassword = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CanLogin = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorReceptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorReceptions_DoctorInfos_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "DoctorInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorReceptions_DoctorId",
                table: "DoctorReceptions",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorReceptions_LoginUsername",
                table: "DoctorReceptions",
                column: "LoginUsername",
                unique: true,
                filter: "[LoginUsername] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DoctorReceptions");
        }
    }
}
