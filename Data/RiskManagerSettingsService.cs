using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

#nullable enable

namespace Risk_Manager.Data
{
    /// <summary>
    /// JSON-based settings service with zero external dependencies.
    /// Stores per-account risk management settings in JSON files.
    /// </summary>
    public class RiskManagerSettingsService : IDisposable
    {
        private static readonly Lazy<RiskManagerSettingsService> _instance =
            new(() => new RiskManagerSettingsService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the singleton instance of the settings service.
        /// </summary>
        public static RiskManagerSettingsService Instance => _instance.Value;

        private readonly string _settingsFolder;
        private readonly ConcurrentDictionary<string, CachedAccountSettings> _cache = new();
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromSeconds(30);
        private readonly object _fileLock = new();
        private bool _disposed;
        private bool _isInitialized;
        private string? _initializationError;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private RiskManagerSettingsService()
        {
            try
            {
                _settingsFolder = GetSettingsFolder();
                if (!Directory.Exists(_settingsFolder))
                {
                    Directory.CreateDirectory(_settingsFolder);
                }
                _isInitialized = true;
            }
            catch (Exception ex)
            {
                _initializationError = ex.Message;
                _isInitialized = false;
                _settingsFolder = string.Empty;
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
        /// Gets the settings folder path.
        /// </summary>
        public string SettingsFolder => _settingsFolder;

        private static string GetSettingsFolder()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(appDataPath, "RiskManager");
        }

        private string GetSettingsFilePath(string accountNumber)
        {
            // Sanitize account number for use as filename
            var safeAccountNumber = string.Join("_", accountNumber.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(_settingsFolder, $"{safeAccountNumber}.json");
        }

        #region Account Settings CRUD

        /// <summary>
        /// Gets the settings for an account.
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

            try
            {
                lock (_fileLock)
                {
                    var filePath = GetSettingsFilePath(accountNumber);
                    if (!File.Exists(filePath))
                        return null;

                    var json = File.ReadAllText(filePath);
                    var settings = JsonSerializer.Deserialize<AccountSettings>(json, _jsonOptions);
                    
                    if (settings != null)
                    {
                        _cache[accountNumber] = new CachedAccountSettings(settings, _cacheExpiration);
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
        public AccountSettings? GetOrCreateSettings(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber) || !_isInitialized)
                return null;

            var existing = GetSettings(accountNumber);
            if (existing != null)
                return existing;

            try
            {
                var settings = new AccountSettings
                {
                    AccountNumber = accountNumber,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                SaveSettings(settings);
                return settings;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetOrCreateSettings failed: {ex}");
                return null;
            }
        }

        private void SaveSettings(AccountSettings settings)
        {
            if (!_isInitialized || settings == null)
                return;

            try
            {
                lock (_fileLock)
                {
                    settings.UpdatedAt = DateTime.UtcNow;
                    var filePath = GetSettingsFilePath(settings.AccountNumber);
                    var json = JsonSerializer.Serialize(settings, _jsonOptions);
                    File.WriteAllText(filePath, json);
                    _cache[settings.AccountNumber] = new CachedAccountSettings(settings, _cacheExpiration);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveSettings failed: {ex}");
            }
        }

        #endregion

        #region Update Methods

        public void UpdateDailyLossLimit(string accountNumber, decimal? limit)
        {
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.DailyLossLimit = limit;
                SaveSettings(settings);
            }
        }

        public void UpdateDailyProfitTarget(string accountNumber, decimal? target)
        {
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.DailyProfitTarget = target;
                SaveSettings(settings);
            }
        }

        public void UpdatePositionLossLimit(string accountNumber, decimal? limit)
        {
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.PositionLossLimit = limit;
                SaveSettings(settings);
            }
        }

        public void UpdatePositionProfitTarget(string accountNumber, decimal? target)
        {
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.PositionProfitTarget = target;
                SaveSettings(settings);
            }
        }

        public void UpdateDefaultContractLimit(string accountNumber, int? limit)
        {
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.DefaultContractLimit = limit;
                SaveSettings(settings);
            }
        }

        public void UpdateWeeklyLossLimit(string accountNumber, decimal? limit)
        {
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.WeeklyLossLimit = limit;
                SaveSettings(settings);
            }
        }

        public void UpdateWeeklyProfitTarget(string accountNumber, decimal? target)
        {
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.WeeklyProfitTarget = target;
                SaveSettings(settings);
            }
        }

