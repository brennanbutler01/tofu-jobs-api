using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tofu_Jobs_Server.Migrations
{
    public partial class board : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BoardId",
                table: "JobLists",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Boards",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Title = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boards", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobLists_BoardId",
                table: "JobLists",
                column: "BoardId");

            migrationBuilder.AddForeignKey(
                name: "FK_JobLists_Boards_BoardId",
                table: "JobLists",
                column: "BoardId",
                principalTable: "Boards",
                principalColumn: "Id");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_JobLists_Boards_BoardId",
                table: "JobLists");

            migrationBuilder.DropTable(
                name: "Boards");

            migrationBuilder.DropIndex(
                name: "IX_JobLists_BoardId",
                table: "JobLists");

            migrationBuilder.DropColumn(
                name: "BoardId",
                table: "JobLists");
        }
    }
}
