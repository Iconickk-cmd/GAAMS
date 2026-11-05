using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GAAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnforceOneCurrentPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_EmployeePositionHistories_OneCurrentPosition",
                table: "EmployeePositionHistories",
                column: "EmployeeId",
                unique: true,
                filter: "\"IsCurrent\" = TRUE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_EmployeePositionHistories_OneCurrentPosition",
                table: "EmployeePositionHistories");
        }
    }
}
