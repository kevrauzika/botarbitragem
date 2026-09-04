using BotArbitragem.Application.Contracts;

namespace BotArbitragem.Application.Abstractions;

public interface IOpportunityScanService
{
    Task<OpportunityScanResult> ScanAsync(CancellationToken cancellationToken);
}
