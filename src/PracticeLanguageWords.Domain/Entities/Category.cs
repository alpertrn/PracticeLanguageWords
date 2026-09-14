namespace PracticeLanguageWords.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public int CategoryGroupId { get; set; }
    public int LanguageId { get; set; }
    public string Name { get; set; } = string.Empty;

    public CategoryGroup CategoryGroup { get; set; } = null!;
    public Language Language { get; set; } = null!;
    public ICollection<Word> Words { get; set; } = new List<Word>();
}
