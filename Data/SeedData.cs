using HalalStocks.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Data;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        var context = serviceProvider.GetRequiredService<AppDbContext>();
        var userManager = serviceProvider.GetRequiredService<UserManager<AppUser>>();

        await context.Database.MigrateAsync();

        // Seed categories
        if (!context.StockCategories.Any())
        {
            var categories = new List<StockCategory>
            {
                new() { NameAr = "بنوك", NameEn = "Banks" },
                new() { NameAr = "عقارات", NameEn = "Real Estate" },
                new() { NameAr = "صناعة", NameEn = "Industry" },
                new() { NameAr = "استثمار متنوع", NameEn = "Diversified Investment" },
            };
            context.StockCategories.AddRange(categories);
            await context.SaveChangesAsync();
        }


        // Seed stocks
        if (!context.Stocks.Any())
        {
            var banksId = context.StockCategories.First(c => c.NameEn == "Banks").Id;
            var realEstateId = context.StockCategories.First(c => c.NameEn == "Real Estate").Id;
            var industryId = context.StockCategories.First(c => c.NameEn == "Industry").Id;
            var diversifiedId = context.StockCategories.First(c => c.NameEn == "Diversified Investment").Id;
            var stocks = new List<Stock>
            {
                new() { TickerSymbol = "COMI", NameAr = "البنك التجاري الدولي", NameEn = "CIB Egypt", CategoryId = banksId, ShariahStatus = ShariahStatus.Halal },
                new() { TickerSymbol = "ADIB", NameAr = "بنك أبوظبي الإسلامي مصر", NameEn = "ADIB Egypt", CategoryId = banksId, ShariahStatus = ShariahStatus.Halal },
                new() { TickerSymbol = "EKHW", NameAr = "الإسكان والتعمير", NameEn = "Housing and Development Bank", CategoryId = realEstateId, ShariahStatus = ShariahStatus.Halal },
                new() { TickerSymbol = "MNHD", NameAr = "مدينة نصر للإسكان والتعمير", NameEn = "MNHD", CategoryId = realEstateId, ShariahStatus = ShariahStatus.Halal },
                new() { TickerSymbol = "PHDC", NameAr = "بالم هيلز للتعمير", NameEn = "Palm Hills", CategoryId = realEstateId, ShariahStatus = ShariahStatus.MustPurify, PurificationRatio = 2.5m, Notes = "نسبة إيرادات غير متوافقة لا تتجاوز الحد المسموح" },
                new() { TickerSymbol = "SWDY", NameAr = "السويدي إليكتريك", NameEn = "El Sewedy Electric", CategoryId = industryId, ShariahStatus = ShariahStatus.Halal },
                new() { TickerSymbol = "SKPC", NameAr = "سيدي كرير للبتروكيماويات", NameEn = "SKPC", CategoryId = industryId, ShariahStatus = ShariahStatus.MustPurify, PurificationRatio = 1.8m },
                new() { TickerSymbol = "ORWE", NameAr = "أوراسكوم للاستثمار والتنمية الصناعية", NameEn = "OCI", CategoryId = diversifiedId, ShariahStatus = ShariahStatus.Halal },
                new() { TickerSymbol = "EFIH", NameAr = "إي إف جي هيرميس القابضة", NameEn = "EFG Hermes", CategoryId = diversifiedId, ShariahStatus = ShariahStatus.MustPurify, PurificationRatio = 3.2m, Notes = "تتضمن محفظة الأوراق المالية بعض الأدوات غير المتوافقة" },
                new() { TickerSymbol = "CIEB", NameAr = "البنك التجاري الدولي الإسلامي", NameEn = "CIB Islamic", CategoryId = banksId, ShariahStatus = ShariahStatus.Prohibited, Notes = "يتعامل بالفائدة الربوية" },
            };
            context.Stocks.AddRange(stocks);
            await context.SaveChangesAsync();
        }

        // Seed indices
        if (!context.StockIndices.Any())
        {
            context.StockIndices.AddRange(
                new StockIndex { NameAr = "EGX 30", NameEn = "EGX 30 Index" },
                new StockIndex { NameAr = "الشريعة 33", NameEn = "EGX Shariah 33" }
            );
            await context.SaveChangesAsync();
        }

        // Seed financial statement types
        if (!context.FinancialStatementTypes.Any())
        {
            context.FinancialStatementTypes.AddRange(
                new FinancialStatementType { NameAr = "ربع سنوي - الربع الأول (Q1)", NameEn = "Q1 Quarterly", DisplayOrder = 1 },
                new FinancialStatementType { NameAr = "ربع سنوي - الربع الثاني (Q2)", NameEn = "Q2 Quarterly", DisplayOrder = 2 },
                new FinancialStatementType { NameAr = "ربع سنوي - الربع الثالث (Q3)", NameEn = "Q3 Quarterly", DisplayOrder = 3 },
                new FinancialStatementType { NameAr = "سنوي", NameEn = "Annual", DisplayOrder = 4 },
                new FinancialStatementType { NameAr = "نصف سنوي", NameEn = "Semi-Annual", DisplayOrder = 5 }
            );
            await context.SaveChangesAsync();
        }

        // Seed site settings
        if (!context.SiteSettings.Any())
        {
            context.SiteSettings.Add(new SiteSettings());
            await context.SaveChangesAsync();
        }

        // Seed admin user
        const string adminEmail = "admin@halalstocks.eg";
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "مدير النظام",
                Role = UserRole.SuperAdmin,
                EmailConfirmed = true
            };
            await userManager.CreateAsync(admin, "Admin@123456");
        }
    }
}
