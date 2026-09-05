namespace BotArbitragem.Application.Abstractions;

public interface IAlertDeduplicator
{
    Task<bool> TryReserveAsync(string fingerprint, Guid matchId, DateTimeOffset createdAt, CancellationToken cancellationToken);
    Task ReleaseAsync(string fingerprint, CancellationToken cancellationToken);
}

