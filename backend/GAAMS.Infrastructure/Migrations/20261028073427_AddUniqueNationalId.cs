using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GAAMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueNationalId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Employees_NationalId",
                table: "Employees",
                column: "NationalId",
                unique: true,
                filter: "\"NationalId\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Employees_NationalId",
                table: "Employees");
        }
    }
}
