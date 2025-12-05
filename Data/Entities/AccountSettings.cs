using System;
using System.Collections.Generic;

#nullable enable

namespace Risk_Manager.Data.Entities
{
    /// <summary>
    /// Represents per-account risk management settings stored in the local SQLite database.
    /// </summary>
    public class AccountSettings
    {
        public int Id { get; set; }

        /// <summary>
        /// The unique account number/identifier.
        /// </summary>
        public string AccountNumber { get; set; } = string.Empty;

        /// <summary>
        /// Daily Loss Limit in USD. Null means not set.
        /// </summary>
        public decimal? DailyLossLimit { get; set; }

        /// <summary>
        /// Daily Profit Target in USD. Null means not set.
        /// </summary>
        public decimal? DailyProfitTarget { get; set; }

        /// <summary>
        /// Position Loss Limit in USD. Null means not set.
        /// </summary>
        public decimal? PositionLossLimit { get; set; }

        /// <summary>
        /// Position Profit Target in USD. Null means not set.
        /// </summary>
        public decimal? PositionProfitTarget { get; set; }

        /// <summary>
        /// Default contract limit for all symbols. Null means not set.
        /// </summary>
        public int? DefaultContractLimit { get; set; }

        /// <summary>
        /// When the record was created.
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the record was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ICollection<BlockedSymbol> BlockedSymbols { get; set; } = new List<BlockedSymbol>();
        public virtual ICollection<SymbolContractLimit> SymbolContractLimits { get; set; } = new List<SymbolContractLimit>();
        public virtual ICollection<TradingTimeRestriction> TradingTimeRestrictions { get; set; } = new List<TradingTimeRestriction>();
        public virtual TradingLock? TradingLock { get; set; }
        public virtual SettingsLock? SettingsLock { get; set; }
    }
}
