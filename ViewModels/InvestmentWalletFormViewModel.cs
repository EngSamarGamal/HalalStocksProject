using System.ComponentModel.DataAnnotations;
using HalalStocks.Models;

namespace HalalStocks.ViewModels;

public class InvestmentWalletFormViewModel
{
    public int? Id { get; set; }

    [Required(ErrorMessage = "يرجى إدخال السيولة")]
    [Display(Name = "السيولة بالمحفظة")]
    public decimal? CashBalance { get; set; }
}
