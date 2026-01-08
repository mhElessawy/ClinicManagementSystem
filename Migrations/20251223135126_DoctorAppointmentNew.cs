using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ClinicManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class DoctorAppointmentNew : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {


            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    AppointmentDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AppointmentTime = table.Column<TimeSpan>(type: "time", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    CreatedBy = table.Column<int>(type: "int", nullable: false),
                    CreatedByType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DeletedBy = table.Column<int>(type: "int", nullable: true),
                    DeletedByType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DeletionDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_DoctorInfos_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "DoctorInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Appointments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "DoctorAssists");

            migrationBuilder.DropTable(
                name: "PatientDiagnoses");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "DoctorInfos");

            migrationBuilder.DropTable(
                name: "Specialists");

            migrationBuilder.DropTable(
                name: "UserInfos");

            migrationBuilder.DropTable(
                name: "Departments");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
