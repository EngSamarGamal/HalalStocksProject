using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HalalStocks.Migrations
{
    /// <inheritdoc />
    public partial class AddEntryOptionalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "BuyDate",
                table: "CalculationEntries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuyInvoiceNumber",
                table: "CalculationEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SellDate",
                table: "CalculationEntries",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SellInvoiceNumber",
                table: "CalculationEntries",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BuyDate",
                table: "CalculationEntries");

            migrationBuilder.DropColumn(
                name: "BuyInvoiceNumber",
                table: "CalculationEntries");

            migrationBuilder.DropColumn(
                name: "SellDate",
                table: "CalculationEntries");

            migrationBuilder.DropColumn(
                name: "SellInvoiceNumber",
                table: "CalculationEntries");
        }
    }
}
