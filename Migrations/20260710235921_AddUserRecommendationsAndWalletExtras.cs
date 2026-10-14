using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalalStocks.Migrations
{
    /// <inheritdoc />
    public partial class AddUserRecommendationsAndWalletExtras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletItems_WalletId",
                table: "WalletItems");

            migrationBuilder.AddColumn<decimal>(
                name: "ExpectedLossPercent",
                table: "WalletItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TargetProfitPercent",
                table: "WalletItems",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "UserStockRecommendations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    StockId = table.Column<int>(type: "int", nullable: false),
                    PurityGradeLabel = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneralTrend = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClosingPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Support1 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Support2 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Resistance1 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Resistance2 = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    StopLoss = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    YtdChangePercent = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    RecommendationTypeId = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserStockRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserStockRecommendations_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UserStockRecommendations_RecommendationTypes_RecommendationTypeId",
                        column: x => x.RecommendationTypeId,
                        principalTable: "RecommendationTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserStockRecommendations_Stocks_StockId",
                        column: x => x.StockId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WalletItems_WalletId_StockId",
                table: "WalletItems",
                columns: new[] { "WalletId", "StockId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserStockRecommendations_RecommendationTypeId",
                table: "UserStockRecommendations",
                column: "RecommendationTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStockRecommendations_StockId",
                table: "UserStockRecommendations",
                column: "StockId");

            migrationBuilder.CreateIndex(
                name: "IX_UserStockRecommendations_UserId_StockId",
                table: "UserStockRecommendations",
                columns: new[] { "UserId", "StockId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserStockRecommendations");

            migrationBuilder.DropIndex(
                name: "IX_WalletItems_WalletId_StockId",
                table: "WalletItems");

            migrationBuilder.DropColumn(
                name: "ExpectedLossPercent",
                table: "WalletItems");

            migrationBuilder.DropColumn(
                name: "TargetProfitPercent",
                table: "WalletItems");

            migrationBuilder.CreateIndex(
                name: "IX_WalletItems_WalletId",
                table: "WalletItems",
                column: "WalletId");
        }
    }
}
