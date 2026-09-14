using ClosedXML.Excel;
using PracticeLanguageWords.Application.DTOs;
using PracticeLanguageWords.Application.Interfaces;

namespace PracticeLanguageWords.Infrastructure.Import;

/// <summary>
/// Ilk sayfadaki ilk satir baslik kabul edilir. Kolon eslesmesi baslik adina gore yapilir,
/// boylece kolon sirasi degisse bile import bozulmaz. Eski kolon adlari da desteklenir.
/// </summary>
public class ExcelWordImportParser : IWordImportParser
{
    public bool CanParse(string fileExtension) =>
        fileExtension.Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
        || fileExtension.Equals(".xlsm", StringComparison.OrdinalIgnoreCase);

    public Task<IReadOnlyList<ParsedWordRow>> ParseAsync(Stream fileStream, CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook(fileStream);
        var worksheet = workbook.Worksheets.First();

        var headerRow = worksheet.FirstRowUsed()
            ?? throw new InvalidOperationException("Excel dosyasi bos gorunuyor.");

        var columns = new Dictionary<string, int>();
        foreach (var cell in headerRow.CellsUsed())
        {
            var header = ImportHeaders.Normalize(cell.GetString());
            if (header.Length > 0 && !columns.ContainsKey(header))
            {
                columns[header] = cell.Address.ColumnNumber;
            }
        }

        var rows = new List<ParsedWordRow>();
        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? headerRow.RowNumber();

        for (var rowNumber = headerRow.RowNumber() + 1; rowNumber <= lastRow; rowNumber++)
        {
            ct.ThrowIfCancellationRequested();

            var row = worksheet.Row(rowNumber);
            if (row.IsEmpty())
            {
                continue;
            }

            rows.Add(new ParsedWordRow(
                rowNumber,
                GetValue(row, columns, ImportHeaders.Term) ?? string.Empty,
                GetValue(row, columns, ImportHeaders.Meaning) ?? string.Empty,
                GetValue(row, columns, ImportHeaders.Read),
                GetValue(row, columns, ImportHeaders.Memory),
                GetValue(row, columns, ImportHeaders.Example),
                GetValue(row, columns, ImportHeaders.ExampleMeaning),
                GetValue(row, columns, ImportHeaders.Example2),
                GetValue(row, columns, ImportHeaders.ExampleMeaning2)));
        }

        return Task.FromResult<IReadOnlyList<ParsedWordRow>>(rows);
    }

    private static string? GetValue(IXLRow row, IReadOnlyDictionary<string, int> columns, IEnumerable<string> headerAliases)
    {
        foreach (var alias in headerAliases)
        {
            if (!columns.TryGetValue(alias, out var columnNumber))
            {
                continue;
            }

            var value = row.Cell(columnNumber).GetString().Trim();
            if (value.Length > 0)
            {
                return value;
            }
        }

        return null;
    }
}
