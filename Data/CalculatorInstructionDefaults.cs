using HalalStocks.Models;

namespace HalalStocks.Data;

/// <summary>The text each calculator page showed before its instructions became editable.</summary>
public static class CalculatorInstructionDefaults
{
    public const string CapitalPurification =
        "<p>احسب نصيبك من رأس مال الشركة الموجَّه إلى الاستثمارات الربوية، بناءً على عدد الأسهم التي تمتلكها وإجمالي أصول الشركة ونسبة أنشطتها الربوية.</p>";

    public const string DividendPurification =
        "<p>احسب المبلغ الواجب إخراجه للتطهير من توزيعات الأرباح التي حصلت عليها من السهم، سواء كانت توزيعات نقدية أو توزيعات أسهم.</p>";

    public const string HomeAlerts =
        "<p><strong>تنبيه:</strong> نسعى من خلال مراجعاتنا الدورية إلى تمكين المستثمرين من الوصول للفرص المتوافقة مع الشريعة وحث الشركات المساهمة على تعزيز القيم الأخلاقية والشفافية، مؤكدين أن نتائجنا هي مادة استرشادية واجتهادية لا تشكل توصيات مالية بالبيع أو الشراء ولا تعكس الجدوى الاقتصادية للسهم. وعليه، فإن قرار الاستثمار يظل مسؤولية المستثمر المنفردة، مع إخلاء مسؤوليتنا القانونية والمالية عن أي قرارات ناتجة عن هذه النتائج التي تخضع للتحديث المستمر وفقاً للبيانات المالية الطارئة.</p>" +
        "<p><strong>تنبيه:</strong> نحن لا نستلم أو نُحصّل أي اشتراكات أو تقديم استشارات مدفوعة بدون ترخيص او اي مدفوعات مالية من المستخدمين. الاستفادة من جميع التقارير وأدوات تقييم المحافظ تتم بشكل مجاني حصرياً عبر فتح حساب تداول من خلال الشركات المتعاقدة معنا، والتي تخضع جميعها لإشراف ورقابة الهيئة العامة للرقابة المالية (FRA).</p>";

    public static string? For(CalculatorType type) => type switch
    {
        CalculatorType.CapitalPurification  => CapitalPurification,
        CalculatorType.DividendPurification => DividendPurification,
        CalculatorType.HomeAlerts           => HomeAlerts,
        _ => null
    };
}
