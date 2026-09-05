using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Contracts;
using BotArbitragem.Application.Exceptions;
using Microsoft.Extensions.Options;

namespace BotArbitragem.Infrastructure.Providers.GanheMaisBet;

public sealed class GanheMaisBetClient(HttpClient httpClient, IOptions<GanheMaisBetOptions> options) : IOddsProvider
{
    public async Task<IReadOnlyList<ProviderEvent>> GetUpcomingOddsAsync(
        string sportKey,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("GanheMaisBet:ApiKey não configurada.");

        var now = DateTimeOffset.UtcNow;
        var limit = Math.Clamp(settings.MaximumMatchesPerRequest, 1, 20);
        var path = $"/api/v1/matches?status=scheduled&from={Uri.EscapeDataString(now.ToString("O"))}" +
                   $"&to={Uri.EscapeDataString(now.AddHours(settings.LookAheadHours).ToString("O"))}&limit={limit}";
        var matches = await GetAsync<ApiEnvelope<List<ApiMatch>>>(path, settings.ApiKey, cancellationToken);
        var result = new List<ProviderEvent>();

        foreach (var match in matches.Data)
        {
            var odds = await GetAsync<ApiEnvelope<ApiOdds>>( 
                $"/api/v1/matches/{Uri.EscapeDataString(match.Id)}/odds",
                settings.ApiKey,
                cancellationToken);
            var quotes = ParseOneXTwo(match, odds.Data);
            result.Add(new ProviderEvent(match.Id, sportKey, match.CompetitionId,
                match.HomeTeamName, match.AwayTeamName, match.KickoffAt, quotes));
        }

        return result;
    }

    private async Task<T> GetAsync<T>(string path, string apiKey, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new OddsProviderException($"GanheMaisBet respondeu com status {(int)response.StatusCode}.");
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
                   ?? throw new OddsProviderException("GanheMaisBet retornou uma resposta vazia.");
        }
        catch (OddsProviderException) { throw; }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OddsProviderException("GanheMaisBet excedeu o tempo limite de resposta.");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            throw new OddsProviderException("Não foi possível consultar a GanheMaisBet.");
        }
    }

    private static IReadOnlyList<ProviderOddsQuote> ParseOneXTwo(ApiMatch match, ApiOdds odds)
    {
        if (!odds.Markets.TryGetProperty("1x2", out var market) || market.ValueKind != JsonValueKind.Object)
            return [];

        var capturedAt = odds.UpdatedAt ?? DateTimeOffset.UtcNow;
        var quotes = new List<ProviderOddsQuote>();
        AddSelection(market, "home", match.HomeTeamName, capturedAt, quotes);
        AddSelection(market, "draw", "Draw", capturedAt, quotes);
        AddSelection(market, "away", match.AwayTeamName, capturedAt, quotes);
        return quotes;
    }

    private static void AddSelection(JsonElement market, string key, string selection,
        DateTimeOffset capturedAt, List<ProviderOddsQuote> quotes)
    {
        if (!market.TryGetProperty(key, out var prices) || prices.ValueKind != JsonValueKind.Object) return;
        foreach (var price in prices.EnumerateObject())
        {
            if (string.Equals(price.Name, "stake", StringComparison.OrdinalIgnoreCase)) continue;
            decimal value;
            if (price.Value.ValueKind == JsonValueKind.Number && price.Value.TryGetDecimal(out value) ||
                price.Value.ValueKind == JsonValueKind.String &&
                decimal.TryParse(price.Value.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out value))
            {
                if (value > 1m) quotes.Add(new ProviderOddsQuote(price.Name, "h2h", selection, value, capturedAt));
            }
        }
    }

    private sealed record ApiEnvelope<T>([property: JsonPropertyName("data")] T Data);
    private sealed record ApiMatch(
        [property: JsonPropertyName("id")] string Id,
        [property: JsonPropertyName("competitionId")] string CompetitionId,
        [property: JsonPropertyName("homeTeamName")] string HomeTeamName,
        [property: JsonPropertyName("awayTeamName")] string AwayTeamName,
        [property: JsonPropertyName("kickoffAt")] DateTimeOffset KickoffAt);
    private sealed record ApiOdds(
        [property: JsonPropertyName("markets")] JsonElement Markets,
        [property: JsonPropertyName("updatedAt")] DateTimeOffset? UpdatedAt);
}
