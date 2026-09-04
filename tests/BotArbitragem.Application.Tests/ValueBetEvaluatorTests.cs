using BotArbitragem.Application.Models;
using BotArbitragem.Application.Services;
using Xunit;

namespace BotArbitragem.Application.Tests;

public sealed class ValueBetEvaluatorTests
{
    private static readonly ValueBetPolicy Policy = new();

    [Fact]
    public void Evaluate_QualifiesCandidate_WhenAllFiltersPass()
    {
        var result = ValueBetEvaluator.Evaluate(0.60m, 2.00m, Policy);
        Assert.True(result.IsQualified);
        Assert.Empty(result.RejectionReasons);
    }

    [Fact]
    public void Evaluate_RejectsCandidate_WhenValueIsInsufficient()
    {
        var result = ValueBetEvaluator.Evaluate(0.51m, 2.00m, Policy);
        Assert.False(result.IsQualified);
        Assert.Contains(result.RejectionReasons, reason => reason.Contains("EV"));
    }

    [Fact]
    public void Evaluate_RejectsCandidate_WhenOddsExceedRiskLimit()
    {
        var result = ValueBetEvaluator.Evaluate(0.30m, 6.00m, Policy);
        Assert.False(result.IsQualified);
        Assert.Contains(result.RejectionReasons, reason => reason.Contains("faixa"));
    }
}
