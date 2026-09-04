using BotArbitragem.Application.Services;
using Xunit;

namespace BotArbitragem.Application.Tests;

public sealed class NoVigProbabilityCalculatorTests
{
    [Fact]
    public void CalculateThreeWay_RemovesMarginAndNormalizesProbabilities()
    {
        var result = NoVigProbabilityCalculator.CalculateThreeWay(2.00m, 3.50m, 4.00m);
        var sum = result.HomeProbability + result.DrawProbability + result.AwayProbability;
        Assert.InRange(sum, 0.999999m, 1.000001m);
        Assert.True(result.MarketOverround > 0m);
    }

    [Fact]
    public void CalculateThreeWay_RejectsInvalidOdds() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => NoVigProbabilityCalculator.CalculateThreeWay(1m, 3m, 4m));
}
