#nullable enable

namespace Risk_Manager.Data.Entities
{
    /// <summary>
    /// Represents a blocked symbol for an account.
    /// </summary>
    public class BlockedSymbol
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to AccountSettings.
        /// </summary>
        public int AccountSettingsId { get; set; }

        /// <summary>
        /// The symbol name that is blocked (e.g., "AAPL", "MSFT").
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        // Navigation property
        public virtual AccountSettings AccountSettings { get; set; } = null!;
    }
}
