namespace BotArbitragem.Domain;

internal static class Guard
{
    public static string RequiredText(string? value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("O valor é obrigatório.", parameterName);

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
            throw new ArgumentException($"O valor deve ter no máximo {maximumLength} caracteres.", parameterName);

        return normalized;
    }
}

