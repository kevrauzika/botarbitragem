using BotArbitragem.Application.Contracts;

namespace BotArbitragem.Application.Abstractions;

public interface IOpportunityPublisher
{
    Task<OpportunityPublicationResult?> PublishMatchAsync(Guid matchId, CancellationToken cancellationToken);
}

