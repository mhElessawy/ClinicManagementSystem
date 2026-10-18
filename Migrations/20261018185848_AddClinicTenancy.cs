using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicTenancy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PatientInvoices_DoctorInfos_DoctorId",
                table: "PatientInvoices");

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "UserInfos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "Patients",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "PatientInvoices",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "PatientInvoiceItems",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "PatientDiagnoses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "Locations",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "DoctorSubscriptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "DoctorReceptions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "DoctorInfos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "DoctorGroups",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "DoctorGroupMembers",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "DoctorAssists",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "Appointments",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ClinicId",
                table: "AppointmentIntakes",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Clinics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Code = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clinics", x => x.Id);
                });

            // Preserve existing group boundaries. Ambiguous legacy data must be reviewed
            // before assigning a clinic; never silently expose it to a different clinic.
            migrationBuilder.Sql("""
                IF EXISTS (SELECT DoctorId FROM DoctorGroupMembers GROUP BY DoctorId HAVING COUNT(DISTINCT DoctorGroupId) > 1)
                    THROW 51000, 'Clinic migration: a doctor belongs to multiple groups. Resolve these memberships before retrying.', 1;

                INSERT INTO Clinics (Name, Code, Phone, Email, Active, CreatedAt)
                SELECT GroupName, CONCAT('CL-GROUP-', Id), '', '', Active, GETUTCDATE() FROM DoctorGroups;
                UPDATE g SET ClinicId = c.Id FROM DoctorGroups g
                JOIN Clinics c ON c.Code = CONCAT('CL-GROUP-', g.Id);
                UPDATE d SET ClinicId = g.ClinicId FROM DoctorInfos d
                JOIN DoctorGroupMembers m ON m.DoctorId = d.Id
                JOIN DoctorGroups g ON g.Id = m.DoctorGroupId;

                IF EXISTS (SELECT 1 FROM DoctorInfos WHERE ClinicId IS NULL)
                   OR EXISTS (SELECT 1 FROM Patients WHERE DoctorId IS NULL)
                   OR EXISTS (SELECT 1 FROM UserInfos WHERE ISNULL(RoleId, 0) <> 1 AND GroupId IS NULL)
                BEGIN
                    INSERT INTO Clinics (Name, Code, Phone, Email, Active, CreatedAt)
                    VALUES (N'Legacy clinic - review assignment', 'CL-LEGACY', '', '', 1, GETUTCDATE());
                    DECLARE @legacyId int = SCOPE_IDENTITY();
                    INSERT INTO DoctorGroups (GroupName, Active, CreatedDate, ClinicId)
                    VALUES (N'Legacy clinic', 1, GETUTCDATE(), @legacyId);
                    DECLARE @legacyGroupId int = SCOPE_IDENTITY();
                    UPDATE DoctorInfos SET ClinicId = @legacyId WHERE ClinicId IS NULL;
                    INSERT INTO DoctorGroupMembers (DoctorGroupId, DoctorId, AddedDate, ClinicId)
                    SELECT @legacyGroupId, Id, GETUTCDATE(), @legacyId FROM DoctorInfos WHERE ClinicId = @legacyId;
                    UPDATE Patients SET ClinicId = @legacyId WHERE DoctorId IS NULL;
                    UPDATE UserInfos SET GroupId = @legacyGroupId WHERE ISNULL(RoleId, 0) <> 1 AND GroupId IS NULL;
                END;

                UPDATE u SET ClinicId = g.ClinicId FROM UserInfos u
                JOIN DoctorGroups g ON g.Id = u.GroupId WHERE ISNULL(u.RoleId, 0) <> 1;
                UPDATE UserInfos SET RoleId = 6 WHERE RoleId = 2 AND ClinicId IS NOT NULL;
                UPDATE m SET ClinicId = g.ClinicId FROM DoctorGroupMembers m JOIN DoctorGroups g ON g.Id = m.DoctorGroupId;
                UPDATE p SET ClinicId = d.ClinicId FROM Patients p JOIN DoctorInfos d ON d.Id = p.DoctorId;
                UPDATE a SET ClinicId = d.ClinicId FROM DoctorAssists a JOIN DoctorInfos d ON d.Id = a.DoctorId;
                UPDATE r SET ClinicId = d.ClinicId FROM DoctorReceptions r JOIN DoctorInfos d ON d.Id = r.DoctorId;
                UPDATE s SET ClinicId = d.ClinicId FROM DoctorSubscriptions s JOIN DoctorInfos d ON d.Id = s.DoctorId;
                UPDATE l SET ClinicId = d.ClinicId FROM Locations l JOIN DoctorInfos d ON d.Id = l.DoctorId;
                UPDATE a SET ClinicId = p.ClinicId FROM Appointments a JOIN Patients p ON p.Id = a.PatientId;
                UPDATE a SET ClinicId = p.ClinicId FROM AppointmentIntakes a JOIN Appointments p ON p.Id = a.AppointmentId;
                UPDATE a SET ClinicId = p.ClinicId FROM PatientDiagnoses a JOIN Patients p ON p.Id = a.PatientId;
                UPDATE a SET ClinicId = p.ClinicId FROM PatientInvoices a JOIN Patients p ON p.Id = a.PatientId;
                UPDATE a SET ClinicId = p.ClinicId FROM PatientInvoiceItems a JOIN PatientInvoices p ON p.Id = a.InvoiceId;

                IF EXISTS (SELECT 1 FROM Appointments a JOIN DoctorInfos d ON d.Id = a.DoctorId WHERE a.ClinicId <> d.ClinicId)
                   OR EXISTS (SELECT 1 FROM PatientDiagnoses a JOIN DoctorInfos d ON d.Id = a.DoctorId WHERE a.ClinicId <> d.ClinicId)
                   OR EXISTS (SELECT 1 FROM PatientInvoices a JOIN DoctorInfos d ON d.Id = a.DoctorId WHERE a.ClinicId <> d.ClinicId)
                   OR EXISTS (SELECT 1 FROM Appointments a JOIN Locations l ON l.Id = a.LocationId WHERE a.ClinicId <> l.ClinicId)
                   OR EXISTS (SELECT 1 FROM PatientDiagnoses a JOIN Appointments p ON p.Id = a.AppointmentId WHERE a.ClinicId <> p.ClinicId)
                   OR EXISTS (SELECT 1 FROM PatientInvoices a JOIN Appointments p ON p.Id = a.AppointmentId WHERE a.ClinicId <> p.ClinicId)
                    THROW 51001, 'Clinic migration: clinical records span multiple groups. Review their clinic assignment before retrying.', 1;
                """);

            migrationBuilder.UpdateData(
                table: "UserInfos",
                keyColumn: "Id",
                keyValue: 1,
                column: "ClinicId",
                value: null);

            migrationBuilder.CreateIndex(
                name: "IX_UserInfos_ClinicId",
                table: "UserInfos",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_ClinicId",
                table: "Patients",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientInvoices_ClinicId",
                table: "PatientInvoices",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientInvoiceItems_ClinicId",
                table: "PatientInvoiceItems",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientDiagnoses_ClinicId",
                table: "PatientDiagnoses",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_Locations_ClinicId",
                table: "Locations",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorSubscriptions_ClinicId",
                table: "DoctorSubscriptions",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorReceptions_ClinicId",
                table: "DoctorReceptions",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorInfos_ClinicId",
                table: "DoctorInfos",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorGroups_ClinicId",
                table: "DoctorGroups",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorGroupMembers_ClinicId",
                table: "DoctorGroupMembers",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAssists_ClinicId",
                table: "DoctorAssists",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ClinicId",
                table: "Appointments",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_AppointmentIntakes_ClinicId",
                table: "AppointmentIntakes",
                column: "ClinicId");

            migrationBuilder.CreateIndex(
                name: "IX_Clinics_Code",
                table: "Clinics",
                column: "Code",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_AppointmentIntakes_Clinics_ClinicId",
                table: "AppointmentIntakes",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Appointments_Clinics_ClinicId",
                table: "Appointments",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorAssists_Clinics_ClinicId",
                table: "DoctorAssists",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorGroupMembers_Clinics_ClinicId",
                table: "DoctorGroupMembers",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorGroups_Clinics_ClinicId",
                table: "DoctorGroups",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorInfos_Clinics_ClinicId",
                table: "DoctorInfos",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorReceptions_Clinics_ClinicId",
                table: "DoctorReceptions",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DoctorSubscriptions_Clinics_ClinicId",
                table: "DoctorSubscriptions",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Locations_Clinics_ClinicId",
                table: "Locations",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientDiagnoses_Clinics_ClinicId",
                table: "PatientDiagnoses",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientInvoiceItems_Clinics_ClinicId",
                table: "PatientInvoiceItems",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientInvoices_Clinics_ClinicId",
                table: "PatientInvoices",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PatientInvoices_DoctorInfos_DoctorId",
                table: "PatientInvoices",
                column: "DoctorId",
                principalTable: "DoctorInfos",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Patients_Clinics_ClinicId",
                table: "Patients",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserInfos_Clinics_ClinicId",
                table: "UserInfos",
                column: "ClinicId",
                principalTable: "Clinics",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AppointmentIntakes_Clinics_ClinicId",
                table: "AppointmentIntakes");

            migrationBuilder.DropForeignKey(
                name: "FK_Appointments_Clinics_ClinicId",
                table: "Appointments");

            migrationBuilder.DropForeignKey(
                name: "FK_DoctorAssists_Clinics_ClinicId",
                table: "DoctorAssists");

            migrationBuilder.DropForeignKey(
                name: "FK_DoctorGroupMembers_Clinics_ClinicId",
                table: "DoctorGroupMembers");

            migrationBuilder.DropForeignKey(
                name: "FK_DoctorGroups_Clinics_ClinicId",
                table: "DoctorGroups");

            migrationBuilder.DropForeignKey(
                name: "FK_DoctorInfos_Clinics_ClinicId",
                table: "DoctorInfos");

            migrationBuilder.DropForeignKey(
                name: "FK_DoctorReceptions_Clinics_ClinicId",
                table: "DoctorReceptions");

            migrationBuilder.DropForeignKey(
                name: "FK_DoctorSubscriptions_Clinics_ClinicId",
                table: "DoctorSubscriptions");

            migrationBuilder.DropForeignKey(
                name: "FK_Locations_Clinics_ClinicId",
                table: "Locations");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientDiagnoses_Clinics_ClinicId",
                table: "PatientDiagnoses");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientInvoiceItems_Clinics_ClinicId",
                table: "PatientInvoiceItems");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientInvoices_Clinics_ClinicId",
                table: "PatientInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_PatientInvoices_DoctorInfos_DoctorId",
                table: "PatientInvoices");

            migrationBuilder.DropForeignKey(
                name: "FK_Patients_Clinics_ClinicId",
                table: "Patients");

            migrationBuilder.DropForeignKey(
                name: "FK_UserInfos_Clinics_ClinicId",
                table: "UserInfos");

            migrationBuilder.DropTable(
                name: "Clinics");

            migrationBuilder.DropIndex(
                name: "IX_UserInfos_ClinicId",
                table: "UserInfos");

            migrationBuilder.DropIndex(
                name: "IX_Patients_ClinicId",
                table: "Patients");

            migrationBuilder.DropIndex(
                name: "IX_PatientInvoices_ClinicId",
                table: "PatientInvoices");

            migrationBuilder.DropIndex(
                name: "IX_PatientInvoiceItems_ClinicId",
                table: "PatientInvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_PatientDiagnoses_ClinicId",
                table: "PatientDiagnoses");

            migrationBuilder.DropIndex(
                name: "IX_Locations_ClinicId",
                table: "Locations");

            migrationBuilder.DropIndex(
                name: "IX_DoctorSubscriptions_ClinicId",
                table: "DoctorSubscriptions");

            migrationBuilder.DropIndex(
                name: "IX_DoctorReceptions_ClinicId",
                table: "DoctorReceptions");

            migrationBuilder.DropIndex(
                name: "IX_DoctorInfos_ClinicId",
                table: "DoctorInfos");

            migrationBuilder.DropIndex(
                name: "IX_DoctorGroups_ClinicId",
                table: "DoctorGroups");

            migrationBuilder.DropIndex(
                name: "IX_DoctorGroupMembers_ClinicId",
                table: "DoctorGroupMembers");

            migrationBuilder.DropIndex(
                name: "IX_DoctorAssists_ClinicId",
                table: "DoctorAssists");

            migrationBuilder.DropIndex(
                name: "IX_Appointments_ClinicId",
                table: "Appointments");

            migrationBuilder.DropIndex(
                name: "IX_AppointmentIntakes_ClinicId",
                table: "AppointmentIntakes");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "UserInfos");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "Patients");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "PatientInvoices");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "PatientInvoiceItems");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "PatientDiagnoses");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "Locations");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "DoctorSubscriptions");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "DoctorReceptions");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "DoctorInfos");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "DoctorGroups");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "DoctorGroupMembers");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "DoctorAssists");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "ClinicId",
                table: "AppointmentIntakes");

            migrationBuilder.AddForeignKey(
                name: "FK_PatientInvoices_DoctorInfos_DoctorId",
                table: "PatientInvoices",
                column: "DoctorId",
                principalTable: "DoctorInfos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
