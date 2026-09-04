using BotArbitragem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BotArbitragem.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202609040002_OddsIdempotency")]
public sealed class OddsIdempotency : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder) =>
        migrationBuilder.CreateIndex(
            "IX_odds_quotes_MatchId_Bookmaker_Market_Selection_CapturedAt",
            "odds_quotes",
            ["MatchId", "Bookmaker", "Market", "Selection", "CapturedAt"],
            unique: true);

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropIndex("IX_odds_quotes_MatchId_Bookmaker_Market_Selection_CapturedAt", "odds_quotes");
}
