namespace PracticeLanguageWords.Application.DTOs;

/// <summary>Tarayicidan gelen Push API aboneligi (PushSubscription.toJSON() ciktisi).</summary>
public record PushSubscriptionRequest(string Endpoint, string P256dh, string Auth);

public record PushUnsubscribeRequest(string Endpoint);
