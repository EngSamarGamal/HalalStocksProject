using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalalStocks.Migrations
{
    /// <inheritdoc />
    public partial class RemoveInvestmentStyleFromPortfolioRequest : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Style",
                table: "PortfolioRequests");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Style",
                table: "PortfolioRequests",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
