namespace BotArbitragem.Application.Contracts;

public sealed record CreateBetCommand(Guid OpportunityId, decimal Stake, string Currency, string Mode, string? Notes);
public sealed record SettleBetCommand(decimal ReturnAmount, string? Notes);

public sealed record BetView(Guid Id, Guid OpportunityId, Guid MatchId, string Mode, string Currency, decimal Stake,
    decimal PotentialReturn, string Status, decimal? ReturnAmount, decimal? ProfitLoss, DateTimeOffset PlacedAt,
    DateTimeOffset? SettledAt, string? Notes, string Competition, string HomeTeam, string AwayTeam,
    DateTimeOffset KickoffAt, IReadOnlyList<BetLegView> Legs);

public sealed record BetLegView(string Bookmaker, string Selection, decimal DecimalOdds, decimal StakeAmount);

public sealed record PerformanceSummary(string Currency, int TotalRecords, int PendingRecords, int SettledRecords,
    decimal TotalStaked, decimal PendingExposure, decimal SettledStake, decimal TotalReturn, decimal ProfitLoss,
    decimal? ReturnOnInvestment);
