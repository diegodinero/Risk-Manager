using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

#nullable enable

namespace Risk_Manager.Data
{
    /// <summary>
    /// Lightweight settings service using Microsoft.Data.Sqlite directly (no EF Core).
    /// Provides per-account risk management settings persistence with caching.
    /// </summary>
    public class RiskManagerSettingsService : IDisposable
    {
        private static readonly Lazy<RiskManagerSettingsService> _instance =
            new(() => new RiskManagerSettingsService(), System.Threading.LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// Gets the singleton instance of the settings service.
        /// </summary>
        public static RiskManagerSettingsService Instance => _instance.Value;

        private readonly string _connectionString = string.Empty;
        private readonly ConcurrentDictionary<string, CachedAccountSettings> _cache = new();
        private readonly TimeSpan _cacheExpiration = TimeSpan.FromSeconds(30);
        private readonly object _dbLock = new();
        private bool _disposed;
        private bool _isInitialized;
        private string? _initializationError;

        private RiskManagerSettingsService()
        {
            try
            {
                var dbPath = GetDatabasePath();
                _connectionString = $"Data Source={dbPath}";
                InitializeDatabase();
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

        private static string GetDatabasePath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var riskManagerPath = Path.Combine(appDataPath, "RiskManager");
            
            if (!Directory.Exists(riskManagerPath))
            {
                Directory.CreateDirectory(riskManagerPath);
            }

            return Path.Combine(riskManagerPath, "riskmanager.db");
        }

        private void InitializeDatabase()
        {
            lock (_dbLock)
            {
                using var connection = new SqliteConnection(_connectionString);
                connection.Open();

                using var command = connection.CreateCommand();
                command.CommandText = @"
                    CREATE TABLE IF NOT EXISTS AccountSettings (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        AccountNumber TEXT NOT NULL UNIQUE,
                        DailyLossLimit REAL,
                        DailyProfitTarget REAL,
                        PositionLossLimit REAL,
                        PositionProfitTarget REAL,
                        DefaultContractLimit INTEGER,
                        CreatedAt TEXT NOT NULL,
                        UpdatedAt TEXT NOT NULL
                    );

                    CREATE TABLE IF NOT EXISTS BlockedSymbols (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        AccountNumber TEXT NOT NULL,
                        Symbol TEXT NOT NULL,
                        UNIQUE(AccountNumber, Symbol)
                    );

                    CREATE TABLE IF NOT EXISTS SymbolContractLimits (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        AccountNumber TEXT NOT NULL,
                        Symbol TEXT NOT NULL,
                        ContractLimit INTEGER NOT NULL,
                        UNIQUE(AccountNumber, Symbol)
                    );

                    CREATE TABLE IF NOT EXISTS TradingTimeRestrictions (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        AccountNumber TEXT NOT NULL,
                        DayOfWeek INTEGER NOT NULL,
                        StartTime TEXT NOT NULL,
                        EndTime TEXT NOT NULL,
                        IsAllowed INTEGER NOT NULL,
                        Name TEXT
                    );

                    CREATE TABLE IF NOT EXISTS TradingLocks (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        AccountNumber TEXT NOT NULL UNIQUE,
                        IsLocked INTEGER NOT NULL,
                        LockTime TEXT,
                        LockDayOfWeek INTEGER,
                        LockReason TEXT
                    );

                    CREATE TABLE IF NOT EXISTS SettingsLocks (
                        Id INTEGER PRIMARY KEY AUTOINCREMENT,
                        AccountNumber TEXT NOT NULL UNIQUE,
                        IsLocked INTEGER NOT NULL,
                        LockTime TEXT,
                        LockDayOfWeek INTEGER,
                        LockReason TEXT
                    );

                    CREATE INDEX IF NOT EXISTS idx_blocked_symbols_account ON BlockedSymbols(AccountNumber);
                    CREATE INDEX IF NOT EXISTS idx_symbol_limits_account ON SymbolContractLimits(AccountNumber);
                    CREATE INDEX IF NOT EXISTS idx_time_restrictions_account ON TradingTimeRestrictions(AccountNumber);
                ";
                command.ExecuteNonQuery();
            }
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
                lock (_dbLock)
                {
                    using var connection = new SqliteConnection(_connectionString);
                    connection.Open();

                    var settings = LoadAccountSettings(connection, accountNumber);
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

        private AccountSettings? LoadAccountSettings(SqliteConnection connection, string accountNumber)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM AccountSettings WHERE AccountNumber = @accountNumber";
            cmd.Parameters.AddWithValue("@accountNumber", accountNumber);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            var settings = new AccountSettings
            {
                Id = reader.GetInt32(0),
                AccountNumber = reader.GetString(1),
                DailyLossLimit = reader.IsDBNull(2) ? null : (decimal?)reader.GetDouble(2),
                DailyProfitTarget = reader.IsDBNull(3) ? null : (decimal?)reader.GetDouble(3),
                PositionLossLimit = reader.IsDBNull(4) ? null : (decimal?)reader.GetDouble(4),
                PositionProfitTarget = reader.IsDBNull(5) ? null : (decimal?)reader.GetDouble(5),
                DefaultContractLimit = reader.IsDBNull(6) ? null : (int?)reader.GetInt32(6),
                CreatedAt = DateTime.Parse(reader.GetString(7)),
                UpdatedAt = DateTime.Parse(reader.GetString(8))
            };

            // Load related data
            settings.BlockedSymbols = LoadBlockedSymbols(connection, accountNumber);
            settings.SymbolContractLimits = LoadSymbolContractLimits(connection, accountNumber);
            settings.TradingTimeRestrictions = LoadTradingTimeRestrictions(connection, accountNumber);
            settings.TradingLock = LoadTradingLock(connection, accountNumber);
            settings.SettingsLock = LoadSettingsLock(connection, accountNumber);

            return settings;
        }

        private List<string> LoadBlockedSymbols(SqliteConnection connection, string accountNumber)
        {
            var symbols = new List<string>();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Symbol FROM BlockedSymbols WHERE AccountNumber = @accountNumber";
            cmd.Parameters.AddWithValue("@accountNumber", accountNumber);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                symbols.Add(reader.GetString(0));
            }
            return symbols;
        }

        private Dictionary<string, int> LoadSymbolContractLimits(SqliteConnection connection, string accountNumber)
        {
            var limits = new Dictionary<string, int>();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT Symbol, ContractLimit FROM SymbolContractLimits WHERE AccountNumber = @accountNumber";
            cmd.Parameters.AddWithValue("@accountNumber", accountNumber);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                limits[reader.GetString(0)] = reader.GetInt32(1);
            }
            return limits;
        }

        private List<TradingTimeRestriction> LoadTradingTimeRestrictions(SqliteConnection connection, string accountNumber)
        {
            var restrictions = new List<TradingTimeRestriction>();
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT DayOfWeek, StartTime, EndTime, IsAllowed, Name FROM TradingTimeRestrictions WHERE AccountNumber = @accountNumber";
            cmd.Parameters.AddWithValue("@accountNumber", accountNumber);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                restrictions.Add(new TradingTimeRestriction
                {
                    DayOfWeek = (DayOfWeek)reader.GetInt32(0),
                    StartTime = TimeSpan.Parse(reader.GetString(1)),
                    EndTime = TimeSpan.Parse(reader.GetString(2)),
                    IsAllowed = reader.GetInt32(3) == 1,
                    Name = reader.IsDBNull(4) ? null : reader.GetString(4)
                });
            }
            return restrictions;
        }

