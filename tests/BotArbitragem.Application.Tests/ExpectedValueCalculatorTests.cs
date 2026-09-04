using BotArbitragem.Application.Services;
using Xunit;

namespace BotArbitragem.Application.Tests;

public sealed class ExpectedValueCalculatorTests
{
    [Fact]
    public void Calculate_ReturnsPositiveValue_WhenEstimatedProbabilityExceedsBreakEven()
    {
        var result = ExpectedValueCalculator.Calculate(0.60m, 2.00m);
        Assert.Equal(0.20m, result);
        Assert.True(ExpectedValueCalculator.HasPositiveValue(0.60m, 2.00m));
    }

    [Fact]
    public void Calculate_ReturnsNegativeValue_WhenPriceIsNotGoodEnough() =>
        Assert.Equal(-0.10m, ExpectedValueCalculator.Calculate(0.45m, 2.00m));

    [Fact]
    public void CalculateImpliedProbability_UsesDecimalOdds() =>
        Assert.Equal(0.50m, ExpectedValueCalculator.CalculateImpliedProbability(2.00m));

    [Theory]
    [InlineData(-0.01, 2.0)]
    [InlineData(1.01, 2.0)]
    [InlineData(0.50, 1.0)]
    public void Calculate_RejectsInvalidInput(decimal probability, decimal odds) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ExpectedValueCalculator.Calculate(probability, odds));
}
