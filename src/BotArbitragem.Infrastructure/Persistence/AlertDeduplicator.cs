using BotArbitragem.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BotArbitragem.Infrastructure.Persistence;

public sealed class AlertDeduplicator(AppDbContext dbContext) : IAlertDeduplicator
{
    public async Task<bool> TryReserveAsync(
        string fingerprint,
        Guid matchId,
        DateTimeOffset createdAt,
        CancellationToken cancellationToken)
    {
        var alert = new PublishedAlert
        {
            Id = Guid.NewGuid(),
            Fingerprint = fingerprint,
            MatchId = matchId,
            CreatedAt = createdAt
        };
        dbContext.PublishedAlerts.Add(alert);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            dbContext.Entry(alert).State = EntityState.Detached;
            return false;
        }
    }

    public Task ReleaseAsync(string fingerprint, CancellationToken cancellationToken) =>
        dbContext.PublishedAlerts
            .Where(alert => alert.Fingerprint == fingerprint)
            .ExecuteDeleteAsync(cancellationToken);
}