        private LockInfo? LoadTradingLock(SqliteConnection connection, string accountNumber)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT IsLocked, LockTime, LockDayOfWeek, LockReason FROM TradingLocks WHERE AccountNumber = @accountNumber";
            cmd.Parameters.AddWithValue("@accountNumber", accountNumber);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new LockInfo
            {
                IsLocked = reader.GetInt32(0) == 1,
                LockTime = reader.IsDBNull(1) ? null : DateTime.Parse(reader.GetString(1)),
                LockDayOfWeek = reader.IsDBNull(2) ? null : (DayOfWeek?)reader.GetInt32(2),
                LockReason = reader.IsDBNull(3) ? null : reader.GetString(3)
            };
        }

        private LockInfo? LoadSettingsLock(SqliteConnection connection, string accountNumber)
        {
            using var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT IsLocked, LockTime, LockDayOfWeek, LockReason FROM SettingsLocks WHERE AccountNumber = @accountNumber";
            cmd.Parameters.AddWithValue("@accountNumber", accountNumber);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return null;

            return new LockInfo
            {
                IsLocked = reader.GetInt32(0) == 1,
                LockTime = reader.IsDBNull(1) ? null : DateTime.Parse(reader.GetString(1)),
                LockDayOfWeek = reader.IsDBNull(2) ? null : (DayOfWeek?)reader.GetInt32(2),
                LockReason = reader.IsDBNull(3) ? null : reader.GetString(3)
            };
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
                lock (_dbLock)
                {
                    using var connection = new SqliteConnection(_connectionString);
                    connection.Open();

                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = @"
                        INSERT INTO AccountSettings (AccountNumber, CreatedAt, UpdatedAt)
                        VALUES (@accountNumber, @now, @now)";
                    cmd.Parameters.AddWithValue("@accountNumber", accountNumber);
                    cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));
                    cmd.ExecuteNonQuery();

