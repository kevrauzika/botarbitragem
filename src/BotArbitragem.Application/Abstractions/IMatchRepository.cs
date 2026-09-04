using BotArbitragem.Domain.Entities;

namespace BotArbitragem.Application.Abstractions;

public interface IMatchRepository
{
    Task<IReadOnlyList<FootballMatch>> ListAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken);
    Task<FootballMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<FootballMatch?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken);
    Task AddAsync(FootballMatch match, CancellationToken cancellationToken);
    Task<bool> AddOddsIfNewAsync(OddsQuote quote, CancellationToken cancellationToken);
}
