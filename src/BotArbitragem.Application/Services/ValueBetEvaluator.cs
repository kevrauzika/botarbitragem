using BotArbitragem.Application.Contracts;
using BotArbitragem.Application.Models;

namespace BotArbitragem.Application.Services;

public static class ValueBetEvaluator
{
    public static ValueBetAssessment Evaluate(decimal estimatedProbability, decimal marketOdds, ValueBetPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        var ev = ExpectedValueCalculator.Calculate(estimatedProbability, marketOdds);
        var implied = ExpectedValueCalculator.CalculateImpliedProbability(marketOdds);
        var edge = estimatedProbability - implied;
        var reasons = new List<string>();
        if (ev < policy.MinimumExpectedValue) reasons.Add("EV abaixo do mínimo configurado.");
        if (edge < policy.MinimumEdge) reasons.Add("Edge abaixo do mínimo configurado.");
        if (estimatedProbability < policy.MinimumEstimatedProbability) reasons.Add("Probabilidade estimada abaixo do mínimo configurado.");
        if (marketOdds < policy.MinimumOdds || marketOdds > policy.MaximumOdds) reasons.Add("Odd fora da faixa de risco configurada.");
        return new ValueBetAssessment(estimatedProbability, marketOdds, implied, ev, edge, reasons.Count == 0, reasons);
    }
}
