using PracticeLanguageWords.Application.Common;
using PracticeLanguageWords.Application.Interfaces;
using PracticeLanguageWords.Domain.Entities;
using PracticeLanguageWords.Domain.Enums;
using PracticeLanguageWords.Domain.Interfaces;
using PracticeLanguageWords.Domain.Models;

namespace PracticeLanguageWords.Application.Services;

/// <summary>
/// Iki farkli secim stratejisi kullanir - bunlar birbirine KARISTIRILMAMALI:
///
///  - Kategori pratigi: AGIRLIKLI rastgele secim. Zor isaretlenen kelime en sik, kolay
///    isaretlenen en seyrek gelir (Difficulty = "gosterilme sikligi" kavramini tasir).
///  - Bilmediğim Kelimeler quizi: DUZ (agirliksiz) rastgele secim. Difficulty burada
///    kategori pratiginin bir yan etkisi olabilir (ayni kelime kategoride "Kolay"
///    olarak da isaretlenmis olabilir, IsUnknown bundan etkilenmez) - bu yuzden quiz
///    havuzunu Difficulty'ye gore agirliklandirmak, hala listede olan bir kelimeyi
///    sirf "Kolay" etiketi tasidigi icin daha seyrek sinamak gibi istenmeyen bir sonuc
///    dogurur. Quizin tek "adil" kisitlamasi gun bazli uygunluktur (bkz.
///    GetUnknownCandidatesAsync), agirlik degil.
///
/// Ikisi de ayni oturum-ici tekrar-engelleme (anti-repeat) mantigini paylasir.
/// </summary>
public class WordSelectionService : IWordSelectionService
{
    private const int WeightHard = 5;
    private const int WeightNeverSeen = 3;
    private const int WeightMedium = 3;
    private const int WeightEasy = 1;

    private readonly IUnitOfWork _uow;

    public WordSelectionService(IUnitOfWork uow)
    {
        _uow = uow;
    }

    public async Task<Word?> SelectFromCategoryAsync(int userId, int categoryId, IReadOnlyCollection<int> recentlyShownWordIds, CancellationToken ct = default)
    {
        var candidates = await _uow.Words.GetCandidatesInCategoryAsync(userId, categoryId, ct);
        var pool = NarrowPool(candidates, recentlyShownWordIds);

        if (pool is null)
        {
            return null;
        }

        var chosenId = WeightedPick(pool);
        return await _uow.Words.GetByIdAsync(chosenId, ct);
    }

    public async Task<Word?> SelectFromUnknownAsync(int userId, int? languageId, IReadOnlyCollection<int> recentlyShownWordIds, CancellationToken ct = default)
    {
        // Bugun zaten puanlanmis kelimeler havuzdan disaridadir (bkz. GetUnknownCandidatesAsync) -
        // degerlendirme boylece birkac gune yayilir, ayni gun tekrar cikip listeden dusurmez.
        var candidates = await _uow.Words.GetUnknownCandidatesAsync(userId, languageId, TurkeyClock.Today(), ct);
        var pool = NarrowPool(candidates, recentlyShownWordIds);

        if (pool is null)
        {
            return null;
        }

        // BILEREK agirliksiz: bu havuzdaki her kelime esit sansla gelir (bkz. sinif ustu aciklama).
        var chosenId = pool[Random.Shared.Next(pool.Count)].WordId;
        return await _uow.Words.GetByIdAsync(chosenId, ct);
    }

    /// <summary>
    /// Anti-repeat: son gosterilen kelimeleri havuzdan cikarir. Havuz tamamen bosalirsa
    /// (ornegin kategoride 3 kelime varsa) tum havuza geri doner, boylece sonsuz dongu
    /// modu asla kilitlenmez. Aday listesi bastan bosa, havuz secilecek kelime yoktur (null).
    /// </summary>
    private static List<WordCandidate>? NarrowPool(List<WordCandidate> candidates, IReadOnlyCollection<int> recentlyShownWordIds)
    {
        if (candidates.Count == 0)
        {
            return null;
        }

        var pool = candidates.Where(c => !recentlyShownWordIds.Contains(c.WordId)).ToList();
        return pool.Count == 0 ? candidates : pool;
    }

    private static int WeightedPick(IReadOnlyList<WordCandidate> pool)
    {
        var totalWeight = 0;
        foreach (var candidate in pool)
        {
            totalWeight += GetWeight(candidate.Difficulty);
        }

        var roll = Random.Shared.Next(totalWeight);
        var cumulative = 0;

        foreach (var candidate in pool)
        {
            cumulative += GetWeight(candidate.Difficulty);
            if (roll < cumulative)
            {
                return candidate.WordId;
            }
        }

        return pool[^1].WordId; // teorik olarak ulasilmaz; savunmaci kod
    }

    private static int GetWeight(DifficultyLevel? difficulty) => difficulty switch
    {
        DifficultyLevel.Hard => WeightHard,
        DifficultyLevel.Medium => WeightMedium,
        DifficultyLevel.Easy => WeightEasy,
        _ => WeightNeverSeen // hic gorulmemis kelime
    };
}
