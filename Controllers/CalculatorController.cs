using HalalStocks.Services.Interfaces;
using iText.IO.Font;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using Microsoft.AspNetCore.Mvc;
using SysPath = System.IO.Path;

namespace HalalStocks.Controllers;

public class PurificationEntry
{
    public string  Ticker        { get; set; } = "";
    public string  NameAr        { get; set; } = "";
    public decimal Buy           { get; set; }
    public decimal Sell          { get; set; }
    public decimal Profit        { get; set; }
    public decimal Ratio         { get; set; }
    public decimal Purification  { get; set; }
}

public class CalculatorController : Controller
{
    private readonly IStockService    _stockService;
    private readonly IWebHostEnvironment _env;

    public CalculatorController(IStockService stockService, IWebHostEnvironment env)
    {
        _stockService = stockService;
        _env          = env;
    }

    [Route("calculator")]
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated != true)
            return View("~/Views/Calculator/GuestGate.cshtml");
        return View("~/Views/Calculator/Index.cshtml");
    }

    [HttpGet("/api/stocks/purifiable")]
    public async Task<IActionResult> Purifiable()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized();

        var stocks = await _stockService.GetPurifiableStocksAsync();
        return Json(stocks.Select(s => new
        {
            ticker            = s.TickerSymbol,
            nameAr            = s.NameAr,
            purificationRatio = s.PurificationRatio ?? 0m
        }));
    }

    [HttpPost("/api/calculator/pdf")]
    public IActionResult GeneratePdf([FromBody] List<PurificationEntry> items)
    {
        if (items == null || items.Count == 0)
            return BadRequest("لا توجد بيانات.");

        var totalProfit       = items.Sum(x => x.Profit);
        var totalPurification = items.Sum(x => x.Purification);
        var today             = DateTime.Now.ToString("yyyy/MM/dd");

        // Arabic Typesetting font — proper OpenType shaping tables for connected Arabic letters
        var fontPath    = SysPath.Combine(_env.WebRootPath, "fonts", "arabtype.ttf");
        var fontProgram = FontProgramFactory.CreateFont(fontPath);
        var font        = PdfFontFactory.CreateFont(fontProgram, PdfEncodings.IDENTITY_H,
                              PdfFontFactory.EmbeddingStrategy.PREFER_EMBEDDED);

        var headerGreen = new DeviceRgb(0x19, 0x87, 0x54);
        var lightGreen  = new DeviceRgb(0xf0, 0xfa, 0xf4);
        var altRow      = new DeviceRgb(0xf8, 0xf9, 0xfa);

        using var ms     = new MemoryStream();
        var pdfWriter    = new PdfWriter(ms);
        var pdfDoc       = new PdfDocument(pdfWriter);
        var doc          = new Document(pdfDoc, PageSize.A4.Rotate());
        doc.SetMargins(15, 15, 15, 15);
        doc.SetFont(font);

        // ── Logo + header ─────────────────────────────────────────────
        var logoPath = SysPath.Combine(_env.WebRootPath, "logo3.jpeg");
        if (System.IO.File.Exists(logoPath))
        {
            doc.Add(new iText.Layout.Element.Image(ImageDataFactory.Create(logoPath))
                .SetWidth(50).SetHeight(50)
                .SetBorderRadius(new BorderRadius(25))
                .SetHorizontalAlignment(HorizontalAlignment.CENTER));
        }

        doc.Add(new Paragraph("كاشف")
            .SetBaseDirection(BaseDirection.RIGHT_TO_LEFT)
            .SetFont(font).SetFontSize(18).SetBold()
            .SetFontColor(headerGreen)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginTop(4));

        doc.Add(new Paragraph("تقرير حاسبة تطهير الدخل المحظور")
            .SetBaseDirection(BaseDirection.RIGHT_TO_LEFT)
            .SetFont(font).SetFontSize(13).SetBold()
            .SetTextAlignment(TextAlignment.CENTER));

        doc.Add(new Paragraph("التاريخ: " + today)
            .SetBaseDirection(BaseDirection.RIGHT_TO_LEFT)
            .SetFont(font).SetFontSize(10)
            .SetFontColor(ColorConstants.GRAY)
            .SetTextAlignment(TextAlignment.CENTER)
            .SetMarginBottom(10));

        // ── Table — columns reversed RIGHT→LEFT ───────────────────────
        // Visual: مبلغ التطهير | نسبة التطهير | الربح | قيمة البيع | قيمة الشراء | اسم السهم | رمز السهم
        var table = new Table(UnitValue.CreatePercentArray(new float[] { 3, 2, 3, 3, 3, 4, 2 }))
            .UseAllAvailableWidth()
            .SetMarginBottom(10);

        Cell HeaderCell(string text) => new Cell()
            .SetBackgroundColor(headerGreen)
            .SetFontColor(ColorConstants.WHITE)
            .SetFont(font).SetFontSize(9).SetBold()
            .SetTextAlignment(TextAlignment.CENTER)
            .SetPadding(6)
            .Add(new Paragraph(text)
                .SetBaseDirection(BaseDirection.RIGHT_TO_LEFT));

        table.AddHeaderCell(HeaderCell("مبلغ التطهير (ج.م)"));
        table.AddHeaderCell(HeaderCell("نسبة التطهير"));
        table.AddHeaderCell(HeaderCell("الربح (ج.م)"));
        table.AddHeaderCell(HeaderCell("قيمة البيع (ج.م)"));
        table.AddHeaderCell(HeaderCell("قيمة الشراء (ج.م)"));
        table.AddHeaderCell(HeaderCell("اسم السهم"));
        table.AddHeaderCell(HeaderCell("رمز السهم"));

        var rowIndex = 0;
        foreach (var item in items)
        {
            var bg = rowIndex++ % 2 == 0 ? ColorConstants.WHITE : altRow;
            var border = new SolidBorder(new DeviceRgb(0xde, 0xe2, 0xe6), 0.5f);

            Cell DataCell(string text, bool arabic = false, bool green = false, bool bold = false)
            {
                var p = new Paragraph(text).SetFont(font).SetFontSize(9);
                if (arabic) p.SetBaseDirection(BaseDirection.RIGHT_TO_LEFT);
                if (green)  p.SetFontColor(headerGreen);
                if (bold)   p.SetBold();
                return new Cell()
                    .SetBackgroundColor(bg)
                    .SetBorderBottom(border)
                    .SetPadding(5)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .Add(p);
            }

            table.AddCell(DataCell(item.Purification.ToString("N2"), green: true, bold: true));
            table.AddCell(DataCell(item.Ratio.ToString("N2") + "%"));
            table.AddCell(DataCell(item.Profit.ToString("N2")));
            table.AddCell(DataCell(item.Sell.ToString("N2")));
            table.AddCell(DataCell(item.Buy.ToString("N2")));
            table.AddCell(DataCell(item.NameAr, arabic: true));
            table.AddCell(DataCell(item.Ticker, bold: true));
        }

        // Totals row
        Cell TotalCell(string text, bool arabic = false, bool green = false) => new Cell()
            .SetBackgroundColor(lightGreen)
            .SetBorderTop(new SolidBorder(headerGreen, 1))
            .SetPadding(6).SetTextAlignment(TextAlignment.CENTER)
            .Add(new Paragraph(text).SetFont(font).SetFontSize(9).SetBold()
                .SetFontColor(green ? headerGreen : ColorConstants.BLACK)
                .SetBaseDirection(arabic ? BaseDirection.RIGHT_TO_LEFT : BaseDirection.LEFT_TO_RIGHT));

        table.AddCell(TotalCell(totalPurification.ToString("N2"), green: true));
        table.AddCell(TotalCell(""));
        table.AddCell(TotalCell(totalProfit.ToString("N2")));
        table.AddCell(new Cell(1, 4)
            .SetBackgroundColor(lightGreen)
            .SetBorderTop(new SolidBorder(headerGreen, 1))
            .SetPadding(6).SetTextAlignment(TextAlignment.CENTER)
            .Add(new Paragraph("الإجمالي")
                .SetBaseDirection(BaseDirection.RIGHT_TO_LEFT)
                .SetFont(font).SetFontSize(9).SetBold()));

        doc.Add(table);

        doc.Add(new Paragraph("ملاحظة: يُنصح بصرف مبلغ التطهير في وجوه الخير والصدقات")
            .SetBaseDirection(BaseDirection.RIGHT_TO_LEFT)
            .SetFont(font).SetFontSize(8)
            .SetFontColor(ColorConstants.GRAY)
            .SetBorderTop(new SolidBorder(new DeviceRgb(0xcc, 0xcc, 0xcc), 0.5f))
            .SetPaddingTop(8));

        doc.Close();
        return File(ms.ToArray(), "application/pdf", "تقرير-التطهير.pdf");
    }
}
