using System.Security.Cryptography;
using System.Text;

namespace BotArbitragem.Api.Security;

public sealed class AdminApiKeyEndpointFilter(IConfiguration configuration) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var configuredKey = configuration["Administration:ApiKey"];
        if (string.IsNullOrWhiteSpace(configuredKey))
            return Results.Problem("Administration:ApiKey não configurada.",
                statusCode: StatusCodes.Status503ServiceUnavailable);

        if (!context.HttpContext.Request.Headers.TryGetValue("X-Admin-Key", out var suppliedKey) ||
            !KeysMatch(configuredKey, suppliedKey.ToString())) return Results.Unauthorized();
        return await next(context);
    }

    private static bool KeysMatch(string expected, string supplied)
    {
        var expectedBytes = Encoding.UTF8.GetBytes(expected);
        var suppliedBytes = Encoding.UTF8.GetBytes(supplied);
        return expectedBytes.Length == suppliedBytes.Length &&
               CryptographicOperations.FixedTimeEquals(expectedBytes, suppliedBytes);
    }
}
