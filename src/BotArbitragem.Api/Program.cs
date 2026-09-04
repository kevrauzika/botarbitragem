using BotArbitragem.Api.Endpoints;
using BotArbitragem.Application.Models;
using BotArbitragem.Infrastructure;
using BotArbitragem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.Configure<ValueBetPolicy>(builder.Configuration.GetSection("ValueBetPolicy"));

var app = builder.Build();
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
app.Run();

public partial class Program;
