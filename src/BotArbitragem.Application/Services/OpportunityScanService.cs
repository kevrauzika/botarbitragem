using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Contracts;
using BotArbitragem.Application.Models;
using BotArbitragem.Domain.Entities;
using Microsoft.Extensions.Options;

namespace BotArbitragem.Application.Services;

public sealed class OpportunityScanService(
    IMatchRepository matchRepository,
    IOpportunityRepository opportunityRepository,
    IOpportunityNotifier notifier,
    IOptions<ValueBetPolicy> valueBetPolicy,
    IOptions<OpportunityPolicy> opportunityPolicy) : IOpportunityScanService
{
    public async Task<OpportunityScanResult> ScanAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var policy = opportunityPolicy.Value;
        var matches = await LoadMatchesAsync(now, now.AddHours(policy.LookAheadHours), cancellationToken);
        var candidates = matches.SelectMany(match => FindCandidates(match, now, policy)).ToList();
        var activeFingerprints = candidates.Select(x => x.Fingerprint).ToHashSet(StringComparer.Ordinal);
        var created = 0;
        var refreshed = 0;
        var notifications = 0;

        foreach (var candidate in candidates)
        {
            var result = await opportunityRepository.UpsertAsync(candidate, now,
                TimeSpan.FromMinutes(policy.AlertCooldownMinutes), cancellationToken);
            if (result.Created) created++; else refreshed++;

            if (!result.ShouldNotify) continue;
            var match = matches.First(x => x.Id == candidate.MatchId);
            if (await notifier.NotifyAsync(result.Opportunity, match, cancellationToken))
            {
                await opportunityRepository.MarkNotifiedAsync(result.Opportunity.Id, now, cancellationToken);
                notifications++;
            }
        }

        var expired = await opportunityRepository.ExpireNotSeenAsync(activeFingerprints, now, cancellationToken);
        return new OpportunityScanResult(matches.Count, candidates.Count, created, refreshed, expired, notifications);
    }

    private async Task<List<FootballMatch>> LoadMatchesAsync(
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken)
    {
        const int pageSize = 100;
        var matches = new List<FootballMatch>();
        for (var page = 1; ; page++)
        {
            var result = await matchRepository.ListAsync(from, to, page, pageSize, cancellationToken);
            matches.AddRange(result.Items);
            if (page >= result.TotalPages) return matches;
        }
    }

    private IEnumerable<OpportunityCandidate> FindCandidates(FootballMatch match, DateTimeOffset now,
        OpportunityPolicy policy)
    {
        var minimumCapturedAt = now.AddMinutes(-policy.MaximumOddsAgeMinutes);
        var latestQuotes = match.OddsQuotes
            .Where(x => string.Equals(x.Market, "h2h", StringComparison.OrdinalIgnoreCase) &&
                        x.CapturedAt >= minimumCapturedAt && x.CapturedAt <= now.AddMinutes(2))
            .GroupBy(x => new { x.Bookmaker, Selection = CanonicalSelection(match, x.Selection) })
            .Where(x => x.Key.Selection is not null)
            .Select(x => x.OrderByDescending(q => q.CapturedAt).First())
            .ToList();

        var snapshots = latestQuotes.GroupBy(x => x.Bookmaker)
            .Select(group => BuildSnapshot(match, group))
            .Where(x => x is not null)
            .Cast<BookmakerSnapshot>()
            .ToList();
        if (snapshots.Count == 0) yield break;

        var fairProbabilities = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            [match.HomeTeam] = Median(snapshots.Select(x => x.HomeProbability)),
            ["Draw"] = Median(snapshots.Select(x => x.DrawProbability)),
            [match.AwayTeam] = Median(snapshots.Select(x => x.AwayProbability))
        };

        var bestQuotes = new[] { match.HomeTeam, "Draw", match.AwayTeam }
            .Select(selection => latestQuotes
                .Where(x => string.Equals(CanonicalSelection(match, x.Selection), selection, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(x => x.DecimalOdds)
                .First())
            .ToList();

        foreach (var quote in bestQuotes)
        {
            var selection = CanonicalSelection(match, quote.Selection)!;
            var assessment = ValueBetEvaluator.Evaluate(fairProbabilities[selection], quote.DecimalOdds,
                valueBetPolicy.Value);
            if (!assessment.IsQualified) continue;

            yield return new OpportunityCandidate(match.Id,
                $"{match.Id}:value-bet:h2h:{selection.ToLowerInvariant()}", "value-bet", "h2h", selection,
                assessment.EstimatedProbability, assessment.MarketOdds, assessment.ExpectedValue, assessment.Edge,
                null, match.KickoffAt,
                [new OpportunityLegCandidate(quote.Bookmaker, selection, quote.DecimalOdds, 100m)]);
        }

        var inverseTotal = bestQuotes.Sum(x => 1m / x.DecimalOdds);
        var profit = (1m / inverseTotal) - 1m;
        if (profit < policy.MinimumArbitrageProfit) yield break;

        var legs = bestQuotes.Select(quote =>
        {
            var selection = CanonicalSelection(match, quote.Selection)!;
            var stakePercentage = ((1m / quote.DecimalOdds) / inverseTotal) * 100m;
            return new OpportunityLegCandidate(quote.Bookmaker, selection, quote.DecimalOdds, stakePercentage);
        }).ToList();
        yield return new OpportunityCandidate(match.Id, $"{match.Id}:arbitrage:h2h", "arbitrage", "h2h",
            null, null, null, null, null, profit, match.KickoffAt, legs);
    }

    private static BookmakerSnapshot? BuildSnapshot(FootballMatch match, IEnumerable<OddsQuote> quotes)
    {
        var values = quotes.Select(x => new { Quote = x, Selection = CanonicalSelection(match, x.Selection) })
            .Where(x => x.Selection is not null)
            .ToDictionary(x => x.Selection!, x => x.Quote, StringComparer.OrdinalIgnoreCase);
        if (!values.TryGetValue(match.HomeTeam, out var home) || !values.TryGetValue("Draw", out var draw) ||
            !values.TryGetValue(match.AwayTeam, out var away)) return null;

        var noVig = NoVigProbabilityCalculator.CalculateThreeWay(home.DecimalOdds, draw.DecimalOdds,
            away.DecimalOdds);
        return new BookmakerSnapshot(noVig.HomeProbability, noVig.DrawProbability, noVig.AwayProbability);
    }

    private static string? CanonicalSelection(FootballMatch match, string selection)
    {
        if (string.Equals(selection, match.HomeTeam, StringComparison.OrdinalIgnoreCase)) return match.HomeTeam;
        if (string.Equals(selection, match.AwayTeam, StringComparison.OrdinalIgnoreCase)) return match.AwayTeam;
        if (string.Equals(selection, "Draw", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(selection, "Empate", StringComparison.OrdinalIgnoreCase)) return "Draw";
        return null;
    }

    private static decimal Median(IEnumerable<decimal> source)
    {
        var values = source.Order().ToArray();
        var middle = values.Length / 2;
        return values.Length % 2 == 0 ? (values[middle - 1] + values[middle]) / 2m : values[middle];
    }

    private sealed record BookmakerSnapshot(decimal HomeProbability, decimal DrawProbability,
        decimal AwayProbability);
}
