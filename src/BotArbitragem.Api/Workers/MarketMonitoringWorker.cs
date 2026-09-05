using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Exceptions;
using BotArbitragem.Application.Models;
using Microsoft.Extensions.Options;

namespace BotArbitragem.Api.Workers;

public sealed class MarketMonitoringWorker(
    IServiceScopeFactory scopeFactory,
    IOptions<MonitoringOptions> monitoringOptions,
    TimeProvider timeProvider,
    ILogger<MarketMonitoringWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = monitoringOptions.Value;
        if (!settings.Enabled)
        {
            logger.LogInformation("Monitoramento automático de odds está desativado.");
            return;
        }

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
                logger.LogError("Ciclo de monitoramento falhou com {ErrorType}.", exception.GetType().Name);
            }

            await Task.Delay(TimeSpan.FromMinutes(settings.IntervalMinutes), timeProvider, stoppingToken);
        }
    }

    private async Task RunCycleAsync(MonitoringOptions settings, CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var ingestion = scope.ServiceProvider.GetRequiredService<IOddsIngestionService>();
        var repository = scope.ServiceProvider.GetRequiredService<IMatchRepository>();
        var publisher = scope.ServiceProvider.GetRequiredService<IOpportunityPublisher>();

        foreach (var sportKey in settings.SportKeys)
        {
            try
            {
                await ingestion.ImportAsync(sportKey, cancellationToken);
            }
            catch (Exception exception) when (exception is OddsProviderException or ArgumentException or InvalidOperationException)
            {
                logger.LogWarning("Ingestão de {SportKey} falhou com {ErrorType}.", sportKey, exception.GetType().Name);
            }
        }

        var now = timeProvider.GetUtcNow();
        var processed = 0;
        var pageSize = Math.Min(settings.MaximumMatchesPerCycle, 100);
        var page = 1;
        while (processed < settings.MaximumMatchesPerCycle)
        {
            var matches = await repository.ListAsync(now, now.AddHours(settings.LookAheadHours), page, pageSize, cancellationToken);
            if (matches.Items.Count == 0) break;

            foreach (var match in matches.Items.Take(settings.MaximumMatchesPerCycle - processed))
            {
                try
                {
                    await publisher.PublishMatchAsync(match.Id, cancellationToken);
                }
                catch (GroupNotificationException)
                {
                    logger.LogWarning("Telegram indisponível; publicação será tentada no próximo ciclo.");
                    return;
                }
            }

            processed += matches.Items.Count;
            if (page >= matches.TotalPages) break;
            page++;
        }
    }
}
