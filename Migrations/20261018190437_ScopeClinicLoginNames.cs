using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class ScopeClinicLoginNames : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_DoctorInfos_Specialists_SpecialistId", "DoctorInfos");
            migrationBuilder.AddForeignKey("FK_DoctorInfos_Specialists_SpecialistId", "DoctorInfos", "SpecialistId", "Specialists", principalColumn: "Id");
            migrationBuilder.DropIndex(
                name: "IX_UserInfos_ClinicId",
                table: "UserInfos");

            migrationBuilder.DropIndex(
                name: "IX_DoctorReceptions_ClinicId",
                table: "DoctorReceptions");

            migrationBuilder.DropIndex(
                name: "IX_DoctorReceptions_LoginUsername",
                table: "DoctorReceptions");

            migrationBuilder.DropIndex(
                name: "IX_DoctorInfos_ClinicId",
                table: "DoctorInfos");

            migrationBuilder.DropIndex(
                name: "IX_DoctorInfos_LoginUsername",
                table: "DoctorInfos");

            migrationBuilder.DropIndex(
                name: "IX_DoctorAssists_ClinicId",
                table: "DoctorAssists");

            migrationBuilder.DropIndex(
                name: "IX_DoctorAssists_LoginUsername",
                table: "DoctorAssists");

            migrationBuilder.CreateIndex(
                name: "IX_UserInfos_ClinicId_UserName",
                table: "UserInfos",
                columns: new[] { "ClinicId", "UserName" },
                unique: true,
                filter: "[ClinicId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorReceptions_ClinicId_LoginUsername",
                table: "DoctorReceptions",
                columns: new[] { "ClinicId", "LoginUsername" },
                unique: true,
                filter: "[LoginUsername] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorInfos_ClinicId_LoginUsername",
                table: "DoctorInfos",
                columns: new[] { "ClinicId", "LoginUsername" },
                unique: true,
                filter: "[LoginUsername] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAssists_ClinicId_LoginUsername",
                table: "DoctorAssists",
                columns: new[] { "ClinicId", "LoginUsername" },
                unique: true,
                filter: "[LoginUsername] IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey("FK_DoctorInfos_Specialists_SpecialistId", "DoctorInfos");
            migrationBuilder.AddForeignKey("FK_DoctorInfos_Specialists_SpecialistId", "DoctorInfos", "SpecialistId", "Specialists", principalColumn: "Id", onDelete: ReferentialAction.SetNull);
            migrationBuilder.DropIndex(
                name: "IX_UserInfos_ClinicId_UserName",
                table: "UserInfos");

            migrationBuilder.DropIndex(
                name: "IX_DoctorReceptions_ClinicId_LoginUsername",
                table: "DoctorReceptions");

            migrationBuilder.DropIndex(
                name: "IX_DoctorInfos_ClinicId_LoginUsername",
                table: "DoctorInfos");

            migrationBuilder.DropIndex(
                name: "IX_DoctorAssists_ClinicId_LoginUsername",
                table: "DoctorAssists");

            migrationBuilder.CreateIndex(
                name: "IX_UserInfos_ClinicId",
                table: "UserInfos",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorReceptions_ClinicId",
                table: "DoctorReceptions",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorReceptions_LoginUsername",
                table: "DoctorReceptions",
                column: "LoginUsername",
                unique: true,
                filter: "[LoginUsername] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorInfos_ClinicId",
                table: "DoctorInfos",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorInfos_LoginUsername",
                table: "DoctorInfos",
                column: "LoginUsername",
                unique: true,
                filter: "[LoginUsername] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAssists_ClinicId",
                table: "DoctorAssists",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAssists_LoginUsername",
                table: "DoctorAssists",
                column: "LoginUsername",
                unique: true,
                filter: "[LoginUsername] IS NOT NULL");
        }
    }
}