        public void UpdateFeatureToggleEnabled(string accountNumber, bool enabled)
        {
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.FeatureToggleEnabled = enabled;
                SaveSettings(settings);
            }
        }

        #endregion

        #region Blocked Symbols

        public void SetBlockedSymbols(string accountNumber, IEnumerable<string> symbols)
        {
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.BlockedSymbols = symbols
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim().ToUpperInvariant())
                    .Distinct()
                    .ToList();
                SaveSettings(settings);
            }
        }

        public bool IsSymbolBlocked(string accountNumber, string symbol)
        {
            if (string.IsNullOrEmpty(accountNumber) || string.IsNullOrEmpty(symbol))
                return false;

            var settings = GetSettings(accountNumber);
            if (settings?.BlockedSymbols == null)
                return false;

            var normalizedSymbol = symbol.Trim().ToUpperInvariant();
            return settings.BlockedSymbols.Contains(normalizedSymbol);
        }

        #endregion

        #region Symbol Contract Limits

        public void SetSymbolContractLimits(string accountNumber, IDictionary<string, int> symbolLimits)
        {
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.SymbolContractLimits = symbolLimits
                    .Where(k => !string.IsNullOrWhiteSpace(k.Key))
                    .ToDictionary(k => k.Key.Trim().ToUpperInvariant(), k => k.Value);
                SaveSettings(settings);
            }
        }

        public int? GetContractLimit(string accountNumber, string symbol)
        {
            if (string.IsNullOrEmpty(accountNumber))
                return null;

            var settings = GetSettings(accountNumber);
            if (settings == null)
                return null;

            // Check for symbol-specific limit first
            var normalizedSymbol = symbol?.Trim().ToUpperInvariant();
            if (!string.IsNullOrEmpty(normalizedSymbol) &&
                settings.SymbolContractLimits != null &&
                settings.SymbolContractLimits.TryGetValue(normalizedSymbol, out var limit))
            {
                return limit;
            }

            // Fall back to default limit
            return settings.DefaultContractLimit;
        }

        #endregion

        #region Trading Time Restrictions

        public void SetTradingTimeRestrictions(string accountNumber, IEnumerable<TradingTimeRestriction> restrictions)
        {
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.TradingTimeRestrictions = restrictions.ToList();
                SaveSettings(settings);
            }
        }

        public bool IsTradingAllowedNow(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber))
                return true;

            var settings = GetSettings(accountNumber);

            // Check trading lock first
            if (settings?.TradingLock?.IsLocked == true)
                return false;

            // If no time restrictions are set, trading is allowed
            if (settings?.TradingTimeRestrictions == null || !settings.TradingTimeRestrictions.Any())
                return true;

            // Using local time since trading hours are defined in local time
            var now = DateTime.Now;
            var currentDayOfWeek = now.DayOfWeek;
            var currentTime = now.TimeOfDay;

            var applicableRestrictions = settings.TradingTimeRestrictions
                .Where(r => r.DayOfWeek == currentDayOfWeek);

            if (!applicableRestrictions.Any())
                return true;

            return applicableRestrictions.Any(r =>
                r.IsAllowed &&
                currentTime >= r.StartTime &&
                currentTime <= r.EndTime);
        }

        #endregion

        #region Locks

        public void SetTradingLock(string accountNumber, bool isLocked, string? reason = null)
        {
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.TradingLock = new LockInfo
                {
                    IsLocked = isLocked,
                    LockTime = isLocked ? DateTime.UtcNow : null,
                    LockDayOfWeek = isLocked ? DateTime.UtcNow.DayOfWeek : null,
                    LockReason = reason
                };
                SaveSettings(settings);
            }
        }

        public void SetSettingsLock(string accountNumber, bool isLocked, string? reason = null)
        {
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.SettingsLock = new LockInfo
                {
                    IsLocked = isLocked,
                    LockTime = isLocked ? DateTime.UtcNow : null,
                    LockDayOfWeek = isLocked ? DateTime.UtcNow.DayOfWeek : null,
                    LockReason = reason
                };
                SaveSettings(settings);
            }
        }

        public bool IsTradingLocked(string accountNumber)
        {
            var settings = GetSettings(accountNumber);
            return settings?.TradingLock?.IsLocked == true;
        }

        public bool AreSettingsLocked(string accountNumber)
        {
            var settings = GetSettings(accountNumber);
            return settings?.SettingsLock?.IsLocked == true;
        }

        #endregion

        #region Cache Management

        public void InvalidateCache(string accountNumber)
        {
            _cache.TryRemove(accountNumber, out _);
        }

        public void ClearCache()
        {
            _cache.Clear();
        }

        #endregion

        public void Dispose()
        {
            if (!_disposed)
            {
                _cache.Clear();
                _disposed = true;
            }
        }

        private class CachedAccountSettings
        {
            public AccountSettings Settings { get; }
            public DateTime ExpiresAt { get; }
            public bool IsExpired => DateTime.UtcNow > ExpiresAt;

            public CachedAccountSettings(AccountSettings settings, TimeSpan expiration)
            {
                Settings = settings;
                ExpiresAt = DateTime.UtcNow.Add(expiration);
            }
        }
    }

    #region Data Classes

    /// <summary>
    /// Account settings data class.
    /// </summary>
    public class AccountSettings
    {
        public string AccountNumber { get; set; } = string.Empty;
        
        // Feature Toggle Master Switch
        public bool FeatureToggleEnabled { get; set; } = true;
        
        // Daily Limits
        public decimal? DailyLossLimit { get; set; }
        public decimal? DailyProfitTarget { get; set; }
        
        // Position Limits
        public decimal? PositionLossLimit { get; set; }
        public decimal? PositionProfitTarget { get; set; }
        
        // Weekly Limits
        public decimal? WeeklyLossLimit { get; set; }
        public decimal? WeeklyProfitTarget { get; set; }
        
        // Contract Limits
        public int? DefaultContractLimit { get; set; }
        
        // Timestamps
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        // Collections
        public List<string> BlockedSymbols { get; set; } = new();
        public Dictionary<string, int> SymbolContractLimits { get; set; } = new();
        public List<TradingTimeRestriction> TradingTimeRestrictions { get; set; } = new();
        
        // Locks
        public LockInfo? TradingLock { get; set; }
        public LockInfo? SettingsLock { get; set; }
    }

    /// <summary>
    /// Trading time restriction data class.
    /// </summary>
    public class TradingTimeRestriction
    {
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public bool IsAllowed { get; set; } = true;
        public string? Name { get; set; }
    }

    /// <summary>
    /// Lock information data class.
    /// </summary>
    public class LockInfo
    {
        public bool IsLocked { get; set; }
        public DateTime? LockTime { get; set; }
        public DayOfWeek? LockDayOfWeek { get; set; }
        public string? LockReason { get; set; }
    }

    #endregion
}
