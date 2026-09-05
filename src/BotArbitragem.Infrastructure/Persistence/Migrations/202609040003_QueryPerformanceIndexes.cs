using BotArbitragem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BotArbitragem.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202609040003_QueryPerformanceIndexes")]
public sealed class QueryPerformanceIndexes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateIndex("IX_matches_KickoffAt", "matches", "KickoffAt");
        migrationBuilder.CreateIndex(
            "IX_odds_quotes_MatchId_CapturedAt",
            "odds_quotes",
            ["MatchId", "CapturedAt"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex("IX_matches_KickoffAt", "matches");
        migrationBuilder.DropIndex("IX_odds_quotes_MatchId_CapturedAt", "odds_quotes");
    }
}

