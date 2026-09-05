namespace BotArbitragem.Domain.Entities;

public sealed class FootballMatch
{
    private FootballMatch() { }

    public FootballMatch(string externalId, string competition, string homeTeam, string awayTeam, DateTimeOffset kickoffAt)
    {
        Id = Guid.NewGuid();
        ExternalId = Guard.RequiredText(externalId, 100, nameof(externalId));
        Competition = Guard.RequiredText(competition, 150, nameof(competition));
        HomeTeam = Guard.RequiredText(homeTeam, 150, nameof(homeTeam));
        AwayTeam = Guard.RequiredText(awayTeam, 150, nameof(awayTeam));
        if (string.Equals(HomeTeam, AwayTeam, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Os times mandante e visitante devem ser diferentes.", nameof(awayTeam));
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
