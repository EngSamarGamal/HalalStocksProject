namespace HalalStocks.Models;

// Which calculator (or other admin-editable content block) an instructions row belongs to.
// Stored as an int, so never renumber. HomeAlerts reuses this same admin "الإرشادات" mechanism
// for the homepage's "تنبيهات هامة" accordion — not a calculator, but the same edit/store/render shape.
public enum CalculatorType
{
    CapitalPurification  = 1,
    DividendPurification = 2,
    HomeAlerts            = 3
}

/// <summary>
/// Admin-editable instructions (rich-text HTML) shown at the top of one calculator page.
/// One row per calculator; the enum itself is the key. A missing row means "use the built-in default".
/// </summary>
public class CalculatorInstruction
{
    public CalculatorType Type        { get; set; }
    public string?        ContentHtml { get; set; }
}
