using BotArbitragem.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BotArbitragem.Infrastructure.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<FootballMatch> Matches => Set<FootballMatch>();
    public DbSet<OddsQuote> OddsQuotes => Set<OddsQuote>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<OpportunityLeg> OpportunityLegs => Set<OpportunityLeg>();
    public DbSet<BetRecord> BetRecords => Set<BetRecord>();
    public DbSet<BetLeg> BetLegs => Set<BetLeg>();
    public DbSet<PublishedAlert> PublishedAlerts => Set<PublishedAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<FootballMatch>(entity =>
        {
            entity.ToTable("matches");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.ExternalId).IsUnique();
            entity.HasIndex(x => x.KickoffAt);
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
            entity.HasIndex(x => new { x.MatchId, x.CapturedAt });
            entity.HasIndex(x => new { x.MatchId, x.Bookmaker, x.Market, x.Selection, x.CapturedAt }).IsUnique();
            entity.HasOne<FootballMatch>().WithMany(x => x.OddsQuotes).HasForeignKey(x => x.MatchId);
        });

        modelBuilder.Entity<Opportunity>(entity =>
        {
            entity.ToTable("opportunities");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Fingerprint).IsUnique();
            entity.HasIndex(x => new { x.Status, x.LastSeenAt });
            entity.Property(x => x.Fingerprint).HasMaxLength(300).IsRequired();
            entity.Property(x => x.Kind).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Market).HasMaxLength(50).IsRequired();
            entity.Property(x => x.Selection).HasMaxLength(150);
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.EstimatedProbability).HasPrecision(12, 8);
            entity.Property(x => x.MarketOdds).HasPrecision(10, 4);
            entity.Property(x => x.ExpectedValue).HasPrecision(12, 8);
            entity.Property(x => x.Edge).HasPrecision(12, 8);
            entity.Property(x => x.ProfitPercentage).HasPrecision(12, 8);
            entity.HasOne<FootballMatch>().WithMany().HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(x => x.Legs).WithOne().HasForeignKey(x => x.OpportunityId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<OpportunityLeg>(entity =>
        {
            entity.ToTable("opportunity_legs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Bookmaker).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Selection).HasMaxLength(150).IsRequired();
            entity.Property(x => x.DecimalOdds).HasPrecision(10, 4);
            entity.Property(x => x.StakePercentage).HasPrecision(10, 6);
        });

        modelBuilder.Entity<BetRecord>(entity =>
        {
            entity.ToTable("bet_records");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.Status, x.PlacedAt });
            entity.Property(x => x.Mode).HasMaxLength(10).IsRequired();
            entity.Property(x => x.Currency).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Stake).HasPrecision(14, 2);
            entity.Property(x => x.PotentialReturn).HasPrecision(14, 2);
            entity.Property(x => x.ReturnAmount).HasPrecision(14, 2);
            entity.Property(x => x.ProfitLoss).HasPrecision(14, 2);
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.HasOne<Opportunity>().WithMany().HasForeignKey(x => x.OpportunityId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<FootballMatch>().WithMany().HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany(x => x.Legs).WithOne().HasForeignKey(x => x.BetRecordId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BetLeg>(entity =>
        {
            entity.ToTable("bet_legs");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Bookmaker).HasMaxLength(100).IsRequired();
            entity.Property(x => x.Selection).HasMaxLength(150).IsRequired();
            entity.Property(x => x.DecimalOdds).HasPrecision(10, 4);
            entity.Property(x => x.StakeAmount).HasPrecision(14, 2);
        });

        modelBuilder.Entity<PublishedAlert>(entity =>
        {
            entity.ToTable("published_alerts");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Fingerprint).IsUnique();
            entity.HasIndex(x => new { x.MatchId, x.CreatedAt });
            entity.Property(x => x.Fingerprint).HasMaxLength(64).IsRequired();
            entity.HasOne<FootballMatch>().WithMany().HasForeignKey(x => x.MatchId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
