using HalalStocks.Data;
using HalalStocks.Models;
using HalalStocks.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HalalStocks.Services.Implementations;

public class FatwaService : IFatwaService
{
    private readonly AppDbContext _db;
    public FatwaService(AppDbContext db) => _db = db;

    // The single gate for everything public: a question is never visible until it is published AND answered.
    private IQueryable<FatwaQuestion> PublicQuery =>
        _db.FatwaQuestions.Where(q => q.IsPublished && q.Answer != null && q.Answer != "");

    public async Task<(IEnumerable<FatwaQuestion> Items, int Total)> GetPublishedAsync(string? search, int page, int pageSize)
    {
        var query = PublicQuery;
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(q => q.Question.Contains(term) || q.Answer!.Contains(term));
        }
        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(q => q.PublishedAt).ThenByDescending(q => q.Id)
            .Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize)
            .AsNoTracking().ToListAsync();
        return (items, total);
    }

    public async Task<IEnumerable<FatwaQuestion>> GetFeaturedAsync(int count) =>
        await PublicQuery.Where(q => q.IsFeatured)
            .OrderByDescending(q => q.PublishedAt).ThenByDescending(q => q.Id)
            .Take(count).AsNoTracking().ToListAsync();

    public Task<int> CountPendingForUserAsync(string userId) =>
        _db.FatwaQuestions.CountAsync(q => q.UserId == userId && q.Status == FatwaStatus.Pending);

    public async Task<FatwaQuestion> AskAsync(string userId, string question)
    {
        var entity = new FatwaQuestion
        {
            UserId = userId,
            Question = question.Trim(),
            Status = FatwaStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        _db.FatwaQuestions.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task<(IEnumerable<FatwaQuestion> Items, int Total)> GetForAdminAsync(FatwaStatus? status, int page, int pageSize)
    {
        var query = _db.FatwaQuestions.AsQueryable();
        if (status.HasValue) query = query.Where(q => q.Status == status.Value);
        var total = await query.CountAsync();
        // Pending first so the queue drains, then newest.
        var items = await query
            .OrderBy(q => q.Status == FatwaStatus.Pending ? 0 : 1).ThenByDescending(q => q.CreatedAt)
            .Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize)
            .AsNoTracking().ToListAsync();
        return (items, total);
    }

    public async Task<(int All, int Pending, int Answered, int Published)> GetCountsAsync()
    {
        var groups = await _db.FatwaQuestions.GroupBy(q => q.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() }).ToListAsync();
        int Get(FatwaStatus s) => groups.FirstOrDefault(g => g.Status == s)?.Count ?? 0;
        return (groups.Sum(g => g.Count), Get(FatwaStatus.Pending), Get(FatwaStatus.Answered), Get(FatwaStatus.Published));
    }

    public Task<FatwaQuestion?> GetByIdAsync(int id) =>
        _db.FatwaQuestions.Include(q => q.User).FirstOrDefaultAsync(q => q.Id == id);

    public async Task SaveAnswerAsync(int id, string? answerHtml, bool publish, bool feature)
    {
        var q = await _db.FatwaQuestions.FindAsync(id);
        if (q == null) return;

        var answer = string.IsNullOrWhiteSpace(answerHtml) ? null : answerHtml.Trim();
        var now = DateTime.UtcNow;

        if (answer == null)
        {
            q.AnsweredAt = null;
        }
        else if (answer != q.Answer || q.AnsweredAt == null)
        {
            q.AnsweredAt = now;
        }
        q.Answer = answer;

        // Invariants: published ⟹ answered; featured ⟹ published.
        publish = publish && answer != null;
        feature = feature && publish;

        if (publish && !q.IsPublished) q.PublishedAt = now;
        if (!publish) q.PublishedAt = null;

        q.IsPublished = publish;
        q.IsFeatured = feature;
        q.Status = answer == null ? FatwaStatus.Pending
                 : publish ? FatwaStatus.Published
                 : FatwaStatus.Answered;

        await _db.SaveChangesAsync();
    }

    public async Task DeleteAsync(int id)
    {
        var q = await _db.FatwaQuestions.FindAsync(id);
        if (q != null) { _db.FatwaQuestions.Remove(q); await _db.SaveChangesAsync(); }
    }
}
