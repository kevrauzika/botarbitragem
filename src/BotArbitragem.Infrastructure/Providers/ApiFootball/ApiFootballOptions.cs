namespace BotArbitragem.Infrastructure.Providers.ApiFootball;

public sealed class ApiFootballOptions
{
    public const string SectionName = "ApiFootball";
    public string BaseUrl { get; init; } = "https://v3.football.api-sports.io";
    public string ApiKey { get; init; } = string.Empty;
    public int MaximumMatchesPerCycle { get; init; } = 8;
    public int[] MainLeagueIds { get; init; } = [2, 3, 11, 13, 39, 61, 71, 72, 73, 78, 94, 135, 140];
}
