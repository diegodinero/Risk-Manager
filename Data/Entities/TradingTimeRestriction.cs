using System;

#nullable enable

namespace Risk_Manager.Data.Entities
{
    /// <summary>
    /// Represents a trading time restriction window for an account.
    /// </summary>
    public class TradingTimeRestriction
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to AccountSettings.
        /// </summary>
        public int AccountSettingsId { get; set; }

        /// <summary>
        /// The day of week this restriction applies to.
        /// </summary>
        public DayOfWeek DayOfWeek { get; set; }

        /// <summary>
        /// Start time of the allowed trading window (in local time).
        /// </summary>
        public TimeSpan StartTime { get; set; }

        /// <summary>
        /// End time of the allowed trading window (in local time).
        /// </summary>
        public TimeSpan EndTime { get; set; }

        /// <summary>
        /// Whether trading is allowed during this time window.
        /// </summary>
        public bool IsAllowed { get; set; } = true;

        /// <summary>
        /// Optional name/description for this time window (e.g., "NY Session").
        /// </summary>
        public string? Name { get; set; }

        // Navigation property
        public virtual AccountSettings AccountSettings { get; set; } = null!;
    }
}
