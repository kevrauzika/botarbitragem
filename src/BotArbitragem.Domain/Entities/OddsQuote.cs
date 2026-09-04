namespace BotArbitragem.Domain.Entities;

public sealed class OddsQuote
{
    private OddsQuote() { }

    public OddsQuote(Guid matchId, string bookmaker, string market, string selection, decimal decimalOdds, DateTimeOffset capturedAt)
    {
        if (decimalOdds <= 1m)
            throw new ArgumentOutOfRangeException(nameof(decimalOdds), "A odd decimal deve ser maior que 1.");

        Id = Guid.NewGuid();
        MatchId = matchId;
        Bookmaker = bookmaker;
        Market = market;
        Selection = selection;
        DecimalOdds = decimalOdds;
        CapturedAt = capturedAt;
    }

    public Guid Id { get; private set; }
    public Guid MatchId { get; private set; }
    public string Bookmaker { get; private set; } = null!;
    public string Market { get; private set; } = null!;
    public string Selection { get; private set; } = null!;
    public decimal DecimalOdds { get; private set; }
    public DateTimeOffset CapturedAt { get; private set; }
}