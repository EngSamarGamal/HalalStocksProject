using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalalStocks.Migrations
{
    /// <inheritdoc />
    public partial class AddStockIndexAndShariahRatios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AdditionalNotes",
                table: "Stocks",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastReviewedAt",
                table: "Stocks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "LiquidAssetsRatio",
                table: "Stocks",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NonShariahInvestmentRatio",
                table: "Stocks",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NonShariahObligationsRatio",
                table: "Stocks",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ProhibitedIncomeRatio",
                table: "Stocks",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "StockIndices",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NameAr = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    NameEn = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockIndices", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StockStockIndex",
                columns: table => new
                {
                    IndicesId = table.Column<int>(type: "int", nullable: false),
                    StocksId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StockStockIndex", x => new { x.IndicesId, x.StocksId });
                    table.ForeignKey(
                        name: "FK_StockStockIndex_StockIndices_IndicesId",
                        column: x => x.IndicesId,
                        principalTable: "StockIndices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_StockStockIndex_Stocks_StocksId",
                        column: x => x.StocksId,
                        principalTable: "Stocks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StockStockIndex_StocksId",
                table: "StockStockIndex",
                column: "StocksId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "StockStockIndex");

            migrationBuilder.DropTable(
                name: "StockIndices");

            migrationBuilder.DropColumn(
                name: "AdditionalNotes",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "LastReviewedAt",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "LiquidAssetsRatio",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "NonShariahInvestmentRatio",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "NonShariahObligationsRatio",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "ProhibitedIncomeRatio",
                table: "Stocks");
        }
    }
}
