namespace BotArbitragem.Domain.Entities;

public sealed class BetRecord
{
    private BetRecord() { }

    public BetRecord(Guid opportunityId, Guid matchId, string mode, string currency, decimal stake,
        decimal potentialReturn, DateTimeOffset placedAt, string? notes)
    {
        if (stake <= 0m) throw new ArgumentOutOfRangeException(nameof(stake));
        if (mode is not ("paper" or "actual")) throw new ArgumentException("Modo deve ser paper ou actual.", nameof(mode));
        Id = Guid.NewGuid();
        OpportunityId = opportunityId;
        MatchId = matchId;
        Mode = mode;
        Currency = currency.ToUpperInvariant();
        Stake = stake;
        PotentialReturn = potentialReturn;
        Status = "pending";
        PlacedAt = placedAt;
        Notes = notes;
    }

    public Guid Id { get; private set; }
    public Guid OpportunityId { get; private set; }
    public Guid MatchId { get; private set; }
    public string Mode { get; private set; } = null!;
    public string Currency { get; private set; } = null!;
    public decimal Stake { get; private set; }
    public decimal PotentialReturn { get; private set; }
    public string Status { get; private set; } = null!;
    public decimal? ReturnAmount { get; private set; }
    public decimal? ProfitLoss { get; private set; }
    public DateTimeOffset PlacedAt { get; private set; }
    public DateTimeOffset? SettledAt { get; private set; }
    public string? Notes { get; private set; }
    public ICollection<BetLeg> Legs { get; } = new List<BetLeg>();

    public void AddLeg(string bookmaker, string selection, decimal odds, decimal stakeAmount) =>
        Legs.Add(new BetLeg(Id, bookmaker, selection, odds, stakeAmount));

    public void Settle(decimal returnAmount, DateTimeOffset settledAt, string? notes)
    {
        if (Status != "pending") throw new InvalidOperationException("Registro já foi finalizado.");
        if (returnAmount < 0m) throw new ArgumentOutOfRangeException(nameof(returnAmount));
        ReturnAmount = returnAmount;
        ProfitLoss = returnAmount - Stake;
        Status = "settled";
        SettledAt = settledAt;
        if (!string.IsNullOrWhiteSpace(notes)) Notes = notes;
    }

    public void Void(DateTimeOffset settledAt, string? notes)
    {
        if (Status != "pending") throw new InvalidOperationException("Registro já foi finalizado.");
        ReturnAmount = Stake;
        ProfitLoss = 0m;
        Status = "void";
        SettledAt = settledAt;
        if (!string.IsNullOrWhiteSpace(notes)) Notes = notes;
    }
}
