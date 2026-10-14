using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalalStocks.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalyticsFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add the new discriminator first, backfill it from the column it replaces,
            // and only then drop that column — so no article's kind is lost in the swap.
            migrationBuilder.AddColumn<int>(
                name: "Type",
                table: "Articles",
                type: "int",
                nullable: false,
                defaultValue: 0); // ArticleType.Standard

            migrationBuilder.Sql(@"
                UPDATE Articles SET Type = 1 WHERE IsShariahStandard = 1; -- ArticleType.ShariahStandard
            ");

            migrationBuilder.DropColumn(
                name: "IsShariahStandard",
                table: "Articles");

            migrationBuilder.AddColumn<bool>(
                name: "IsAnalyst",
                table: "AspNetUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "ApprovalStatus",
                table: "Articles",
                type: "int",
                nullable: false,
                defaultValue: 0); // ArticleApprovalStatus.NotRequired

            migrationBuilder.AddColumn<string>(
                name: "HideReason",
                table: "Articles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AutoAssignedForType",
                table: "ArticleCategories",
                type: "int",
                nullable: true);

            // Seed the auto-assigned category for Analytical articles (id resolved at
            // runtime via IArticleService.GetCategoryForTypeAsync — see ArticleCategory.cs).
            migrationBuilder.Sql(@"
                INSERT INTO ArticleCategories (NameAr, AutoAssignedForType)
                VALUES (N'تحليلات وآراء', 2); -- ArticleType.Analytical
            ");

            migrationBuilder.CreateTable(
                name: "AnalystProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    SummaryAr = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhotoPath = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalystProfiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalystProfiles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ArticleComments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ContentAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleComments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleComments_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleComments_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ArticleLikes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ArticleId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ArticleLikes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ArticleLikes_Articles_ArticleId",
                        column: x => x.ArticleId,
                        principalTable: "Articles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ArticleLikes_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalystProfiles_UserId",
                table: "AnalystProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleComments_ArticleId",
                table: "ArticleComments",
                column: "ArticleId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleComments_UserId",
                table: "ArticleComments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ArticleLikes_ArticleId_UserId",
                table: "ArticleLikes",
                columns: new[] { "ArticleId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ArticleLikes_UserId",
                table: "ArticleLikes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalystProfiles");

            migrationBuilder.DropTable(
                name: "ArticleComments");

            migrationBuilder.DropTable(
                name: "ArticleLikes");

            migrationBuilder.DropColumn(
                name: "IsAnalyst",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "ApprovalStatus",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "HideReason",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "Articles");

            migrationBuilder.DropColumn(
                name: "AutoAssignedForType",
                table: "ArticleCategories");

            migrationBuilder.AddColumn<bool>(
                name: "IsShariahStandard",
                table: "Articles",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
