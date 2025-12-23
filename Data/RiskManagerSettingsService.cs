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
            // Note: Daily loss limits are stored as negative values internally
            // The UI converts positive user input to negative before calling this method
            // Validation: Ensure limit is not positive (should be negative or null)
            if (limit.HasValue && limit.Value > 0)
            {
                throw new ArgumentException("Daily loss limit must be negative or zero for internal storage.", nameof(limit));
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
                    targetSettings.BlockedSymbols = sourceSettings.BlockedSymbols != null 
                        ? new List<string>(sourceSettings.BlockedSymbols) 
                        : new List<string>();
                    targetSettings.SymbolContractLimits = sourceSettings.SymbolContractLimits != null 
                        ? new Dictionary<string, int>(sourceSettings.SymbolContractLimits) 
                        : new Dictionary<string, int>();
                    targetSettings.TradingTimeRestrictions = sourceSettings.TradingTimeRestrictions != null
                        ? sourceSettings.TradingTimeRestrictions
                            .Where(tr => tr != null)  // Filter out null items
                            .Select(tr => new TradingTimeRestriction
                            {
                                DayOfWeek = tr.DayOfWeek,
                                StartTime = tr.StartTime,
                                EndTime = tr.EndTime,
                                IsAllowed = tr.IsAllowed,
                                Name = tr.Name
                            }).ToList()
                        : new List<TradingTimeRestriction>();
                    
                    // Copy lock settings using helper method
                    targetSettings.TradingLock = CopyLockInfo(sourceSettings.TradingLock);
                    targetSettings.SettingsLock = CopyLockInfo(sourceSettings.SettingsLock);
                    
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

        /// <summary>
        /// Helper method to copy LockInfo objects with null safety.
        /// </summary>
        private LockInfo? CopyLockInfo(LockInfo? source)
        {
            if (source == null)
                return null;
            
            return new LockInfo
            {
                IsLocked = source.IsLocked,
                LockTime = source.LockTime,
                LockDayOfWeek = source.LockDayOfWeek,
                LockReason = source.LockReason,
                LockDuration = source.LockDuration,
                LockExpirationTime = source.LockExpirationTime
            };
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

        public void SetTradingLock(string accountNumber, bool isLocked, string? reason = null, TimeSpan? duration = null)
        {
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                DateTime? expirationTime = null;
                if (isLocked && duration.HasValue)
                {
                    expirationTime = DateTime.UtcNow.Add(duration.Value);
                }

                settings.TradingLock = new LockInfo
                {
                    IsLocked = isLocked,
                    LockTime = isLocked ? DateTime.UtcNow : null,
                    LockDayOfWeek = isLocked ? DateTime.UtcNow.DayOfWeek : null,
                    LockReason = reason,
                    LockDuration = duration,
                    LockExpirationTime = expirationTime
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
            if (settings?.TradingLock == null || !settings.TradingLock.IsLocked)
                return false;

            // Check if lock has expired
            if (settings.TradingLock.LockExpirationTime.HasValue)
            {
                if (DateTime.UtcNow >= settings.TradingLock.LockExpirationTime.Value)
                {
                    // Lock has expired, auto-unlock
                    SetTradingLock(accountNumber, false, "Auto-unlocked after duration expired");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets the remaining lock time for an account. Returns null if not locked or lock is indefinite.
        /// </summary>
        public TimeSpan? GetRemainingLockTime(string accountNumber)
        {
            var settings = GetSettings(accountNumber);
            if (settings?.TradingLock == null || !settings.TradingLock.IsLocked)
                return null;

            if (!settings.TradingLock.LockExpirationTime.HasValue)
                return null; // Indefinite lock

            var remaining = settings.TradingLock.LockExpirationTime.Value - DateTime.UtcNow;
            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }

        /// <summary>
        /// Gets the lock status string with remaining time.
        /// </summary>
        public string GetLockStatusString(string accountNumber)
        {
            if (!IsTradingLocked(accountNumber))
                return "Unlocked";

            var remainingTime = GetRemainingLockTime(accountNumber);
            if (!remainingTime.HasValue)
                return "Locked";

            // Format remaining time
            var ts = remainingTime.Value;
            if (ts.TotalDays >= 1)
                return $"Locked ({ts.Days}d {ts.Hours}h {ts.Minutes}m)";
            else if (ts.TotalHours >= 1)
                return $"Locked ({ts.Hours}h {ts.Minutes}m)";
            else if (ts.TotalMinutes >= 1)
                return $"Locked ({ts.Minutes}m)";
            else
                return "Locked (<1m)"; // Less than 1 minute remaining
        }

        public bool AreSettingsLocked(string accountNumber)
        {
            var settings = GetSettings(accountNumber);
            return settings?.SettingsLock?.IsLocked == true;
        }

        #endregion

        #region Daily Loss Warning Management

        /// <summary>
        /// Records that a daily loss warning notification has been sent.
        /// </summary>
        public void SetDailyLossWarningSent(string accountNumber, decimal pnlValue)
        {
            var settings = GetOrCreateSettings(accountNumber);
            if (settings != null)
            {
                settings.DailyLossWarning = new DailyLossWarningInfo
                {
                    WarningNotificationSent = true,
                    WarningDate = DateTime.UtcNow.Date,
                    WarningPnLValue = pnlValue
                };
                SaveSettings(settings);
            }
        }

        /// <summary>
        /// Checks if a daily loss warning has already been sent today.
        /// Automatically resets if the date has changed.
        /// 
        /// TIMEZONE NOTE: Uses UTC for date comparison to ensure consistent behavior across timezones.
        /// While account locks use 5 PM ET for expiration, warning resets are based on UTC midnight
        /// for simplicity and to prevent edge cases with timezone conversions. This means:
        /// - Warnings reset at UTC midnight (e.g., 7 PM ET / 4 PM PT in winter)
        /// - Account locks expire at 5 PM ET (market close time)
        /// This design is intentional to decouple warning resets from trading hours and ensure
        /// warnings are cleared daily regardless of when trading occurs globally.
        /// </summary>
        public bool HasDailyLossWarningSent(string accountNumber)
        {
            var settings = GetSettings(accountNumber);
            if (settings?.DailyLossWarning == null)
                return false;

            // Reset warning if it's a new day (UTC-based for consistency)
            var today = DateTime.UtcNow.Date;
            if (settings.DailyLossWarning.WarningDate.HasValue &&
                settings.DailyLossWarning.WarningDate.Value.Date != today)
            {
                // New day - reset the warning
                ResetDailyLossWarning(accountNumber);
                return false;
            }

            return settings.DailyLossWarning.WarningNotificationSent;
        }

        /// <summary>
        /// Resets the daily loss warning state (called at the start of a new trading day).
        /// </summary>
        public void ResetDailyLossWarning(string accountNumber)
        {
            var settings = GetSettings(accountNumber);
            if (settings != null)
            {
                settings.DailyLossWarning = null;
                SaveSettings(settings);
            }
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
        
        // Daily Loss Limit Warning Tracking
        public DailyLossWarningInfo? DailyLossWarning { get; set; }
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
        
        /// <summary>
        /// The duration of the lock (optional). If null, lock is indefinite.
        /// </summary>
        public TimeSpan? LockDuration { get; set; }
        
        /// <summary>
        /// The time when the lock will expire (optional). If null, lock is indefinite.
        /// </summary>
        public DateTime? LockExpirationTime { get; set; }
    }

    /// <summary>
    /// Daily Loss Warning tracking data class.
    /// Tracks when the 80% warning was last sent to prevent duplicate notifications.
    /// </summary>
    public class DailyLossWarningInfo
    {
        /// <summary>
        /// Whether a warning has been sent for the current day
        /// </summary>
        public bool WarningNotificationSent { get; set; }
        
        /// <summary>
        /// The date when the warning was sent (used to reset daily)
        /// </summary>
        public DateTime? WarningDate { get; set; }
        
        /// <summary>
        /// The P&L value when the warning was sent (for audit purposes)
        /// </summary>
        public decimal? WarningPnLValue { get; set; }
    }

    #endregion
}
