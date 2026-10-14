using HalalStocks.Models;

namespace HalalStocks.Helpers;

// Arabic labels for الحكم الشرعي — the same wording the public stock filter uses on the
// homepage, so the recommendation/portfolio surfaces stay consistent with the screening table.
public static class ShariahStatusHelper
{
    public static string Format(ShariahStatus status) => status switch
    {
        ShariahStatus.Halal                       => "🟢 متوافق مع الشريعة",
        ShariahStatus.MustPurify                  => "🟡 غير متوافق مع الشريعة",
        ShariahStatus.Prohibited                  => "🔴 محظور",
        ShariahStatus.NotReviewed                 => "⬜ لم يتم مراجعته",
        ShariahStatus.AwaitingFinancialStatements => "🕐 في انتظار القوائم المالية",
        ShariahStatus.StatusBlocked               => "🚫 تم حجب إعلان الموقف الشرعي",
        _                                         => string.Empty
    };

    public static string BadgeClass(ShariahStatus status) => status switch
    {
        ShariahStatus.Halal                       => "bg-success",
        ShariahStatus.MustPurify                  => "bg-danger text-dark",
        ShariahStatus.AwaitingFinancialStatements => "bg-info text-dark",
        _                                         => "bg-danger"
    };
}
