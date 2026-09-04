using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Models;
using BotArbitragem.Infrastructure.Providers.TheOddsApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BotArbitragem.Infrastructure.Automation;

public sealed class OpportunityAutomationWorker(IServiceScopeFactory scopeFactory,
    IOptions<AutomationOptions> automationOptions,
    IOptions<TheOddsApiOptions> providerOptions,
    ILogger<OpportunityAutomationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = automationOptions.Value;
        var interval = TimeSpan.FromSeconds(Math.Max(30, settings.IntervalSeconds));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunCycleAsync(settings, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Falha no ciclo automático de odds.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task RunCycleAsync(AutomationOptions settings, CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        if (settings.IngestionEnabled)
        {
            if (string.IsNullOrWhiteSpace(providerOptions.Value.ApiKey))
            {
                logger.LogWarning("Coleta automática habilitada, mas OddsProvider:ApiKey não foi configurada.");
            }
            else
            {
                var ingestion = scope.ServiceProvider.GetRequiredService<IOddsIngestionService>();
                foreach (var sport in providerOptions.Value.AllowedSports)
                {
                    try
                    {
                        var result = await ingestion.ImportAsync(sport, cancellationToken);
                        logger.LogInformation("Coleta {Sport}: {Events} eventos e {Odds} odds novas.", sport,
                            result.EventsReceived, result.OddsCreated);
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Falha ao coletar a liga {Sport}.", sport);
                    }
                }
            }
        }

        if (!settings.ScanEnabled) return;
        var scanner = scope.ServiceProvider.GetRequiredService<IOpportunityScanService>();
        var scan = await scanner.ScanAsync(cancellationToken);
        logger.LogInformation("Análise: {Matches} partidas, {Candidates} oportunidades e {Notifications} alertas.",
            scan.MatchesScanned, scan.QualifiedCandidates, scan.NotificationsSent);
    }
}
