namespace BotArbitragem.Infrastructure.Persistence;

public sealed class PublishedAlert
{
    public Guid Id { get; init; }
    public string Fingerprint { get; init; } = null!;
    public Guid MatchId { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}

