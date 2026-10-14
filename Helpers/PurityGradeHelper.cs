using HalalStocks.Models;

namespace HalalStocks.Helpers;

public static class PurityGradeHelper
{
    private static readonly (PurityGrade Grade, string Label)[] OrderedGrades =
    {
        (PurityGrade.Pure,                       "نقي"),
        (PurityGrade.SemiPure,                   "شبه نقي"),
        (PurityGrade.ShariaMixedA,               "شرعي مختلط A"),
        (PurityGrade.ShariaMixedB,               "شرعي مختلط B"),
        (PurityGrade.ShariaMixedC,               "شرعي مختلط C"),
        (PurityGrade.NonShariahProhibitedIncome,  "غير شرعي دخل محظور"),
        (PurityGrade.NonShariahDebts,            "غير شرعي قروض"),
        (PurityGrade.NonShariahRibaInvestments,  "غير شرعي استثمارات ربوية"),
        (PurityGrade.NonShariahActivity,         "غير شرعي نشاط"),
    };

    public static string Format(PurityGrade grades)
    {
        if (grades == PurityGrade.None) return string.Empty;

        var parts = OrderedGrades
            .Where(g => grades.HasFlag(g.Grade))
            .Select(g => g.Label);

        return string.Join("، ", parts);
    }
}
