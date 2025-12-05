using System;

#nullable enable

namespace Risk_Manager.Data.Entities
{
    /// <summary>
    /// Represents a trading lock for an account (prevents trading).
    /// </summary>
    public class TradingLock
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to AccountSettings.
        /// </summary>
        public int AccountSettingsId { get; set; }

        /// <summary>
        /// Whether trading is currently locked.
        /// </summary>
        public bool IsLocked { get; set; }

        /// <summary>
        /// The time when the lock was activated. Null if not locked.
        /// </summary>
        public DateTime? LockTime { get; set; }

        /// <summary>
        /// The day of week when the lock was set. Null if not applicable.
        /// </summary>
        public DayOfWeek? LockDayOfWeek { get; set; }

        /// <summary>
        /// Reason for the lock (optional).
        /// </summary>
        public string? LockReason { get; set; }

        // Navigation property
        public virtual AccountSettings AccountSettings { get; set; } = null!;
    }
}
