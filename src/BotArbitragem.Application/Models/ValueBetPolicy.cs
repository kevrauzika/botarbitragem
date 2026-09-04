namespace BotArbitragem.Application.Models;

public sealed class ValueBetPolicy
{
    public decimal MinimumExpectedValue { get; init; } = 0.05m;
    public decimal MinimumEdge { get; init; } = 0.03m;
    public decimal MinimumEstimatedProbability { get; init; } = 0.20m;
    public decimal MinimumOdds { get; init; } = 1.40m;
    public decimal MaximumOdds { get; init; } = 5.00m;
}
