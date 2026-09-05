using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Contracts;
using BotArbitragem.Domain.Entities;

namespace BotArbitragem.Application.Services;

public sealed class OddsIngestionService(IOddsProvider provider, IMatchRepository repository) : IOddsIngestionService
{
    public async Task<IngestionResult> ImportAsync(string sportKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sportKey)) throw new ArgumentException("Sport key é obrigatória.", nameof(sportKey));

        var events = await provider.GetUpcomingOddsAsync(sportKey, cancellationToken);
        var matchesCreated = 0;
        var oddsCreated = 0;
        var oddsSkipped = 0;

        foreach (var item in events)
        {
            var candidate = new FootballMatch(item.ExternalId, item.Competition, item.HomeTeam, item.AwayTeam, item.KickoffAt);
            var (match, created) = await repository.GetOrAddAsync(candidate, cancellationToken);
            if (created) matchesCreated++;

            foreach (var quote in item.Odds.Where(x => x.DecimalOdds > 1m))
            {
                var entity = new OddsQuote(match.Id, quote.Bookmaker, quote.Market, quote.Selection, quote.DecimalOdds, quote.CapturedAt);
                if (await repository.AddOddsIfNewAsync(entity, cancellationToken)) oddsCreated++;
                else oddsSkipped++;
            }
        }

        return new IngestionResult(sportKey, events.Count, matchesCreated, oddsCreated, oddsSkipped);
    }
}
