using PracticeLanguageWords.Application.DTOs;

namespace PracticeLanguageWords.Application.Interfaces;

public interface ILanguageAdminService
{
    Task<IReadOnlyList<LanguageDto>> GetAllAsync(CancellationToken ct = default);
    Task<LanguageEditDto?> GetForEditAsync(int id, CancellationToken ct = default);

    /// <returns>Basariliysa null; ayni kod zaten varsa hata mesaji doner.</returns>
    Task<string?> CreateAsync(LanguageEditDto dto, CancellationToken ct = default);
    Task<string?> UpdateAsync(LanguageEditDto dto, CancellationToken ct = default);

    /// <summary>Dile bagli kategori varsa silmeyi reddeder.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

public interface ICategoryGroupAdminService
{
    Task<IReadOnlyList<CategoryGroupDto>> GetAllAsync(CancellationToken ct = default);
    Task<CategoryGroupEditDto?> GetForEditAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(CategoryGroupEditDto dto, CancellationToken ct = default);
    Task UpdateAsync(CategoryGroupEditDto dto, CancellationToken ct = default);

    /// <summary>Icinde kategori varsa silmeyi reddeder.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

public interface ICategoryAdminService
{
    Task<IReadOnlyList<CategoryAdminDto>> GetAllAsync(int? languageId = null, CancellationToken ct = default);
    Task<CategoryEditDto?> GetForEditAsync(int id, CancellationToken ct = default);
    Task<int> CreateAsync(CategoryEditDto dto, CancellationToken ct = default);
    Task UpdateAsync(CategoryEditDto dto, CancellationToken ct = default);

    /// <summary>Icinde kelime varsa silmeyi reddeder (best practice: veri kaybini engelle).</summary>
    Task<bool> DeleteAsync(int id, CancellationToken ct = default);
}

public interface IWordAdminService
{
    Task<PagedResult<WordAdminDto>> SearchAsync(string? term, int? categoryId, int? languageId, int page, int pageSize, CancellationToken ct = default);
    Task<WordEditDto?> GetForEditAsync(int id, CancellationToken ct = default);

    /// <returns>Basariliysa null; ayni kategoride ayni kelime zaten varsa hata mesaji doner.</returns>
    Task<string?> CreateAsync(WordEditDto dto, CancellationToken ct = default);
    Task<string?> UpdateAsync(WordEditDto dto, CancellationToken ct = default);

    /// <summary>
    /// Kelimeyi siler. Kullanicilarin "bilmedigim kelimeler" listesindeki ilgili kayitlar
    /// (UserWordProgress) veritabani seviyesinde CASCADE ile birlikte silinir.
    /// </summary>
    Task DeleteAsync(int id, CancellationToken ct = default);

    Task<ImportResultDto> ImportAsync(int categoryId, IReadOnlyList<ParsedWordRow> rows, CancellationToken ct = default);
}

/// <summary>
/// Acik/Kapali prensibi (Open/Closed) ornegi: yeni bir dosya formati eklemek icin
/// bu arayuzu implemente eden yeni bir sinif yazmak yeterli, mevcut kod degismez.
/// </summary>
public interface IWordImportParser
{
    bool CanParse(string fileExtension);
    Task<IReadOnlyList<ParsedWordRow>> ParseAsync(Stream fileStream, CancellationToken ct = default);
}

public interface IWordImportParserFactory
{
    IWordImportParser GetParser(string fileExtension);
}
