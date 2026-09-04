namespace BotArbitragem.Application.Models;

public sealed class OpportunityPolicy
{
    public int LookAheadHours { get; init; } = 48;
    public int MaximumOddsAgeMinutes { get; init; } = 15;
    public decimal MinimumArbitrageProfit { get; init; } = 0.005m;
    public int AlertCooldownMinutes { get; init; } = 30;
}
