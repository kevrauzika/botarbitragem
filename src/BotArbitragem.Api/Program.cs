var builder = WebApplication.CreateBuilder(args);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

var app = builder.Build();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapGet("/", () => Results.Ok(new
{
    name = "BotArbitragem API",
    description = "Análise probabilística de odds de futebol.",
    disclaimer = "Dados informativos. Não há garantia de retorno."
}));
app.MapHealthChecks("/health");
app.Run();

public partial class Program;