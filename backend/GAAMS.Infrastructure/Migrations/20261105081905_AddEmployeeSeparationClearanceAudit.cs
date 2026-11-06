using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace GAAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmployeeSeparationClearanceAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EmployeeSeparationClearanceAudits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmployeeSeparationClearanceId = table.Column<int>(type: "integer", nullable: false),
                    PreviousClearedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PreviousRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    PreviousClearedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CorrectedClearedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CorrectedRemarks = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CorrectionReason = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    CorrectedBy = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    CorrectedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeSeparationClearanceAudits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeSeparationClearanceAudits_EmployeeSeparationClearan~",
                        column: x => x.EmployeeSeparationClearanceId,
                        principalTable: "EmployeeSeparationClearances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeSeparationClearanceAudits_ClearanceId",
                table: "EmployeeSeparationClearanceAudits",
                column: "EmployeeSeparationClearanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmployeeSeparationClearanceAudits");
        }
    }
}
