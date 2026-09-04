using BotArbitragem.Application.Contracts;
using BotArbitragem.Domain.Entities;

namespace BotArbitragem.Application.Abstractions;

public interface IBetRepository
{
    Task AddAsync(BetRecord record, CancellationToken cancellationToken);
    Task<BetRecord?> GetTrackedAsync(Guid id, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<BetView>> ListAsync(string? status, string? mode, CancellationToken cancellationToken);
    Task<IReadOnlyList<PerformanceSummary>> GetPerformanceAsync(CancellationToken cancellationToken);
}
