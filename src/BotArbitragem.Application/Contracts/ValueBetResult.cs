namespace BotArbitragem.Application.Contracts;

public sealed record ValueBetResult(
    decimal EstimatedProbability,
    decimal MarketOdds,
    decimal ExpectedValue,
    decimal ImpliedProbability,
    decimal Edge,
    bool HasPositiveValue);
