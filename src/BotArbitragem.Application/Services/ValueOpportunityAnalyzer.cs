using BotArbitragem.Application.Contracts;
using BotArbitragem.Application.Models;
using BotArbitragem.Domain.Entities;

namespace BotArbitragem.Application.Services;

public static class ValueOpportunityAnalyzer
{
    public static MatchOpportunityAnalysis Analyze(
        FootballMatch match,
        ValueBetPolicy valueBetPolicy,
        OpportunityAnalysisPolicy analysisPolicy,
        DateTimeOffset analyzedAt)
    {
        ArgumentNullException.ThrowIfNull(match);
        ArgumentNullException.ThrowIfNull(valueBetPolicy);
        ArgumentNullException.ThrowIfNull(analysisPolicy);

        if (analysisPolicy.MinimumReferenceBookmakers < 1)
            throw new ArgumentOutOfRangeException(nameof(analysisPolicy.MinimumReferenceBookmakers));
        if (analysisPolicy.MaximumQuoteAgeMinutes < 1)
            throw new ArgumentOutOfRangeException(nameof(analysisPolicy.MaximumQuoteAgeMinutes));
        if (analysisPolicy.MaximumFutureSkewMinutes < 0)
            throw new ArgumentOutOfRangeException(nameof(analysisPolicy.MaximumFutureSkewMinutes));

        var oldestAccepted = analyzedAt.AddMinutes(-analysisPolicy.MaximumQuoteAgeMinutes);
        var newestAccepted = analyzedAt.AddMinutes(analysisPolicy.MaximumFutureSkewMinutes);
        var completeMarkets = match.OddsQuotes
            .Where(quote => string.Equals(quote.Market, "h2h", StringComparison.OrdinalIgnoreCase))
            .Where(quote => quote.CapturedAt >= oldestAccepted && quote.CapturedAt <= newestAccepted)
            .Select(quote => new QuoteWithOutcome(quote, ResolveOutcome(match, quote.Selection)))
            .Where(item => item.Outcome.HasValue)
            .GroupBy(item => item.Quote.Bookmaker.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(CreateCompleteMarket)
            .Where(market => market is not null)
            .Cast<CompleteMarket>()
            .ToList();

        var opportunities = new List<ValueOpportunity>();
        var candidatesEvaluated = 0;

        foreach (var candidateMarket in completeMarkets)
        {
            var references = completeMarkets
                .Where(market => !string.Equals(market.Bookmaker, candidateMarket.Bookmaker, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (references.Count < analysisPolicy.MinimumReferenceBookmakers)
                continue;

            foreach (var outcome in Enum.GetValues<Outcome>())
            {
                candidatesEvaluated++;
                var fairProbability = Median(references.Select(market => market.Probabilities[outcome]));
                var quote = candidateMarket.Quotes[outcome];
                var assessment = ValueBetEvaluator.Evaluate(fairProbability, quote.DecimalOdds, valueBetPolicy);

                if (!assessment.IsQualified)
                    continue;

                opportunities.Add(new ValueOpportunity(
                    candidateMarket.Bookmaker,
                    quote.Market,
                    quote.Selection,
                    quote.DecimalOdds,
                    fairProbability,
                    assessment.ImpliedProbability,
                    assessment.ExpectedValue,
                    assessment.Edge,
                    references.Count,
                    quote.CapturedAt));
            }
        }

        return new MatchOpportunityAnalysis(
            match.Id,
            analyzedAt,
            completeMarkets.Count,
            candidatesEvaluated,
            opportunities.OrderByDescending(item => item.ExpectedValue).ToList());
    }

    private static CompleteMarket? CreateCompleteMarket(IGrouping<string, QuoteWithOutcome> group)
    {
        var quotes = group
            .GroupBy(item => item.Outcome!.Value)
            .ToDictionary(
                items => items.Key,
                items => items.OrderByDescending(item => item.Quote.CapturedAt).First().Quote);

        if (quotes.Count != 3)
            return null;

        var noVig = NoVigProbabilityCalculator.CalculateThreeWay(
            quotes[Outcome.Home].DecimalOdds,
            quotes[Outcome.Draw].DecimalOdds,
            quotes[Outcome.Away].DecimalOdds);

        var probabilities = new Dictionary<Outcome, decimal>
        {
            [Outcome.Home] = noVig.HomeProbability,
            [Outcome.Draw] = noVig.DrawProbability,
            [Outcome.Away] = noVig.AwayProbability
        };

        return new CompleteMarket(group.Key, quotes, probabilities);
    }

    private static Outcome? ResolveOutcome(FootballMatch match, string selection)
    {
        var normalized = selection.Trim();
        if (string.Equals(normalized, match.HomeTeam.Trim(), StringComparison.OrdinalIgnoreCase)) return Outcome.Home;
        if (string.Equals(normalized, match.AwayTeam.Trim(), StringComparison.OrdinalIgnoreCase)) return Outcome.Away;
        if (string.Equals(normalized, "Draw", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(normalized, "Empate", StringComparison.OrdinalIgnoreCase)) return Outcome.Draw;
        return null;
    }

    private static decimal Median(IEnumerable<decimal> values)
    {
        var ordered = values.Order().ToArray();
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 1
            ? ordered[middle]
            : (ordered[middle - 1] + ordered[middle]) / 2m;
    }

    private enum Outcome { Home, Draw, Away }

    private sealed record QuoteWithOutcome(OddsQuote Quote, Outcome? Outcome);

    private sealed record CompleteMarket(
        string Bookmaker,
        IReadOnlyDictionary<Outcome, OddsQuote> Quotes,
        IReadOnlyDictionary<Outcome, decimal> Probabilities);
}

