using BotArbitragem.Application.Contracts;
using BotArbitragem.Domain.Entities;

namespace BotArbitragem.Application.Abstractions;

public interface IMatchRepository
{
    Task<PagedResult<FootballMatch>> ListAsync(DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken cancellationToken);
    Task<FootballMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task<FootballMatch?> GetByIdWithLatestOddsAsync(Guid id, int oddsLimit, CancellationToken cancellationToken);
    Task<FootballMatch?> GetByIdWithOddsAsync(Guid id, DateTimeOffset oddsFrom, DateTimeOffset oddsTo, CancellationToken cancellationToken);
    Task<FootballMatch?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken);
    Task<(FootballMatch Match, bool Created)> GetOrAddAsync(FootballMatch match, CancellationToken cancellationToken);
    Task<bool> AddOddsIfNewAsync(OddsQuote quote, CancellationToken cancellationToken);
}