                    InvalidateCache(accountNumber);
                    return GetSettings(accountNumber);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetOrCreateSettings failed: {ex}");
                return null;
            }
        }

        #endregion

        #region Update Methods

        public void UpdateDailyLossLimit(string accountNumber, decimal? limit)
        {
            UpdateSettingsField(accountNumber, "DailyLossLimit", limit.HasValue ? (object)(double)limit.Value : DBNull.Value);
        }

        public void UpdateDailyProfitTarget(string accountNumber, decimal? target)
        {
            UpdateSettingsField(accountNumber, "DailyProfitTarget", target.HasValue ? (object)(double)target.Value : DBNull.Value);
        }

        public void UpdatePositionLossLimit(string accountNumber, decimal? limit)
        {
            UpdateSettingsField(accountNumber, "PositionLossLimit", limit.HasValue ? (object)(double)limit.Value : DBNull.Value);
        }

        public void UpdatePositionProfitTarget(string accountNumber, decimal? target)
        {
            UpdateSettingsField(accountNumber, "PositionProfitTarget", target.HasValue ? (object)(double)target.Value : DBNull.Value);
        }

        public void UpdateDefaultContractLimit(string accountNumber, int? limit)
        {
            UpdateSettingsField(accountNumber, "DefaultContractLimit", limit.HasValue ? (object)limit.Value : DBNull.Value);
        }

        private void UpdateSettingsField(string accountNumber, string fieldName, object value)
        {
            if (string.IsNullOrEmpty(accountNumber) || !_isInitialized)
                return;

            try
            {
                // Ensure account exists
                GetOrCreateSettings(accountNumber);

                lock (_dbLock)
                {
                    using var connection = new SqliteConnection(_connectionString);
                    connection.Open();

                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = $@"
                        UPDATE AccountSettings 
                        SET {fieldName} = @value, UpdatedAt = @now
                        WHERE AccountNumber = @accountNumber";
                    cmd.Parameters.AddWithValue("@value", value);
                    cmd.Parameters.AddWithValue("@now", DateTime.UtcNow.ToString("O"));
                    cmd.Parameters.AddWithValue("@accountNumber", accountNumber);
                    cmd.ExecuteNonQuery();

                    InvalidateCache(accountNumber);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UpdateSettingsField failed: {ex}");
            }
        }

        #endregion

        #region Blocked Symbols

        public void SetBlockedSymbols(string accountNumber, IEnumerable<string> symbols)
        {
            if (string.IsNullOrEmpty(accountNumber) || !_isInitialized)
                return;

            try
            {
                GetOrCreateSettings(accountNumber);

                lock (_dbLock)
                {
                    using var connection = new SqliteConnection(_connectionString);
                    connection.Open();

                    using var transaction = connection.BeginTransaction();

                    // Clear existing
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM BlockedSymbols WHERE AccountNumber = @accountNumber";
                        cmd.Parameters.AddWithValue("@accountNumber", accountNumber);
                        cmd.ExecuteNonQuery();
                    }

                    // Add new
                    foreach (var symbol in symbols.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct())
                    {
                        using var cmd = connection.CreateCommand();
                        cmd.CommandText = "INSERT INTO BlockedSymbols (AccountNumber, Symbol) VALUES (@accountNumber, @symbol)";
                        cmd.Parameters.AddWithValue("@accountNumber", accountNumber);
                        cmd.Parameters.AddWithValue("@symbol", symbol.Trim().ToUpperInvariant());
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    InvalidateCache(accountNumber);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetBlockedSymbols failed: {ex}");
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
            if (string.IsNullOrEmpty(accountNumber) || !_isInitialized)
                return;

            try
            {
                GetOrCreateSettings(accountNumber);

                lock (_dbLock)
                {
                    using var connection = new SqliteConnection(_connectionString);
                    connection.Open();

                    using var transaction = connection.BeginTransaction();

                    // Clear existing
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM SymbolContractLimits WHERE AccountNumber = @accountNumber";
                        cmd.Parameters.AddWithValue("@accountNumber", accountNumber);
                        cmd.ExecuteNonQuery();
                    }

                    // Add new
                    foreach (var kvp in symbolLimits.Where(k => !string.IsNullOrWhiteSpace(k.Key)))
                    {
                        using var cmd = connection.CreateCommand();
                        cmd.CommandText = "INSERT INTO SymbolContractLimits (AccountNumber, Symbol, ContractLimit) VALUES (@accountNumber, @symbol, @limit)";
                        cmd.Parameters.AddWithValue("@accountNumber", accountNumber);
                        cmd.Parameters.AddWithValue("@symbol", kvp.Key.Trim().ToUpperInvariant());
                        cmd.Parameters.AddWithValue("@limit", kvp.Value);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    InvalidateCache(accountNumber);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetSymbolContractLimits failed: {ex}");
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
            if (string.IsNullOrEmpty(accountNumber) || !_isInitialized)
                return;

            try
            {
                GetOrCreateSettings(accountNumber);

                lock (_dbLock)
                {
                    using var connection = new SqliteConnection(_connectionString);
                    connection.Open();

                    using var transaction = connection.BeginTransaction();

                    // Clear existing
                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = "DELETE FROM TradingTimeRestrictions WHERE AccountNumber = @accountNumber";
                        cmd.Parameters.AddWithValue("@accountNumber", accountNumber);
                        cmd.ExecuteNonQuery();
                    }

                    // Add new
                    foreach (var restriction in restrictions)
                    {
                        using var cmd = connection.CreateCommand();
                        cmd.CommandText = @"
                            INSERT INTO TradingTimeRestrictions (AccountNumber, DayOfWeek, StartTime, EndTime, IsAllowed, Name) 
                            VALUES (@accountNumber, @dayOfWeek, @startTime, @endTime, @isAllowed, @name)";
                        cmd.Parameters.AddWithValue("@accountNumber", accountNumber);
                        cmd.Parameters.AddWithValue("@dayOfWeek", (int)restriction.DayOfWeek);
                        cmd.Parameters.AddWithValue("@startTime", restriction.StartTime.ToString());
                        cmd.Parameters.AddWithValue("@endTime", restriction.EndTime.ToString());
                        cmd.Parameters.AddWithValue("@isAllowed", restriction.IsAllowed ? 1 : 0);
                        cmd.Parameters.AddWithValue("@name", restriction.Name ?? (object)DBNull.Value);
                        cmd.ExecuteNonQuery();
                    }

                    transaction.Commit();
                    InvalidateCache(accountNumber);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetTradingTimeRestrictions failed: {ex}");
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
            SetLock(accountNumber, "TradingLocks", isLocked, reason);
        }

        public void SetSettingsLock(string accountNumber, bool isLocked, string? reason = null)
        {
            SetLock(accountNumber, "SettingsLocks", isLocked, reason);
        }

        private void SetLock(string accountNumber, string tableName, bool isLocked, string? reason)
        {
            if (string.IsNullOrEmpty(accountNumber) || !_isInitialized)
                return;

            try
            {
                GetOrCreateSettings(accountNumber);

                lock (_dbLock)
                {
                    using var connection = new SqliteConnection(_connectionString);
                    connection.Open();

                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = $@"
                        INSERT INTO {tableName} (AccountNumber, IsLocked, LockTime, LockDayOfWeek, LockReason)
                        VALUES (@accountNumber, @isLocked, @lockTime, @lockDayOfWeek, @reason)
                        ON CONFLICT(AccountNumber) DO UPDATE SET
                            IsLocked = @isLocked,
                            LockTime = @lockTime,
                            LockDayOfWeek = @lockDayOfWeek,
                            LockReason = @reason";
                    cmd.Parameters.AddWithValue("@accountNumber", accountNumber);
                    cmd.Parameters.AddWithValue("@isLocked", isLocked ? 1 : 0);
                    cmd.Parameters.AddWithValue("@lockTime", isLocked ? DateTime.UtcNow.ToString("O") : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@lockDayOfWeek", isLocked ? (int)DateTime.UtcNow.DayOfWeek : (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@reason", reason ?? (object)DBNull.Value);
                    cmd.ExecuteNonQuery();

                    InvalidateCache(accountNumber);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"SetLock failed: {ex}");
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
        public int Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public decimal? DailyLossLimit { get; set; }
        public decimal? DailyProfitTarget { get; set; }
        public decimal? PositionLossLimit { get; set; }
        public decimal? PositionProfitTarget { get; set; }
        public int? DefaultContractLimit { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public List<string> BlockedSymbols { get; set; } = new();
        public Dictionary<string, int> SymbolContractLimits { get; set; } = new();
        public List<TradingTimeRestriction> TradingTimeRestrictions { get; set; } = new();
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
