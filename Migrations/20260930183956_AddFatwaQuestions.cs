using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalalStocks.Migrations
{
    /// <inheritdoc />
    public partial class AddFatwaQuestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FatwaQuestions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Question = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    Answer = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    IsPublished = table.Column<bool>(type: "bit", nullable: false),
                    IsFeatured = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AnsweredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PublishedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FatwaQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FatwaQuestions_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FatwaQuestions_IsPublished_IsFeatured_PublishedAt",
                table: "FatwaQuestions",
                columns: new[] { "IsPublished", "IsFeatured", "PublishedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_FatwaQuestions_Status",
                table: "FatwaQuestions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_FatwaQuestions_UserId",
                table: "FatwaQuestions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FatwaQuestions");
        }
    }
}
