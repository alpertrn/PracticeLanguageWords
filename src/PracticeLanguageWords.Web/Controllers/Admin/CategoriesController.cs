using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;

namespace PracticeLanguageWords.Web.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[Route("admin/categories")]
public class CategoriesController : Controller
{
    private readonly ICategoryAdminService _categoryService;
    private readonly ICategoryGroupAdminService _groupService;
    private readonly ILanguageAdminService _languageService;

    public CategoriesController(
        ICategoryAdminService categoryService,
        ICategoryGroupAdminService groupService,
        ILanguageAdminService languageService)
    {
        _categoryService = categoryService;
        _groupService = groupService;
        _languageService = languageService;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(int? languageId, CancellationToken ct)
    {
        var categories = await _categoryService.GetAllAsync(languageId, ct);

        ViewBag.LanguageId = languageId;
        ViewBag.Languages = await _languageService.GetAllAsync(ct);

        return View("~/Views/Admin/Categories/Index.cshtml", categories);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(int? languageId, CancellationToken ct)
    {
        await LoadLookupsAsync(ct);
        return View("~/Views/Admin/Categories/Edit.cshtml", new CategoryEditDto(0, string.Empty, 0, languageId ?? 0));
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(CategoryEditDto model, CancellationToken ct)
    {
        var validationError = Validate(model);
        if (validationError is not null)
        {
            ModelState.AddModelError(string.Empty, validationError);
            await LoadLookupsAsync(ct);
            return View("~/Views/Admin/Categories/Edit.cshtml", model);
        }

        await _categoryService.CreateAsync(model, ct);
        TempData["Success"] = "Kategori eklendi.";
        return RedirectToAction(nameof(Index), new { languageId = model.LanguageId });
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var category = await _categoryService.GetForEditAsync(id, ct);
        if (category is null)
        {
            return NotFound();
        }

        await LoadLookupsAsync(ct);
        return View("~/Views/Admin/Categories/Edit.cshtml", category);
    }

    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CategoryEditDto model, CancellationToken ct)
    {
        model = model with { Id = id };

        var validationError = Validate(model);
        if (validationError is not null)
        {
            ModelState.AddModelError(string.Empty, validationError);
            await LoadLookupsAsync(ct);
            return View("~/Views/Admin/Categories/Edit.cshtml", model);
        }

        await _categoryService.UpdateAsync(model, ct);
        TempData["Success"] = "Kategori güncellendi.";
        return RedirectToAction(nameof(Index), new { languageId = model.LanguageId });
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _categoryService.DeleteAsync(id, ct);

        TempData[deleted ? "Success" : "Error"] = deleted
            ? "Kategori silindi."
            : "Bu kategoride kelimeler var. Önce kelimeleri silin veya başka kategoriye taşıyın.";

        return RedirectToAction(nameof(Index));
    }

    private static string? Validate(CategoryEditDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            return "Kategori adı zorunludur.";
        }

        if (model.LanguageId == 0)
        {
            return "Dil seçmelisiniz.";
        }

        if (model.CategoryGroupId == 0)
        {
            return "Kategori grubu seçmelisiniz.";
        }

        return null;
    }

    private async Task LoadLookupsAsync(CancellationToken ct)
    {
        ViewBag.Groups = await _groupService.GetAllAsync(ct);
        ViewBag.Languages = await _languageService.GetAllAsync(ct);
    }
}
