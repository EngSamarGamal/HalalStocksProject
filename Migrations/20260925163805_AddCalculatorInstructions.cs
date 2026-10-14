using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HalalStocks.Migrations
{
    /// <inheritdoc />
    public partial class AddCalculatorInstructions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CalculatorInstructions",
                columns: table => new
                {
                    Type = table.Column<int>(type: "int", nullable: false),
                    ContentHtml = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CalculatorInstructions", x => x.Type);
                });

            migrationBuilder.InsertData(
                table: "CalculatorInstructions",
                columns: new[] { "Type", "ContentHtml" },
                values: new object[,]
                {
                    { 1, "<p>احسب نصيبك من رأس مال الشركة الموجَّه إلى الاستثمارات الربوية، بناءً على عدد الأسهم التي تمتلكها وإجمالي أصول الشركة ونسبة أنشطتها الربوية.</p>" },
                    { 2, "<p>احسب المبلغ الواجب إخراجه للتطهير من توزيعات الأرباح التي حصلت عليها من السهم، سواء كانت توزيعات نقدية أو توزيعات أسهم.</p>" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CalculatorInstructions");
        }
    }
}
