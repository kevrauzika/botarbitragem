using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Contracts;
using BotArbitragem.Application.Exceptions;
using BotArbitragem.Application.Models;
using BotArbitragem.Application.Services;
using BotArbitragem.Domain.Entities;
using Xunit;

namespace BotArbitragem.Application.Tests;

public sealed class OpportunityPublisherTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-04T12:00:00Z");

    [Fact]
    public async Task PublishMatchAsync_SendsEachOpportunityOnlyOnce()
    {
        var match = CreateMatchWithOpportunity();
        var notifier = new FakeNotifier();
        var publisher = CreatePublisher(match, new FakeDeduplicator(), notifier);

        var first = await publisher.PublishMatchAsync(match.Id, CancellationToken.None);
        var second = await publisher.PublishMatchAsync(match.Id, CancellationToken.None);

        Assert.NotNull(first);
        Assert.True(first.MessagesSent > 0);
        Assert.Equal(first.MessagesSent, notifier.Messages.Count);
        Assert.Equal(0, second!.MessagesSent);
        Assert.Equal(first.OpportunitiesFound, second.DuplicatesSkipped);
        Assert.Contains(notifier.Messages, message => message.Contains("Time A x Time B"));
    }

    [Fact]
    public async Task PublishMatchAsync_ReleasesReservation_WhenNotificationFails()
    {
        var match = CreateMatchWithOpportunity();
        var deduplicator = new FakeDeduplicator();
        var failingPublisher = CreatePublisher(match, deduplicator, new FakeNotifier { ShouldFail = true });

        await Assert.ThrowsAsync<GroupNotificationException>(() =>
            failingPublisher.PublishMatchAsync(match.Id, CancellationToken.None));

        var notifier = new FakeNotifier();
        var retryPublisher = CreatePublisher(match, deduplicator, notifier);
        var retry = await retryPublisher.PublishMatchAsync(match.Id, CancellationToken.None);
        Assert.True(retry!.MessagesSent > 0);
    }

    private static OpportunityPublisher CreatePublisher(
        FootballMatch match,
        IAlertDeduplicator deduplicator,
        IGroupNotifier notifier) =>
        new(new FakeRepository(match), deduplicator, notifier, new ValueBetPolicy(),
            new OpportunityAnalysisPolicy(), new FixedTimeProvider(Now));

    private static FootballMatch CreateMatchWithOpportunity()
    {
        var match = new FootballMatch("event-1", "Brasileirão", "Time A", "Time B", Now.AddDays(1));
        AddMarket(match, "Book A", 2.00m, 3.50m, 4.00m, Now.AddMinutes(-5));
        AddMarket(match, "Book B", 2.05m, 3.45m, 3.90m, Now.AddMinutes(-4));
        AddMarket(match, "Book C", 2.25m, 3.30m, 3.40m, Now.AddMinutes(-3));
        return match;
    }

    private static void AddMarket(FootballMatch match, string bookmaker, decimal home, decimal draw, decimal away, DateTimeOffset capturedAt)
    {
        match.OddsQuotes.Add(new OddsQuote(match.Id, bookmaker, "h2h", match.HomeTeam, home, capturedAt));
        match.OddsQuotes.Add(new OddsQuote(match.Id, bookmaker, "h2h", "Draw", draw, capturedAt));
        match.OddsQuotes.Add(new OddsQuote(match.Id, bookmaker, "h2h", match.AwayTeam, away, capturedAt));
    }

    private sealed class FakeRepository(FootballMatch match) : IMatchRepository
    {
        public Task<FootballMatch?> GetByIdWithOddsAsync(Guid id, DateTimeOffset oddsFrom, DateTimeOffset oddsTo, CancellationToken cancellationToken) =>
            Task.FromResult<FootballMatch?>(id == match.Id ? match : null);
        public Task<FootballMatch?> GetByIdAsync(Guid id, CancellationToken cancellationToken) => Task.FromResult<FootballMatch?>(match);
        public Task<FootballMatch?> GetByIdWithLatestOddsAsync(Guid id, int oddsLimit, CancellationToken cancellationToken) => GetByIdAsync(id, cancellationToken);
        public Task<FootballMatch?> GetByExternalIdAsync(string externalId, CancellationToken cancellationToken) => Task.FromResult<FootballMatch?>(match);
        public Task<(FootballMatch Match, bool Created)> GetOrAddAsync(FootballMatch value, CancellationToken cancellationToken) => Task.FromResult((match, false));
        public Task<bool> AddOddsIfNewAsync(OddsQuote quote, CancellationToken cancellationToken) => Task.FromResult(true);
        public Task<PagedResult<FootballMatch>> ListAsync(DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize, CancellationToken cancellationToken) =>
            Task.FromResult(new PagedResult<FootballMatch>([match], page, pageSize, 1));
    }

    private sealed class FakeDeduplicator : IAlertDeduplicator
    {
        private readonly HashSet<string> _fingerprints = [];
        public Task<bool> TryReserveAsync(string fingerprint, Guid matchId, DateTimeOffset createdAt, CancellationToken cancellationToken) =>
            Task.FromResult(_fingerprints.Add(fingerprint));
        public Task ReleaseAsync(string fingerprint, CancellationToken cancellationToken)
        {
            _fingerprints.Remove(fingerprint);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeNotifier : IGroupNotifier
    {
        public bool ShouldFail { get; init; }
        public List<string> Messages { get; } = [];
        public Task SendAsync(string message, CancellationToken cancellationToken)
        {
            if (ShouldFail) throw new GroupNotificationException("Falha simulada.");
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}

