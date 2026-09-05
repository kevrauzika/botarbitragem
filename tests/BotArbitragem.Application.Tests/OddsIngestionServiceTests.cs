using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Contracts;
using BotArbitragem.Application.Services;
using BotArbitragem.Domain.Entities;
using Xunit;

namespace BotArbitragem.Application.Tests;

public sealed class OddsIngestionServiceTests
{
    [Fact]
    public async Task ImportAsync_CreatesMatchAndSkipsRepeatedOdds()
    {
        var capturedAt = DateTimeOffset.Parse("2026-09-04T12:00:00Z");
        var provider = new FakeProvider([
            new ProviderEvent("event-1", "soccer_brazil_campeonato", "Brasileirão Série A", "Time A", "Time B",
                capturedAt.AddDays(1), [new ProviderOddsQuote("Book A", "h2h", "Time A", 2.10m, capturedAt)])
        ]);
        var repository = new FakeRepository();
        var service = new OddsIngestionService(provider, repository);

        var first = await service.ImportAsync("soccer_brazil_campeonato", CancellationToken.None);
        var second = await service.ImportAsync("soccer_brazil_campeonato", CancellationToken.None);

        Assert.Equal(1, first.MatchesCreated);
        Assert.Equal(1, first.OddsCreated);
        Assert.Equal(0, second.MatchesCreated);
        Assert.Equal(1, second.OddsSkipped);
    }

    private sealed class FakeProvider(IReadOnlyList<ProviderEvent> events) : IOddsProvider
    {
        public Task<IReadOnlyList<ProviderEvent>> GetUpcomingOddsAsync(string sportKey, CancellationToken cancellationToken) =>
            Task.FromResult(events);
    }

    private sealed class FakeRepository : IMatchRepository
    {
        private readonly List<FootballMatch> _matches = [];
        private readonly HashSet<string> _odds = [];

        public Task<PagedResult<FootballMatch>> ListAsync(DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResult<FootballMatch>(_matches, page, pageSize, _matches.Count));
        public Task<FootballMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(_matches.FirstOrDefault(x => x.Id == id));
        public Task<FootballMatch?> GetByIdWithLatestOddsAsync(Guid id, int oddsLimit, CancellationToken cancellationToken) =>
            GetByIdAsync(id, cancellationToken);
        public Task<FootballMatch?> GetByIdWithOddsAsync(Guid id, DateTimeOffset oddsFrom, DateTimeOffset oddsTo, CancellationToken cancellationToken) =>
            GetByIdAsync(id, cancellationToken);
        public Task<FootballMatch?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken) =>
            Task.FromResult(_matches.FirstOrDefault(x => x.ExternalId == externalId));
        public Task<(FootballMatch Match, bool Created)> GetOrAddAsync(FootballMatch match, CancellationToken cancellationToken)
        {
            var existing = _matches.FirstOrDefault(x => x.ExternalId == match.ExternalId);
            if (existing is not null) return Task.FromResult((existing, false));
            _matches.Add(match);
            return Task.FromResult((match, true));
        }
        public Task<bool> AddOddsIfNewAsync(OddsQuote quote, CancellationToken cancellationToken)
        {
            var key = $"{quote.MatchId}|{quote.Bookmaker}|{quote.Market}|{quote.Selection}|{quote.CapturedAt:O}";
            return Task.FromResult(_odds.Add(key));
        }
    }
}
