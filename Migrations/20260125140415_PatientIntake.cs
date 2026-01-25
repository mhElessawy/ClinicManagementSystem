using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class PatientIntake : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppointmentIntakes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AppointmentId = table.Column<int>(type: "int", nullable: false),
                    BloodPressure = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    HeartRate = table.Column<int>(type: "int", nullable: true),
                    Temperature = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    Height = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ChiefComplaint = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CurrentSymptoms = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SymptomDuration = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PainLevel = table.Column<int>(type: "int", nullable: true),
                    CurrentMedications = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Allergies = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PreviousConditions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SpecialtyQuestionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalNotes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PatientArrived = table.Column<bool>(type: "bit", nullable: false),
                    ArrivalTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReadyForDoctor = table.Column<bool>(type: "bit", nullable: false),
                    PerformedBy = table.Column<int>(type: "int", nullable: false),
                    PerformedByName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IntakeDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppointmentIntakes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AppointmentIntakes_Appointments_AppointmentId",
                        column: x => x.AppointmentId,
                        principalTable: "Appointments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IntakeQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SpecialistId = table.Column<int>(type: "int", nullable: false),
                    QuestionAr = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    QuestionEn = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    QuestionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Options = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IntakeQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IntakeQuestions_Specialists_SpecialistId",
                        column: x => x.SpecialistId,
                        principalTable: "Specialists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentIntakes_AppointmentId",
                table: "AppointmentIntakes",
                column: "AppointmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IntakeQuestions_SpecialistId",
                table: "IntakeQuestions",
                column: "SpecialistId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppointmentIntakes");

            migrationBuilder.DropTable(
                name: "IntakeQuestions");
        }
    }
}
