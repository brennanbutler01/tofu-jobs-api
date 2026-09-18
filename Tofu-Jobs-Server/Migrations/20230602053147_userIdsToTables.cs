using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tofu_Jobs_Server.Migrations
{
    public partial class userIdsToTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Jobs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "JobLists",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Interviews",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "CoverLetters",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "UserId",
                table: "Companies",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Jobs");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "JobLists");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Interviews");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CoverLetters");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Companies");
        }
    }
}
