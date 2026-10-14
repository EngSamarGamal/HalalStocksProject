using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalalStocks.Migrations
{
    /// <inheritdoc />
    public partial class AddRecommendationTypesLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Recommendation",
                table: "StockRecommendations",
                newName: "RecommendationTypeId");

            migrationBuilder.CreateTable(
                name: "RecommendationTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsExpiredMarker = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecommendationTypes", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "RecommendationTypes",
                columns: new[] { "NameAr", "IsExpiredMarker" },
                values: new object[,]
                {
                    { "متاجرة", false },
                    { "شراء الانخفاضات", false },
                    { "احتفاظ", false },
                    { "منتهية", true }
                });

            migrationBuilder.Sql(@"
                UPDATE StockRecommendations
                SET RecommendationTypeId = CASE
                    WHEN RecommendationTypeId = 0 THEN 1
                    WHEN RecommendationTypeId = 1 THEN 2
                    WHEN RecommendationTypeId = 2 THEN 3
                    WHEN RecommendationTypeId = 3 THEN 4
                    ELSE 1
                END;
            ");

            migrationBuilder.CreateIndex(
                name: "IX_StockRecommendations_RecommendationTypeId",
                table: "StockRecommendations",
                column: "RecommendationTypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockRecommendations_RecommendationTypes_RecommendationTypeId",
                table: "StockRecommendations",
                column: "RecommendationTypeId",
                principalTable: "RecommendationTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockRecommendations_RecommendationTypes_RecommendationTypeId",
                table: "StockRecommendations");

            migrationBuilder.DropTable(
                name: "RecommendationTypes");

            migrationBuilder.DropIndex(
                name: "IX_StockRecommendations_RecommendationTypeId",
                table: "StockRecommendations");

            migrationBuilder.RenameColumn(
                name: "RecommendationTypeId",
                table: "StockRecommendations",
                newName: "Recommendation");
        }
    }
}
