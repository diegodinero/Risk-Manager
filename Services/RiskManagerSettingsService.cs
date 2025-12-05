using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.EntityFrameworkCore;
using Risk_Manager.Data;
using Risk_Manager.Data.Entities;

#nullable enable

namespace Risk_Manager.Services
{
    /// <summary>
    /// Service for managing risk manager settings with caching for optimal performance.
    /// </summary>
    public class RiskManagerSettingsService : IDisposable
    {
        private static readonly Lazy<RiskManagerSettingsService> _instance =
            new(() => new RiskManagerSettingsService(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the singleton instance of the settings service.
        /// </summary>
        public static RiskManagerSettingsService Instance => _instance.Value;

        // Cache for account settings with expiration
        private readonly ConcurrentDictionary<string, CachedSettings> _cache = new();
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromSeconds(30);
        private readonly object _dbLock = new();
        private bool _disposed;
        private bool _isInitialized;
        private string? _initializationError;

        private RiskManagerSettingsService()
        {
            try
            {
                EnsureDatabaseCreated();
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _initializationError = ex.Message;
                _isInitialized = false;
                System.Diagnostics.Debug.WriteLine($"RiskManagerSettingsService initialization failed: {ex}");
            }
        }

        /// <summary>
        /// Gets whether the service was successfully initialized.
        /// </summary>
        public bool IsInitialized => _isInitialized;

        /// <summary>
        /// Gets the initialization error message if any.
        /// </summary>
        public string? InitializationError => _initializationError;

        /// <summary>
        /// Ensures the database and tables are created.
        /// </summary>
        public void EnsureDatabaseCreated()
        {
            lock (_dbLock)
            {
                using var context = CreateContext();
                context.Database.EnsureCreated();
            }
        }

        /// <summary>
        /// Creates a new database context.
        /// </summary>
        private static RiskManagerDbContext CreateContext()
        {
            return new RiskManagerDbContext();
        }

        /// <summary>
        /// Gets or creates account settings within an existing context.
        /// This helper method reduces code duplication across setter methods.
        /// </summary>
        private static AccountSettings GetOrCreateAccountSettingsInternal(
            RiskManagerDbContext context, 
            string accountNumber,
            Func<IQueryable<AccountSettings>, IQueryable<AccountSettings>>? includeFunc = null)
        {
            var query = context.AccountSettings.AsQueryable();
            if (includeFunc != null)
            {
                query = includeFunc(query);
            }
            
            var settings = query.FirstOrDefault(s => s.AccountNumber == accountNumber);

            if (settings == null)
            {
                settings = new AccountSettings
                {
                    AccountNumber = accountNumber,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                context.AccountSettings.Add(settings);
                context.SaveChanges();
            }

            return settings;
        }

        /// <summary>
        /// Gets the settings for an account, using cache when available.
        /// </summary>
        public AccountSettings? GetSettings(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber) || !_isInitialized)
                return null;

            // Check cache first
            if (_cache.TryGetValue(accountNumber, out var cached) && !cached.IsExpired)
            {
                return cached.Settings;
            }

            // Load from database
            try
            {
                lock (_dbLock)
                {
                    using var context = CreateContext();
                    var settings = context.AccountSettings
                        .Include(s => s.BlockedSymbols)
                        .Include(s => s.SymbolContractLimits)
                        .Include(s => s.TradingTimeRestrictions)
                        .Include(s => s.TradingLock)
                        .Include(s => s.SettingsLock)
                    .AsNoTracking()
                    .FirstOrDefault(s => s.AccountNumber == accountNumber);

                // Update cache
                if (settings != null)
                {
                    _cache[accountNumber] = new CachedSettings(settings, _cacheExpiration);
                }
                else
                {
                    _cache.TryRemove(accountNumber, out _);
                }

                return settings;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetSettings failed: {ex}");
                return null;
            }
        }

        /// <summary>
        /// Gets or creates settings for an account.
        /// </summary>
        public AccountSettings GetOrCreateSettings(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber))
                throw new ArgumentException("Account number cannot be null or empty.", nameof(accountNumber));

            var existing = GetSettings(accountNumber);
            if (existing != null)
                return existing;

            // Create new settings
            lock (_dbLock)
            {
                using var context = CreateContext();
                
                // Double-check in case another thread created it
                existing = context.AccountSettings
                    .Include(s => s.BlockedSymbols)
                    .Include(s => s.SymbolContractLimits)
                    .Include(s => s.TradingTimeRestrictions)
                    .Include(s => s.TradingLock)
                    .Include(s => s.SettingsLock)
                    .FirstOrDefault(s => s.AccountNumber == accountNumber);

                if (existing != null)
                {
                    _cache[accountNumber] = new CachedSettings(existing, _cacheExpiration);
                    return existing;
                }

                var newSettings = new AccountSettings
                {
                    AccountNumber = accountNumber,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                context.AccountSettings.Add(newSettings);
                context.SaveChanges();

                // Reload with navigation properties
                var created = context.AccountSettings
                    .Include(s => s.BlockedSymbols)
                    .Include(s => s.SymbolContractLimits)
                    .Include(s => s.TradingTimeRestrictions)
                    .Include(s => s.TradingLock)
                    .Include(s => s.SettingsLock)
                    .AsNoTracking()
                    .First(s => s.AccountNumber == accountNumber);

                _cache[accountNumber] = new CachedSettings(created, _cacheExpiration);
                return created;
            }
        }

        /// <summary>
        /// Updates the daily loss limit for an account.
        /// </summary>
        public void UpdateDailyLossLimit(string accountNumber, decimal? limit)
        {
            UpdateSettings(accountNumber, settings => settings.DailyLossLimit = limit);
        }

        /// <summary>
        /// Updates the daily profit target for an account.
        /// </summary>
        public void UpdateDailyProfitTarget(string accountNumber, decimal? target)
        {
            UpdateSettings(accountNumber, settings => settings.DailyProfitTarget = target);
        }

        /// <summary>
        /// Updates the position loss limit for an account.
        /// </summary>
        public void UpdatePositionLossLimit(string accountNumber, decimal? limit)
        {
            UpdateSettings(accountNumber, settings => settings.PositionLossLimit = limit);
        }

        /// <summary>
        /// Updates the position profit target for an account.
        /// </summary>
        public void UpdatePositionProfitTarget(string accountNumber, decimal? target)
        {
            UpdateSettings(accountNumber, settings => settings.PositionProfitTarget = target);
        }

        /// <summary>
        /// Updates the default contract limit for an account.
        /// </summary>
        public void UpdateDefaultContractLimit(string accountNumber, int? limit)
        {
            UpdateSettings(accountNumber, settings => settings.DefaultContractLimit = limit);
        }

        /// <summary>
        /// Sets the blocked symbols for an account.
        /// </summary>
        public void SetBlockedSymbols(string accountNumber, IEnumerable<string> symbols)
        {
            if (string.IsNullOrEmpty(accountNumber))
                throw new ArgumentException("Account number cannot be null or empty.", nameof(accountNumber));

            lock (_dbLock)
            {
                using var context = CreateContext();
                var settings = GetOrCreateAccountSettingsInternal(
                    context, 
                    accountNumber, 
                    q => q.Include(s => s.BlockedSymbols));

                // Clear existing and add new
                context.BlockedSymbols.RemoveRange(settings.BlockedSymbols);
                
                foreach (var symbol in symbols.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct())
                {
                    settings.BlockedSymbols.Add(new BlockedSymbol
                    {
                        AccountSettingsId = settings.Id,
                        Symbol = symbol.Trim().ToUpperInvariant()
                    });
                }

                settings.UpdatedAt = DateTime.UtcNow;
                context.SaveChanges();

                InvalidateCache(accountNumber);
            }
        }

        /// <summary>
        /// Sets the symbol-specific contract limits for an account.
        /// </summary>
        public void SetSymbolContractLimits(string accountNumber, IDictionary<string, int> symbolLimits)
        {
            if (string.IsNullOrEmpty(accountNumber))
                throw new ArgumentException("Account number cannot be null or empty.", nameof(accountNumber));

            lock (_dbLock)
            {
                using var context = CreateContext();
                var settings = GetOrCreateAccountSettingsInternal(
                    context, 
                    accountNumber, 
                    q => q.Include(s => s.SymbolContractLimits));

                // Clear existing and add new
                context.SymbolContractLimits.RemoveRange(settings.SymbolContractLimits);
                
                foreach (var kvp in symbolLimits.Where(k => !string.IsNullOrWhiteSpace(k.Key)))
                {
                    settings.SymbolContractLimits.Add(new SymbolContractLimit
                    {
                        AccountSettingsId = settings.Id,
                        Symbol = kvp.Key.Trim().ToUpperInvariant(),
                        ContractLimit = kvp.Value
                    });
                }

                settings.UpdatedAt = DateTime.UtcNow;
                context.SaveChanges();

                InvalidateCache(accountNumber);
            }
        }

        /// <summary>
        /// Sets the trading time restrictions for an account.
        /// </summary>
        public void SetTradingTimeRestrictions(string accountNumber, IEnumerable<TradingTimeRestriction> restrictions)
        {
            if (string.IsNullOrEmpty(accountNumber))
                throw new ArgumentException("Account number cannot be null or empty.", nameof(accountNumber));

            lock (_dbLock)
            {
                using var context = CreateContext();
                var settings = GetOrCreateAccountSettingsInternal(
                    context, 
                    accountNumber, 
                    q => q.Include(s => s.TradingTimeRestrictions));

                // Clear existing and add new
                context.TradingTimeRestrictions.RemoveRange(settings.TradingTimeRestrictions);
                
                foreach (var restriction in restrictions)
                {
                    settings.TradingTimeRestrictions.Add(new TradingTimeRestriction
                    {
                        AccountSettingsId = settings.Id,
                        DayOfWeek = restriction.DayOfWeek,
                        StartTime = restriction.StartTime,
                        EndTime = restriction.EndTime,
                        IsAllowed = restriction.IsAllowed,
                        Name = restriction.Name
                    });
                }

                settings.UpdatedAt = DateTime.UtcNow;
                context.SaveChanges();

                InvalidateCache(accountNumber);
            }
        }

        /// <summary>
        /// Sets the trading lock status for an account.
        /// </summary>
        public void SetTradingLock(string accountNumber, bool isLocked, string? reason = null)
        {
            if (string.IsNullOrEmpty(accountNumber))
                throw new ArgumentException("Account number cannot be null or empty.", nameof(accountNumber));

            lock (_dbLock)
            {
                using var context = CreateContext();
                var settings = GetOrCreateAccountSettingsInternal(
                    context, 
                    accountNumber, 
                    q => q.Include(s => s.TradingLock));

                if (settings.TradingLock == null)
                {
                    settings.TradingLock = new TradingLock
                    {
                        AccountSettingsId = settings.Id
                    };
                    context.TradingLocks.Add(settings.TradingLock);
                }

                settings.TradingLock.IsLocked = isLocked;
                settings.TradingLock.LockTime = isLocked ? DateTime.UtcNow : null;
                settings.TradingLock.LockDayOfWeek = isLocked ? DateTime.UtcNow.DayOfWeek : null;
                settings.TradingLock.LockReason = reason;
                settings.UpdatedAt = DateTime.UtcNow;

                context.SaveChanges();

                InvalidateCache(accountNumber);
            }
        }

        /// <summary>
        /// Sets the settings lock status for an account.
        /// </summary>
        public void SetSettingsLock(string accountNumber, bool isLocked, string? reason = null)
        {
            if (string.IsNullOrEmpty(accountNumber))
                throw new ArgumentException("Account number cannot be null or empty.", nameof(accountNumber));

            lock (_dbLock)
            {
                using var context = CreateContext();
                var settings = GetOrCreateAccountSettingsInternal(
                    context, 
                    accountNumber, 
                    q => q.Include(s => s.SettingsLock));

                if (settings.SettingsLock == null)
                {
                    settings.SettingsLock = new SettingsLock
                    {
                        AccountSettingsId = settings.Id
                    };
                    context.SettingsLocks.Add(settings.SettingsLock);
                }

                settings.SettingsLock.IsLocked = isLocked;
                settings.SettingsLock.LockTime = isLocked ? DateTime.UtcNow : null;
                settings.SettingsLock.LockDayOfWeek = isLocked ? DateTime.UtcNow.DayOfWeek : null;
                settings.SettingsLock.LockReason = reason;
                settings.UpdatedAt = DateTime.UtcNow;

                context.SaveChanges();

                InvalidateCache(accountNumber);
            }
        }

        /// <summary>
        /// Checks if a symbol is blocked for an account.
        /// </summary>
        public bool IsSymbolBlocked(string accountNumber, string symbol)
        {
            if (string.IsNullOrEmpty(accountNumber) || string.IsNullOrEmpty(symbol))
                return false;

            var settings = GetSettings(accountNumber);
            if (settings?.BlockedSymbols == null)
                return false;

            // Normalize the input symbol to match storage format (uppercase)
            var normalizedSymbol = symbol.Trim().ToUpperInvariant();
            return settings.BlockedSymbols.Any(b => b.Symbol == normalizedSymbol);
        }

        /// <summary>
        /// Gets the contract limit for a specific symbol.
        /// Returns the symbol-specific limit if set, otherwise the default limit.
        /// </summary>
        public int? GetContractLimit(string accountNumber, string symbol)
        {
            if (string.IsNullOrEmpty(accountNumber))
                return null;

            var settings = GetSettings(accountNumber);
            if (settings == null)
                return null;

            // Normalize the input symbol to match storage format (uppercase)
            var normalizedSymbol = symbol?.Trim().ToUpperInvariant();
            
            // Check for symbol-specific limit first
            var symbolLimit = settings.SymbolContractLimits?
                .FirstOrDefault(l => l.Symbol == normalizedSymbol);

            if (symbolLimit != null)
                return symbolLimit.ContractLimit;

            // Fall back to default limit
            return settings.DefaultContractLimit;
        }

        /// <summary>
        /// Checks if trading is currently allowed based on time restrictions.
        /// </summary>
        public bool IsTradingAllowedNow(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber))
                return true; // Default to allowed if no account

