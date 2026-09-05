using BotArbitragem.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BotArbitragem.Infrastructure.Persistence.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("202609040004_PublishedAlerts")]
public sealed class PublishedAlerts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable("published_alerts", table => new
        {
            Id = table.Column<Guid>(type: "uuid", nullable: false),
            Fingerprint = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
            MatchId = table.Column<Guid>(type: "uuid", nullable: false),
            CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
        }, constraints: table =>
        {
            table.PrimaryKey("PK_published_alerts", x => x.Id);
            table.ForeignKey("FK_published_alerts_matches_MatchId", x => x.MatchId, "matches", "Id", onDelete: ReferentialAction.Cascade);
        });

        migrationBuilder.CreateIndex("IX_published_alerts_Fingerprint", "published_alerts", "Fingerprint", unique: true);
        migrationBuilder.CreateIndex(
            "IX_published_alerts_MatchId_CreatedAt",
            "published_alerts",
            ["MatchId", "CreatedAt"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder) =>
        migrationBuilder.DropTable("published_alerts");
}

