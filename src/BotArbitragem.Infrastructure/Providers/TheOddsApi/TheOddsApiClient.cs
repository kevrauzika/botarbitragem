using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Contracts;
using BotArbitragem.Application.Exceptions;
using Microsoft.Extensions.Options;

namespace BotArbitragem.Infrastructure.Providers.TheOddsApi;

public sealed class TheOddsApiClient(HttpClient httpClient, IOptions<TheOddsApiOptions> options) : IOddsProvider
{
    public async Task<IReadOnlyList<ProviderEvent>> GetUpcomingOddsAsync(string sportKey, CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("OddsProvider:ApiKey não configurada.");
        if (settings.AllowedSports.Length > 0 && !settings.AllowedSports.Contains(sportKey, StringComparer.OrdinalIgnoreCase))
            throw new ArgumentException("Liga não autorizada na configuração.", nameof(sportKey));

        var path = $"/v4/sports/{Uri.EscapeDataString(sportKey)}/odds/" +
            $"?apiKey={Uri.EscapeDataString(settings.ApiKey)}&regions={Uri.EscapeDataString(settings.Regions)}" +
            $"&markets={Uri.EscapeDataString(settings.Markets)}&oddsFormat=decimal&dateFormat=iso";
        List<ApiEvent> events;
        try
        {
            using var response = await httpClient.GetAsync(path, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new OddsProviderException($"O provedor de odds respondeu com status {(int)response.StatusCode}.");

            events = await response.Content.ReadFromJsonAsync<List<ApiEvent>>(cancellationToken: cancellationToken) ?? [];
        }
        catch (OddsProviderException)
        {
            throw;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OddsProviderException("O provedor de odds excedeu o tempo limite de resposta.");
        }
        catch (HttpRequestException)
        {
            throw new OddsProviderException("Não foi possível conectar ao provedor de odds.");
        }
        catch (JsonException)
        {
            throw new OddsProviderException("O provedor de odds retornou uma resposta inválida.");
        }

        return events.Select(item => new ProviderEvent(item.Id, item.SportKey, item.SportTitle,
            item.HomeTeam, item.AwayTeam, item.CommenceTime,
            item.Bookmakers.SelectMany(bookmaker => bookmaker.Markets.SelectMany(market => market.Outcomes.Select(outcome =>
                new ProviderOddsQuote(bookmaker.Title, market.Key, outcome.Name, outcome.Price, bookmaker.LastUpdate)))).ToList())).ToList();
    }

    private sealed record ApiEvent(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("sport_key")] string SportKey,
        [property: JsonPropertyName("sport_title")] string SportTitle,
        [property: JsonPropertyName("commence_time")] DateTimeOffset CommenceTime,
        [property: JsonPropertyName("home_team")] string HomeTeam,
        [property: JsonPropertyName("away_team")] string AwayTeam,
        [property: JsonPropertyName("bookmakers")] List<ApiBookmaker> Bookmakers);

    private sealed record ApiBookmaker(
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("last_update")] DateTimeOffset LastUpdate,
        [property: JsonPropertyName("markets")] List<ApiMarket> Markets);

    private sealed record ApiMarket(
        [property: JsonPropertyName("key")] string Key,
        [property: JsonPropertyName("outcomes")] List<ApiOutcome> Outcomes);

    private sealed record ApiOutcome(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("price")] decimal Price);
}
