namespace BotArbitragem.Application.Contracts;

public sealed record ProviderEvent(
    string ExternalId,
    string SportKey,
    string Competition,
    string HomeTeam,
    string AwayTeam,
    DateTimeOffset KickoffAt,
    IReadOnlyList<ProviderOddsQuote> Odds);

public sealed record ProviderOddsQuote(
    string Bookmaker,
    string Market,
    string Selection,
    decimal DecimalOdds,
    DateTimeOffset CapturedAt);

public sealed record IngestionResult(string SportKey, int EventsReceived, int MatchesCreated, int OddsCreated, int OddsSkipped);
