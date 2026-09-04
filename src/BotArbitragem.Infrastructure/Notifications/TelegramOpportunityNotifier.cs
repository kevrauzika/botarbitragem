using System.Globalization;
using BotArbitragem.Application.Abstractions;
using BotArbitragem.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotArbitragem.Infrastructure.Notifications;

public sealed class TelegramOpportunityNotifier(HttpClient httpClient, IOptions<TelegramOptions> options,
    ILogger<TelegramOpportunityNotifier> logger) : IOpportunityNotifier
{
    public async Task<bool> NotifyAsync(Opportunity opportunity, FootballMatch match,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (!settings.Enabled || string.IsNullOrWhiteSpace(settings.BotToken) ||
            string.IsNullOrWhiteSpace(settings.ChatId)) return false;

        var title = opportunity.Kind == "arbitrage" ? "ARBITRAGEM ENCONTRADA" : "VALUE BET ENCONTRADA";
        var metric = opportunity.Kind == "arbitrage"
            ? $"Lucro teórico: {opportunity.ProfitPercentage!.Value:P2}"
            : $"EV: {opportunity.ExpectedValue!.Value:P2} | Edge: {opportunity.Edge!.Value:P2}";
        var legs = string.Join(Environment.NewLine, opportunity.Legs.Select(x =>
            $"- {x.Selection}: {x.DecimalOdds.ToString("0.00", CultureInfo.InvariantCulture)} em {x.Bookmaker}" +
            (opportunity.Kind == "arbitrage" ? $" | stake {x.StakePercentage:0.00}%" : string.Empty)));
        var message = $"{title}{Environment.NewLine}{match.HomeTeam} x {match.AwayTeam}" +
                      $"{Environment.NewLine}{match.Competition} | {match.KickoffAt:dd/MM HH:mm} UTC" +
                      $"{Environment.NewLine}{metric}{Environment.NewLine}{legs}";

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post,
                $"/bot{Uri.EscapeDataString(settings.BotToken)}/sendMessage")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["chat_id"] = settings.ChatId,
                    ["text"] = message
                })
            };
            using var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode) return true;

            logger.LogWarning("Telegram recusou a notificação com status {StatusCode}.", response.StatusCode);
            return false;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logger.LogWarning("Falha ao enviar alerta ao Telegram ({ExceptionType}).", exception.GetType().Name);
            return false;
        }
    }
}
