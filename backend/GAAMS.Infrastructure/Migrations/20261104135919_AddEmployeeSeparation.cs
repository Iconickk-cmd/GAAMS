using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GAAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeSeparation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeSeparations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    SeparationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Reason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    ProposedLastWorkingDate = table.Column<DateTime>(type: "date", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    RequestedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "PendingApproval"),
                    DecisionBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    DecisionAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DecisionComments = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    FinalizedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSeparations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSeparations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeSeparationClearances",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeSeparationId = table.Column<int>(type: "integer", nullable: false),
                    DepartmentName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Status = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false, defaultValue: "Pending"),
                    Remarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    ClearedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
                    ClearedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSeparationClearances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSeparationClearances_EmployeeSeparations_EmployeeSe~",
                        column: x => x.EmployeeSeparationId,
                        principalTable: "EmployeeSeparations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSeparationClearances_SeparationId",
                table: "EmployeeSeparationClearances",
                column: "EmployeeSeparationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSeparations_OneActiveRequest",
                table: "EmployeeSeparations",
                column: "EmployeeId",
                unique: true,
                filter: "\"Status\" IN ('PendingApproval', 'Approved', 'PendingClearance')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeSeparationClearances");

            migrationBuilder.DropTable(
                name: "EmployeeSeparations");
        }
    }
}
