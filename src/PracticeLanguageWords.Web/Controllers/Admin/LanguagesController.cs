using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;

namespace PracticeLanguageWords.Web.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[Route("admin/languages")]
public class LanguagesController : Controller
{
    private readonly ILanguageAdminService _languageService;

    public LanguagesController(ILanguageAdminService languageService) => _languageService = languageService;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var languages = await _languageService.GetAllAsync(ct);
        return View("~/Views/Admin/Languages/Index.cshtml", languages);
    }

    [HttpGet("create")]
    public IActionResult Create() =>
        View("~/Views/Admin/Languages/Edit.cshtml", new LanguageEditDto(0, string.Empty, string.Empty, string.Empty, true, 0));

    [HttpPost("create")]
    public async Task<IActionResult> Create(LanguageEditDto model, CancellationToken ct)
    {
        var validationError = Validate(model);
        if (validationError is not null)
        {
            ModelState.AddModelError(string.Empty, validationError);
            return View("~/Views/Admin/Languages/Edit.cshtml", model);
        }

        var error = await _languageService.CreateAsync(model, ct);
        if (error is not null)
        {
            ModelState.AddModelError(string.Empty, error);
            return View("~/Views/Admin/Languages/Edit.cshtml", model);
        }

        TempData["Success"] = "Dil eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var language = await _languageService.GetForEditAsync(id, ct);
        return language is null
            ? NotFound()
            : View("~/Views/Admin/Languages/Edit.cshtml", language);
    }

    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, LanguageEditDto model, CancellationToken ct)
    {
        model = model with { Id = id };

        var validationError = Validate(model);
        if (validationError is not null)
        {
            ModelState.AddModelError(string.Empty, validationError);
            return View("~/Views/Admin/Languages/Edit.cshtml", model);
        }

        var error = await _languageService.UpdateAsync(model, ct);
        if (error is not null)
        {
            ModelState.AddModelError(string.Empty, error);
            return View("~/Views/Admin/Languages/Edit.cshtml", model);
        }

        TempData["Success"] = "Dil güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _languageService.DeleteAsync(id, ct);

        TempData[deleted ? "Success" : "Error"] = deleted
            ? "Dil silindi."
            : "Bu dile bağlı kategoriler var. Silmek yerine dili pasif yapabilirsiniz.";

        return RedirectToAction(nameof(Index));
    }

    private static string? Validate(LanguageEditDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Code))
        {
            return "Dil kodu zorunludur (örn: de).";
        }

        if (string.IsNullOrWhiteSpace(model.Name))
        {
            return "Dil adı zorunludur (örn: Almanca).";
        }

        if (string.IsNullOrWhiteSpace(model.SpeechCode))
        {
            return "Seslendirme kodu zorunludur (örn: de-DE).";
        }

        return null;
    }
}

[Authorize(Policy = "AdminOnly")]
[Route("admin/category-groups")]
public class CategoryGroupsController : Controller
{
    private readonly ICategoryGroupAdminService _groupService;

    public CategoryGroupsController(ICategoryGroupAdminService groupService) => _groupService = groupService;

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var groups = await _groupService.GetAllAsync(ct);
        return View("~/Views/Admin/CategoryGroups/Index.cshtml", groups);
    }

    [HttpGet("create")]
    public IActionResult Create() =>
        View("~/Views/Admin/CategoryGroups/Edit.cshtml", new CategoryGroupEditDto(0, string.Empty, "Flashcard"));

    [HttpPost("create")]
    public async Task<IActionResult> Create(CategoryGroupEditDto model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(string.Empty, "Grup adı zorunludur.");
            return View("~/Views/Admin/CategoryGroups/Edit.cshtml", model);
        }

        await _groupService.CreateAsync(model, ct);
        TempData["Success"] = "Kategori grubu eklendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var group = await _groupService.GetForEditAsync(id, ct);
        return group is null
            ? NotFound()
            : View("~/Views/Admin/CategoryGroups/Edit.cshtml", group);
    }

    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CategoryGroupEditDto model, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(model.Name))
        {
            ModelState.AddModelError(string.Empty, "Grup adı zorunludur.");
            return View("~/Views/Admin/CategoryGroups/Edit.cshtml", model);
        }

        await _groupService.UpdateAsync(model with { Id = id }, ct);
        TempData["Success"] = "Kategori grubu güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var deleted = await _groupService.DeleteAsync(id, ct);

        TempData[deleted ? "Success" : "Error"] = deleted
            ? "Kategori grubu silindi."
            : "Bu grupta kategoriler var. Önce kategorileri taşıyın veya silin.";

        return RedirectToAction(nameof(Index));
    }
}
