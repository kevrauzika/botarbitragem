using BotArbitragem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BotArbitragem.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202609040001_InitialCreate")]
public sealed class InitialCreate : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("matches", table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            ExternalId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            Competition = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
            HomeTeam = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
            AwayTeam = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
            KickoffAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table => table.PrimaryKey("PK_matches", x => x.Id));

        migrationBuilder.CreateTable("odds_quotes", table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            MatchId = table.Column<Guid>(type: "uuid", nullable: false),
            Bookmaker = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            Market = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            Selection = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            DecimalOdds = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
            CapturedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_odds_quotes", x => x.Id);
            table.ForeignKey("FK_odds_quotes_matches_MatchId", x => x.MatchId, "matches", "Id", onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateIndex("IX_matches_ExternalId", "matches", "ExternalId", unique: true);
        migrationBuilder.CreateIndex("IX_odds_quotes_MatchId", "odds_quotes", "MatchId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("odds_quotes");
        migrationBuilder.DropTable("matches");
    }
}
