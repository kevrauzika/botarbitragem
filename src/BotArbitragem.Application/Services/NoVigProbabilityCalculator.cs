using BotArbitragem.Application.Contracts;

namespace BotArbitragem.Application.Services;

public static class NoVigProbabilityCalculator
{
    public static NoVigResult CalculateThreeWay(decimal homeOdds, decimal drawOdds, decimal awayOdds)
    {
        Validate(homeOdds, nameof(homeOdds));
        Validate(drawOdds, nameof(drawOdds));
        Validate(awayOdds, nameof(awayOdds));
        var home = 1m / homeOdds;
        var draw = 1m / drawOdds;
        var away = 1m / awayOdds;
        var total = home + draw + away;
        return new NoVigResult(home / total, draw / total, away / total, total - 1m);
    }

    private static void Validate(decimal odds, string name)
    {
        if (odds <= 1m) throw new ArgumentOutOfRangeException(name, "A odd deve ser maior que 1.");
    }
}
