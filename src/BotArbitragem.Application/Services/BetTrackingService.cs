using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Contracts;
using BotArbitragem.Domain.Entities;

namespace BotArbitragem.Application.Services;

public sealed class BetTrackingService(IOpportunityRepository opportunityRepository, IBetRepository betRepository)
    : IBetTrackingService
{
    public async Task<BetView> CreateAsync(CreateBetCommand command, CancellationToken cancellationToken)
    {
        if (command.Stake <= 0m) throw new ArgumentException("A stake deve ser maior que zero.");
        if (string.IsNullOrWhiteSpace(command.Currency) || command.Currency.Trim().Length != 3)
            throw new ArgumentException("Informe uma moeda com três letras, por exemplo BRL.");
        if (command.Mode is not ("paper" or "actual"))
            throw new ArgumentException("O modo deve ser paper ou actual.");

        var opportunity = await opportunityRepository.GetByIdAsync(command.OpportunityId, cancellationToken)
            ?? throw new KeyNotFoundException("Oportunidade não encontrada.");
        if (opportunity.Status != "active") throw new InvalidOperationException("A oportunidade não está ativa.");
        if (opportunity.Legs.Count == 0) throw new InvalidOperationException("A oportunidade não possui pernas.");

        var potentialReturn = opportunity.Kind == "arbitrage"
            ? command.Stake * (1m + opportunity.ProfitPercentage!.Value)
            : command.Stake * opportunity.Legs[0].DecimalOdds;
        var record = new BetRecord(opportunity.Id, opportunity.MatchId, command.Mode, command.Currency.Trim(),
            command.Stake, potentialReturn, DateTimeOffset.UtcNow, command.Notes);
        var allocated = 0m;
        for (var index = 0; index < opportunity.Legs.Count; index++)
        {
            var leg = opportunity.Legs[index];
            var legStake = index == opportunity.Legs.Count - 1
                ? command.Stake - allocated
                : decimal.Round(command.Stake * leg.StakePercentage / 100m, 2, MidpointRounding.ToZero);
            record.AddLeg(leg.Bookmaker, leg.Selection, leg.DecimalOdds, legStake);
            allocated += legStake;
        }
        await betRepository.AddAsync(record, cancellationToken);
        return (await betRepository.ListAsync(null, null, cancellationToken)).First(x => x.Id == record.Id);
    }

    public async Task<BetView> SettleAsync(Guid id, SettleBetCommand command, CancellationToken cancellationToken)
    {
        var record = await GetAsync(id, cancellationToken);
        record.Settle(command.ReturnAmount, DateTimeOffset.UtcNow, command.Notes);
        await betRepository.SaveChangesAsync(cancellationToken);
        return (await betRepository.ListAsync(null, null, cancellationToken)).First(x => x.Id == id);
    }

    public async Task<BetView> VoidAsync(Guid id, string? notes, CancellationToken cancellationToken)
    {
        var record = await GetAsync(id, cancellationToken);
        record.Void(DateTimeOffset.UtcNow, notes);
        await betRepository.SaveChangesAsync(cancellationToken);
        return (await betRepository.ListAsync(null, null, cancellationToken)).First(x => x.Id == id);
    }

    private async Task<BetRecord> GetAsync(Guid id, CancellationToken cancellationToken) =>
        await betRepository.GetTrackedAsync(id, cancellationToken) ??
        throw new KeyNotFoundException("Registro não encontrado.");
}
