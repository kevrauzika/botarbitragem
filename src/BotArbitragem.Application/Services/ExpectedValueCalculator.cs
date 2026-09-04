namespace BotArbitragem.Application.Services;

public static class ExpectedValueCalculator
{
    public static decimal Calculate(decimal estimatedProbability, decimal decimalOdds)
    {
        if (estimatedProbability is < 0m or > 1m)
            throw new ArgumentOutOfRangeException(nameof(estimatedProbability));
        if (decimalOdds <= 1m)
            throw new ArgumentOutOfRangeException(nameof(decimalOdds));

        return (estimatedProbability * decimalOdds) - 1m;
    }

    public static bool HasPositiveValue(decimal estimatedProbability, decimal decimalOdds) =>
        Calculate(estimatedProbability, decimalOdds) > 0m;
}