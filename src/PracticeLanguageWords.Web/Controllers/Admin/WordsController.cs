using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PracticeLanguageWords.Application.Common;
using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;

namespace PracticeLanguageWords.Web.Controllers.Admin;

[Authorize(Policy = "AdminOnly")]
[Route("admin/words")]
public class WordsController : Controller
{
    private const long MaxUploadBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedExtensions = { ".csv", ".xlsx", ".xlsm" };

    private readonly IWordAdminService _wordService;
    private readonly ICategoryAdminService _categoryService;
    private readonly ILanguageAdminService _languageService;
    private readonly IWordImportParserFactory _parserFactory;

    public WordsController(
        IWordAdminService wordService,
        ICategoryAdminService categoryService,
        ILanguageAdminService languageService,
        IWordImportParserFactory parserFactory)
    {
        _wordService = wordService;
        _categoryService = categoryService;
        _languageService = languageService;
        _parserFactory = parserFactory;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(string? term, int? categoryId, int? languageId, int page = 1, CancellationToken ct = default)
    {
        var result = await _wordService.SearchAsync(term, categoryId, languageId, page, 25, ct);

        ViewBag.Term = term;
        ViewBag.CategoryId = categoryId;
        ViewBag.LanguageId = languageId;
        await LoadLookupsAsync(languageId, ct);

        return View("~/Views/Admin/Words/Index.cshtml", result);
    }

    [HttpGet("create")]
    public async Task<IActionResult> Create(int? categoryId, CancellationToken ct)
    {
        await LoadLookupsAsync(null, ct);
        return View("~/Views/Admin/Words/Edit.cshtml",
            new WordEditDto(null, categoryId ?? 0, string.Empty, string.Empty, null, null, null, null, null, null));
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(WordEditDto model, CancellationToken ct)
    {
        var validationError = Validate(model);
        if (validationError is not null)
        {
            ModelState.AddModelError(string.Empty, validationError);
            await LoadLookupsAsync(null, ct);
            return View("~/Views/Admin/Words/Edit.cshtml", model);
        }

        var error = await _wordService.CreateAsync(model, ct);
        if (error is not null)
        {
            ModelState.AddModelError(string.Empty, error);
            await LoadLookupsAsync(null, ct);
            return View("~/Views/Admin/Words/Edit.cshtml", model);
        }

        TempData["Success"] = "Kelime eklendi.";
        return RedirectToAction(nameof(Index), new { categoryId = model.CategoryId });
    }

    [HttpGet("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var word = await _wordService.GetForEditAsync(id, ct);
        if (word is null)
        {
            return NotFound();
        }

        await LoadLookupsAsync(null, ct);
        return View("~/Views/Admin/Words/Edit.cshtml", word);
    }

    [HttpPost("{id:int}/edit")]
    public async Task<IActionResult> Edit(int id, WordEditDto model, CancellationToken ct)
    {
        model = model with { Id = id };

        var validationError = Validate(model);
        if (validationError is not null)
        {
            ModelState.AddModelError(string.Empty, validationError);
            await LoadLookupsAsync(null, ct);
            return View("~/Views/Admin/Words/Edit.cshtml", model);
        }

        var error = await _wordService.UpdateAsync(model, ct);
        if (error is not null)
        {
            ModelState.AddModelError(string.Empty, error);
            await LoadLookupsAsync(null, ct);
            return View("~/Views/Admin/Words/Edit.cshtml", model);
        }

        TempData["Success"] = "Kelime güncellendi.";
        return RedirectToAction(nameof(Index), new { categoryId = model.CategoryId });
    }

    [HttpPost("{id:int}/delete")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _wordService.DeleteAsync(id, ct);
        TempData["Success"] = "Kelime silindi. Kullanıcıların listelerinden de kaldırıldı.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet("import")]
    public async Task<IActionResult> Import(CancellationToken ct)
    {
        await LoadLookupsAsync(null, ct);
        return View("~/Views/Admin/Words/Import.cshtml");
    }

    [HttpPost("import")]
    [RequestSizeLimit(MaxUploadBytes)]
    public async Task<IActionResult> Import(int categoryId, IFormFile? file, CancellationToken ct)
    {
        await LoadLookupsAsync(null, ct);

        if (categoryId == 0)
        {
            ModelState.AddModelError(string.Empty, "Kategori seçmelisiniz.");
            return View("~/Views/Admin/Words/Import.cshtml");
        }

        if (file is null || file.Length == 0)
        {
            ModelState.AddModelError(string.Empty, "Bir dosya seçmelisiniz.");
            return View("~/Views/Admin/Words/Import.cshtml");
        }

        if (file.Length > MaxUploadBytes)
        {
            ModelState.AddModelError(string.Empty, "Dosya boyutu en fazla 5 MB olabilir.");
            return View("~/Views/Admin/Words/Import.cshtml");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, "Sadece .csv, .xlsx ve .xlsm dosyaları yüklenebilir.");
            return View("~/Views/Admin/Words/Import.cshtml");
        }

        try
        {
            var parser = _parserFactory.GetParser(extension);

            await using var stream = file.OpenReadStream();
            var rows = await parser.ParseAsync(stream, ct);

            var result = await _wordService.ImportAsync(categoryId, rows, ct);

            ViewBag.ImportResult = result;
            TempData["Success"] = $"{result.Inserted} kelime eklendi, {result.Skipped} satır atlandı.";
            return View("~/Views/Admin/Words/Import.cshtml");
        }
        catch (BusinessRuleException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View("~/Views/Admin/Words/Import.cshtml");
        }
        catch (Exception)
        {
            ModelState.AddModelError(string.Empty, "Dosya okunamadı. Başlık satırını ve dosya formatını kontrol edin.");
            return View("~/Views/Admin/Words/Import.cshtml");
        }
    }

    private static string? Validate(WordEditDto model)
    {
        if (model.CategoryId == 0)
        {
            return "Kategori seçmelisiniz.";
        }

        if (string.IsNullOrWhiteSpace(model.TermText))
        {
            return "Kelime alanı zorunludur.";
        }

        if (string.IsNullOrWhiteSpace(model.MeaningText))
        {
            return "Türkçe anlam zorunludur.";
        }

        return null;
    }

    private async Task LoadLookupsAsync(int? languageId, CancellationToken ct)
    {
        ViewBag.Categories = await _categoryService.GetAllAsync(languageId, ct);
        ViewBag.Languages = await _languageService.GetAllAsync(ct);
    }
}
