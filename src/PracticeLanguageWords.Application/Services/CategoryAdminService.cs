using PracticeLanguageWords.Application.Common;
using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;
using PracticeLanguageWords.Domain.Entities;
using PracticeLanguageWords.Domain.Interfaces;

namespace PracticeLanguageWords.Application.Services;

public class CategoryAdminService : ICategoryAdminService
{
    private readonly IUnitOfWork _uow;

    public CategoryAdminService(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<CategoryAdminDto>> GetAllAsync(int? languageId = null, CancellationToken ct = default)
    {
        var categories = await _uow.Categories.GetAllWithWordCountAsync(languageId, ct);

        return categories
            .Select(c => new CategoryAdminDto(c.Id, c.Name, c.CategoryGroupId, c.CategoryGroupName, c.LanguageId, c.LanguageName, c.WordCount))
            .OrderBy(c => c.LanguageName)
            .ThenBy(c => c.Name)
            .ToList();
    }

    public async Task<CategoryEditDto?> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var category = await _uow.Categories.GetByIdAsync(id, ct);
        return category is null
            ? null
            : new CategoryEditDto(category.Id, category.Name, category.CategoryGroupId, category.LanguageId);
    }

    public async Task<int> CreateAsync(CategoryEditDto dto, CancellationToken ct = default)
    {
        await EnsureReferencesExistAsync(dto, ct);

        var category = new Category
        {
            Name = dto.Name.Trim(),
            CategoryGroupId = dto.CategoryGroupId,
            LanguageId = dto.LanguageId
        };

        _uow.Categories.Add(category);
        await _uow.SaveChangesAsync(ct);

        return category.Id;
    }

    public async Task UpdateAsync(CategoryEditDto dto, CancellationToken ct = default)
    {
        var category = await _uow.Categories.GetByIdAsync(dto.Id, ct)
            ?? throw new NotFoundException("Kategori bulunamadı.");

        await EnsureReferencesExistAsync(dto, ct);

        category.Name = dto.Name.Trim();
        category.CategoryGroupId = dto.CategoryGroupId;
        category.LanguageId = dto.LanguageId;

        await _uow.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var category = await _uow.Categories.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Kategori bulunamadı.");

        // Guvenlik: icinde kelime olan kategori silinemez. Aksi halde tek tikla
        // yuzlerce kelime ve kullanici ilerlemesi kaybolurdu.
        if (await _uow.Categories.HasWordsAsync(id, ct))
        {
            return false;
        }

        _uow.Categories.Remove(category);
        await _uow.SaveChangesAsync(ct);
        return true;
    }

    private async Task EnsureReferencesExistAsync(CategoryEditDto dto, CancellationToken ct)
    {
        _ = await _uow.CategoryGroups.GetByIdAsync(dto.CategoryGroupId, ct)
            ?? throw new NotFoundException("Kategori grubu bulunamadı.");

        _ = await _uow.Languages.GetByIdAsync(dto.LanguageId, ct)
            ?? throw new NotFoundException("Dil bulunamadı.");
    }
}

public class CategoryGroupAdminService : ICategoryGroupAdminService
{
    private readonly IUnitOfWork _uow;

    public CategoryGroupAdminService(IUnitOfWork uow) => _uow = uow;

    public async Task<IReadOnlyList<CategoryGroupDto>> GetAllAsync(CancellationToken ct = default)
    {
        var groups = await _uow.CategoryGroups.GetAllAsync(ct);
        var categories = await _uow.Categories.GetAllWithWordCountAsync(null, ct);

        return groups
            .Select(g => new CategoryGroupDto(g.Id, g.Name, g.UiTemplate, categories.Count(c => c.CategoryGroupId == g.Id)))
            .ToList();
    }

    public async Task<CategoryGroupEditDto?> GetForEditAsync(int id, CancellationToken ct = default)
    {
        var group = await _uow.CategoryGroups.GetByIdAsync(id, ct);
        return group is null ? null : new CategoryGroupEditDto(group.Id, group.Name, group.UiTemplate);
    }

    public async Task<int> CreateAsync(CategoryGroupEditDto dto, CancellationToken ct = default)
    {
        var group = new CategoryGroup
        {
            Name = dto.Name.Trim(),
            UiTemplate = string.IsNullOrWhiteSpace(dto.UiTemplate) ? "Flashcard" : dto.UiTemplate.Trim()
        };

        _uow.CategoryGroups.Add(group);
        await _uow.SaveChangesAsync(ct);
        return group.Id;
    }

    public async Task UpdateAsync(CategoryGroupEditDto dto, CancellationToken ct = default)
    {
        var group = await _uow.CategoryGroups.GetByIdAsync(dto.Id, ct)
            ?? throw new NotFoundException("Kategori grubu bulunamadı.");

        group.Name = dto.Name.Trim();
        group.UiTemplate = string.IsNullOrWhiteSpace(dto.UiTemplate) ? "Flashcard" : dto.UiTemplate.Trim();

        await _uow.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(int id, CancellationToken ct = default)
    {
        var group = await _uow.CategoryGroups.GetByIdAsync(id, ct)
            ?? throw new NotFoundException("Kategori grubu bulunamadı.");

        if (await _uow.CategoryGroups.HasCategoriesAsync(id, ct))
        {
            return false;
        }

        _uow.CategoryGroups.Remove(group);
        await _uow.SaveChangesAsync(ct);
        return true;
    }
}
