using HalalStocks.Models;

namespace HalalStocks.Helpers;

public static class ArticleTypeHelper
{
    private static readonly (ArticleType Type, string Label)[] OrderedTypes =
    {
        (ArticleType.Standard,        "مقال عادي"),
        (ArticleType.ShariahStandard, "معيار شرعي"),
        (ArticleType.Analytical,      "تحليل ورأي"),
    };

    public static IEnumerable<(ArticleType Type, string Label)> All => OrderedTypes;

    public static string Label(ArticleType type) =>
        OrderedTypes.First(t => t.Type == type).Label;
}
