using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Contracts;
using BotArbitragem.Application.Services;
using BotArbitragem.Application.Models;
using Microsoft.Extensions.Options;

namespace BotArbitragem.Api.Endpoints;

public static class AnalysisEndpoints
{
    public static IEndpointRouteBuilder MapAnalysisEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/analysis/expected-value", (ExpectedValueRequest request) =>
        {
            if (request.EstimatedProbability is < 0m or > 1m || request.MarketOdds <= 1m)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["analysis"] = ["Probabilidade deve estar entre 0 e 1 e a odd deve ser maior que 1."] });
            var ev = ExpectedValueCalculator.Calculate(request.EstimatedProbability, request.MarketOdds);
            var result = new ValueBetResult(request.EstimatedProbability, request.MarketOdds, ev,
                ExpectedValueCalculator.CalculateImpliedProbability(request.MarketOdds),
                ExpectedValueCalculator.CalculateEdge(request.EstimatedProbability, request.MarketOdds), ev > 0m);
            return Results.Ok(result);
        }).WithTags("Analysis");

        app.MapPost("/api/analysis/no-vig/1x2", (ThreeWayOddsRequest request) =>
        {
            if (request.HomeOdds <= 1m || request.DrawOdds <= 1m || request.AwayOdds <= 1m)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["odds"] = ["Todas as odds devem ser maiores que 1."] });
            return Results.Ok(NoVigProbabilityCalculator.CalculateThreeWay(request.HomeOdds, request.DrawOdds, request.AwayOdds));
        }).WithTags("Analysis");

        app.MapPost("/api/analysis/value-bet", (ExpectedValueRequest request, IOptions<ValueBetPolicy> policy) =>
        {
            if (request.EstimatedProbability is < 0m or > 1m || request.MarketOdds <= 1m)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["analysis"] = ["Probabilidade deve estar entre 0 e 1 e a odd deve ser maior que 1."] });
            return Results.Ok(ValueBetEvaluator.Evaluate(request.EstimatedProbability, request.MarketOdds, policy.Value));
        }).WithTags("Analysis");

        app.MapGet("/api/analysis/matches/{id:guid}/value-bets", async (
            Guid id,
            IMatchRepository repository,
            IOptions<ValueBetPolicy> valueBetPolicy,
            IOptions<OpportunityAnalysisPolicy> analysisPolicy,
            TimeProvider timeProvider,
            CancellationToken ct) =>
        {
            var analyzedAt = timeProvider.GetUtcNow();
            var match = await repository.GetByIdWithOddsAsync(
                id,
                analyzedAt.AddMinutes(-analysisPolicy.Value.MaximumQuoteAgeMinutes),
                analyzedAt.AddMinutes(analysisPolicy.Value.MaximumFutureSkewMinutes),
                ct);
            if (match is null) return Results.NotFound();

            var result = ValueOpportunityAnalyzer.Analyze(
                match,
                valueBetPolicy.Value,
                analysisPolicy.Value,
                analyzedAt);
            return Results.Ok(result);
        }).WithTags("Analysis");
        return app;
    }
}

public sealed record ExpectedValueRequest(decimal EstimatedProbability, decimal MarketOdds);
public sealed record ThreeWayOddsRequest(decimal HomeOdds, decimal DrawOdds, decimal AwayOdds);
