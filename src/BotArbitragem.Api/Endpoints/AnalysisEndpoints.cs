using BotArbitragem.Application.Contracts;
using BotArbitragem.Application.Services;

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
        return app;
    }
}

public sealed record ExpectedValueRequest(decimal EstimatedProbability, decimal MarketOdds);
