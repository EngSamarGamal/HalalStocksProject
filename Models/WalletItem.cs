namespace HalalStocks.Models;

public class WalletItem
{
    public int Id { get; set; }
    public int WalletId { get; set; }
    public InvestmentWallet Wallet { get; set; } = null!;

    public int StockId { get; set; }
    public Stock Stock { get; set; } = null!;

    public int Quantity { get; set; }
    public decimal PurchasePrice { get; set; }
    public string PurchasePriceSource { get; set; } = "ClosePrice";
    public decimal TotalPurchase { get; set; }
    public decimal RelativeWeightPercent { get; set; }
    public decimal TargetPrice { get; set; }
    public string TargetPriceSource { get; set; } = "Resistance1";
    public decimal StopLossPrice { get; set; }
    public string StopLossSource { get; set; } = "Support1";
    public decimal ExpectedProfit { get; set; }
    public decimal ExpectedLoss { get; set; }
    public decimal TargetProfitPercent { get; set; }
    public decimal ExpectedLossPercent { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
