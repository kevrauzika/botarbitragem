using BotArbitragem.Domain.Entities;
using Xunit;

namespace BotArbitragem.Application.Tests;

public sealed class DomainEntityTests
{
    [Fact]
    public void FootballMatch_NormalizesTextAndRejectsSameTeams()
    {
        var match = new FootballMatch(" event-1 ", " Série A ", " Time A ", " Time B ", DateTimeOffset.UtcNow);

        Assert.Equal("event-1", match.ExternalId);
        Assert.Equal("Série A", match.Competition);
        Assert.Throws<ArgumentException>(() =>
            new FootballMatch("event-2", "Série A", "Time A", " time a ", DateTimeOffset.UtcNow));
    }

    [Fact]
    public void FootballMatch_RejectsTextBeyondPersistenceLimit() =>
        Assert.Throws<ArgumentException>(() =>
            new FootballMatch(new string('a', 101), "Série A", "Time A", "Time B", DateTimeOffset.UtcNow));

    [Fact]
    public void OddsQuote_RejectsMissingIdentifiersAndText()
    {
        Assert.Throws<ArgumentException>(() =>
            new OddsQuote(Guid.Empty, "Book", "h2h", "Time A", 2m, DateTimeOffset.UtcNow));
        Assert.Throws<ArgumentException>(() =>
            new OddsQuote(Guid.NewGuid(), " ", "h2h", "Time A", 2m, DateTimeOffset.UtcNow));
    }
}

