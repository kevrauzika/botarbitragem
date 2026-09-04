namespace BotArbitragem.Domain.Entities;

public sealed class FootballMatch
{
    private FootballMatch() { }

    public FootballMatch(string externalId, string competition, string homeTeam, string awayTeam, DateTimeOffset kickoffAt)
    {
        Id = Guid.NewGuid();
        ExternalId = externalId;
        Competition = competition;
        HomeTeam = homeTeam;
        AwayTeam = awayTeam;
        KickoffAt = kickoffAt;
    }

    public Guid Id { get; private set; }
    public string ExternalId { get; private set; } = null!;
    public string Competition { get; private set; } = null!;
    public string HomeTeam { get; private set; } = null!;
    public string AwayTeam { get; private set; } = null!;
    public DateTimeOffset KickoffAt { get; private set; }
    public ICollection<OddsQuote> OddsQuotes { get; } = new List<OddsQuote>();
}