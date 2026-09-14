namespace PracticeLanguageWords.Domain.Models;

/// <summary>
/// Haftalik lider tablosu projeksiyonu: bir kullanicinin verilen hafta araligindaki
/// toplam kart sayisi (UserStreakLogs.CardCount toplami). Sadece o hafta en az 1
/// aktivitesi olan kullanicilar bu listede yer alir.
/// </summary>
public record LeaderboardRow(int UserId, string Username, string FirstName, string LastName, int WeeklyCardCount, DateTime? LastSeenAtUtc);
