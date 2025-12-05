#nullable enable

namespace Risk_Manager.Data.Entities
{
    /// <summary>
    /// Represents a symbol-specific contract limit for an account.
    /// </summary>
    public class SymbolContractLimit
    {
        public int Id { get; set; }

        /// <summary>
        /// Foreign key to AccountSettings.
        /// </summary>
        public int AccountSettingsId { get; set; }

        /// <summary>
        /// The symbol name (e.g., "AAPL", "MSFT").
        /// </summary>
        public string Symbol { get; set; } = string.Empty;

        /// <summary>
        /// The maximum number of contracts allowed for this symbol.
        /// </summary>
        public int ContractLimit { get; set; }

        // Navigation property
        public virtual AccountSettings AccountSettings { get; set; } = null!;
    }
}
