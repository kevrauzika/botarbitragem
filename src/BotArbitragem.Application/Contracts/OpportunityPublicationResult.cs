namespace BotArbitragem.Application.Contracts;

public sealed record OpportunityPublicationResult(
    Guid MatchId,
    int OpportunitiesFound,
    int MessagesSent,
    int DuplicatesSkipped);

