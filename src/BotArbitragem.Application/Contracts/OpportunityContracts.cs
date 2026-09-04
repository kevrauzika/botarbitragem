using BotArbitragem.Domain.Entities;

namespace BotArbitragem.Application.Contracts;

public sealed record OpportunityCandidate(
    Guid MatchId,
    string Fingerprint,
    string Kind,
    string Market,
    string? Selection,
    decimal? EstimatedProbability,
    decimal? MarketOdds,
    decimal? ExpectedValue,
    decimal? Edge,
    decimal? ProfitPercentage,
    DateTimeOffset ExpiresAt,
    IReadOnlyList<OpportunityLegCandidate> Legs);

public sealed record OpportunityLegCandidate(string Bookmaker, string Selection, decimal DecimalOdds,
    decimal StakePercentage);

public sealed record OpportunityScanResult(int MatchesScanned, int QualifiedCandidates, int Created,
    int Refreshed, int Expired, int NotificationsSent);

public sealed record OpportunityUpsertResult(Opportunity Opportunity, bool Created, bool ShouldNotify);

public sealed record OpportunityView(
    Guid Id,
    Guid MatchId,
    string Kind,
    string Market,
    string? Selection,
    string Status,
    string Competition,
    string HomeTeam,
    string AwayTeam,
    DateTimeOffset KickoffAt,
    decimal? EstimatedProbability,
    decimal? MarketOdds,
    decimal? ExpectedValue,
    decimal? Edge,
    decimal? ProfitPercentage,
    DateTimeOffset DetectedAt,
    DateTimeOffset LastSeenAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? NotifiedAt,
    IReadOnlyList<OpportunityLegView> Legs);

public sealed record OpportunityLegView(string Bookmaker, string Selection, decimal DecimalOdds,
    decimal StakePercentage);
