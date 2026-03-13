// Stub implementations of TradingPlatform.BusinessLayer types.
// Used only when the real Quantower DLLs are not present (e.g., CI/sandbox builds).
// The stubs provide just enough API surface to allow compilation; they have no runtime behaviour.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TradingPlatform.BusinessLayer
{
    // ── Core singleton ───────────────────────────────────────────────────────
    public class Core
    {
        private static readonly Core _instance = new Core();
        public static Core Instance => _instance;

        public IEnumerable<Account> Accounts { get; } = Enumerable.Empty<Account>();
        public IEnumerable<Position> Positions { get; } = Enumerable.Empty<Position>();
        public IEnumerable<Order> Orders { get; } = Enumerable.Empty<Order>();

        public IList GetTrades(TradesHistoryRequestParameters request, object? cancellationToken)
            => new List<HistoryItem>();

        public void ClosePosition(Position position) { }
        public void CancelOrder(Order order) { }

        public AdvancedTradingOperations AdvancedTradingOperations { get; } = new AdvancedTradingOperations();
    }

    public class AdvancedTradingOperations
    {
        public void Flatten() { }
    }

    // ── Account ──────────────────────────────────────────────────────────────
    public class Account
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public double Balance { get; set; }
        public ConnectionInfo? Connection { get; set; }
        public IEnumerable<AdditionalInfoItem>? AdditionalInfo { get; set; }
    }

    public class ConnectionInfo
    {
        public string? VendorName { get; set; }
        public string? Name { get; set; }
        public ConnectionState State { get; set; }
    }

    public enum ConnectionState { Connected, Disconnected }

    public class AdditionalInfoItem
    {
        public string Id { get; set; } = "";
        public object? Value { get; set; }
    }

    // ── Position ─────────────────────────────────────────────────────────────
    public class Position
    {
        public Account? Account { get; set; }
        public double Quantity { get; set; }
        public Symbol? Symbol { get; set; }
        public PnLItem? GrossPnL { get; set; }
        public PnLItem? NetPnL { get; set; }
    }

    public class PnLItem
    {
        public double Value { get; set; }
    }

    // ── Order ────────────────────────────────────────────────────────────────
    public class Order
    {
        public Account? Account { get; set; }
        public Symbol? Symbol { get; set; }
        public OrderStatus Status { get; set; }
    }

    public enum OrderStatus { None, Opened, PartiallyFilled, Filled, Canceled, Rejected }

    // ── Symbol ───────────────────────────────────────────────────────────────
    public class Symbol
    {
        public string? Name { get; set; }
        public override string ToString() => Name ?? "";
    }

    // ── Trades history ───────────────────────────────────────────────────────
    public enum TradeSide { Buy, Sell }

    public class TradesHistoryRequestParameters
    {
        public DateTime From { get; set; }
        public DateTime To { get; set; }
    }

    public class HistoryItem
    {
        public TradeSide Side { get; set; }
        public double GrossPnl { get; set; }
        public double Fee { get; set; }
        public Account? Account { get; set; }
        public Symbol? Symbol { get; set; }
        public DateTime OpenTime { get; set; }
        public DateTime CloseTime { get; set; }
        public double OpenPrice { get; set; }
        public double ClosePrice { get; set; }
        public double Quantity { get; set; }
    }

    // ── Table comparing type (used in TableColumnDefinition) ─────────────────
    public enum TableComparingType { String, Int, Double }
}
