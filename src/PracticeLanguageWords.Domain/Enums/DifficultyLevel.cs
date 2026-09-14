namespace PracticeLanguageWords.Domain.Enums;

/// <summary>
/// Kullanicinin karta verdigi zorluk derecesi (Kolay / Orta / Zor).
/// Sadece "Zor" isaretlenen kelimeler "Bilmediğim Kelimeler" listesine dusser
/// ve daha sonra yazarak cevaplama modunda sorulur.
/// Agirlikli rastgele secim algoritmasi da bu degeri kullanir.
/// </summary>
public enum DifficultyLevel : byte
{
    Easy = 1,
    Medium = 2,
    Hard = 3
}
