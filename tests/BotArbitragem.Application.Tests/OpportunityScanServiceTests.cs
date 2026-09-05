using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Contracts;
using BotArbitragem.Application.Models;
using BotArbitragem.Application.Services;
using BotArbitragem.Domain.Entities;
using Microsoft.Extensions.Options;
using Xunit;

namespace BotArbitragem.Application.Tests;

public sealed class OpportunityScanServiceTests
{
    [Fact]
    public async Task ScanAsync_FindsArbitrageAndCalculatesBalancedStakes()
    {
        var now = DateTimeOffset.UtcNow;
        var match = new FootballMatch("event-1", "Liga", "Casa", "Fora", now.AddHours(3));
        AddBook(match, "Book A", 2.20m, 3.00m, 3.00m, now);
        AddBook(match, "Book B", 2.00m, 4.00m, 3.50m, now);
        var opportunities = new FakeOpportunityRepository();
        var service = CreateService(match, opportunities);

        var result = await service.ScanAsync(CancellationToken.None);

        var arbitrage = Assert.Single(opportunities.Candidates.Where(x => x.Kind == "arbitrage"));
        Assert.True(arbitrage.ProfitPercentage > 0.005m);
        Assert.InRange(arbitrage.Legs.Sum(x => x.StakePercentage), 99.999m, 100.001m);
        var payouts = arbitrage.Legs.Select(x => x.StakePercentage * x.DecimalOdds).ToArray();
        Assert.All(payouts, payout => Assert.InRange(payout, payouts[0] - 0.001m, payouts[0] + 0.001m));
        Assert.Equal(1, result.MatchesScanned);
    }

    [Fact]
    public async Task ScanAsync_FindsValueBetFromNoVigMarketConsensus()
    {
        var now = DateTimeOffset.UtcNow;
        var match = new FootballMatch("event-value", "Liga", "Casa", "Fora", now.AddHours(3));
        AddBook(match, "Book A", 2.00m, 3.20m, 3.80m, now);
        AddBook(match, "Book B", 2.00m, 3.20m, 3.80m, now);
        AddBook(match, "Book C", 2.00m, 3.20m, 3.80m, now);
        AddBook(match, "Book D", 2.30m, 2.90m, 3.40m, now);
        var opportunities = new FakeOpportunityRepository();
        var service = CreateService(match, opportunities);

        await service.ScanAsync(CancellationToken.None);

        var valueBet = Assert.Single(opportunities.Candidates.Where(x => x.Kind == "value-bet"));
        Assert.Equal("Casa", valueBet.Selection);
        Assert.Equal(2.30m, valueBet.MarketOdds);
        Assert.True(valueBet.ExpectedValue >= 0.05m);
    }

    [Fact]
    public async Task ScanAsync_IgnoresStaleOdds()
    {
        var now = DateTimeOffset.UtcNow;
        var match = new FootballMatch("event-2", "Liga", "Casa", "Fora", now.AddHours(3));
        AddBook(match, "Book A", 2.20m, 4.00m, 3.50m, now.AddHours(-1));
        var opportunities = new FakeOpportunityRepository();
        var service = CreateService(match, opportunities);

        var result = await service.ScanAsync(CancellationToken.None);

        Assert.Empty(opportunities.Candidates);
        Assert.Equal(0, result.QualifiedCandidates);
    }

    private static OpportunityScanService CreateService(FootballMatch match,
        FakeOpportunityRepository opportunities) => new(
        new FakeMatchRepository(match), opportunities, new FakeNotifier(),
        Options.Create(new ValueBetPolicy()), Options.Create(new OpportunityPolicy()));

    private static void AddBook(FootballMatch match, string bookmaker, decimal home, decimal draw, decimal away,
        DateTimeOffset capturedAt)
    {
        match.OddsQuotes.Add(new OddsQuote(match.Id, bookmaker, "h2h", match.HomeTeam, home, capturedAt));
        match.OddsQuotes.Add(new OddsQuote(match.Id, bookmaker, "h2h", "Draw", draw, capturedAt));
        match.OddsQuotes.Add(new OddsQuote(match.Id, bookmaker, "h2h", match.AwayTeam, away, capturedAt));
    }

    private sealed class FakeMatchRepository(FootballMatch match) : IMatchRepository
    {
        public Task<PagedResult<FootballMatch>> ListAsync(DateTimeOffset? from, DateTimeOffset? to,
            int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResult<FootballMatch>(page == 1 ? [match] : [], page, pageSize, 1));
        public Task<FootballMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<FootballMatch?>(match);
        public Task<FootballMatch?> GetByIdWithLatestOddsAsync(Guid id, int oddsLimit,
            CancellationToken cancellationToken) => Task.FromResult<FootballMatch?>(match);
        public Task<FootballMatch?> GetByIdWithOddsAsync(Guid id, DateTimeOffset oddsFrom, DateTimeOffset oddsTo,
            CancellationToken cancellationToken) => Task.FromResult<FootballMatch?>(match);
        public Task<FootballMatch?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken) =>
            Task.FromResult<FootballMatch?>(match);
        public Task<(FootballMatch Match, bool Created)> GetOrAddAsync(FootballMatch value,
            CancellationToken cancellationToken) => Task.FromResult((value, true));
        public Task<bool> AddOddsIfNewAsync(OddsQuote quote, CancellationToken cancellationToken) =>
            Task.FromResult(true);
    }

    private sealed class FakeOpportunityRepository : IOpportunityRepository
    {
        public List<OpportunityCandidate> Candidates { get; } = [];
        public Task<IReadOnlyList<OpportunityView>> ListAsync(string? status, string? kind,
            CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<OpportunityView>>([]);
        public Task<OpportunityView?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<OpportunityView?>(null);
        public Task<OpportunityUpsertResult> UpsertAsync(OpportunityCandidate candidate, DateTimeOffset now,
            TimeSpan alertCooldown, CancellationToken cancellationToken)
        {
            Candidates.Add(candidate);
            var entity = new Opportunity(candidate.MatchId, candidate.Fingerprint, candidate.Kind, candidate.Market, now);
            return Task.FromResult(new OpportunityUpsertResult(entity, true, false));
        }
        public Task<int> ExpireNotSeenAsync(IReadOnlySet<string> activeFingerprints, DateTimeOffset scanStartedAt,
            CancellationToken cancellationToken) => Task.FromResult(0);
        public Task MarkNotifiedAsync(Guid id, DateTimeOffset notifiedAt, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FakeNotifier : IOpportunityNotifier
    {
        public Task<bool> NotifyAsync(Opportunity opportunity, FootballMatch match,
            CancellationToken cancellationToken) => Task.FromResult(false);
    }
}
