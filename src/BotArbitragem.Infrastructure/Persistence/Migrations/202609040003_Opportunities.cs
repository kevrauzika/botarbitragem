using BotArbitragem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BotArbitragem.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202609040003_Opportunities")]
public sealed class Opportunities : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("opportunities", table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            MatchId = table.Column<Guid>(type: "uuid", nullable: false),
            Fingerprint = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
            Kind = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
            Market = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
            Selection = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true),
            Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
            EstimatedProbability = table.Column<decimal>(type: "numeric(12,8)", precision: 12, scale: 8, nullable: true),
            MarketOdds = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: true),
            ExpectedValue = table.Column<decimal>(type: "numeric(12,8)", precision: 12, scale: 8, nullable: true),
            Edge = table.Column<decimal>(type: "numeric(12,8)", precision: 12, scale: 8, nullable: true),
            ProfitPercentage = table.Column<decimal>(type: "numeric(12,8)", precision: 12, scale: 8, nullable: true),
            DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
            ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
            NotifiedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_opportunities", x => x.Id);
            table.ForeignKey("FK_opportunities_matches_MatchId", x => x.MatchId, "matches", "Id",
                onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateTable("opportunity_legs", table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            OpportunityId = table.Column<Guid>(type: "uuid", nullable: false),
            Bookmaker = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
            Selection = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
            DecimalOdds = table.Column<decimal>(type: "numeric(10,4)", precision: 10, scale: 4, nullable: false),
            StakePercentage = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_opportunity_legs", x => x.Id);
            table.ForeignKey("FK_opportunity_legs_opportunities_OpportunityId", x => x.OpportunityId,
                "opportunities", "Id", onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateIndex("IX_opportunities_Fingerprint", "opportunities", "Fingerprint", unique: true);
        migrationBuilder.CreateIndex("IX_opportunities_MatchId", "opportunities", "MatchId");
        migrationBuilder.CreateIndex("IX_opportunities_Status_LastSeenAt", "opportunities", ["Status", "LastSeenAt"]);
        migrationBuilder.CreateIndex("IX_opportunity_legs_OpportunityId", "opportunity_legs", "OpportunityId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("opportunity_legs");
        migrationBuilder.DropTable("opportunities");
    }
}
