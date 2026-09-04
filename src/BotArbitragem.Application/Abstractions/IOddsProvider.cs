using BotArbitragem.Application.Contracts;

namespace BotArbitragem.Application.Abstractions;

public interface IOddsProvider
{
    Task<IReadOnlyList<ProviderEvent>> GetUpcomingOddsAsync(string sportKey, CancellationToken cancellationToken);
}
