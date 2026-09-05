namespace BotArbitragem.Application.Models;

public sealed class MonitoringOptions
{
    public bool Enabled { get; init; }
    public int IntervalMinutes { get; init; } = 10;
    public int LookAheadHours { get; init; } = 48;
    public int MaximumMatchesPerCycle { get; init; } = 500;
    public string[] SportKeys { get; init; } = ["football"];
}