            var settings = GetSettings(accountNumber);
            
            // Check trading lock first
            if (settings?.TradingLock?.IsLocked == true)
                return false;

            // If no time restrictions are set, trading is allowed
            if (settings?.TradingTimeRestrictions == null || !settings.TradingTimeRestrictions.Any())
                return true;

            // Using local time intentionally since trading hours are defined in local time 
            // (e.g., "NY Session 8 AM - 5 PM EST")
            var now = DateTime.Now;
            var currentDayOfWeek = now.DayOfWeek;
            var currentTime = now.TimeOfDay;

            // Check if any restriction allows trading at this time
            var applicableRestrictions = settings.TradingTimeRestrictions
                .Where(r => r.DayOfWeek == currentDayOfWeek);

            if (!applicableRestrictions.Any())
                return true; // No restrictions for today means trading is allowed

            // Check if current time falls within an allowed window
            return applicableRestrictions.Any(r => 
                r.IsAllowed && 
                currentTime >= r.StartTime && 
                currentTime <= r.EndTime);
        }

        /// <summary>
        /// Checks if settings are locked for an account.
        /// </summary>
        public bool AreSettingsLocked(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber))
                return false;

            var settings = GetSettings(accountNumber);
            return settings?.SettingsLock?.IsLocked == true;
        }

        /// <summary>
        /// Checks if trading is locked for an account.
        /// </summary>
        public bool IsTradingLocked(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber))
                return false;

            var settings = GetSettings(accountNumber);
            return settings?.TradingLock?.IsLocked == true;
        }

        /// <summary>
        /// Gets all account settings from the database.
        /// </summary>
        public List<AccountSettings> GetAllSettings()
        {
            lock (_dbLock)
            {
                using var context = CreateContext();
                return context.AccountSettings
                    .Include(s => s.BlockedSymbols)
                    .Include(s => s.SymbolContractLimits)
                    .Include(s => s.TradingTimeRestrictions)
                    .Include(s => s.TradingLock)
                    .Include(s => s.SettingsLock)
                    .AsNoTracking()
                    .ToList();
            }
        }

        /// <summary>
        /// Deletes settings for an account.
        /// </summary>
        public void DeleteSettings(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber))
                return;

            lock (_dbLock)
            {
                using var context = CreateContext();
                var settings = context.AccountSettings
                    .FirstOrDefault(s => s.AccountNumber == accountNumber);

                if (settings != null)
                {
                    context.AccountSettings.Remove(settings);
                    context.SaveChanges();
                }

                InvalidateCache(accountNumber);
            }
        }

        /// <summary>
        /// Invalidates the cache for a specific account.
        /// </summary>
        public void InvalidateCache(string accountNumber)
        {
            _cache.TryRemove(accountNumber, out _);
        }

        /// <summary>
        /// Clears the entire cache.
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
        }

        private void UpdateSettings(string accountNumber, Action<AccountSettings> updateAction)
        {
            if (string.IsNullOrEmpty(accountNumber))
                throw new ArgumentException("Account number cannot be null or empty.", nameof(accountNumber));

            lock (_dbLock)
            {
                using var context = CreateContext();
                var settings = context.AccountSettings
                    .FirstOrDefault(s => s.AccountNumber == accountNumber);

                if (settings == null)
                {
                    settings = new AccountSettings
                    {
                        AccountNumber = accountNumber,
                        CreatedAt = DateTime.UtcNow
                    };
                    context.AccountSettings.Add(settings);
                }

                updateAction(settings);
                settings.UpdatedAt = DateTime.UtcNow;
                context.SaveChanges();

                InvalidateCache(accountNumber);
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _cache.Clear();
                _disposed = true;
            }
        }

        /// <summary>
        /// Cached settings with expiration.
        /// </summary>
        private class CachedSettings
        {
            public AccountSettings Settings { get; }
            public DateTime ExpiresAt { get; }

            public bool IsExpired => DateTime.UtcNow > ExpiresAt;

            public CachedSettings(AccountSettings settings, TimeSpan expiration)
            {
                Settings = settings;
                ExpiresAt = DateTime.UtcNow.Add(expiration);
            }
        }
    }
}
