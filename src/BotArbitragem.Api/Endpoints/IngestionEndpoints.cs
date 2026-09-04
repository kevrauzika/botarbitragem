using BotArbitragem.Application.Abstractions;

namespace BotArbitragem.Api.Endpoints;

public static class IngestionEndpoints
{
    public static IEndpointRouteBuilder MapIngestionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/ingestion/{sportKey}", async (string sportKey, HttpRequest request,
            IConfiguration configuration, IOddsIngestionService service, CancellationToken ct) =>
        {
            var configuredKey = configuration["Administration:ApiKey"];
            if (string.IsNullOrWhiteSpace(configuredKey))
                return Results.Problem("Administration:ApiKey não configurada.", statusCode: StatusCodes.Status503ServiceUnavailable);
            if (!request.Headers.TryGetValue("X-Admin-Key", out var suppliedKey) ||
                !string.Equals(suppliedKey.ToString(), configuredKey, StringComparison.Ordinal))
                return Results.Unauthorized();

            try
            {
                return Results.Ok(await service.ImportAsync(sportKey, ct));
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new { message = exception.Message });
            }
        }).WithTags("Ingestion");
        return app;
    }
}
