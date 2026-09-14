using System.Text.Json;

namespace PracticeLanguageWords.Web.Services;

/// <summary>
/// Oturum ici "son gosterilen kelimeler" listesi. Agirlikli secim algoritmasi bu listedeki
/// kelimeleri atlayarak ayni kelimenin art arda gelmesini engeller (anti-repeat).
/// </summary>
public interface IRecentWordsTracker
{
    IReadOnlyCollection<int> Get(string scopeKey);
    void Push(string scopeKey, int wordId);
    void Clear(string scopeKey);
}

public class SessionRecentWordsTracker : IRecentWordsTracker
{
    /// <summary>Kac kelime geriye dogru tekrar engellenecek.</summary>
    private const int WindowSize = 8;

    private readonly IHttpContextAccessor _httpContextAccessor;

    public SessionRecentWordsTracker(IHttpContextAccessor httpContextAccessor) =>
        _httpContextAccessor = httpContextAccessor;

    private ISession? Session => _httpContextAccessor.HttpContext?.Session;

    public IReadOnlyCollection<int> Get(string scopeKey)
    {
        var raw = Session?.GetString(BuildKey(scopeKey));
        if (string.IsNullOrEmpty(raw))
        {
            return Array.Empty<int>();
        }

        return JsonSerializer.Deserialize<List<int>>(raw) ?? new List<int>();
    }

    public void Push(string scopeKey, int wordId)
    {
        if (Session is null)
        {
            return;
        }

        var list = Get(scopeKey).ToList();
        list.Remove(wordId);
        list.Add(wordId);

        if (list.Count > WindowSize)
        {
            list = list.Skip(list.Count - WindowSize).ToList();
        }

        Session.SetString(BuildKey(scopeKey), JsonSerializer.Serialize(list));
    }

    public void Clear(string scopeKey) => Session?.Remove(BuildKey(scopeKey));

    private static string BuildKey(string scopeKey) => $"recent-words:{scopeKey}";
}
