using BotArbitragem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BotArbitragem.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202609040004_BetTracking")]
public sealed class BetTracking : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("bet_records", table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            OpportunityId = table.Column<Guid>(type: "uuid", nullable: false),
            MatchId = table.Column<Guid>(type: "uuid", nullable: false),
            Mode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
            Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
            Stake = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
            PotentialReturn = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false),
            Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
            ReturnAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
            ProfitLoss = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: true),
            PlacedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            SettledAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_bet_records", x => x.Id);
            table.ForeignKey("FK_bet_records_matches_MatchId", x => x.MatchId, "matches", "Id",
                onDelete: ReferentialAction.Restrict);
            table.ForeignKey("FK_bet_records_opportunities_OpportunityId", x => x.OpportunityId, "opportunities",
                "Id", onDelete: ReferentialAction.Restrict);
        });

        migrationBuilder.CreateTable("bet_legs", table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            BetRecordId = table.Column<Guid>(type: "uuid", nullable: false),
            Bookmaker = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            Selection = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
            DecimalOdds = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
            StakeAmount = table.Column<decimal>(type: "numeric(14,2)", precision: 14, scale: 2, nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_bet_legs", x => x.Id);
            table.ForeignKey("FK_bet_legs_bet_records_BetRecordId", x => x.BetRecordId, "bet_records", "Id",
                onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateIndex("IX_bet_records_MatchId", "bet_records", "MatchId");
        migrationBuilder.CreateIndex("IX_bet_records_OpportunityId", "bet_records", "OpportunityId");
        migrationBuilder.CreateIndex("IX_bet_records_Status_PlacedAt", "bet_records", ["Status", "PlacedAt"]);
        migrationBuilder.CreateIndex("IX_bet_legs_BetRecordId", "bet_legs", "BetRecordId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("bet_legs");
        migrationBuilder.DropTable("bet_records");
    }
}
