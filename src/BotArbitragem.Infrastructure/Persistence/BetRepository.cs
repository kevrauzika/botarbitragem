using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Contracts;
using BotArbitragem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotArbitragem.Infrastructure.Persistence;

public sealed class BetRepository(AppDbContext dbContext) : IBetRepository
{
    public async Task AddAsync(BetRecord record, CancellationToken cancellationToken)
    {
        dbContext.BetRecords.Add(record);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task<BetRecord?> GetTrackedAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.BetRecords.Include(x => x.Legs).FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken) => dbContext.SaveChangesAsync(cancellationToken);

    public async Task<IReadOnlyList<BetView>> ListAsync(string? status, string? mode,
        CancellationToken cancellationToken)
    {
        var query = dbContext.BetRecords.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(mode)) query = query.Where(x => x.Mode == mode);
        query = query.OrderByDescending(x => x.PlacedAt).Take(500);
        return await Project(query).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PerformanceSummary>> GetPerformanceAsync(CancellationToken cancellationToken)
    {
        var records = await dbContext.BetRecords.AsNoTracking().ToListAsync(cancellationToken);
        return records.GroupBy(x => x.Currency).Select(group =>
        {
            var settled = group.Where(x => x.Status is "settled" or "void").ToList();
            var settledStake = settled.Sum(x => x.Stake);
            var totalReturn = settled.Sum(x => x.ReturnAmount ?? 0m);
            var profitLoss = settled.Sum(x => x.ProfitLoss ?? 0m);
            return new PerformanceSummary(group.Key, group.Count(), group.Count(x => x.Status == "pending"),
                settled.Count, group.Sum(x => x.Stake), group.Where(x => x.Status == "pending").Sum(x => x.Stake),
                settledStake, totalReturn, profitLoss,
                settledStake == 0m ? null : profitLoss / settledStake);
        }).OrderBy(x => x.Currency).ToList();
    }

    private IQueryable<BetView> Project(IQueryable<BetRecord> records) =>
        from record in records
        join match in dbContext.Matches.AsNoTracking() on record.MatchId equals match.Id
        select new BetView(record.Id, record.OpportunityId, record.MatchId, record.Mode, record.Currency,
            record.Stake, record.PotentialReturn, record.Status, record.ReturnAmount, record.ProfitLoss,
            record.PlacedAt, record.SettledAt, record.Notes, match.Competition, match.HomeTeam, match.AwayTeam,
            match.KickoffAt, record.Legs.Select(leg => new BetLegView(leg.Bookmaker, leg.Selection,
                leg.DecimalOdds, leg.StakeAmount)).ToList());
}
