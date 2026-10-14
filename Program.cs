using HalalStocks.Data;
using HalalStocks.Helpers;
using HalalStocks.Models;
using HalalStocks.Models.Settings;
using HalalStocks.Services;
using HalalStocks.Services.Implementations;
using HalalStocks.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentity<AppUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders()
.AddClaimsPrincipalFactory<AppUserClaimsPrincipalFactory>();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/error/403";

    // One cookie serves both audiences ("admin" is only a role claim, not a separate scheme),
    // so route the challenge by area: the admin panel keeps its own branded form, while every
    // public page goes to the site login instead of dumping visitors on the admin one.
    options.Events.OnRedirectToLogin = context =>
    {
        var loginPath = context.Request.Path.StartsWithSegments("/admin") ? "/admin/login" : "/account/login";
        var returnUrl = context.Request.Path + context.Request.QueryString;
        context.Response.Redirect($"{loginPath}?returnUrl={Uri.EscapeDataString(returnUrl)}");
        return Task.CompletedTask;
    };
});

builder.Services.AddScoped<ISiteSettingsService, SiteSettingsService>();
builder.Services.AddScoped<IStockService, StockService>();
builder.Services.AddScoped<IArticleService, ArticleService>();
builder.Services.AddScoped<IVideoService, VideoService>();
builder.Services.AddScoped<IBannerService, BannerService>();
builder.Services.AddScoped<IFatwaService, FatwaService>();
builder.Services.AddScoped<IYouTubeService, YouTubeService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ICalculationService, CalculationService>();
builder.Services.AddScoped<IZakatService, ZakatService>();
builder.Services.AddScoped<ICapitalPurificationService, CapitalPurificationService>();
builder.Services.AddScoped<IDividendPurificationService, DividendPurificationService>();
builder.Services.AddScoped<ICalculatorInstructionService, CalculatorInstructionService>();
builder.Services.AddScoped<IGoldPriceService, GoldPriceService>();
builder.Services.AddScoped<IRecommendationService, RecommendationService>();
builder.Services.AddScoped<IRecommendationRequestService, RecommendationRequestService>();
builder.Services.AddScoped<IInvestmentWalletService, InvestmentWalletService>();
builder.Services.AddScoped<IRealPortfolioService, RealPortfolioService>();
builder.Services.AddScoped<IRealPortfolioViewModelBuilder, RealPortfolioViewModelBuilder>();
builder.Services.AddScoped<ITradingViewPriceService, TradingViewPriceService>();
builder.Services.AddScoped<IUserRecommendationService, UserRecommendationService>();
builder.Services.AddScoped<IWalletManagementRequestService, WalletManagementRequestService>();
builder.Services.AddScoped<IAnalystService, AnalystService>();
builder.Services.AddScoped<IArticleEngagementService, ArticleEngagementService>();
builder.Services.AddHttpClient();   // registers IHttpClientFactory
builder.Services.AddMemoryCache();

// Storage service — R2 in production, local fallback in development
builder.Services.Configure<CloudflareR2Settings>(builder.Configuration.GetSection("CloudflareR2"));
var r2AccountId = builder.Configuration["CloudflareR2:AccountId"];
if (string.IsNullOrEmpty(r2AccountId))
    builder.Services.AddScoped<IStorageService, LocalStorageService>();
else
    builder.Services.AddScoped<IStorageService, R2StorageService>();

builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = 31457280; // 30 MB
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 31457280; // 30 MB
});

builder.Services.AddAntiforgery(o => o.HeaderName = "RequestVerificationToken");
builder.Services.AddControllersWithViews(options =>
{
    // The site is Arabic-only; override the framework's English model-binding messages.
    var messages = options.ModelBindingMessageProvider;
    // Field names are omitted on purpose — they would render as English property names.
    messages.SetValueMustNotBeNullAccessor(_ => "هذا الحقل مطلوب");
    messages.SetMissingBindRequiredValueAccessor(_ => "لم يتم إرسال قيمة لهذا الحقل");
    messages.SetMissingKeyOrValueAccessor(() => "هذا الحقل مطلوب");
    messages.SetMissingRequestBodyRequiredValueAccessor(() => "لم يتم إرسال البيانات المطلوبة");
    messages.SetAttemptedValueIsInvalidAccessor((value, _) => $"القيمة '{value}' غير صالحة");
    messages.SetNonPropertyAttemptedValueIsInvalidAccessor(value => $"القيمة '{value}' غير صالحة");
    messages.SetUnknownValueIsInvalidAccessor(_ => "القيمة المدخلة غير صالحة");
    messages.SetNonPropertyUnknownValueIsInvalidAccessor(() => "القيمة المدخلة غير صالحة");
    messages.SetValueIsInvalidAccessor(value => $"القيمة {value} غير صالحة");
    messages.SetValueMustBeANumberAccessor(_ => "هذا الحقل يجب أن يكون رقماً");
    messages.SetNonPropertyValueMustBeANumberAccessor(() => "هذه القيمة يجب أن تكون رقماً");

    options.ModelMetadataDetailsProviders.Add(new ArabicValidationMessagesProvider());
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/error/500");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/error/{0}");
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    await SeedData.InitializeAsync(scope.ServiceProvider);
}

app.Run();
