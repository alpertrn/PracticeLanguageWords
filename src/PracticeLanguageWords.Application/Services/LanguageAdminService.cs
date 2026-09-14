using PracticeLanguageWords.Application.Common;
using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;
using PracticeLanguageWords.Domain.Entities;
using PracticeLanguageWords.Domain.Interfaces;

namespace PracticeLanguageWords.Application.Services;

public class LanguageAdminService : ILanguageAdminService
{
    private readonly IUnitOfWork _uow;

    public LanguageAdminService(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<LanguageDto>> GetAllAsync(CancellationToken ct = default)
    {
        var languages = await _uow.Languages.GetAllAsync(onlyActive: false, ct);
        var categories = await _uow.Categories.GetAllWithWordCountAsync(null, ct);

        return languages
            .Select(l => new LanguageDto(
                l.Id,
                l.Code,
                l.Name,
                l.SpeechCode,
                l.IsActive,
                l.DisplayOrder,
                categories.Count(c => c.LanguageId == l.Id)))
            .ToList();
    }

    public async Task<LanguageEditDto?> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var language = await _uow.Languages.GetByIdAsync(id, ct);
        return language is null
            ? null
            : new LanguageEditDto(language.Id, language.Code, language.Name, language.SpeechCode, language.IsActive, language.DisplayOrder);
    }

    public async Task<string?> CreateAsync(LanguageEditDto dto, CancellationToken ct = default)
    {
        var code = dto.Code.Trim().ToLowerInvariant();

        if (await _uow.Languages.CodeExistsAsync(code, null, ct))
        {
            return $"'{code}' kodlu dil zaten kayıtlı.";
        }

        _uow.Languages.Add(new Language
        {
            Code = code,
            Name = dto.Name.Trim(),
            SpeechCode = dto.SpeechCode.Trim(),
            IsActive = dto.IsActive,
            DisplayOrder = dto.DisplayOrder
        });

        await _uow.SaveChangesAsync(ct);
        return null;
    }

    public async Task<string?> UpdateAsync(LanguageEditDto dto, CancellationToken ct = default)
    {
        var language = await _uow.Languages.GetByIdAsync(dto.Id, ct)
            ?? throw new NotFoundException("Dil bulunamadı.");

        var code = dto.Code.Trim().ToLowerInvariant();

        if (await _uow.Languages.CodeExistsAsync(code, language.Id, ct))
        {
            return $"'{code}' kodlu dil zaten kayıtlı.";
        }

        language.Code = code;
        language.Name = dto.Name.Trim();
        language.SpeechCode = dto.SpeechCode.Trim();
        language.IsActive = dto.IsActive;
        language.DisplayOrder = dto.DisplayOrder;

        await _uow.SaveChangesAsync(ct);
        return null;
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var language = await _uow.Languages.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Dil bulunamadı.");

        // Dile bagli kategori varsa silinmez; admin dili "pasif" yaparak gizleyebilir.
        if (await _uow.Languages.HasCategoriesAsync(id, ct))
        {
            return false;
        }

        _uow.Languages.Remove(language);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
