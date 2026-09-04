using BotArbitragem.Application.Abstractions;
using BotArbitragem.Api.Security;

namespace BotArbitragem.Api.Endpoints;

public static class IngestionEndpoints
{
    public static IEndpointRouteBuilder MapIngestionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/ingestion/{sportKey}", async (string sportKey,
            IOddsIngestionService service, CancellationToken ct) =>
        {
            try
            {
                return Results.Ok(await service.ImportAsync(sportKey, ct));
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new { message = exception.Message });
            }
        }).AddEndpointFilter<AdminApiKeyEndpointFilter>().WithTags("Ingestion");
        return app;
    }
}
