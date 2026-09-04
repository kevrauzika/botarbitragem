using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Services;
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
        services.AddScoped<IMatchRepository, MatchRepository>();
        services.AddScoped<IOddsIngestionService, OddsIngestionService>();
        services.Configure<TheOddsApiOptions>(configuration.GetSection(TheOddsApiOptions.SectionName));
        services.AddHttpClient<IOddsProvider, TheOddsApiClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<TheOddsApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        return services;
    }
}
