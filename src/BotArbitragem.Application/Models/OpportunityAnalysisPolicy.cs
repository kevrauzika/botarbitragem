namespace BotArbitragem.Application.Models;

public sealed class OpportunityAnalysisPolicy
{
    public int MinimumReferenceBookmakers { get; init; } = 2;
    public int MaximumQuoteAgeMinutes { get; init; } = 30;
    public int MaximumFutureSkewMinutes { get; init; } = 5;
}

