using PracticeLanguageWords.Application.Common;
using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;
using PracticeLanguageWords.Domain.Entities;
using PracticeLanguageWords.Domain.Interfaces;

namespace PracticeLanguageWords.Application.Services;

public class WordAdminService : IWordAdminService
{
    private readonly IUnitOfWork _uow;

    public WordAdminService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<PagedResult<WordAdminDto>> SearchAsync(string? term, int? categoryId, int? languageId, int page, int pageSize, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize is < 1 or > 200 ? 25 : pageSize;

        var words = await _uow.Words.SearchAsync(term, categoryId, languageId, page, pageSize, ct);
        var total = await _uow.Words.CountSearchAsync(term, categoryId, languageId, ct);

        var items = words.Select(w => new WordAdminDto(
            w.Id,
            w.CategoryId,
            w.Category.Name,
            w.Category.Language.Name,
            w.TermText,
            w.MeaningText,
            w.TermRead,
            w.MemoryConnection,
            w.ExampleSentence,
            w.ExampleSentenceMeaning,
            w.ExampleSentence2,
            w.ExampleSentenceMeaning2)).ToList();

        return new PagedResult<WordAdminDto>(items, total, page, pageSize);
    }

    public async Task<WordEditDto?> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var word = await _uow.Words.GetByIdAsync(id, ct);
        if (word is null)
        {
            return null;
        }

        return new WordEditDto(
            word.Id,
            word.CategoryId,
            word.TermText,
            word.MeaningText,
            word.TermRead,
            word.MemoryConnection,
            word.ExampleSentence,
            word.ExampleSentenceMeaning,
            word.ExampleSentence2,
            word.ExampleSentenceMeaning2);
    }

    public async Task<string?> CreateAsync(WordEditDto dto, CancellationToken ct = default)
    {
        _ = await _uow.Categories.GetByIdAsync(dto.CategoryId, ct)
            ?? throw new NotFoundException("Kategori bulunamadı.");

        var term = dto.TermText.Trim();

        if (await _uow.Words.ExistsAsync(dto.CategoryId, term, null, ct))
        {
            return $"'{term}' kelimesi bu kategoride zaten kayıtlı.";
        }

        var word = new Word
        {
            CategoryId = dto.CategoryId,
            TermText = term,
            MeaningText = dto.MeaningText.Trim(),
            TermRead = Clean(dto.TermRead),
            MemoryConnection = Clean(dto.MemoryConnection),
            ExampleSentence = Clean(dto.ExampleSentence),
            ExampleSentenceMeaning = Clean(dto.ExampleSentenceMeaning),
            ExampleSentence2 = Clean(dto.ExampleSentence2),
            ExampleSentenceMeaning2 = Clean(dto.ExampleSentenceMeaning2),
            CreatedAt = DateTime.UtcNow
        };

        _uow.Words.Add(word);
        await _uow.SaveChangesAsync(ct);
        return null;
    }

    public async Task<string?> UpdateAsync(WordEditDto dto, CancellationToken ct = default)
    {
        if (dto.Id is null)
        {
            throw new BusinessRuleException("Güncellenecek kelime kimliği eksik.");
        }

        var word = await _uow.Words.GetByIdAsync(dto.Id.Value, ct)
            ?? throw new NotFoundException("Kelime bulunamadı.");

        var term = dto.TermText.Trim();

        if (await _uow.Words.ExistsAsync(dto.CategoryId, term, word.Id, ct))
        {
            return $"'{term}' kelimesi bu kategoride zaten kayıtlı.";
        }

        word.CategoryId = dto.CategoryId;
        word.TermText = term;
        word.MeaningText = dto.MeaningText.Trim();
        word.TermRead = Clean(dto.TermRead);
        word.MemoryConnection = Clean(dto.MemoryConnection);
        word.ExampleSentence = Clean(dto.ExampleSentence);
        word.ExampleSentenceMeaning = Clean(dto.ExampleSentenceMeaning);
        word.ExampleSentence2 = Clean(dto.ExampleSentence2);
        word.ExampleSentenceMeaning2 = Clean(dto.ExampleSentenceMeaning2);
        word.UpdatedAt = DateTime.UtcNow;

        await _uow.SaveChangesAsync(ct);
        return null;
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        var word = await _uow.Words.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Kelime bulunamadı.");

        // UserWordProgresses uzerindeki FK CASCADE oldugu icin, bu kelime
        // tum kullanicilarin "bilmedigim kelimeler" listesinden de otomatik dusulur.
        _uow.Words.Remove(word);
        await _uow.SaveChangesAsync(ct);
    }

    public async Task<ImportResultDto> ImportAsync(int categoryId, IReadOnlyList<ParsedWordRow> rows, CancellationToken ct = default)
    {
        _ = await _uow.Categories.GetByIdAsync(categoryId, ct)
            ?? throw new NotFoundException("Kategori bulunamadı.");

        var existingWords = await _uow.Words.GetByCategoryIdAsync(categoryId, ct);

        // Veritabanindaki mevcut kelimeler + dosya icindeki tekrarlar birlikte kontrol edilir.
        var seen = new HashSet<string>(
            existingWords.Select(w => w.TermText.Trim()),
            StringComparer.OrdinalIgnoreCase);

        var toInsert = new List<Word>();
        var errors = new List<ImportRowError>();

        foreach (var row in rows)
        {
            var term = row.TermText.Trim();
            var meaning = row.MeaningText.Trim();

            if (term.Length == 0 || meaning.Length == 0)
            {
                errors.Add(new ImportRowError(row.RowNumber, term, "Kelime ve Türkçe anlam alanları zorunludur."));
                continue;
            }

            if (!seen.Add(term))
            {
                errors.Add(new ImportRowError(row.RowNumber, term, "Bu kelime zaten mevcut (tekrar kayıt atlandı)."));
                continue;
            }

            toInsert.Add(new Word
            {
                CategoryId = categoryId,
                TermText = term,
                MeaningText = meaning,
                TermRead = Clean(row.TermRead),
                MemoryConnection = Clean(row.MemoryConnection),
                ExampleSentence = Clean(row.ExampleSentence),
                ExampleSentenceMeaning = Clean(row.ExampleSentenceMeaning),
                ExampleSentence2 = Clean(row.ExampleSentence2),
                ExampleSentenceMeaning2 = Clean(row.ExampleSentenceMeaning2),
                CreatedAt = DateTime.UtcNow
            });
        }

        if (toInsert.Count > 0)
        {
            _uow.Words.AddRange(toInsert);
            await _uow.SaveChangesAsync(ct);
        }

        return new ImportResultDto(toInsert.Count, errors.Count, errors);
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
