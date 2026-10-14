using HalalStocks.Helpers;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using HalalStocks.ViewModels;

namespace HalalStocks.Services.Implementations;

public class RealPortfolioViewModelBuilder : IRealPortfolioViewModelBuilder
{
    private readonly IRealPortfolioService _portfolioService;
    private readonly ITradingViewPriceService _priceService;
    private readonly IStockService _stockService;

    public RealPortfolioViewModelBuilder(
        IRealPortfolioService portfolioService,
        ITradingViewPriceService priceService,
        IStockService stockService)
    {
        _portfolioService = portfolioService;
        _priceService     = priceService;
        _stockService     = stockService;
    }

    public async Task<RealPortfolioIndexViewModel> BuildAsync(string userId, RealPortfolioItem? editingItem)
    {
        var portfolio = await _portfolioService.GetByUserIdAsync(userId);
        var items = portfolio?.Items.ToList() ?? new List<RealPortfolioItem>();

        var vm = new RealPortfolioIndexViewModel
        {
            PageState       = RecommendationsPageState.List,
            HasPortfolio    = portfolio != null,
            Capital         = portfolio?.Capital ?? 0,
            InvestedCapital = items.Sum(i => i.TotalPurchase)
        };

        var prices = items.Count == 0
            ? new Dictionary<string, decimal>()
            : await _priceService.GetPricesAsync(items.Select(i => i.Stock.TickerSymbol));

        vm.PriceFetchFailed = items.Count > 0 && prices.Count == 0;

        var freshPrices = new Dictionary<int, decimal>();
        foreach (var item in items)
        {
            var hasLivePrice = prices.TryGetValue(item.Stock.TickerSymbol.Trim().ToUpperInvariant(), out var livePrice);
            if (hasLivePrice) freshPrices[item.Id] = livePrice;

            vm.Rows.Add(new RealPortfolioRowViewModel
            {
                Id                    = item.Id,
                PurchaseDate          = item.PurchaseDate,
                StockTicker           = item.Stock.TickerSymbol,
                StockNameAr           = item.Stock.NameAr,
                SectorNameAr          = item.Stock.Category?.NameAr ?? string.Empty,
                ShariahLabel          = ShariahStatusHelper.Format(item.Stock.ShariahStatus),
                ShariahBadgeClass     = ShariahStatusHelper.BadgeClass(item.Stock.ShariahStatus),
                Quantity              = item.Quantity,
                PurchasePrice         = item.PurchasePrice,
                TotalPurchase         = item.TotalPurchase,
                PurchaseWeightPercent = item.PurchaseWeightPercent,
                MarketPrice           = hasLivePrice ? livePrice : item.LastMarketPrice,
                MarketPriceAt         = hasLivePrice ? DateTime.UtcNow : item.LastMarketPriceAt,
                IsPriceStale          = !hasLivePrice && item.LastMarketPrice != null
            });
        }

        await _portfolioService.SaveMarketPricesAsync(freshPrices);

        if (editingItem != null)
        {
            vm.Form = new RealPortfolioItemFormViewModel
            {
                Id                   = editingItem.Id,
                PurchaseDate         = editingItem.PurchaseDate,
                StockId              = editingItem.StockId,
                Quantity             = editingItem.Quantity,
                PurchasePrice        = editingItem.PurchasePrice,
                SelectedStockTicker  = editingItem.Stock.TickerSymbol,
                SelectedStockNameAr  = editingItem.Stock.NameAr,
                SelectedSectorNameAr = editingItem.Stock.Category?.NameAr ?? string.Empty,
                SelectedShariahLabel = ShariahStatusHelper.Format(editingItem.Stock.ShariahStatus),
                SelectedMarketPrice  = vm.Rows.FirstOrDefault(r => r.Id == editingItem.Id)?.MarketPrice
            };
        }

        return vm;
    }

    public async Task<RealPortfolioItemFormViewModel> HydrateFormAsync(RealPortfolioItemFormViewModel vm)
    {
        if (vm.StockId is null) return vm;

        var stock = await _stockService.GetByIdAsync(vm.StockId.Value);
        if (stock == null) return vm;

        vm.SelectedStockTicker  = stock.TickerSymbol;
        vm.SelectedStockNameAr  = stock.NameAr;
        vm.SelectedSectorNameAr = stock.Category?.NameAr ?? string.Empty;
        vm.SelectedShariahLabel = ShariahStatusHelper.Format(stock.ShariahStatus);
        vm.SelectedMarketPrice  = await _priceService.GetPriceAsync(stock.TickerSymbol);
        return vm;
    }
}
