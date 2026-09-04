using BotArbitragem.Application.Abstractions;
using BotArbitragem.Domain.Entities;
using BotArbitragem.Api.Security;

namespace BotArbitragem.Api.Endpoints;

public static class MatchEndpoints
{
    public static IEndpointRouteBuilder MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/matches").WithTags("Matches");
        group.MapGet("/", async (DateTimeOffset? from, DateTimeOffset? to, IMatchRepository repository, CancellationToken ct) =>
            Results.Ok(await repository.ListAsync(from, to, ct)));
        group.MapGet("/{id:guid}", async (Guid id, IMatchRepository repository, CancellationToken ct) =>
            await repository.GetByIdAsync(id, ct) is { } match ? Results.Ok(match) : Results.NotFound());
        group.MapPost("/", async (CreateMatchRequest request, IMatchRepository repository, CancellationToken ct) =>
        {
            if (string.IsNullOrWhiteSpace(request.ExternalId) || string.IsNullOrWhiteSpace(request.HomeTeam) || string.IsNullOrWhiteSpace(request.AwayTeam) || string.IsNullOrWhiteSpace(request.Competition))
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["match"] = ["Todos os campos são obrigatórios."] });
            if (await repository.GetByExternalIdAsync(request.ExternalId, ct) is not null)
                return Results.Conflict(new { message = "Partida já cadastrada." });
            var match = new FootballMatch(request.ExternalId, request.Competition, request.HomeTeam, request.AwayTeam, request.KickoffAt);
            await repository.AddAsync(match, ct);
            return Results.Created($"/api/matches/{match.Id}", match);
        }).AddEndpointFilter<AdminApiKeyEndpointFilter>();
        group.MapPost("/{id:guid}/odds", async (Guid id, CreateOddsRequest request, IMatchRepository repository, CancellationToken ct) =>
        {
            if (await repository.GetByIdAsync(id, ct) is null) return Results.NotFound();
            if (request.DecimalOdds <= 1m) return Results.ValidationProblem(new Dictionary<string, string[]> { ["decimalOdds"] = ["A odd deve ser maior que 1."] });
            var quote = new OddsQuote(id, request.Bookmaker, request.Market, request.Selection, request.DecimalOdds, DateTimeOffset.UtcNow);
            if (!await repository.AddOddsIfNewAsync(quote, ct))
                return Results.Conflict(new { message = "Cotação já registrada para este instante." });
            return Results.Created($"/api/matches/{id}", quote);
        }).AddEndpointFilter<AdminApiKeyEndpointFilter>();
        return app;
    }
}

public sealed record CreateMatchRequest(string ExternalId, string Competition, string HomeTeam, string AwayTeam, DateTimeOffset KickoffAt);
public sealed record CreateOddsRequest(string Bookmaker, string Market, string Selection, decimal DecimalOdds);
