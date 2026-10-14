namespace HalalStocks.Models;

/// <summary>
/// Which public surface an article belongs to. Replaces the former
/// Article.IsShariahStandard bool — each value maps to exactly one section,
/// so adding a future type never requires existing predicates to change.
/// </summary>
public enum ArticleType
{
    Standard,        // مقال عادي — /articles + homepage "آخر المقالات"
    ShariahStandard, // homepage "المعايير والضوابط وفقا للشريعة الإسلامية" + /articles
    Analytical       // homepage "تحليلات وآراء" + /analytics only
}

/// <summary>
/// Admin-approval state for analyst-authored articles. Layered on top of
/// Article.IsPublished, which remains the universal visibility gate:
/// IsPublished == true implies ApprovalStatus is NotRequired or Approved.
/// </summary>
public enum ArticleApprovalStatus
{
    NotRequired, // admin-authored — no approval gate
    Pending,     // awaiting admin review, not public
    Approved,    // admin approved, may be published
    Hidden       // pulled by an admin for a guideline violation
}
