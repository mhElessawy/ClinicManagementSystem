using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class PatientIntake1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AppointmentId",
                table: "PatientDiagnoses",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PatientDiagnoses_AppointmentId",
                table: "PatientDiagnoses",
                column: "AppointmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientDiagnoses_Appointments_AppointmentId",
                table: "PatientDiagnoses",
                column: "AppointmentId",
                principalTable: "Appointments",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatientDiagnoses_Appointments_AppointmentId",
                table: "PatientDiagnoses");

            migrationBuilder.DropIndex(
                name: "IX_PatientDiagnoses_AppointmentId",
                table: "PatientDiagnoses");

            migrationBuilder.DropColumn(
                name: "AppointmentId",
                table: "PatientDiagnoses");
        }
    }
}
