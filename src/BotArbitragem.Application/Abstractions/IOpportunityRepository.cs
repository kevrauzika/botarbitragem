using BotArbitragem.Application.Contracts;
using BotArbitragem.Domain.Entities;

namespace BotArbitragem.Application.Abstractions;

public interface IOpportunityRepository
{
    Task<IReadOnlyList<OpportunityView>> ListAsync(string? status, string? kind, CancellationToken cancellationToken);
    Task<OpportunityView?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<OpportunityUpsertResult> UpsertAsync(OpportunityCandidate candidate, DateTimeOffset now,
        TimeSpan alertCooldown, CancellationToken cancellationToken);
    Task<int> ExpireNotSeenAsync(IReadOnlySet<string> activeFingerprints, DateTimeOffset scanStartedAt,
        CancellationToken cancellationToken);
    Task MarkNotifiedAsync(Guid id, DateTimeOffset notifiedAt, CancellationToken cancellationToken);
}
