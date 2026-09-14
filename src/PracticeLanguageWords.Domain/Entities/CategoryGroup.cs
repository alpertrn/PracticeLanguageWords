namespace PracticeLanguageWords.Domain.Entities;

public class CategoryGroup
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string UiTemplate { get; set; } = "Flashcard";

    public ICollection<Category> Categories { get; set; } = new List<Category>();
}
