using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Models;
using BotArbitragem.Application.Services;
using BotArbitragem.Infrastructure.Automation;
using BotArbitragem.Infrastructure.Health;
using BotArbitragem.Infrastructure.Notifications;
using BotArbitragem.Infrastructure.Persistence;
using BotArbitragem.Infrastructure.Notifications.Telegram;
using BotArbitragem.Infrastructure.Providers.TheOddsApi;
using BotArbitragem.Infrastructure.Providers.GanheMaisBet;
using BotArbitragem.Infrastructure.Providers.ApiFootball;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using PublicationTelegramOptions = BotArbitragem.Infrastructure.Notifications.Telegram.TelegramOptions;
using ScanTelegramOptions = BotArbitragem.Infrastructure.Notifications.TelegramOptions;

namespace BotArbitragem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' não configurada.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));
        services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgres");
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<IOpportunityRepository, OpportunityRepository>();
        services.AddScoped<IBetRepository, BetRepository>();
        services.AddScoped<IBetTrackingService, BetTrackingService>();
        services.AddScoped<IAlertDeduplicator, AlertDeduplicator>();
        services.AddScoped<IOddsIngestionService, OddsIngestionService>();
        services.AddScoped<IOpportunityScanService, OpportunityScanService>();
        services.Configure<TheOddsApiOptions>(configuration.GetSection(TheOddsApiOptions.SectionName));
        services.Configure<GanheMaisBetOptions>(configuration.GetSection(GanheMaisBetOptions.SectionName));
        services.Configure<ApiFootballOptions>(configuration.GetSection(ApiFootballOptions.SectionName));
        services.Configure<OpportunityPolicy>(configuration.GetSection("OpportunityPolicy"));
        services.Configure<AutomationOptions>(configuration.GetSection("Automation"));
        if (!string.IsNullOrWhiteSpace(configuration["ApiFootball:ApiKey"]))
        {
            services.AddHttpClient<IOddsProvider, ApiFootballClient>((serviceProvider, client) =>
                {
                    var provider = serviceProvider.GetRequiredService<IOptions<ApiFootballOptions>>().Value;
                    client.BaseAddress = new Uri(provider.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(30);
                })
                .RemoveAllLoggers();
        }
        else if (!string.IsNullOrWhiteSpace(configuration["GanheMaisBet:ApiKey"]))
        {
            services.AddHttpClient<IOddsProvider, GanheMaisBetClient>((serviceProvider, client) =>
                {
                    var provider = serviceProvider.GetRequiredService<IOptions<GanheMaisBetOptions>>().Value;
                    client.BaseAddress = new Uri(provider.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(30);
                })
                .RemoveAllLoggers();
        }
        else
        {
            services.AddHttpClient<IOddsProvider, TheOddsApiClient>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<TheOddsApiOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            })
            // O provedor exige a chave na query string. Desabilitar o logger HTTP impede vazamento da URL.
            .RemoveAllLoggers();
        }
        services.Configure<PublicationTelegramOptions>(configuration.GetSection(PublicationTelegramOptions.SectionName));
        services.Configure<ScanTelegramOptions>(configuration.GetSection(ScanTelegramOptions.SectionName));
        services.AddHttpClient<IGroupNotifier, TelegramGroupNotifier>(client =>
            {
                client.BaseAddress = new Uri("https://api.telegram.org/");
                client.Timeout = TimeSpan.FromSeconds(15);
            })
            // O token faz parte da URL exigida pelo Telegram e jamais deve aparecer em logs.
            .RemoveAllLoggers();
        services.AddHttpClient<IOpportunityNotifier, TelegramOpportunityNotifier>(client =>
            {
                client.BaseAddress = new Uri("https://api.telegram.org/");
                client.Timeout = TimeSpan.FromSeconds(15);
            })
            .RemoveAllLoggers();
        services.AddHostedService<OpportunityAutomationWorker>();
        return services;
    }
}
