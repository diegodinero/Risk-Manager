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
                System.Diagnostics.Debug.WriteLine($"GetSettings: Retrieved from cache for account '{accountNumber}'");
                return cached.Settings;
            }

            try
            {
                lock (_fileLock)
                {
                    var filePath = GetSettingsFilePath(accountNumber);
                    System.Diagnostics.Debug.WriteLine($"GetSettings: Loading settings for account '{accountNumber}' from file: {filePath}");
                    
                    if (!File.Exists(filePath))
                    {
                        System.Diagnostics.Debug.WriteLine($"GetSettings: No settings file found for account '{accountNumber}'");
                        return null;
                    }

                    var json = File.ReadAllText(filePath);
                    var settings = JsonSerializer.Deserialize<AccountSettings>(json, _jsonOptions);
                    
                    if (settings != null)
                    {
                        _cache[accountNumber] = new CachedAccountSettings(settings, _cacheExpiration);
                        System.Diagnostics.Debug.WriteLine($"GetSettings: Successfully loaded settings for account '{accountNumber}'");
                    }
                    return settings;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetSettings failed for account '{accountNumber}': {ex}");
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
                    
                    // Debug logging
                    System.Diagnostics.Debug.WriteLine($"Saving settings for account '{settings.AccountNumber}' to file: {filePath}");
                    
                    File.WriteAllText(filePath, json);
                    _cache[settings.AccountNumber] = new CachedAccountSettings(settings, _cacheExpiration);
                    
                    System.Diagnostics.Debug.WriteLine($"Settings saved successfully for account '{settings.AccountNumber}'");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SaveSettings failed for account '{settings?.AccountNumber}': {ex}");
            }
        }

        #endregion

        #region Update Methods

        public void UpdateDailyLossLimit(string accountNumber, decimal? limit)
        {
            // Validate that limit is not negative
            if (limit.HasValue && limit.Value < 0)
            {
                throw new ArgumentException("Daily loss limit cannot be negative.", nameof(limit));
            }
            
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.DailyLossLimit = limit;
                SaveSettings(settings);
            }
        }

        public void UpdateDailyProfitTarget(string accountNumber, decimal? target)
        {
            // Validate that target is not negative
            if (target.HasValue && target.Value < 0)
            {
                throw new ArgumentException("Daily profit target cannot be negative.", nameof(target));
            }
            
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.DailyProfitTarget = target;
                SaveSettings(settings);
            }
        }

        public void UpdatePositionLossLimit(string accountNumber, decimal? limit)
        {
            // Validate that limit is not negative
            if (limit.HasValue && limit.Value < 0)
            {
                throw new ArgumentException("Position loss limit cannot be negative.", nameof(limit));
            }
            
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.PositionLossLimit = limit;
                SaveSettings(settings);
            }
        }

        public void UpdatePositionProfitTarget(string accountNumber, decimal? target)
        {
            // Validate that target is not negative
            if (target.HasValue && target.Value < 0)
            {
                throw new ArgumentException("Position profit target cannot be negative.", nameof(target));
            }
            
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.PositionProfitTarget = target;
                SaveSettings(settings);
            }
        }

        public void UpdateDefaultContractLimit(string accountNumber, int? limit)
        {
            // Validate that limit is positive
            if (limit.HasValue && limit.Value <= 0)
            {
                throw new ArgumentException("Default contract limit must be positive.", nameof(limit));
            }
            
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.DefaultContractLimit = limit;
                SaveSettings(settings);
            }
        }

        public void UpdateWeeklyLossLimit(string accountNumber, decimal? limit)
        {
            // Validate that limit is not negative
            if (limit.HasValue && limit.Value < 0)
            {
                throw new ArgumentException("Weekly loss limit cannot be negative.", nameof(limit));
            }
            
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.WeeklyLossLimit = limit;
                SaveSettings(settings);
            }
        }

        public void UpdateWeeklyProfitTarget(string accountNumber, decimal? target)
        {
            // Validate that target is not negative
            if (target.HasValue && target.Value < 0)
            {
                throw new ArgumentException("Weekly profit target cannot be negative.", nameof(target));
            }
            
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

        /// <summary>
        /// Copies all settings from one account to multiple target accounts.
        /// </summary>
        /// <param name="sourceAccountNumber">The account to copy settings from</param>
        /// <param name="targetAccountNumbers">The accounts to copy settings to</param>
        /// <returns>A dictionary mapping account numbers to success/failure status</returns>
        public Dictionary<string, (bool Success, string Message)> CopySettingsToAccounts(
            string sourceAccountNumber, 
            IEnumerable<string> targetAccountNumbers)
        {
            var results = new Dictionary<string, (bool Success, string Message)>();
            
            if (string.IsNullOrEmpty(sourceAccountNumber))
            {
                throw new ArgumentException("Source account number cannot be null or empty.", nameof(sourceAccountNumber));
            }
            
            if (targetAccountNumbers == null || !targetAccountNumbers.Any())
            {
                throw new ArgumentException("Target account numbers cannot be null or empty.", nameof(targetAccountNumbers));
            }
            
            // Get source settings
            var sourceSettings = GetSettings(sourceAccountNumber);
            if (sourceSettings == null)
            {
                throw new InvalidOperationException($"No settings found for source account: {sourceAccountNumber}");
            }
            
            // Copy to each target account
            foreach (var targetAccount in targetAccountNumbers.Where(a => !string.IsNullOrEmpty(a)))
            {
                try
                {
                    // Get or create target settings
                    var targetSettings = GetOrCreateSettings(targetAccount);
                    if (targetSettings == null)
                    {
                        results[targetAccount] = (false, "Failed to create settings");
                        continue;
                    }
                    
                    // Copy all settings (except account number and timestamps)
                    targetSettings.FeatureToggleEnabled = sourceSettings.FeatureToggleEnabled;
                    targetSettings.DailyLossLimit = sourceSettings.DailyLossLimit;
                    targetSettings.DailyProfitTarget = sourceSettings.DailyProfitTarget;
                    targetSettings.PositionLossLimit = sourceSettings.PositionLossLimit;
                    targetSettings.PositionProfitTarget = sourceSettings.PositionProfitTarget;
                    targetSettings.WeeklyLossLimit = sourceSettings.WeeklyLossLimit;
                    targetSettings.WeeklyProfitTarget = sourceSettings.WeeklyProfitTarget;
                    targetSettings.DefaultContractLimit = sourceSettings.DefaultContractLimit;
                    targetSettings.BlockedSymbols = new List<string>(sourceSettings.BlockedSymbols);
                    targetSettings.SymbolContractLimits = new Dictionary<string, int>(sourceSettings.SymbolContractLimits);
                    targetSettings.TradingTimeRestrictions = sourceSettings.TradingTimeRestrictions
                        .Select(tr => new TradingTimeRestriction
                        {
                            DayOfWeek = tr.DayOfWeek,
                            StartTime = tr.StartTime,
                            EndTime = tr.EndTime,
                            IsAllowed = tr.IsAllowed,
                            Name = tr.Name
                        }).ToList();
                    
                    // Copy lock settings
                    if (sourceSettings.TradingLock != null)
                    {
                        targetSettings.TradingLock = new LockInfo
                        {
                            IsLocked = sourceSettings.TradingLock.IsLocked,
                            LockTime = sourceSettings.TradingLock.LockTime,
                            LockDayOfWeek = sourceSettings.TradingLock.LockDayOfWeek,
                            LockReason = sourceSettings.TradingLock.LockReason
                        };
                    }
                    
                    if (sourceSettings.SettingsLock != null)
                    {
                        targetSettings.SettingsLock = new LockInfo
                        {
                            IsLocked = sourceSettings.SettingsLock.IsLocked,
                            LockTime = sourceSettings.SettingsLock.LockTime,
                            LockDayOfWeek = sourceSettings.SettingsLock.LockDayOfWeek,
                            LockReason = sourceSettings.SettingsLock.LockReason
                        };
                    }
                    
                    // Save the target settings
                    SaveSettings(targetSettings);
                    
                    results[targetAccount] = (true, "Settings copied successfully");
                }
                catch (Exception ex)
                {
                    results[targetAccount] = (false, $"Error: {ex.Message}");
                }
            }
            
            return results;
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
            // Validate that all contract limits are positive
            var invalidLimits = symbolLimits.Where(kvp => kvp.Value <= 0).ToList();
            if (invalidLimits.Any())
            {
                var invalidSymbols = string.Join(", ", invalidLimits.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
                throw new ArgumentException($"Symbol contract limits must be positive. Invalid entries: {invalidSymbols}", nameof(symbolLimits));
            }
            
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
