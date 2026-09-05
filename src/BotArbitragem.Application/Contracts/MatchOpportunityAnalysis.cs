namespace BotArbitragem.Application.Contracts;

public sealed record MatchOpportunityAnalysis(
    Guid MatchId,
    DateTimeOffset AnalyzedAt,
    int EligibleBookmakers,
    int CandidatesEvaluated,
    IReadOnlyList<ValueOpportunity> Opportunities);

public sealed record ValueOpportunity(
    string Bookmaker,
    string Market,
    string Selection,
    decimal MarketOdds,
    decimal FairProbability,
    decimal ImpliedProbability,
    decimal ExpectedValue,
    decimal Edge,
    int ReferenceBookmakers,
    DateTimeOffset CapturedAt);

