using BotArbitragem.Api.Security;
using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Exceptions;

namespace BotArbitragem.Api.Endpoints;

public static class IngestionEndpoints
{
    public static IEndpointRouteBuilder MapIngestionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/ingestion/{sportKey}", async (string sportKey, HttpRequest request,
            IConfiguration configuration, IOddsIngestionService service, CancellationToken ct) =>
        {
            var authorization = AdminApiKeyValidator.Validate(request, configuration);
            if (authorization == AdminKeyValidationResult.NotConfigured)
                return Results.Problem("Administration:ApiKey não configurada.", statusCode: StatusCodes.Status503ServiceUnavailable);
            if (authorization != AdminKeyValidationResult.Valid)
                return Results.Unauthorized();

            try
            {
                return Results.Ok(await service.ImportAsync(sportKey, ct));
            }
            catch (ArgumentException exception)
            {
                return Results.BadRequest(new { message = exception.Message });
            }
            catch (OddsProviderException)
            {
                return Results.Problem("O provedor de odds está temporariamente indisponível.",
                    statusCode: StatusCodes.Status502BadGateway);
            }
            catch (InvalidOperationException)
            {
                return Results.Problem("O provedor de odds não está configurado.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }).WithTags("Ingestion").RequireRateLimiting("admin-api");
        return app;
    }

}
