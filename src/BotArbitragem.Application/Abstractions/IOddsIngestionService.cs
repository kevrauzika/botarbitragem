using BotArbitragem.Application.Contracts;

namespace BotArbitragem.Application.Abstractions;

public interface IOddsIngestionService
{
    Task<IngestionResult> ImportAsync(string sportKey, CancellationToken cancellationToken);
}
