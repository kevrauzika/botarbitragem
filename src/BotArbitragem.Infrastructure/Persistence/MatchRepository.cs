using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Contracts;
using BotArbitragem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace BotArbitragem.Infrastructure.Persistence;

public sealed class MatchRepository(AppDbContext dbContext) : IMatchRepository
{
    public async Task<PagedResult<FootballMatch>> ListAsync(
        DateTimeOffset? from,
        DateTimeOffset? to,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Matches.AsNoTracking().AsQueryable();
        if (from.HasValue) query = query.Where(x => x.KickoffAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.KickoffAt <= to.Value);
        var totalItems = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(x => x.KickoffAt)
            .ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return new PagedResult<FootballMatch>(items, page, pageSize, totalItems);
    }

    public Task<FootballMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Matches.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<FootballMatch?> GetByIdWithLatestOddsAsync(Guid id, int oddsLimit, CancellationToken cancellationToken) =>
        dbContext.Matches
            .AsNoTracking()
            .Include(match => match.OddsQuotes.OrderByDescending(quote => quote.CapturedAt).Take(oddsLimit))
            .FirstOrDefaultAsync(match => match.Id == id, cancellationToken);

    public Task<FootballMatch?> GetByIdWithOddsAsync(
        Guid id,
        DateTimeOffset oddsFrom,
        DateTimeOffset oddsTo,
        CancellationToken cancellationToken) =>
        dbContext.Matches
            .AsNoTracking()
            .Include(match => match.OddsQuotes.Where(quote => quote.CapturedAt >= oddsFrom && quote.CapturedAt <= oddsTo))
            .FirstOrDefaultAsync(match => match.Id == id, cancellationToken);

    public Task<FootballMatch?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken) =>
        dbContext.Matches.FirstOrDefaultAsync(x => x.ExternalId == externalId, cancellationToken);

    public async Task<(FootballMatch Match, bool Created)> GetOrAddAsync(FootballMatch match, CancellationToken cancellationToken)
    {
        var existing = await GetByExternalIdAsync(match.ExternalId, cancellationToken);
        if (existing is not null) return (existing, false);

        dbContext.Matches.Add(match);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return (match, true);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            dbContext.Entry(match).State = EntityState.Detached;
            existing = await GetByExternalIdAsync(match.ExternalId, cancellationToken);
            if (existing is null) throw;
            return (existing, false);
        }
    }

    public async Task<bool> AddOddsIfNewAsync(OddsQuote quote, CancellationToken cancellationToken)
    {
        dbContext.OddsQuotes.Add(quote);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            dbContext.Entry(quote).State = EntityState.Detached;
            return false;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}
