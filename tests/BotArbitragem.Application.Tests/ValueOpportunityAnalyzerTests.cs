using BotArbitragem.Application.Models;
using BotArbitragem.Application.Services;
using BotArbitragem.Domain.Entities;
using Xunit;

namespace BotArbitragem.Application.Tests;

public sealed class ValueOpportunityAnalyzerTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-09-04T12:00:00Z");

    [Fact]
    public void Analyze_QualifiesOdd_UsingOtherBookmakersAsReference()
    {
        var match = CreateMatch();
        AddMarket(match, "Book A", 2.00m, 3.50m, 4.00m, Now.AddMinutes(-5));
        AddMarket(match, "Book B", 2.05m, 3.45m, 3.90m, Now.AddMinutes(-4));
        AddMarket(match, "Book C", 2.25m, 3.30m, 3.40m, Now.AddMinutes(-3));

        var result = ValueOpportunityAnalyzer.Analyze(match, new ValueBetPolicy(), new OpportunityAnalysisPolicy(), Now);

        var opportunity = Assert.Single(result.Opportunities,
            item => item.Bookmaker == "Book C" && item.Selection == match.HomeTeam);
        Assert.Equal(2, opportunity.ReferenceBookmakers);
        Assert.True(opportunity.ExpectedValue >= 0.05m);
        Assert.Equal(3, result.EligibleBookmakers);
        Assert.Equal(9, result.CandidatesEvaluated);
    }

    [Fact]
    public void Analyze_DoesNotEvaluate_WhenThereAreTooFewIndependentReferences()
    {
        var match = CreateMatch();
        AddMarket(match, "Book A", 2.00m, 3.50m, 4.00m, Now.AddMinutes(-5));
        AddMarket(match, "Book B", 2.20m, 3.30m, 3.60m, Now.AddMinutes(-4));

        var result = ValueOpportunityAnalyzer.Analyze(match, new ValueBetPolicy(), new OpportunityAnalysisPolicy(), Now);

        Assert.Empty(result.Opportunities);
        Assert.Equal(0, result.CandidatesEvaluated);
    }

    [Fact]
    public void Analyze_IgnoresStaleAndIncompleteMarkets()
    {
        var match = CreateMatch();
        AddMarket(match, "Stale Book", 2.00m, 3.50m, 4.00m, Now.AddMinutes(-31));
        match.OddsQuotes.Add(new OddsQuote(match.Id, "Incomplete Book", "h2h", match.HomeTeam, 2.10m, Now));

        var result = ValueOpportunityAnalyzer.Analyze(match, new ValueBetPolicy(), new OpportunityAnalysisPolicy(), Now);

        Assert.Equal(0, result.EligibleBookmakers);
        Assert.Empty(result.Opportunities);
    }

    private static FootballMatch CreateMatch() =>
        new("event-1", "Brasileirão", "Time A", "Time B", Now.AddDays(1));

    private static void AddMarket(
        FootballMatch match,
        string bookmaker,
        decimal homeOdds,
        decimal drawOdds,
        decimal awayOdds,
        DateTimeOffset capturedAt)
    {
        match.OddsQuotes.Add(new OddsQuote(match.Id, bookmaker, "h2h", match.HomeTeam, homeOdds, capturedAt));
        match.OddsQuotes.Add(new OddsQuote(match.Id, bookmaker, "h2h", "Draw", drawOdds, capturedAt));
        match.OddsQuotes.Add(new OddsQuote(match.Id, bookmaker, "h2h", match.AwayTeam, awayOdds, capturedAt));
    }
}

