using BotArbitragem.Api.Security;
using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Contracts;

namespace BotArbitragem.Api.Endpoints;

public static class BetEndpoints
{
    public static IEndpointRouteBuilder MapBetEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/bets").WithTags("Bet tracking");
        group.MapGet("/", async (string? status, string? mode, IBetRepository repository,
            CancellationToken ct) => Results.Ok(await repository.ListAsync(status, mode, ct)));
        group.MapGet("/performance", async (IBetRepository repository, CancellationToken ct) =>
            Results.Ok(await repository.GetPerformanceAsync(ct)));

        group.MapPost("/", async (CreateBetCommand command, IBetTrackingService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.CreateAsync(command, ct)); }
            catch (KeyNotFoundException exception) { return Results.NotFound(new { message = exception.Message }); }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            { return Results.BadRequest(new { message = exception.Message }); }
        }).AddEndpointFilter<AdminApiKeyEndpointFilter>();

        group.MapPost("/{id:guid}/settle", async (Guid id, SettleBetCommand command,
            IBetTrackingService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.SettleAsync(id, command, ct)); }
            catch (KeyNotFoundException exception) { return Results.NotFound(new { message = exception.Message }); }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            { return Results.BadRequest(new { message = exception.Message }); }
        }).AddEndpointFilter<AdminApiKeyEndpointFilter>();

        group.MapPost("/{id:guid}/void", async (Guid id, VoidBetRequest request,
            IBetTrackingService service, CancellationToken ct) =>
        {
            try { return Results.Ok(await service.VoidAsync(id, request.Notes, ct)); }
            catch (KeyNotFoundException exception) { return Results.NotFound(new { message = exception.Message }); }
            catch (InvalidOperationException exception) { return Results.BadRequest(new { message = exception.Message }); }
        }).AddEndpointFilter<AdminApiKeyEndpointFilter>();
        return app;
    }
}

public sealed record VoidBetRequest(string? Notes);
