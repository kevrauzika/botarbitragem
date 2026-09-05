using BotArbitragem.Api.Security;
using BotArbitragem.Application.Abstractions;
using BotArbitragem.Domain.Entities;

namespace BotArbitragem.Api.Endpoints;

public static class MatchEndpoints
{
    public static IEndpointRouteBuilder MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/matches").WithTags("Matches");
        group.MapGet("/", async (DateTimeOffset? from, DateTimeOffset? to, int? page, int? pageSize,
            IMatchRepository repository, CancellationToken ct) =>
        {
            var requestedPage = page ?? 1;
            var requestedPageSize = pageSize ?? 50;
            if (requestedPage is < 1 or > 10_000 || requestedPageSize is < 1 or > 100)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["pagination"] = ["Page deve estar entre 1 e 10000 e pageSize deve estar entre 1 e 100."]
                });
            return Results.Ok(await repository.ListAsync(from, to, requestedPage, requestedPageSize, ct));
        });
        group.MapGet("/{id:guid}", async (Guid id, int? oddsLimit, IMatchRepository repository, CancellationToken ct) =>
        {
            var requestedOddsLimit = oddsLimit ?? 200;
            if (requestedOddsLimit is < 1 or > 1_000)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["oddsLimit"] = ["OddsLimit deve estar entre 1 e 1000."]
                });
            return await repository.GetByIdWithLatestOddsAsync(id, requestedOddsLimit, ct) is { } match
                ? Results.Ok(match)
                : Results.NotFound();
        });
        group.MapPost("/", async (CreateMatchRequest request, IMatchRepository repository, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.ExternalId) || request.ExternalId.Trim().Length > 100 ||
                string.IsNullOrWhiteSpace(request.HomeTeam) || request.HomeTeam.Trim().Length > 150 ||
                string.IsNullOrWhiteSpace(request.AwayTeam) || request.AwayTeam.Trim().Length > 150 ||
                string.IsNullOrWhiteSpace(request.Competition) || request.Competition.Trim().Length > 150 ||
                string.Equals(request.HomeTeam.Trim(), request.AwayTeam.Trim(), StringComparison.OrdinalIgnoreCase))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["match"] = ["Verifique os campos obrigatórios, seus limites e os times informados."] });
            var candidate = new FootballMatch(request.ExternalId, request.Competition, request.HomeTeam, request.AwayTeam, request.KickoffAt);
            var (match, created) = await repository.GetOrAddAsync(candidate, ct);
            if (!created) return Results.Conflict(new { message = "Partida já cadastrada." });
            return Results.Created($"/api/matches/{match.Id}", match);
        }).AddEndpointFilter<AdminApiKeyEndpointFilter>();
        group.MapPost("/{id:guid}/odds", async (Guid id, CreateOddsRequest request, IMatchRepository repository,
            TimeProvider timeProvider, CancellationToken ct) =>
        {
            if (await repository.GetByIdAsync(id, ct) is null) return Results.NotFound();
            var now = timeProvider.GetUtcNow();
            var capturedAt = request.CapturedAt ?? now;
            if (request.DecimalOdds <= 1m ||
                string.IsNullOrWhiteSpace(request.Bookmaker) || request.Bookmaker.Trim().Length > 100 ||
                string.IsNullOrWhiteSpace(request.Market) || request.Market.Trim().Length > 100 ||
                string.IsNullOrWhiteSpace(request.Selection) || request.Selection.Trim().Length > 100 ||
                capturedAt > now.AddMinutes(5))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["odds"] = ["Verifique a odd, os campos obrigatórios, seus limites e o instante de captura."] });
            var quote = new OddsQuote(id, request.Bookmaker, request.Market, request.Selection, request.DecimalOdds, capturedAt);
            if (!await repository.AddOddsIfNewAsync(quote, ct))
                return Results.Conflict(new { message = "Cotação já registrada para este instante." });
            return Results.Created($"/api/matches/{id}", quote);
        });
        return app;
    }
}

public sealed record CreateMatchRequest(string ExternalId, string Competition, string HomeTeam, string AwayTeam, DateTimeOffset KickoffAt);
public sealed record CreateOddsRequest(
    string Bookmaker,
    string Market,
    string Selection,
    decimal DecimalOdds,
    DateTimeOffset? CapturedAt = null);
