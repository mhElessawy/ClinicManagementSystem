using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ClinicManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class Latest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
           
          

        
        


        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentIntakes");

            migrationBuilder.DropTable(
                name: "DoctorAssists");

            migrationBuilder.DropTable(
                name: "DoctorGroupMembers");

            migrationBuilder.DropTable(
                name: "DoctorReceptions");

            migrationBuilder.DropTable(
                name: "DoctorSubscriptions");

            migrationBuilder.DropTable(
                name: "IntakeQuestions");

            migrationBuilder.DropTable(
                name: "PatientDiagnoses");

            migrationBuilder.DropTable(
                name: "PatientInvoiceItems");

            migrationBuilder.DropTable(
                name: "PatientInvoices");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "Locations");

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
                name: "DoctorGroups");

            migrationBuilder.DropTable(
                name: "Roles");
        }
    }
}
