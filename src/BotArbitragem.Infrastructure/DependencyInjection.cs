using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Services;
using BotArbitragem.Application.Models;
using BotArbitragem.Infrastructure.Automation;
using BotArbitragem.Infrastructure.Health;
using BotArbitragem.Infrastructure.Notifications;
using BotArbitragem.Infrastructure.Persistence;
using BotArbitragem.Infrastructure.Providers.TheOddsApi;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        services.AddScoped<IOddsIngestionService, OddsIngestionService>();
        services.AddScoped<IOpportunityScanService, OpportunityScanService>();
        services.Configure<TheOddsApiOptions>(configuration.GetSection(TheOddsApiOptions.SectionName));
        services.Configure<OpportunityPolicy>(configuration.GetSection("OpportunityPolicy"));
        services.Configure<AutomationOptions>(configuration.GetSection("Automation"));
        services.Configure<TelegramOptions>(configuration.GetSection(TelegramOptions.SectionName));
        services.AddHttpClient<IOddsProvider, TheOddsApiClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<TheOddsApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient<IOpportunityNotifier, TelegramOpportunityNotifier>(client =>
        {
            client.BaseAddress = new Uri("https://api.telegram.org");
            client.Timeout = TimeSpan.FromSeconds(15);
        });
        services.AddHostedService<OpportunityAutomationWorker>();
        return services;
    }
}
