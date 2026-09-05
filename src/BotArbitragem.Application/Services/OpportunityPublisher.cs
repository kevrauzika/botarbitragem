using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Contracts;
using BotArbitragem.Application.Models;
using BotArbitragem.Domain.Entities;

namespace BotArbitragem.Application.Services;

public sealed class OpportunityPublisher(
    IMatchRepository matchRepository,
    IAlertDeduplicator deduplicator,
    IGroupNotifier notifier,
    ValueBetPolicy valueBetPolicy,
    OpportunityAnalysisPolicy analysisPolicy,
    TimeProvider timeProvider) : IOpportunityPublisher
{
    public async Task<OpportunityPublicationResult?> PublishMatchAsync(Guid matchId, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var match = await matchRepository.GetByIdWithOddsAsync(
            matchId,
            now.AddMinutes(-analysisPolicy.MaximumQuoteAgeMinutes),
            now.AddMinutes(analysisPolicy.MaximumFutureSkewMinutes),
            cancellationToken);
        if (match is null) return null;

        var analysis = ValueOpportunityAnalyzer.Analyze(match, valueBetPolicy, analysisPolicy, now);
        var sent = 0;
        var skipped = 0;

        foreach (var opportunity in analysis.Opportunities)
        {
            var fingerprint = CreateFingerprint(match.Id, opportunity);
            if (!await deduplicator.TryReserveAsync(fingerprint, match.Id, now, cancellationToken))
            {
                skipped++;
                continue;
            }

            try
            {
                await notifier.SendAsync(FormatMessage(match, opportunity), cancellationToken);
                sent++;
            }
            catch
            {
                await deduplicator.ReleaseAsync(fingerprint, CancellationToken.None);
                throw;
            }
        }

        return new OpportunityPublicationResult(match.Id, analysis.Opportunities.Count, sent, skipped);
    }

    private static string CreateFingerprint(Guid matchId, ValueOpportunity opportunity)
    {
        var source = string.Join('|',
            matchId.ToString("N"),
            opportunity.Bookmaker.Trim().ToUpperInvariant(),
            opportunity.Market.Trim().ToUpperInvariant(),
            opportunity.Selection.Trim().ToUpperInvariant(),
            opportunity.MarketOdds.ToString(CultureInfo.InvariantCulture),
            opportunity.CapturedAt.UtcTicks.ToString(CultureInfo.InvariantCulture));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(source)));
    }

    private static string FormatMessage(FootballMatch match, ValueOpportunity opportunity) =>
        $"""
        ⚽ OPORTUNIDADE DE VALOR

        Jogo: {match.HomeTeam} x {match.AwayTeam}
        Competição: {match.Competition}
        Início: {match.KickoffAt.UtcDateTime:dd/MM/yyyy HH:mm} UTC

        Mercado: {opportunity.Market}
        Seleção: {opportunity.Selection}
        Casa: {opportunity.Bookmaker}
        Odd: {opportunity.MarketOdds:F2}
        Probabilidade justa: {opportunity.FairProbability:P1}
        EV estimado: {opportunity.ExpectedValue:P1}
        Edge: {opportunity.Edge:P1}
        Referências: {opportunity.ReferenceBookmakers} casas

        Dados informativos. Não há garantia de retorno.
        """;
}
