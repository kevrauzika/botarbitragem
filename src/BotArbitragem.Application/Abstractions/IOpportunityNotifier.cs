using BotArbitragem.Domain.Entities;

namespace BotArbitragem.Application.Abstractions;

public interface IOpportunityNotifier
{
    Task<bool> NotifyAsync(Opportunity opportunity, FootballMatch match, CancellationToken cancellationToken);
}
