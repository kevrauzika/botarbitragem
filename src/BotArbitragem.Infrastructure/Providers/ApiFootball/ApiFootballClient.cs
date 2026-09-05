using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Contracts;
using BotArbitragem.Application.Exceptions;
using Microsoft.Extensions.Options;

namespace BotArbitragem.Infrastructure.Providers.ApiFootball;

public sealed class ApiFootballClient(HttpClient httpClient, IOptions<ApiFootballOptions> options) : IOddsProvider
{
    public async Task<IReadOnlyList<ProviderEvent>> GetUpcomingOddsAsync(string sportKey,
        CancellationToken cancellationToken)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.ApiKey))
            throw new InvalidOperationException("ApiFootball:ApiKey não configurada.");

        var today = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-3)).ToString("yyyy-MM-dd");
        var fixtures = await GetAsync<ApiResponse<ApiFixture>>(
            $"/fixtures?date={today}&timezone=America%2FSao_Paulo", settings.ApiKey, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var selected = fixtures.Response
            .Where(x => x.Fixture.Date > now && x.Fixture.Status.Short == "NS")
            .Where(x => settings.MainLeagueIds.Contains(x.League.Id))
            .OrderBy(x => x.Fixture.Date)
            .Take(Math.Clamp(settings.MaximumMatchesPerCycle, 1, 15))
            .ToList();
        var result = new List<ProviderEvent>();

        foreach (var fixture in selected)
        {
            var odds = await GetAsync<ApiResponse<ApiOdds>>(
                $"/odds?fixture={fixture.Fixture.Id}&bet=1", settings.ApiKey, cancellationToken);
            var quotes = odds.Response.SelectMany(snapshot => snapshot.Bookmakers
                    .SelectMany(bookmaker => bookmaker.Bets.Where(bet => bet.Id == 1)
                        .SelectMany(bet => bet.Values.Select(value => new ProviderOddsQuote(
                            bookmaker.Name,
                            "h2h",
                            MapSelection(value.Value, fixture),
                            ParseOdd(value.Odd),
                            snapshot.Update.ToUniversalTime()))))
                .Where(x => x.DecimalOdds > 1m)
                .ToList();
            result.Add(new ProviderEvent(fixture.Fixture.Id.ToString(CultureInfo.InvariantCulture), sportKey,
                fixture.League.Name, fixture.Teams.Home.Name, fixture.Teams.Away.Name,
                fixture.Fixture.Date.ToUniversalTime(), quotes));
        }

        return result;
    }

    private async Task<T> GetAsync<T>(string path, string apiKey, CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);
            request.Headers.Add("x-apisports-key", apiKey);
            using var response = await httpClient.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new OddsProviderException($"API-Football respondeu com status {(int)response.StatusCode}.");
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
                   ?? throw new OddsProviderException("API-Football retornou uma resposta vazia.");
        }
        catch (OddsProviderException) { throw; }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new OddsProviderException("API-Football excedeu o tempo limite de resposta.");
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            throw new OddsProviderException("Não foi possível consultar a API-Football.");
        }
    }

    private static string MapSelection(string selection, ApiFixture fixture) => selection switch
    {
        "Home" => fixture.Teams.Home.Name,
        "Draw" => "Draw",
        "Away" => fixture.Teams.Away.Name,
        _ => selection
    };

    private static decimal ParseOdd(string value) =>
        decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var odd) ? odd : 0m;

    private sealed record ApiResponse<T>(
        [property: JsonPropertyName("errors")] JsonElement Errors,
        [property: JsonPropertyName("results")] int Results,
        [property: JsonPropertyName("response")] List<T> Response);

    private sealed record ApiFixture(
        [property: JsonPropertyName("fixture")] FixtureInfo Fixture,
        [property: JsonPropertyName("league")] LeagueInfo League,
        [property: JsonPropertyName("teams")] TeamsInfo Teams);

    private sealed record FixtureInfo(
        [property: JsonPropertyName("id")] long Id,
        [property: JsonPropertyName("date")] DateTimeOffset Date,
        [property: JsonPropertyName("status")] StatusInfo Status);

    private sealed record StatusInfo([property: JsonPropertyName("short")] string Short);
    private sealed record LeagueInfo(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string Name);
    private sealed record TeamsInfo(
        [property: JsonPropertyName("home")] TeamInfo Home,
        [property: JsonPropertyName("away")] TeamInfo Away);
    private sealed record TeamInfo([property: JsonPropertyName("name")] string Name);

    private sealed record ApiOdds(
        [property: JsonPropertyName("fixture")] OddsFixture Fixture,
        [property: JsonPropertyName("update")] DateTimeOffset Update,
        [property: JsonPropertyName("bookmakers")] List<ApiBookmaker> Bookmakers);
    private sealed record OddsFixture([property: JsonPropertyName("id")] long Id);
    private sealed record ApiBookmaker(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("bets")] List<ApiBet> Bets);
    private sealed record ApiBet(
        [property: JsonPropertyName("id")] int Id,
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("values")] List<ApiBetValue> Values);
    private sealed record ApiBetValue(
        [property: JsonPropertyName("value")] string Value,
        [property: JsonPropertyName("odd")] string Odd);
}
