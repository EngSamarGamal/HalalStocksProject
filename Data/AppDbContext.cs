using HalalStocks.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Data;

public class AppDbContext : IdentityDbContext<AppUser>
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Stock> Stocks => Set<Stock>();
    public DbSet<StockCategory> StockCategories => Set<StockCategory>();
    public DbSet<Article> Articles => Set<Article>();
    public DbSet<ArticleCategory> ArticleCategories => Set<ArticleCategory>();
    public DbSet<Video> Videos => Set<Video>();
    public DbSet<Banner> Banners => Set<Banner>();
    public DbSet<FatwaQuestion> FatwaQuestions => Set<FatwaQuestion>();
    public DbSet<VideoCategory> VideoCategories => Set<VideoCategory>();
    public DbSet<StockIndex> StockIndices => Set<StockIndex>();
    public DbSet<CalculationSession> CalculationSessions => Set<CalculationSession>();
    public DbSet<CalculationEntry>   CalculationEntries  => Set<CalculationEntry>();
    public DbSet<ZakatRecord>              ZakatRecords             => Set<ZakatRecord>();
    public DbSet<CapitalPurificationRecord> CapitalPurificationRecords => Set<CapitalPurificationRecord>();
    public DbSet<DividendPurificationRecord> DividendPurificationRecords => Set<DividendPurificationRecord>();
    public DbSet<CalculatorInstruction>    CalculatorInstructions   => Set<CalculatorInstruction>();
    public DbSet<GoldPriceSnapshot>        GoldPriceSnapshots       => Set<GoldPriceSnapshot>();
    public DbSet<FinancialStatementType>   FinancialStatementTypes  => Set<FinancialStatementType>();
    public DbSet<StockFinancialReview>     StockFinancialReviews    => Set<StockFinancialReview>();
    public DbSet<PortfolioInfoSection>     PortfolioInfoSections    => Set<PortfolioInfoSection>();
    public DbSet<PortfolioRequest>         PortfolioRequests        => Set<PortfolioRequest>();
    public DbSet<SiteSettings>             SiteSettings             => Set<SiteSettings>();
    public DbSet<RecommendationRequest>    RecommendationRequests   => Set<RecommendationRequest>();
    public DbSet<WalletManagementRequest>  WalletManagementRequests => Set<WalletManagementRequest>();
    public DbSet<RecommendationType>        RecommendationTypes      => Set<RecommendationType>();
    public DbSet<StockRecommendation>      StockRecommendations     => Set<StockRecommendation>();
    public DbSet<UserStockRecommendation>  UserStockRecommendations => Set<UserStockRecommendation>();
    public DbSet<InvestmentWallet>         InvestmentWallets        => Set<InvestmentWallet>();
    public DbSet<WalletItem>               WalletItems              => Set<WalletItem>();
    public DbSet<RealPortfolio>            RealPortfolios           => Set<RealPortfolio>();
    public DbSet<RealPortfolioItem>        RealPortfolioItems       => Set<RealPortfolioItem>();
    public DbSet<AnalystProfile>           AnalystProfiles          => Set<AnalystProfile>();
    public DbSet<ArticleLike>              ArticleLikes             => Set<ArticleLike>();
    public DbSet<ArticleComment>           ArticleComments          => Set<ArticleComment>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Stock>().Property(s => s.PurificationRatio).HasPrecision(5, 2);
        builder.Entity<Stock>().Property(s => s.ProhibitedIncomeRatio).HasPrecision(5, 2);
        builder.Entity<Stock>().Property(s => s.NonShariahInvestmentRatio).HasPrecision(5, 2);
        builder.Entity<Stock>().Property(s => s.NonShariahObligationsRatio).HasPrecision(5, 2);
        builder.Entity<Stock>().Property(s => s.LiquidAssetsRatio).HasPrecision(5, 2);
        builder.Entity<Stock>().Property(s => s.TotalCompanyAssets).HasPrecision(18, 2);

        builder.Entity<Stock>()
            .HasMany(s => s.Indices)
            .WithMany(i => i.Stocks)
            .UsingEntity(j => j.ToTable("StockStockIndex"));

        foreach (var prop in new[] {
            builder.Entity<CalculationSession>().Property(e => e.TotalProfit),
            builder.Entity<CalculationSession>().Property(e => e.TotalPurification) })
            prop.HasPrecision(18, 2);

        foreach (var prop in new[] {
            builder.Entity<CalculationEntry>().Property(e => e.Buy),
            builder.Entity<CalculationEntry>().Property(e => e.Sell),
            builder.Entity<CalculationEntry>().Property(e => e.Profit),
            builder.Entity<CalculationEntry>().Property(e => e.Ratio),
            builder.Entity<CalculationEntry>().Property(e => e.Purification) })
            prop.HasPrecision(18, 2);

        builder.Entity<StockFinancialReview>()
            .HasOne(r => r.Stock)
            .WithMany(s => s.FinancialReviews)
            .HasForeignKey(r => r.StockId)
            .OnDelete(DeleteBehavior.Cascade);

        foreach (var prop in new[] {
            builder.Entity<StockFinancialReview>().Property(e => e.ProhibitedIncomeRatio),
            builder.Entity<StockFinancialReview>().Property(e => e.NonShariahInvestmentRatio),
            builder.Entity<StockFinancialReview>().Property(e => e.NonShariahObligationsRatio),
            builder.Entity<StockFinancialReview>().Property(e => e.LiquidAssetsRatio) })
            prop.HasPrecision(5, 2);

        builder.Entity<PortfolioRequest>().Property(e => e.InvestmentCapital).HasPrecision(18, 2);

        builder.Entity<GoldPriceSnapshot>().Property(e => e.PriceGram21K).HasPrecision(18, 4);
        builder.Entity<GoldPriceSnapshot>().HasIndex(e => e.PriceDate).IsUnique();

        var zakatEntity = builder.Entity<ZakatRecord>();
        zakatEntity.Property(e => e.CashBalance).HasPrecision(18, 2);
        zakatEntity.Property(e => e.StocksMarketValue).HasPrecision(18, 2);
        zakatEntity.Property(e => e.TotalPortfolio).HasPrecision(18, 2);
        zakatEntity.Property(e => e.GoldGramPrice21K).HasPrecision(18, 2);
        zakatEntity.Property(e => e.NisabValue).HasPrecision(18, 2);
        zakatEntity.Property(e => e.ZakatAmount).HasPrecision(18, 2);
        zakatEntity.Property(e => e.ZakatRate).HasPrecision(18, 2);

        var capitalEntity = builder.Entity<CapitalPurificationRecord>();
        // Rows saved before the holding-period feature were computed on a full-year basis, so 365
        // is the value that keeps each historical PurificationAmount consistent with its own inputs.
        capitalEntity.Property(e => e.HoldingDays).HasDefaultValue(365);
        capitalEntity.Property(e => e.OwnershipPercent).HasPrecision(18, 8);
        capitalEntity.Property(e => e.TotalCompanyAssets).HasPrecision(18, 2);
        capitalEntity.Property(e => e.CapitalShare).HasPrecision(18, 2);
        capitalEntity.Property(e => e.RibaActivityRatio).HasPrecision(5, 2);
        capitalEntity.Property(e => e.PurificationAmount).HasPrecision(18, 2);
        capitalEntity
            .HasOne(e => e.Stock)
            .WithMany()
            .HasForeignKey(e => e.StockId)
            .OnDelete(DeleteBehavior.SetNull);

        var fatwaEntity = builder.Entity<FatwaQuestion>();
        fatwaEntity.Property(e => e.Question).HasMaxLength(2000);
        fatwaEntity.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        fatwaEntity.HasIndex(e => new { e.IsPublished, e.IsFeatured, e.PublishedAt });
        fatwaEntity.HasIndex(e => e.Status);

        var dividendEntity = builder.Entity<DividendPurificationRecord>();
        dividendEntity.Property(e => e.SharePrice).HasPrecision(18, 4);
        dividendEntity.Property(e => e.TotalDividends).HasPrecision(18, 2);
        dividendEntity.Property(e => e.PurificationRatio).HasPrecision(5, 2);
        dividendEntity.Property(e => e.PurificationAmount).HasPrecision(18, 2);
        dividendEntity
            .HasOne(e => e.Stock)
            .WithMany()
            .HasForeignKey(e => e.StockId)
            .OnDelete(DeleteBehavior.SetNull);
        dividendEntity.HasIndex(e => new { e.UserId, e.IsPaid });

        // Two columns: the calculator enum is the key, so there can only ever be one row per calculator.
        var instructionEntity = builder.Entity<CalculatorInstruction>();
        instructionEntity.HasKey(e => e.Type);
        instructionEntity.Property(e => e.Type).ValueGeneratedNever();
        instructionEntity.HasData(
            new CalculatorInstruction { Type = CalculatorType.CapitalPurification,  ContentHtml = CalculatorInstructionDefaults.CapitalPurification },
            new CalculatorInstruction { Type = CalculatorType.DividendPurification, ContentHtml = CalculatorInstructionDefaults.DividendPurification },
            new CalculatorInstruction { Type = CalculatorType.HomeAlerts,           ContentHtml = CalculatorInstructionDefaults.HomeAlerts });

        builder.Entity<RecommendationRequest>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RecommendationRequest>()
            .HasIndex(r => r.UserId)
            .IsUnique();

        builder.Entity<WalletManagementRequest>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WalletManagementRequest>()
            .HasIndex(r => r.UserId)
            .IsUnique();

        builder.Entity<StockRecommendation>()
            .HasOne(r => r.Stock)
            .WithMany()
            .HasForeignKey(r => r.StockId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StockRecommendation>()
            .HasOne(r => r.RecommendationType)
            .WithMany(t => t.StockRecommendations)
            .HasForeignKey(r => r.RecommendationTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<StockRecommendation>()
            .HasIndex(r => r.StockId)
            .IsUnique();

        builder.Entity<UserStockRecommendation>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<UserStockRecommendation>()
            .HasOne(r => r.Stock)
            .WithMany()
            .HasForeignKey(r => r.StockId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserStockRecommendation>()
            .HasOne(r => r.RecommendationType)
            .WithMany()
            .HasForeignKey(r => r.RecommendationTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<UserStockRecommendation>()
            .HasIndex(r => new { r.UserId, r.StockId })
            .IsUnique();

        foreach (var prop in new[] {
            builder.Entity<UserStockRecommendation>().Property(e => e.ClosingPrice),
            builder.Entity<UserStockRecommendation>().Property(e => e.Support1),
            builder.Entity<UserStockRecommendation>().Property(e => e.Support2),
            builder.Entity<UserStockRecommendation>().Property(e => e.Resistance1),
            builder.Entity<UserStockRecommendation>().Property(e => e.Resistance2),
            builder.Entity<UserStockRecommendation>().Property(e => e.StopLoss),
            builder.Entity<UserStockRecommendation>().Property(e => e.YtdChangePercent) })
            prop.HasPrecision(18, 2);

        builder.Entity<InvestmentWallet>()
            .HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<InvestmentWallet>()
            .HasIndex(w => w.UserId)
            .IsUnique();

        builder.Entity<WalletItem>()
            .HasOne(i => i.Wallet)
            .WithMany(w => w.Items)
            .HasForeignKey(i => i.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WalletItem>()
            .HasOne(i => i.Stock)
            .WithMany()
            .HasForeignKey(i => i.StockId)
            .OnDelete(DeleteBehavior.Restrict);

        // Deliberately no unique (WalletId, StockId) index — the same stock may be bought in
        // several tranches, and each purchase is its own row (same rule as RealPortfolioItem).
        builder.Entity<WalletItem>()
            .HasIndex(i => i.WalletId);

        foreach (var prop in new[] {
            builder.Entity<InvestmentWallet>().Property(e => e.CashBalance),
            builder.Entity<WalletItem>().Property(e => e.PurchasePrice),
            builder.Entity<WalletItem>().Property(e => e.TotalPurchase),
            builder.Entity<WalletItem>().Property(e => e.RelativeWeightPercent),
            builder.Entity<WalletItem>().Property(e => e.TargetPrice),
            builder.Entity<WalletItem>().Property(e => e.StopLossPrice),
            builder.Entity<WalletItem>().Property(e => e.ExpectedProfit),
            builder.Entity<WalletItem>().Property(e => e.ExpectedLoss),
            builder.Entity<WalletItem>().Property(e => e.TargetProfitPercent),
            builder.Entity<WalletItem>().Property(e => e.ExpectedLossPercent) })
        {
            prop.HasPrecision(18, 2);
        }

        builder.Entity<InvestmentWallet>().Property(e => e.MaxRiskPercent).HasPrecision(5, 2);

        builder.Entity<WalletItem>().Property(e => e.PurchasePriceSource).HasMaxLength(20);
        builder.Entity<WalletItem>().Property(e => e.TargetPriceSource).HasMaxLength(20);
        builder.Entity<WalletItem>().Property(e => e.StopLossSource).HasMaxLength(20);

        builder.Entity<RealPortfolio>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RealPortfolio>()
            .HasIndex(p => p.UserId)
            .IsUnique();

        builder.Entity<RealPortfolioItem>()
            .HasOne(i => i.Portfolio)
            .WithMany(p => p.Items)
            .HasForeignKey(i => i.PortfolioId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<RealPortfolioItem>()
            .HasOne(i => i.Stock)
            .WithMany()
            .HasForeignKey(i => i.StockId)
            .OnDelete(DeleteBehavior.Restrict);

        // Deliberately no unique (PortfolioId, StockId) index — the same stock may be
        // bought on several dates, and each purchase lot is its own row.
        builder.Entity<RealPortfolioItem>()
            .HasIndex(i => i.PortfolioId);

        foreach (var prop in new[] {
            builder.Entity<RealPortfolio>().Property(e => e.Capital),
            builder.Entity<RealPortfolioItem>().Property(e => e.PurchasePrice),
            builder.Entity<RealPortfolioItem>().Property(e => e.TotalPurchase),
            builder.Entity<RealPortfolioItem>().Property(e => e.PurchaseWeightPercent) })
        {
            prop.HasPrecision(18, 2);
        }

        builder.Entity<RealPortfolioItem>().Property(e => e.LastMarketPrice).HasPrecision(18, 2);

        foreach (var prop in new[] {
            builder.Entity<StockRecommendation>().Property(e => e.ClosingPrice),
            builder.Entity<StockRecommendation>().Property(e => e.Support1),
            builder.Entity<StockRecommendation>().Property(e => e.Support2),
            builder.Entity<StockRecommendation>().Property(e => e.Resistance1),
            builder.Entity<StockRecommendation>().Property(e => e.Resistance2),
            builder.Entity<StockRecommendation>().Property(e => e.StopLoss),
            builder.Entity<StockRecommendation>().Property(e => e.YtdChangePercent) })
            prop.HasPrecision(18, 2);

        builder.Entity<Video>()
            .HasOne(v => v.Category)
            .WithMany(c => c.Videos)
            .HasForeignKey(v => v.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Video>()
            .HasIndex(v => v.YouTubeVideoId)
            .IsUnique();

        builder.Entity<Article>()
            .HasOne(a => a.ArticleCategory)
            .WithMany(c => c.Articles)
            .HasForeignKey(a => a.ArticleCategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<AnalystProfile>()
            .HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<AnalystProfile>()
            .HasIndex(p => p.UserId)
            .IsUnique();

        builder.Entity<ArticleLike>()
            .HasOne(l => l.Article)
            .WithMany(a => a.Likes)
            .HasForeignKey(l => l.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict (not Cascade) on the User side: Article.AuthorId already cascades from
        // AppUser, so a second cascade path AppUser -> ArticleLike would give SQL Server two
        // cascade routes to the same table (via Article, and directly) and it rejects that
        // at migration-apply time. Same reasoning applies to ArticleComment below.
        builder.Entity<ArticleLike>()
            .HasOne(l => l.User)
            .WithMany()
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<ArticleLike>()
            .HasIndex(l => new { l.ArticleId, l.UserId })
            .IsUnique();

        builder.Entity<ArticleComment>()
            .HasOne(c => c.Article)
            .WithMany(a => a.Comments)
            .HasForeignKey(c => c.ArticleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<ArticleComment>()
            .HasOne(c => c.User)
            .WithMany()
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
