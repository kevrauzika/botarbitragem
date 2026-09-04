using BotArbitragem.Application.Abstractions;
using BotArbitragem.Api.Security;

namespace BotArbitragem.Api.Endpoints;

public static class OpportunityEndpoints
{
    public static IEndpointRouteBuilder MapOpportunityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/opportunities").WithTags("Opportunities");
        group.MapGet("/", async (string? status, string? kind, IOpportunityRepository repository,
            CancellationToken ct) => Results.Ok(await repository.ListAsync(status, kind, ct)));
        group.MapGet("/{id:guid}", async (Guid id, IOpportunityRepository repository, CancellationToken ct) =>
            await repository.GetByIdAsync(id, ct) is { } opportunity
                ? Results.Ok(opportunity)
                : Results.NotFound());

        app.MapPost("/api/admin/opportunities/scan", async (IOpportunityScanService scanner,
            CancellationToken ct) =>
        {
            return Results.Ok(await scanner.ScanAsync(ct));
        }).AddEndpointFilter<AdminApiKeyEndpointFilter>().WithTags("Opportunities");
        return app;
    }
}
