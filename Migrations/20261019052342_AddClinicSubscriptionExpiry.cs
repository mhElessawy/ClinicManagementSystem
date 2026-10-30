using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ClinicManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddClinicSubscriptionExpiry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateOnly>(
                name: "SubscriptionEndsOn",
                table: "Clinics",
                type: "date",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubscriptionEndsOn",
                table: "Clinics");
        }
    }
}
