using HalalStocks.Models;
using HalalStocks.ViewModels;

namespace HalalStocks.Services.Interfaces;

/// <summary>
/// Builds the المحفظة الفعلية page view model for a given user. Shared by the user's own page
/// (<c>RealPortfolioController</c>) and the admin-on-behalf page
/// (<c>Admin/WalletManagementController</c>) so the live-price fetch and its write-back cache
/// have exactly one implementation.
/// </summary>
public interface IRealPortfolioViewModelBuilder
{
    /// <summary>
    /// Loads the portfolio, fetches live market prices (persisting them as the fallback cache),
    /// and maps every lot to a row. Pass <paramref name="editingItem"/> to also populate the form.
    /// </summary>
    Task<RealPortfolioIndexViewModel> BuildAsync(string userId, RealPortfolioItem? editingItem);

    /// <summary>
    /// Re-populates the display-only stock fields so a rejected form redraws with the
    /// sector / الموقف / market price the user had already seen.
    /// </summary>
    Task<RealPortfolioItemFormViewModel> HydrateFormAsync(RealPortfolioItemFormViewModel vm);
}
