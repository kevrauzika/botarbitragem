namespace BotArbitragem.Domain.Entities;

public sealed class OpportunityLeg
{
    private OpportunityLeg() { }

    public OpportunityLeg(Guid opportunityId, string bookmaker, string selection, decimal decimalOdds,
        decimal stakePercentage)
    {
        Id = Guid.NewGuid();
        OpportunityId = opportunityId;
        Bookmaker = bookmaker;
        Selection = selection;
        DecimalOdds = decimalOdds;
        StakePercentage = stakePercentage;
    }

    public Guid Id { get; private set; }
    public Guid OpportunityId { get; private set; }
    public string Bookmaker { get; private set; } = null!;
    public string Selection { get; private set; } = null!;
    public decimal DecimalOdds { get; private set; }
    public decimal StakePercentage { get; private set; }
}
