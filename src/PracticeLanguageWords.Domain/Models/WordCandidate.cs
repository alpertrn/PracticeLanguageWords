using PracticeLanguageWords.Domain.Enums;

namespace PracticeLanguageWords.Domain.Models;

/// <summary>
/// Agirlikli secim icin gereken minimum bilgi. Tum Word satirini cekmemek icin
/// repository sadece bu projeksiyonu doner (performans).
/// Difficulty null ise kullanici bu kelimeyi hic gormemistir.
/// </summary>
public record WordCandidate(int WordId, DifficultyLevel? Difficulty);
