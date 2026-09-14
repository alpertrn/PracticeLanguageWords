using Microsoft.EntityFrameworkCore;
using PracticeLanguageWords.Domain.Entities;
using PracticeLanguageWords.Domain.Enums;
using PracticeLanguageWords.Domain.Interfaces;
using PracticeLanguageWords.Domain.Models;

namespace PracticeLanguageWords.Infrastructure.Persistence.Repositories;

public class LanguageRepository : ILanguageRepository
{
    private readonly AppDbContext _context;

    public LanguageRepository(AppDbContext context) => _context = context;

    public Task<List<Language>> GetAllAsync(bool onlyActive, CancellationToken ct = default)
    {
        var query = _context.Languages.AsQueryable();

        if (onlyActive)
        {
            query = query.Where(l => l.IsActive);
        }

        return query
            .OrderBy(l => l.DisplayOrder)
            .ThenBy(l => l.Name)
            .ToListAsync(ct);
    }

    public Task<Language?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _context.Languages.FirstOrDefaultAsync(l => l.Id == id, ct);

    public Task<bool> CodeExistsAsync(string code, int? excludeId, CancellationToken ct = default) =>
        _context.Languages.AnyAsync(l => l.Code == code && (excludeId == null || l.Id != excludeId), ct);

    public Task<bool> HasCategoriesAsync(int languageId, CancellationToken ct = default) =>
        _context.Categories.AnyAsync(c => c.LanguageId == languageId, ct);

    public void Add(Language language) => _context.Languages.Add(language);

    public void Remove(Language language) => _context.Languages.Remove(language);
}

public class CategoryGroupRepository : ICategoryGroupRepository
{
    private readonly AppDbContext _context;

    public CategoryGroupRepository(AppDbContext context) => _context = context;

    public Task<List<CategoryGroup>> GetAllAsync(CancellationToken ct = default) =>
        _context.CategoryGroups.OrderBy(g => g.Name).ToListAsync(ct);

    public Task<CategoryGroup?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _context.CategoryGroups.FirstOrDefaultAsync(g => g.Id == id, ct);

    public Task<bool> HasCategoriesAsync(int groupId, CancellationToken ct = default) =>
        _context.Categories.AnyAsync(c => c.CategoryGroupId == groupId, ct);

    public void Add(CategoryGroup group) => _context.CategoryGroups.Add(group);

    public void Remove(CategoryGroup group) => _context.CategoryGroups.Remove(group);
}

public class CategoryRepository : ICategoryRepository
{
    private readonly AppDbContext _context;

    public CategoryRepository(AppDbContext context) => _context = context;

    public Task<List<CategoryWithCount>> GetAllWithWordCountAsync(int? languageId, CancellationToken ct = default)
    {
        var query = _context.Categories.AsNoTracking();

        if (languageId is not null && languageId > 0)
        {
            query = query.Where(c => c.LanguageId == languageId);
        }

        return query
            .Select(c => new CategoryWithCount(
                c.Id,
                c.Name,
                c.CategoryGroupId,
                c.CategoryGroup.Name,
                c.LanguageId,
                c.Language.Name,
                c.Words.Count()))
            .ToListAsync(ct);
    }

    public Task<Category?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _context.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);

    public Task<bool> HasWordsAsync(int categoryId, CancellationToken ct = default) =>
        _context.Words.AnyAsync(w => w.CategoryId == categoryId, ct);

    public void Add(Category category) => _context.Categories.Add(category);

    public void Remove(Category category) => _context.Categories.Remove(category);
}

public class WordRepository : IWordRepository
{
    private readonly AppDbContext _context;

    public WordRepository(AppDbContext context) => _context = context;

    public Task<Word?> GetByIdAsync(int id, CancellationToken ct = default) =>
        _context.Words
            .Include(w => w.Category)
            .ThenInclude(c => c.Language)
            .FirstOrDefaultAsync(w => w.Id == id, ct);

    public Task<List<Word>> GetByCategoryIdAsync(int categoryId, CancellationToken ct = default) =>
        _context.Words.AsNoTracking().Where(w => w.CategoryId == categoryId).ToListAsync(ct);

    public Task<List<Word>> SearchAsync(string? term, int? categoryId, int? languageId, int page, int pageSize, CancellationToken ct = default) =>
        BuildSearchQuery(term, categoryId, languageId)
            .AsNoTracking()
            .Include(w => w.Category)
            .ThenInclude(c => c.Language)
            .OrderBy(w => w.TermText)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

    public Task<int> CountSearchAsync(string? term, int? categoryId, int? languageId, CancellationToken ct = default) =>
        BuildSearchQuery(term, categoryId, languageId).CountAsync(ct);

    public Task<bool> ExistsAsync(int categoryId, string termText, int? excludeWordId, CancellationToken ct = default) =>
        _context.Words.AnyAsync(
            w => w.CategoryId == categoryId
                 && w.TermText == termText
                 && (excludeWordId == null || w.Id != excludeWordId),
            ct);

    public Task<List<WordCandidate>> GetCandidatesInCategoryAsync(int userId, int categoryId, CancellationToken ct = default)
    {
        var query =
            from w in _context.Words.AsNoTracking()
            where w.CategoryId == categoryId
            join p in _context.UserWordProgresses.Where(x => x.UserId == userId)
                on w.Id equals p.WordId into progresses
            from p in progresses.DefaultIfEmpty()
            select new WordCandidate(w.Id, p == null ? (DifficultyLevel?)null : p.Difficulty);

        return query.ToListAsync(ct);
    }

    public Task<List<WordCandidate>> GetUnknownCandidatesAsync(int userId, int? languageId, DateOnly today, CancellationToken ct = default)
    {
        var query = _context.UserWordProgresses
            .AsNoTracking()
            .Where(p => p.UserId == userId && p.IsUnknown)
            // Bugun zaten puanlanmis kelime tekrar cikmasin (birkac gune yayilan degerlendirme).
            .Where(p => p.LastMasteryAttemptDate == null || p.LastMasteryAttemptDate != today);

        if (languageId is not null && languageId > 0)
        {
            query = query.Where(p => p.Word.Category.LanguageId == languageId);
        }

        return query
            .Select(p => new WordCandidate(p.WordId, p.Difficulty))
            .ToListAsync(ct);
    }

    public void Add(Word word) => _context.Words.Add(word);

    public void AddRange(IEnumerable<Word> words) => _context.Words.AddRange(words);

    public void Remove(Word word) => _context.Words.Remove(word);

    private IQueryable<Word> BuildSearchQuery(string? term, int? categoryId, int? languageId)
    {
        var query = _context.Words.AsQueryable();

        if (categoryId.HasValue && categoryId > 0)
        {
            query = query.Where(w => w.CategoryId == categoryId.Value);
        }

        if (languageId.HasValue && languageId > 0)
        {
            query = query.Where(w => w.Category.LanguageId == languageId.Value);
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            var trimmed = term.Trim();
            query = query.Where(w =>
                EF.Functions.Like(w.TermText, $"%{trimmed}%") ||
                EF.Functions.Like(w.MeaningText, $"%{trimmed}%"));
        }

        return query;
    }
}
