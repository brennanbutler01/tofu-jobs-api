using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tofu_Jobs_Server.Migrations
{
    public partial class addDefaultJobLists : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DefaultJobLists",
                table: "JobLists",
                type: "integer",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultJobLists",
                table: "JobLists");
        }
    }
}
