namespace BotArbitragem.Domain.Entities;

public sealed class Opportunity
{
    private Opportunity() { }

    public Opportunity(Guid matchId, string fingerprint, string kind, string market, DateTimeOffset detectedAt)
    {
        Id = Guid.NewGuid();
        MatchId = matchId;
        Fingerprint = fingerprint;
        Kind = kind;
        Market = market;
        Status = "active";
        DetectedAt = detectedAt;
        LastSeenAt = detectedAt;
    }

    public Guid Id { get; private set; }
    public Guid MatchId { get; private set; }
    public string Fingerprint { get; private set; } = null!;
    public string Kind { get; private set; } = null!;
    public string Market { get; private set; } = null!;
    public string? Selection { get; private set; }
    public string Status { get; private set; } = null!;
    public decimal? EstimatedProbability { get; private set; }
    public decimal? MarketOdds { get; private set; }
    public decimal? ExpectedValue { get; private set; }
    public decimal? Edge { get; private set; }
    public decimal? ProfitPercentage { get; private set; }
    public DateTimeOffset DetectedAt { get; private set; }
    public DateTimeOffset LastSeenAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }
    public DateTimeOffset? NotifiedAt { get; private set; }
    public ICollection<OpportunityLeg> Legs { get; } = new List<OpportunityLeg>();

    public void RefreshValueBet(string selection, decimal estimatedProbability, decimal marketOdds,
        decimal expectedValue, decimal edge, DateTimeOffset seenAt, DateTimeOffset expiresAt,
        string bookmaker)
    {
        Selection = selection;
        EstimatedProbability = estimatedProbability;
        MarketOdds = marketOdds;
        ExpectedValue = expectedValue;
        Edge = edge;
        ProfitPercentage = null;
        Refresh(seenAt, expiresAt);
        ReplaceLegs([new OpportunityLeg(Id, bookmaker, selection, marketOdds, 100m)]);
    }

    public void RefreshArbitrage(decimal profitPercentage, DateTimeOffset seenAt, DateTimeOffset expiresAt,
        IEnumerable<OpportunityLeg> legs)
    {
        Selection = null;
        EstimatedProbability = null;
        MarketOdds = null;
        ExpectedValue = null;
        Edge = null;
        ProfitPercentage = profitPercentage;
        Refresh(seenAt, expiresAt);
        ReplaceLegs(legs);
    }

    public void Expire(DateTimeOffset at)
    {
        Status = "expired";
        ExpiresAt = at;
    }

    public void MarkNotified(DateTimeOffset at) => NotifiedAt = at;

    private void Refresh(DateTimeOffset seenAt, DateTimeOffset expiresAt)
    {
        Status = "active";
        LastSeenAt = seenAt;
        ExpiresAt = expiresAt;
    }

    private void ReplaceLegs(IEnumerable<OpportunityLeg> legs)
    {
        Legs.Clear();
        foreach (var leg in legs) Legs.Add(leg);
    }
}
