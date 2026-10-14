using System.ComponentModel.DataAnnotations;
using HalalStocks.Models;

namespace HalalStocks.ViewModels;

public class PortfolioRequestFormViewModel
{
    [Required(ErrorMessage = "رقم الهاتف مطلوب")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "المحافظة مطلوبة")]
    public string Governorate { get; set; } = string.Empty;

    public ResidenceType Residence { get; set; } = ResidenceType.InsideEgypt;

    public string? CountryOfResidence { get; set; }

    [Required(ErrorMessage = "رأس المال مطلوب")]
    [Range(1, double.MaxValue, ErrorMessage = "يجب أن يكون رأس المال أكبر من صفر")]
    public decimal InvestmentCapital { get; set; }

    public InvestmentHorizon Horizon { get; set; } = InvestmentHorizon.MediumTerm;

    public StockPurityPreference StockType { get; set; } = StockPurityPreference.PureOnly;

    public ContactMethod PreferredContact { get; set; } = ContactMethod.WhatsApp;

    public string? WhatsAppNumber { get; set; }

    public string? ContactEmail { get; set; }

    public ContractDelivery ContractDelivery { get; set; } = ContractDelivery.ElectronicMail;
}
