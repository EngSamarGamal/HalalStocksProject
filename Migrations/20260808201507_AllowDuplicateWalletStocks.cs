using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalalStocks.Migrations
{
    /// <inheritdoc />
    public partial class AllowDuplicateWalletStocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletItems_WalletId_StockId",
                table: "WalletItems");

            migrationBuilder.CreateIndex(
                name: "IX_WalletItems_WalletId",
                table: "WalletItems",
                column: "WalletId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WalletItems_WalletId",
                table: "WalletItems");

            migrationBuilder.CreateIndex(
                name: "IX_WalletItems_WalletId_StockId",
                table: "WalletItems",
                columns: new[] { "WalletId", "StockId" },
                unique: true);
        }
    }
}
