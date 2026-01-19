using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ClinicManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AllDataBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Departments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Departments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CanManageDepartments = table.Column<bool>(type: "bit", nullable: false),
                    CanManageSpecialists = table.Column<bool>(type: "bit", nullable: false),
                    CanManageDoctors = table.Column<bool>(type: "bit", nullable: false),
                    CanManagePatients = table.Column<bool>(type: "bit", nullable: false),
                    CanManageDiagnoses = table.Column<bool>(type: "bit", nullable: false),
                    CanManageUsers = table.Column<bool>(type: "bit", nullable: false),
                    CanViewReports = table.Column<bool>(type: "bit", nullable: false),
                    CanManageAssistants = table.Column<bool>(type: "bit", nullable: false),
                    ViewAllPatients = table.Column<bool>(type: "bit", nullable: false),
                    ViewOwnPatientsOnly = table.Column<bool>(type: "bit", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Specialists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<int>(type: "int", nullable: false),
                    SpecialistName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Specialists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Specialists_Departments_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "Departments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    UserPassword = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    UserFullName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    UserTel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    JobTitle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RoleId = table.Column<int>(type: "int", nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserInfos_Roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DoctorInfos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DoctorAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DoctorTitle = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    SpecialistId = table.Column<int>(type: "int", nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    DoctorCivilId = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    DoctorTel1 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    DoctorTel2 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DoctorDetails = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RegDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DoctorPicture = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    LoginUsername = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LoginPassword = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CanLogin = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorInfos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorInfos_Specialists_SpecialistId",
                        column: x => x.SpecialistId,
                        principalTable: "Specialists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DoctorInfos_UserInfos_UserId",
                        column: x => x.UserId,
                        principalTable: "UserInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "DoctorAssists",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    AssistName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AssistTel1 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AssistTel2 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AssistAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    LoginUsername = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    LoginPassword = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    CanLogin = table.Column<bool>(type: "bit", nullable: false),
                    LastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorAssists", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorAssists_DoctorInfos_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "DoctorInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "DoctorSubscriptions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DoctorId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SubscriptionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorSubscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorSubscriptions_DoctorInfos_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "DoctorInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PatientCivilID = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    PatientTel1 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PatientTel2 = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    PatientAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    DoctorId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Patients_DoctorInfos_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "DoctorInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

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

            migrationBuilder.CreateTable(
                name: "PatientDiagnoses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<int>(type: "int", nullable: false),
                    DoctorId = table.Column<int>(type: "int", nullable: true),
                    DiagnosisDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DiagnosisDetails = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DiagnosisFilePath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientDiagnoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientDiagnoses_DoctorInfos_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "DoctorInfos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_PatientDiagnoses_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Departments",
                columns: new[] { "Id", "DepartmentName" },
                values: new object[,]
                {
                    { 1, "Internal Medicine" },
                    { 2, "Surgery" },
                    { 3, "Pediatrics" },
                    { 4, "Orthopedics" },
                    { 5, "Cardiology" }
                });

            migrationBuilder.InsertData(
                table: "Roles",
                columns: new[] { "Id", "Active", "CanManageAssistants", "CanManageDepartments", "CanManageDiagnoses", "CanManageDoctors", "CanManagePatients", "CanManageSpecialists", "CanManageUsers", "CanViewReports", "Description", "RoleName", "ViewAllPatients", "ViewOwnPatientsOnly" },
                values: new object[,]
                {
                    { 1, true, true, true, true, true, true, true, true, true, "Full system access", "Super Admin", true, false },
                    { 2, true, true, true, false, true, true, true, false, true, "Administrative access", "Admin", true, false },
                    { 3, true, false, false, true, false, true, false, false, false, "Doctor access - own patients only", "Doctor", false, true },
                    { 4, true, false, false, false, false, true, false, false, false, "Doctor assistant - limited access", "Assistant", false, true },
                    { 5, true, false, false, false, false, true, false, false, false, "Front desk - patient management only", "Receptionist", true, false }
                });

            migrationBuilder.InsertData(
                table: "Specialists",
                columns: new[] { "Id", "DepartmentId", "Description", "SpecialistName" },
                values: new object[,]
                {
                    { 1, 1, "Heart and cardiovascular diseases", "Cardiology" },
                    { 2, 2, "General surgical procedures", "General Surgery" },
                    { 3, 3, "Children's health care", "Pediatrics" },
                    { 4, 4, "Bone and joint surgery", "Orthopedic Surgery" },
                    { 5, 5, "Heart and blood vessel diseases", "Cardiovascular" }
                });

            migrationBuilder.InsertData(
                table: "UserInfos",
                columns: new[] { "Id", "Active", "JobTitle", "RoleId", "UserFullName", "UserName", "UserPassword", "UserTel" },
                values: new object[] { 1, true, "Administrator", 1, "System Administrator", "admin", "Admin@123", null });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId",
                table: "Appointments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAssists_DoctorId",
                table: "DoctorAssists",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorAssists_LoginUsername",
                table: "DoctorAssists",
                column: "LoginUsername",
                unique: true,
                filter: "[LoginUsername] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorInfos_LoginUsername",
                table: "DoctorInfos",
                column: "LoginUsername",
                unique: true,
                filter: "[LoginUsername] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorInfos_SpecialistId",
                table: "DoctorInfos",
                column: "SpecialistId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorInfos_UserId",
                table: "DoctorInfos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DoctorSubscriptions_DoctorId",
                table: "DoctorSubscriptions",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientDiagnoses_DoctorId",
                table: "PatientDiagnoses",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_PatientDiagnoses_PatientId",
                table: "PatientDiagnoses",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_DoctorId",
                table: "Patients",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Specialists_DepartmentId",
                table: "Specialists",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_UserInfos_RoleId",
                table: "UserInfos",
                column: "RoleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "DoctorAssists");

            migrationBuilder.DropTable(
                name: "DoctorSubscriptions");

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
