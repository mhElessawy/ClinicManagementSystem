using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class UPdatePatientDignosisFile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiagnosisFile",
                table: "PatientDiagnoses");

            migrationBuilder.AddColumn<string>(
                name: "DiagnosisFilePath",
                table: "PatientDiagnoses",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiagnosisFilePath",
                table: "PatientDiagnoses");

            migrationBuilder.AddColumn<byte[]>(
                name: "DiagnosisFile",
                table: "PatientDiagnoses",
                type: "varbinary(max)",
                nullable: true);
        }
    }
}
