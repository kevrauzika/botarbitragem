namespace BotArbitragem.Infrastructure.Providers.TheOddsApi;

public sealed class TheOddsApiOptions
{
    public const string SectionName = "OddsProvider";
    public string BaseUrl { get; init; } = "https://api.the-odds-api.com";
    public string ApiKey { get; init; } = string.Empty;
    public string Regions { get; init; } = "eu";
    public string Markets { get; init; } = "h2h";
    public string[] AllowedSports { get; init; } = [];
}
