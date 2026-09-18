using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tofu_Jobs_Server.Migrations
{
    public partial class fixedActivityCategory : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ActivityCategories",
                table: "Activities",
                newName: "ActivityCategory");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ActivityCategory",
                table: "Activities",
                newName: "ActivityCategories");
        }
    }
}
