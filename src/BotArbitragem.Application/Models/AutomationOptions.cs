namespace BotArbitragem.Application.Models;

public sealed class AutomationOptions
{
    public bool IngestionEnabled { get; init; }
    public bool ScanEnabled { get; init; } = true;
    public int IntervalSeconds { get; init; } = 300;
}
