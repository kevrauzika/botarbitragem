using System.Security.Cryptography;
using System.Text;

namespace BotArbitragem.Api.Security;

public static class AdminApiKeyValidator
{
    public static AdminKeyValidationResult Validate(HttpRequest request, IConfiguration configuration)
    {
        var configured = configuration["Administration:ApiKey"];
        if (string.IsNullOrWhiteSpace(configured)) return AdminKeyValidationResult.NotConfigured;
        if (!request.Headers.TryGetValue("X-Admin-Key", out var supplied)) return AdminKeyValidationResult.Invalid;

        var suppliedHash = SHA256.HashData(Encoding.UTF8.GetBytes(supplied.ToString()));
        var configuredHash = SHA256.HashData(Encoding.UTF8.GetBytes(configured));
        return CryptographicOperations.FixedTimeEquals(suppliedHash, configuredHash)
            ? AdminKeyValidationResult.Valid
            : AdminKeyValidationResult.Invalid;
    }
}

public enum AdminKeyValidationResult { Valid, Invalid, NotConfigured }

