using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Risk_Manager.Data.Entities;

#nullable enable

namespace Risk_Manager.Data
{
    /// <summary>
    /// Entity Framework Core DbContext for Risk Manager settings persistence.
    /// </summary>
    public class RiskManagerDbContext : DbContext
    {
        private static string? _connectionString;

        public DbSet<AccountSettings> AccountSettings { get; set; } = null!;
        public DbSet<BlockedSymbol> BlockedSymbols { get; set; } = null!;
        public DbSet<SymbolContractLimit> SymbolContractLimits { get; set; } = null!;
        public DbSet<TradingTimeRestriction> TradingTimeRestrictions { get; set; } = null!;
        public DbSet<TradingLock> TradingLocks { get; set; } = null!;
        public DbSet<SettingsLock> SettingsLocks { get; set; } = null!;

        public RiskManagerDbContext()
        {
        }

        public RiskManagerDbContext(DbContextOptions<RiskManagerDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Gets the database file path, ensuring the directory exists.
        /// </summary>
        public static string GetDatabasePath()
        {
            // Store the database in the user's local application data folder
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var riskManagerPath = Path.Combine(appDataPath, "RiskManager");
            
            // Ensure directory exists
            if (!Directory.Exists(riskManagerPath))
            {
                Directory.CreateDirectory(riskManagerPath);
            }

            return Path.Combine(riskManagerPath, "riskmanager.db");
        }

        /// <summary>
        /// Gets the connection string for the SQLite database.
        /// </summary>
        public static string GetConnectionString()
        {
            _connectionString ??= $"Data Source={GetDatabasePath()}";
            return _connectionString;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite(GetConnectionString());
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure AccountSettings
            modelBuilder.Entity<AccountSettings>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.AccountNumber).IsUnique();
                entity.Property(e => e.AccountNumber).IsRequired().HasMaxLength(100);
                entity.Property(e => e.DailyLossLimit).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.DailyProfitTarget).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.PositionLossLimit).HasColumnType("decimal(18, 2)");
                entity.Property(e => e.PositionProfitTarget).HasColumnType("decimal(18, 2)");
            });

            // Configure BlockedSymbol
            modelBuilder.Entity<BlockedSymbol>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Symbol).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => new { e.AccountSettingsId, e.Symbol }).IsUnique();
                entity.HasOne(e => e.AccountSettings)
                      .WithMany(a => a.BlockedSymbols)
                      .HasForeignKey(e => e.AccountSettingsId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure SymbolContractLimit
            modelBuilder.Entity<SymbolContractLimit>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Symbol).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => new { e.AccountSettingsId, e.Symbol }).IsUnique();
                entity.HasOne(e => e.AccountSettings)
                      .WithMany(a => a.SymbolContractLimits)
                      .HasForeignKey(e => e.AccountSettingsId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure TradingTimeRestriction
            modelBuilder.Entity<TradingTimeRestriction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Name).HasMaxLength(100);
                entity.HasOne(e => e.AccountSettings)
                      .WithMany(a => a.TradingTimeRestrictions)
                      .HasForeignKey(e => e.AccountSettingsId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure TradingLock
            modelBuilder.Entity<TradingLock>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.AccountSettingsId).IsUnique();
                entity.Property(e => e.LockReason).HasMaxLength(500);
                entity.HasOne(e => e.AccountSettings)
                      .WithOne(a => a.TradingLock)
                      .HasForeignKey<TradingLock>(e => e.AccountSettingsId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configure SettingsLock
            modelBuilder.Entity<SettingsLock>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.AccountSettingsId).IsUnique();
                entity.Property(e => e.LockReason).HasMaxLength(500);
                entity.HasOne(e => e.AccountSettings)
                      .WithOne(a => a.SettingsLock)
                      .HasForeignKey<SettingsLock>(e => e.AccountSettingsId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
