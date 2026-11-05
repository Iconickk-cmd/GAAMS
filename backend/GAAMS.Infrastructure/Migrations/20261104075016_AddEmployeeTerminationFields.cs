using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GAAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeTerminationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "TerminationDate",
                table: "Employees",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TerminationReason",
                table: "Employees",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TerminationType",
                table: "Employees",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TerminationDate",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TerminationReason",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "TerminationType",
                table: "Employees");
        }
    }
}
