using BotArbitragem.Application.Contracts;

namespace BotArbitragem.Application.Abstractions;

public interface IBetTrackingService
{
    Task<BetView> CreateAsync(CreateBetCommand command, CancellationToken cancellationToken);
    Task<BetView> SettleAsync(Guid id, SettleBetCommand command, CancellationToken cancellationToken);
    Task<BetView> VoidAsync(Guid id, string? notes, CancellationToken cancellationToken);
}
