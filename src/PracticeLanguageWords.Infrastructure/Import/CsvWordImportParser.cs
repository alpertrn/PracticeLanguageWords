using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;

namespace PracticeLanguageWords.Infrastructure.Import;

/// <summary>
/// Beklenen kolon basliklari (buyuk/kucuk harf duyarsiz, sirasi onemsiz):
/// TermText; MeaningText; TermRead; MemoryConnection; ExampleSentence; ExampleSentenceMeaning;
/// ExampleSentence2; ExampleSentenceMeaning2 (ikinci ornek cumle - opsiyonel)
/// Eski adlar (EnglishText, TurkishText, TurkishRead, ExampleSentenceTr) da kabul edilir.
/// Ayirici otomatik algilanir (virgul veya noktali virgul - Turkce Excel ciktilarinda ";" yaygindir).
/// </summary>
public class CsvWordImportParser : IWordImportParser
{
    public bool CanParse(string fileExtension) =>
        fileExtension.Equals(".csv", StringComparison.OrdinalIgnoreCase);

    public async Task<IReadOnlyList<ParsedWordRow>> ParseAsync(Stream fileStream, CancellationToken ct = default)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            DetectDelimiter = true,
            HeaderValidated = null,
            MissingFieldFound = null,
            TrimOptions = TrimOptions.Trim,
            PrepareHeaderForMatch = args => ImportHeaders.Normalize(args.Header)
        };

        using var reader = new StreamReader(fileStream);
        using var csv = new CsvReader(reader, config);

        var rows = new List<ParsedWordRow>();

        await csv.ReadAsync();
        csv.ReadHeader();

        var rowNumber = 1; // baslik satiri

        while (await csv.ReadAsync())
        {
            ct.ThrowIfCancellationRequested();
            rowNumber++;

            rows.Add(new ParsedWordRow(
                rowNumber,
                GetField(csv, ImportHeaders.Term) ?? string.Empty,
                GetField(csv, ImportHeaders.Meaning) ?? string.Empty,
                GetField(csv, ImportHeaders.Read),
                GetField(csv, ImportHeaders.Memory),
                GetField(csv, ImportHeaders.Example),
                GetField(csv, ImportHeaders.ExampleMeaning),
                GetField(csv, ImportHeaders.Example2),
                GetField(csv, ImportHeaders.ExampleMeaning2)));
        }

        return rows;
    }

    /// <summary>Verilen alternatif basliklardan ilk bulunani okur.</summary>
    private static string? GetField(IReaderRow csv, IEnumerable<string> headerAliases)
    {
        foreach (var alias in headerAliases)
        {
            if (csv.TryGetField<string>(alias, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
