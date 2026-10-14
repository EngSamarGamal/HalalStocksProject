using HalalStocks.Models;

namespace HalalStocks.Helpers;

public static class ArticleApprovalStatusHelper
{
    public static (string Label, string BadgeClass) Badge(ArticleApprovalStatus status) => status switch
    {
        ArticleApprovalStatus.Pending  => ("بانتظار المراجعة", "bg-warning text-dark"),
        ArticleApprovalStatus.Approved => ("منشور", "bg-success"),
        ArticleApprovalStatus.Hidden   => ("تم الإخفاء", "bg-danger"),
        _                              => ("لا يتطلب مراجعة", "bg-secondary"),
    };
}
