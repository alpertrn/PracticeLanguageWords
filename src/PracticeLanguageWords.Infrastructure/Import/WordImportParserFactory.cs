using PracticeLanguageWords.Application.Common;
using PracticeLanguageWords.Application.Interfaces;

namespace PracticeLanguageWords.Infrastructure.Import;

/// <summary>
/// Kayitli tum parserlar DI uzerinden enjekte edilir. Yeni bir format (orn. .json) eklemek icin
/// sadece yeni bir IWordImportParser yazip DI'a kaydetmek yeterlidir - bu sinif degismez (OCP).
/// </summary>
public class WordImportParserFactory : IWordImportParserFactory
{
    private readonly IEnumerable<IWordImportParser> _parsers;

    public WordImportParserFactory(IEnumerable<IWordImportParser> parsers) => _parsers = parsers;

    public IWordImportParser GetParser(string fileExtension)
    {
        var parser = _parsers.FirstOrDefault(p => p.CanParse(fileExtension));

        if (parser is null)
        {
            throw new BusinessRuleException($"Desteklenmeyen dosya turu: {fileExtension}. Sadece .csv, .xlsx ve .xlsm yuklenebilir.");
        }

        return parser;
    }
}
