using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class ClinicManagerRole : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add GroupId column to UserInfos
            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "UserInfos",
                type: "int",
                nullable: true);

            // Create index for GroupId
            migrationBuilder.CreateIndex(
                name: "IX_UserInfos_GroupId",
                table: "UserInfos",
                column: "GroupId");

            // Add foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_UserInfos_DoctorGroups_GroupId",
                table: "UserInfos",
                column: "GroupId",
                principalTable: "DoctorGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            // Seed the Clinic Manager role
            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Active", "CanManageAssistants", "CanManageDepartments", "CanManageDiagnoses", "CanManageDoctors", "CanManagePatients", "CanManageSpecialists", "CanManageUsers", "CanViewReports", "Description", "RoleName", "ViewAllPatients", "ViewOwnPatientsOnly" },
                values: new object[] { 6, true, true, false, false, true, false, false, false, true, "Manages a group of doctors - can add doctors, view income, manage assistants and receptionists", "Clinic Manager", false, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove Clinic Manager role seed
            migrationBuilder.DeleteData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 6);

            // Drop foreign key
            migrationBuilder.DropForeignKey(
                name: "FK_UserInfos_DoctorGroups_GroupId",
                table: "UserInfos");

            // Drop index
            migrationBuilder.DropIndex(
                name: "IX_UserInfos_GroupId",
                table: "UserInfos");

            // Drop column
            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "UserInfos");
        }
    }
}
