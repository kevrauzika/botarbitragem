using BotArbitragem.Api.Security;
using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Exceptions;

namespace BotArbitragem.Api.Endpoints;

public static class PublicationEndpoints
{
    public static IEndpointRouteBuilder MapPublicationEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/admin/publication/matches/{id:guid}", async (
            Guid id,
            HttpRequest request,
            IConfiguration configuration,
            IOpportunityPublisher publisher,
            CancellationToken ct) =>
        {
            var authorization = Authorize(request, configuration);
            if (authorization is not null) return authorization;

            try
            {
                var result = await publisher.PublishMatchAsync(id, ct);
                return result is null ? Results.NotFound() : Results.Ok(result);
            }
            catch (GroupNotificationException)
            {
                return Results.Problem("O Telegram está temporariamente indisponível.",
                    statusCode: StatusCodes.Status502BadGateway);
            }
            catch (InvalidOperationException)
            {
                return Results.Problem("A integração com o Telegram não está configurada.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }).WithTags("Publication").RequireRateLimiting("admin-api");

        app.MapPost("/api/admin/telegram/test", async (
            HttpRequest request,
            IConfiguration configuration,
            IGroupNotifier notifier,
            CancellationToken ct) =>
        {
            var authorization = Authorize(request, configuration);
            if (authorization is not null) return authorization;

            try
            {
                await notifier.SendAsync("✅ BotArbitragem conectado com sucesso ao grupo.", ct);
                return Results.Ok(new { message = "Mensagem de teste enviada." });
            }
            catch (GroupNotificationException)
            {
                return Results.Problem("O Telegram rejeitou ou não recebeu a mensagem.",
                    statusCode: StatusCodes.Status502BadGateway);
            }
            catch (InvalidOperationException)
            {
                return Results.Problem("A integração com o Telegram não está configurada.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }).WithTags("Publication").RequireRateLimiting("admin-api");

        return app;
    }

    private static IResult? Authorize(HttpRequest request, IConfiguration configuration) =>
        AdminApiKeyValidator.Validate(request, configuration) switch
        {
            AdminKeyValidationResult.Valid => null,
            AdminKeyValidationResult.NotConfigured => Results.Problem(
                "Administration:ApiKey não configurada.",
                statusCode: StatusCodes.Status503ServiceUnavailable),
            _ => Results.Unauthorized()
        };
}
