namespace BotArbitragem.Domain.Entities;

public sealed class BetLeg
{
    private BetLeg() { }

    public BetLeg(Guid betRecordId, string bookmaker, string selection, decimal decimalOdds, decimal stakeAmount)
    {
        Id = Guid.NewGuid();
        BetRecordId = betRecordId;
        Bookmaker = bookmaker;
        Selection = selection;
        DecimalOdds = decimalOdds;
        StakeAmount = stakeAmount;
    }

    public Guid Id { get; private set; }
    public Guid BetRecordId { get; private set; }
    public string Bookmaker { get; private set; } = null!;
    public string Selection { get; private set; } = null!;
    public decimal DecimalOdds { get; private set; }
    public decimal StakeAmount { get; private set; }
}
