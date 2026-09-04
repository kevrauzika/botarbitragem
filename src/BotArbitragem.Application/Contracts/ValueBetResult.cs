namespace BotArbitragem.Application.Contracts;

public sealed record ValueBetResult(
    decimal EstimatedProbability,
    decimal MarketOdds,
    decimal ExpectedValue,
    bool HasPositiveValue);