using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BotArbitragem.Application.Abstractions;
using BotArbitragem.Domain.Entities;

namespace BotArbitragem.Api.Workers;

public sealed class AudienceEngagementWorker(
    IServiceScopeFactory scopeFactory,
    TimeProvider timeProvider,
    ILogger<AudienceEngagementWorker> logger) : BackgroundService
{
    private static readonly TimeSpan BrazilOffset = TimeSpan.FromHours(-3);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Falha no ciclo de comunicação com o grupo.");
            }

            await Task.Delay(TimeSpan.FromMinutes(1), timeProvider, stoppingToken);
        }
    }

    private async Task RunCycleAsync(CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var localNow = now.ToOffset(BrazilOffset);
        var localStart = new DateTimeOffset(localNow.Date, BrazilOffset);
        var localEnd = localStart.AddDays(1);

        await using var scope = scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var notifier = scope.ServiceProvider.GetRequiredService<IGroupNotifier>();
        var deduplicator = scope.ServiceProvider.GetRequiredService<IAlertDeduplicator>();
        var page = await repository.ListAsync(localStart.ToUniversalTime(), localEnd.ToUniversalTime(), 1, 20,
            cancellationToken);
        var matches = page.Items.OrderBy(match => match.KickoffAt).ToList();
        if (matches.Count == 0) return;

        await SendDailyDigestAsync(matches, repository, notifier, deduplicator, now, localNow.Date, cancellationToken);

        foreach (var match in matches.Where(match => match.KickoffAt > now.AddMinutes(9) &&
                                                     match.KickoffAt <= now.AddMinutes(11)))
        {
            await SendReminderAsync(match, repository, notifier, deduplicator, now, cancellationToken);
        }
    }

    private static async Task SendDailyDigestAsync(
        IReadOnlyList<FootballMatch> matches,
        IMatchRepository repository,
        IGroupNotifier notifier,
        IAlertDeduplicator deduplicator,
        DateTimeOffset now,
        DateTime localDate,
        CancellationToken cancellationToken)
    {
        var fingerprint = Hash($"daily|{localDate:yyyy-MM-dd}");
        if (!await deduplicator.TryReserveAsync(fingerprint, matches[0].Id, now, cancellationToken)) return;

        try
        {
            var lines = new List<string>
            {
                $"⚽ JOGOS PRINCIPAIS — {localDate:dd/MM}",
                "",
                "Leitura matemática do mercado 1X2:"
            };
            foreach (var match in matches)
            {
                var detailed = await repository.GetByIdWithLatestOddsAsync(match.Id, 500, cancellationToken);
                lines.Add(FormatMatchLine(detailed ?? match));
            }
            lines.Add("");
            lines.Add("⚠️ Sugestões estatísticas, sem garantia de resultado. Aposte com responsabilidade.");
            await notifier.SendAsync(string.Join('\n', lines), cancellationToken);
        }
        catch
        {
            await deduplicator.ReleaseAsync(fingerprint, CancellationToken.None);
            throw;
        }
    }

    private static async Task SendReminderAsync(
        FootballMatch match,
        IMatchRepository repository,
        IGroupNotifier notifier,
        IAlertDeduplicator deduplicator,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var fingerprint = Hash($"reminder|{match.Id:N}");
        if (!await deduplicator.TryReserveAsync(fingerprint, match.Id, now, cancellationToken)) return;

        try
        {
            var detailed = await repository.GetByIdWithLatestOddsAsync(match.Id, 500, cancellationToken) ?? match;
            await notifier.SendAsync(
                $"⏰ FALTAM 10 MINUTOS\n\n{FormatMatchLine(detailed)}\n\nConfiram a odd antes da entrada.",
                cancellationToken);
            await notifier.SendPollAsync(
                $"{match.HomeTeam} x {match.AwayTeam}: já entraram?",
                ["✅ Já entrei", "⏳ Ainda não", "🚫 Vou ficar de fora"],
                cancellationToken);
        }
        catch
        {
            await deduplicator.ReleaseAsync(fingerprint, CancellationToken.None);
            throw;
        }
    }

    private static string FormatMatchLine(FootballMatch match)
    {
        var kickoff = match.KickoffAt.ToOffset(BrazilOffset);
        var suggestion = CalculateSuggestion(match);
        return suggestion is null
            ? $"• {kickoff:HH:mm} | {match.HomeTeam} x {match.AwayTeam} ({match.Competition})\n  Sem mercado suficiente para uma leitura segura."
            : $"• {kickoff:HH:mm} | {match.HomeTeam} x {match.AwayTeam} ({match.Competition})\n" +
              $"  Leitura: {suggestion.Selection} | {suggestion.Probability:P0} | " +
              $"{suggestion.Bookmaker} @ {suggestion.Odd.ToString("0.00", CultureInfo.InvariantCulture)}";
    }

    private static Suggestion? CalculateSuggestion(FootballMatch match)
    {
        var selections = new[] { match.HomeTeam, "Draw", match.AwayTeam };
        var completeBooks = match.OddsQuotes
            .Where(quote => string.Equals(quote.Market, "h2h", StringComparison.OrdinalIgnoreCase))
            .GroupBy(quote => quote.Bookmaker, StringComparer.OrdinalIgnoreCase)
            .Select(group => selections.Select(selection => group
                    .Where(quote => string.Equals(quote.Selection, selection, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(quote => quote.CapturedAt).FirstOrDefault()).ToArray())
            .Where(quotes => quotes.All(quote => quote is not null))
            .Select(quotes => quotes.Select(quote => quote!).ToArray())
            .ToList();
        if (completeBooks.Count < 2) return null;

        var probabilities = selections.Select((_, index) => Median(completeBooks.Select(book =>
        {
            var inverseSum = book.Sum(quote => 1m / quote.DecimalOdds);
            return (1m / book[index].DecimalOdds) / inverseSum;
        }))).ToArray();
        var chosenIndex = Array.IndexOf(probabilities, probabilities.Max());
        var prices = completeBooks.Select(book => book[chosenIndex]).ToList();
        var target = prices.FirstOrDefault(quote => quote.Bookmaker.Contains("Bet365", StringComparison.OrdinalIgnoreCase))
                     ?? prices.OrderByDescending(quote => quote.DecimalOdds).First();
        var label = chosenIndex == 1 ? "Empate" : selections[chosenIndex];
        return new Suggestion(label, probabilities[chosenIndex], target.Bookmaker, target.DecimalOdds);
    }

    private static decimal Median(IEnumerable<decimal> values)
    {
        var ordered = values.Order().ToArray();
        var middle = ordered.Length / 2;
        return ordered.Length % 2 == 1 ? ordered[middle] : (ordered[middle - 1] + ordered[middle]) / 2m;
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));

    private sealed record Suggestion(string Selection, decimal Probability, string Bookmaker, decimal Odd);
}
