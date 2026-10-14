namespace HalalStocks.Models;

public enum ResidenceType
{
    InsideEgypt,   // داخل مصر
    OutsideEgypt   // خارج مصر
}

public enum InvestmentHorizon
{
    ShortTerm,   // قصير الأجل (مضاربة + متاجرة)
    MediumTerm,  // متوسط الأجل
    LongTerm     // طويل الأجل
}

public enum StockPurityPreference
{
    PureOnly           = 0,  // نقية فقط
    PureAndSemiPure    = 1,  // نقية + شبه نقية فقط
    UpToMixedA         = 2,  // نقية + شبه نقية + مختلط A
    UpToMixedB         = 3,  // نقية + شبه نقية + مختلط A و B
    AllShariahCompliant = 4, // جميع الأسهم المتوافقة مع الشريعة
}

public enum ContactMethod
{
    WhatsApp,  // واتس آب
    Phone,     // تليفون
    Email      // البريد الإلكتروني
}

public enum ContractDelivery
{
    ElectronicMail,  // البريد الإلكتروني
    EgyptianPost     // البريد المصري
}

public enum PortfolioRequestStatus
{
    New,          // جديد
    UnderReview,  // قيد المراجعة
    Contacted,    // تم التواصل
    ActiveClient, // عميل حالي
    Rejected      // مرفوض
}

public class PortfolioRequest
{
    public int Id { get; set; }

    public string UserId { get; set; } = string.Empty;
    public AppUser User { get; set; } = null!;

    public string PhoneNumber { get; set; } = string.Empty;
    public string Governorate { get; set; } = string.Empty;
    public ResidenceType Residence { get; set; }
    public string? CountryOfResidence { get; set; }

    public decimal InvestmentCapital { get; set; }
    public InvestmentHorizon Horizon { get; set; }
    public StockPurityPreference StockType { get; set; }
    public ContactMethod PreferredContact { get; set; }
    public string? WhatsAppNumber { get; set; }
    public string? ContactEmail { get; set; }
    public ContractDelivery ContractDelivery { get; set; }

    public PortfolioRequestStatus Status { get; set; } = PortfolioRequestStatus.New;
    public string? AdminNotes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
