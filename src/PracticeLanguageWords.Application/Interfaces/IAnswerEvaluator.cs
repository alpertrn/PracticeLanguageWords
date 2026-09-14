namespace PracticeLanguageWords.Application.Interfaces;

/// <summary>
/// Kullanicinin yazdigi Turkce cevabin kabul edilebilir anlamlardan biriyle eslesip
/// eslesmedigini degerlendirir. Tek sorumluluk: cevap karsilastirma (SRP) - bu sayede
/// kurallar degistiginde (ornegin yazim hatasi toleransi) sadece bu sinif degisir.
/// </summary>
public interface IAnswerEvaluator
{
    bool IsCorrect(string? userAnswer, IReadOnlyList<string> acceptedMeanings);
}
