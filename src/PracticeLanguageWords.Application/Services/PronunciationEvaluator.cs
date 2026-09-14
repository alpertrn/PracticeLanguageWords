using System.Globalization;
using System.Text;
using PracticeLanguageWords.Application.Interfaces;

namespace PracticeLanguageWords.Application.Services;

/// <summary>
/// Karsilastirma kurallari:
/// 1) Buyuk/kucuk harf ve noktalama yok sayilir.
/// 2) Aksanlar sadelestirilir (ü -> u, é -> e) - tarayici bazen aksansiz metin dondurur.
/// 3) Kelime uzunluguna gore kucuk bir yazim toleransi taninir (Levenshtein mesafesi):
///    <= 4 harf tam eslesme, <= 8 harf 1 fark, daha uzunsa 2 fark.
///    Bunun sebebi konusma tanimanin bazen "give up" yerine "giveup" gibi ufak farklar
///    dondurmesidir; amac kullaniciyi tanima hatasi yuzunden cezalandirmamaktir.
/// 4) Tarayicinin verdigi tum alternatifler denenir, biri tutarsa dogru sayilir.
/// </summary>
public class PronunciationEvaluator : IPronunciationEvaluator
{
    public PronunciationJudgement Judge(IReadOnlyList<string> transcripts, string expectedTerm)
    {
        var expected = Normalize(expectedTerm);
        var best = transcripts.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t))?.Trim() ?? string.Empty;

        if (expected.Length == 0 || transcripts.Count == 0)
        {
            return new PronunciationJudgement(false, best);
        }

        var tolerance = expected.Length <= 4 ? 0 : expected.Length <= 8 ? 1 : 2;

        foreach (var transcript in transcripts)
        {
            if (string.IsNullOrWhiteSpace(transcript))
            {
                continue;
            }

            var candidate = Normalize(transcript);

            if (candidate == expected || LevenshteinDistance(candidate, expected) <= tolerance)
            {
                return new PronunciationJudgement(true, transcript.Trim());
            }
        }

        return new PronunciationJudgement(false, best);
    }

    private static string Normalize(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var ch in decomposed)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);

            if (category == UnicodeCategory.NonSpacingMark)
            {
                continue; // aksan isareti at
            }

            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            // bosluk ve noktalama tamamen atilir: "give up" == "giveup"
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }

    private static int LevenshteinDistance(string a, string b)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        var previous = new int[b.Length + 1];
        var current = new int[b.Length + 1];

        for (var j = 0; j <= b.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            current[0] = i;

            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[b.Length];
    }
}
