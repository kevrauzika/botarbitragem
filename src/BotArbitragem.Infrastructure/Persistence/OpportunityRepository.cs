using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Contracts;
using BotArbitragem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotArbitragem.Infrastructure.Persistence;

public sealed class OpportunityRepository(AppDbContext dbContext) : IOpportunityRepository
{
    public async Task<IReadOnlyList<OpportunityView>> ListAsync(string? status, string? kind,
        CancellationToken cancellationToken)
    {
        var query = dbContext.Opportunities.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(x => x.Status == status);
        if (!string.IsNullOrWhiteSpace(kind)) query = query.Where(x => x.Kind == kind);
        query = query.OrderByDescending(x => x.LastSeenAt).Take(500);
        return await Project(query).ToListAsync(cancellationToken);
    }

    public Task<OpportunityView?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        Project(dbContext.Opportunities.AsNoTracking().Where(x => x.Id == id))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<OpportunityUpsertResult> UpsertAsync(OpportunityCandidate candidate, DateTimeOffset now,
        TimeSpan alertCooldown, CancellationToken cancellationToken)
    {
        var opportunity = await dbContext.Opportunities.Include(x => x.Legs)
            .FirstOrDefaultAsync(x => x.Fingerprint == candidate.Fingerprint, cancellationToken);
        var created = opportunity is null;
        opportunity ??= new Opportunity(candidate.MatchId, candidate.Fingerprint, candidate.Kind, candidate.Market,
            now);

        if (!created)
        {
            dbContext.OpportunityLegs.RemoveRange(opportunity.Legs);
            opportunity.Legs.Clear();
        }

        var legs = candidate.Legs.Select(x => new OpportunityLeg(opportunity.Id, x.Bookmaker, x.Selection,
            x.DecimalOdds, x.StakePercentage));
        if (candidate.Kind == "arbitrage")
            opportunity.RefreshArbitrage(candidate.ProfitPercentage!.Value, now, candidate.ExpiresAt, legs);
        else
            opportunity.RefreshValueBet(candidate.Selection!, candidate.EstimatedProbability!.Value,
                candidate.MarketOdds!.Value, candidate.ExpectedValue!.Value, candidate.Edge!.Value, now,
                candidate.ExpiresAt, candidate.Legs[0].Bookmaker);

        if (created) dbContext.Opportunities.Add(opportunity);
        else
            foreach (var leg in opportunity.Legs) dbContext.Entry(leg).State = EntityState.Added;
        await dbContext.SaveChangesAsync(cancellationToken);
        var shouldNotify = opportunity.NotifiedAt is null || opportunity.NotifiedAt <= now.Subtract(alertCooldown);
        return new OpportunityUpsertResult(opportunity, created, shouldNotify);
    }

    public async Task<int> ExpireNotSeenAsync(IReadOnlySet<string> activeFingerprints, DateTimeOffset scanStartedAt,
        CancellationToken cancellationToken)
    {
        var active = await dbContext.Opportunities
            .Where(x => x.Status == "active" && x.LastSeenAt < scanStartedAt)
            .ToListAsync(cancellationToken);
        foreach (var opportunity in active.Where(x => !activeFingerprints.Contains(x.Fingerprint)))
            opportunity.Expire(scanStartedAt);
        return await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkNotifiedAsync(Guid id, DateTimeOffset notifiedAt, CancellationToken cancellationToken)
    {
        var opportunity = await dbContext.Opportunities.FirstAsync(x => x.Id == id, cancellationToken);
        opportunity.MarkNotified(notifiedAt);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private IQueryable<OpportunityView> Project(IQueryable<Opportunity> opportunities) =>
        from opportunity in opportunities
        join match in dbContext.Matches.AsNoTracking() on opportunity.MatchId equals match.Id
        select new OpportunityView(opportunity.Id, opportunity.MatchId, opportunity.Kind, opportunity.Market,
            opportunity.Selection, opportunity.Status, match.Competition, match.HomeTeam, match.AwayTeam,
            match.KickoffAt, opportunity.EstimatedProbability, opportunity.MarketOdds, opportunity.ExpectedValue,
            opportunity.Edge, opportunity.ProfitPercentage, opportunity.DetectedAt, opportunity.LastSeenAt,
            opportunity.ExpiresAt, opportunity.NotifiedAt,
            opportunity.Legs.Select(leg => new OpportunityLegView(leg.Bookmaker, leg.Selection, leg.DecimalOdds,
                leg.StakePercentage)).ToList());
}
