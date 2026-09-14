using System.Text;
using System.Text.RegularExpressions;
using PracticeLanguageWords.Application.Interfaces;

namespace PracticeLanguageWords.Application.Services;

/// <summary>
/// Karsilastirma kurallari:
/// 1) Buyuk/kucuk harf farki onemsizdir.
/// 2) Turkce karakterler sadelestirilir (gulmek = gülmek), boylece klavye farki cevabi yanlis yapmaz.
/// 3) Noktalama ve fazla bosluklar temizlenir.
/// 4) Parantez icindeki aciklamalar ("gulmek (yuksek sesle)") hem dahil hem haric denenir.
/// 5) Birden fazla anlam varsa (virgulle ayrilmis) bir tanesini bilmek yeterlidir.
/// </summary>
public class AnswerEvaluator : IAnswerEvaluator
{
    private static readonly Regex ParenthesesPattern = new(@"\(.*?\)", RegexOptions.Compiled);
    private static readonly Regex NonWordPattern = new(@"[^\p{L}\p{Nd}\s]", RegexOptions.Compiled);
    private static readonly Regex WhitespacePattern = new(@"\s+", RegexOptions.Compiled);

    public bool IsCorrect(string? userAnswer, IReadOnlyList<string> acceptedMeanings)
    {
        if (string.IsNullOrWhiteSpace(userAnswer) || acceptedMeanings.Count == 0)
        {
            return false;
        }

        var normalizedUser = Normalize(userAnswer);
        if (normalizedUser.Length == 0)
        {
            return false;
        }

        foreach (var meaning in acceptedMeanings)
        {
            if (Normalize(meaning) == normalizedUser)
            {
                return true;
            }

            // Parantezli aciklamayi atarak da dene: "gulmek (yuksek sesle)" -> "gulmek"
            var withoutParentheses = ParenthesesPattern.Replace(meaning, " ");
            if (Normalize(withoutParentheses) == normalizedUser)
            {
                return true;
            }
        }

        return false;
    }

    private static string Normalize(string value)
    {
        var folded = FoldTurkishCharacters(value);
        folded = NonWordPattern.Replace(folded, " ");
        folded = WhitespacePattern.Replace(folded, " ");
        return folded.Trim().ToLowerInvariant();
    }

    private static string FoldTurkishCharacters(string value)
    {
        var builder = new StringBuilder(value.Length);

        foreach (var ch in value)
        {
            builder.Append(ch switch
            {
                'ı' or 'İ' or 'I' or 'i' => 'i',
                'ş' or 'Ş' => 's',
                'ğ' or 'Ğ' => 'g',
                'ü' or 'Ü' => 'u',
                'ö' or 'Ö' => 'o',
                'ç' or 'Ç' => 'c',
                'â' or 'Â' => 'a',
                'î' or 'Î' => 'i',
                'û' or 'Û' => 'u',
                _ => ch
            });
        }

        return builder.ToString();
    }
}
