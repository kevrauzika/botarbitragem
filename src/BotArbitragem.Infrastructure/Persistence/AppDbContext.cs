using BotArbitragem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotArbitragem.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<FootballMatch> Matches => Set<FootballMatch>();
    public DbSet<OddsQuote> OddsQuotes => Set<OddsQuote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FootballMatch>(entity =>
        {
            entity.ToTable("matches");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ExternalId).IsUnique();
            entity.Property(x => x.ExternalId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Competition).HasMaxLength(150).IsRequired();
            entity.Property(x => x.HomeTeam).HasMaxLength(150).IsRequired();
            entity.Property(x => x.AwayTeam).HasMaxLength(150).IsRequired();
        });

        modelBuilder.Entity<OddsQuote>(entity =>
        {
            entity.ToTable("odds_quotes");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Bookmaker).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Market).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Selection).HasMaxLength(100).IsRequired();
            entity.Property(x => x.DecimalOdds).HasPrecision(10, 4);
            entity.HasIndex(x => new { x.MatchId, x.Bookmaker, x.Market, x.Selection, x.CapturedAt }).IsUnique();
            entity.HasOne<FootballMatch>().WithMany(x => x.OddsQuotes).HasForeignKey(x => x.MatchId);
        });
    }
}
