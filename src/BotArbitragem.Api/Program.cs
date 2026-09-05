using BotArbitragem.Api.Endpoints;
using BotArbitragem.Api.Workers;
using BotArbitragem.Application.Abstractions;
using BotArbitragem.Application.Models;
using BotArbitragem.Application.Services;
using BotArbitragem.Infrastructure;
using BotArbitragem.Infrastructure.Notifications.Telegram;
using BotArbitragem.Infrastructure.Persistence;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port &&
    string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("ASPNETCORE_URLS")))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddOptions<ValueBetPolicy>()
    .Bind(builder.Configuration.GetSection("ValueBetPolicy"))
    .Validate(policy => policy.MinimumExpectedValue >= 0m &&
        policy.MinimumEdge >= 0m &&
        policy.MinimumEstimatedProbability is >= 0m and <= 1m &&
        policy.MinimumOdds > 1m &&
        policy.MaximumOdds >= policy.MinimumOdds,
        "A configuração ValueBetPolicy é inválida.")
    .ValidateOnStart();
builder.Services.AddOptions<OpportunityAnalysisPolicy>()
    .Bind(builder.Configuration.GetSection("OpportunityAnalysis"))
    .Validate(policy => policy.MinimumReferenceBookmakers >= 1 &&
        policy.MaximumQuoteAgeMinutes >= 1 &&
        policy.MaximumFutureSkewMinutes >= 0,
        "A configuração OpportunityAnalysis é inválida.")
    .ValidateOnStart();
builder.Services.AddOptions<TelegramOptions>()
    .Bind(builder.Configuration.GetSection(TelegramOptions.SectionName))
    .Validate(options => !options.Enabled ||
        (options.BotToken.Length >= 20 && !string.IsNullOrWhiteSpace(options.ChatId)),
        "Telegram habilitado sem BotToken e ChatId válidos.")
    .ValidateOnStart();
builder.Services.AddOptions<MonitoringOptions>()
    .Bind(builder.Configuration.GetSection("Monitoring"))
    .Validate(options => options.IntervalMinutes is >= 1 and <= 1_440 &&
        options.LookAheadHours is >= 1 and <= 168 &&
        options.MaximumMatchesPerCycle is >= 1 and <= 10_000,
        "A configuração Monitoring é inválida.")
    .Validate(options => !options.Enabled || builder.Configuration.GetValue<bool>("Telegram:Enabled"),
        "Monitoring exige que a integração Telegram esteja habilitada.")
    .ValidateOnStart();
builder.Services.AddScoped<IOpportunityPublisher>(serviceProvider => new OpportunityPublisher(
    serviceProvider.GetRequiredService<IMatchRepository>(),
    serviceProvider.GetRequiredService<IAlertDeduplicator>(),
    serviceProvider.GetRequiredService<IGroupNotifier>(),
    serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ValueBetPolicy>>().Value,
    serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OpportunityAnalysisPolicy>>().Value,
    serviceProvider.GetRequiredService<TimeProvider>()));
builder.Services.AddHostedService<MarketMonitoringWorker>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("admin-api", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}

app.MapGet("/", () => Results.Ok(new
{
    name = "Odds Analíticas 24/7 API",
    description = "Análise probabilística de odds de futebol.",
    disclaimer = "Dados informativos. Não há garantia de retorno."
}));
app.MapHealthChecks("/health");
app.MapMatchEndpoints();
app.MapAnalysisEndpoints();
app.MapIngestionEndpoints();
app.MapOpportunityEndpoints();
app.MapBetEndpoints();
app.MapPublicationEndpoints();
app.Run();

public partial class Program;
