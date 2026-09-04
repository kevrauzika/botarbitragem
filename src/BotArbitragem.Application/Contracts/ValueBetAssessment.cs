namespace BotArbitragem.Application.Contracts;

public sealed record ValueBetAssessment(decimal EstimatedProbability, decimal MarketOdds, decimal ImpliedProbability,
    decimal ExpectedValue, decimal Edge, bool IsQualified, IReadOnlyList<string> RejectionReasons);
