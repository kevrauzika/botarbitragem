using BotArbitragem.Application.Abstractions;
using BotArbitragem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotArbitragem.Infrastructure.Persistence;

public sealed class MatchRepository(AppDbContext dbContext) : IMatchRepository
{
    public async Task<IReadOnlyList<FootballMatch>> ListAsync(DateTimeOffset? from, DateTimeOffset? to, CancellationToken cancellationToken)
    {
        var query = dbContext.Matches.AsNoTracking().Include(x => x.OddsQuotes).AsQueryable();
        if (from.HasValue) query = query.Where(x => x.KickoffAt >= from.Value);
        if (to.HasValue) query = query.Where(x => x.KickoffAt <= to.Value);
        return await query.OrderBy(x => x.KickoffAt).ToListAsync(cancellationToken);
    }

    public Task<FootballMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Matches.AsNoTracking().Include(x => x.OddsQuotes).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<FootballMatch?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken) =>
        dbContext.Matches.FirstOrDefaultAsync(x => x.ExternalId == externalId, cancellationToken);

    public async Task AddAsync(FootballMatch match, CancellationToken cancellationToken)
    {
        dbContext.Matches.Add(match);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> AddOddsIfNewAsync(OddsQuote quote, CancellationToken cancellationToken)
    {
        var exists = await dbContext.OddsQuotes.AnyAsync(x => x.MatchId == quote.MatchId &&
            x.Bookmaker == quote.Bookmaker && x.Market == quote.Market && x.Selection == quote.Selection &&
            x.CapturedAt == quote.CapturedAt, cancellationToken);
        if (exists) return false;

        dbContext.OddsQuotes.Add(quote);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
