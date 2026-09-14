namespace PracticeLanguageWords.Application.Interfaces;

/// <summary>
/// Kullanicinin sesli soyledigi kelimenin dogru olup olmadigini degerlendirir.
///
/// Su anki uygulama (seviye 1) tarayicinin Web Speech API'sinden gelen METNI karsilastirir:
/// ucretsizdir, anahtar gerektirmez, ama "ne dedigini" olcer, "ne kadar iyi soyledigini" degil.
/// Ileride fonem bazli puanlama (orn. Azure Pronunciation Assessment) istenirse bu arayuzu
/// implemente eden yeni bir sinif yazip DI'a kaydetmek yeterlidir - cagiran kod degismez (OCP).
/// </summary>
public interface IPronunciationEvaluator
{
    /// <param name="transcripts">Tarayicinin dondurdugu alternatifler; herhangi biri tutarsa dogru sayilir.</param>
    /// <param name="expectedTerm">Beklenen kelime (hedef dildeki yazimi).</param>
    PronunciationJudgement Judge(IReadOnlyList<string> transcripts, string expectedTerm);
}

public record PronunciationJudgement(bool IsCorrect, string BestTranscript);
