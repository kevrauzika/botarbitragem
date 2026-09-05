namespace BotArbitragem.Infrastructure.Providers.GanheMaisBet;

public sealed class GanheMaisBetOptions
{
    public const string SectionName = "GanheMaisBet";
    public string BaseUrl { get; init; } = "https://ganhemaisbet.com";
    public string ApiKey { get; init; } = string.Empty;
    public int LookAheadHours { get; init; } = 24;
    public int MaximumMatchesPerRequest { get; init; } = 10;
}
