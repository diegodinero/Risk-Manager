using System;
using System.Collections.Generic;
using System.Linq;
using TradingPlatform.BusinessLayer;

namespace Risk_Manager.Data
{
    /// <summary>
    /// Service for fetching trades from Quantower's Core.Instance.GetTrades() API
    /// and converting them to JournalTrade objects
    /// </summary>
    public class GetTradesService
    {
        /// <summary>
        /// Result of a GetTrades API fetch operation
        /// </summary>
        public class FetchResult
        {
            public List<JournalTrade> Trades { get; set; } = new List<JournalTrade>();
            public string ErrorMessage { get; set; } = null;
            public int TotalFills { get; set; }
        }

        /// <summary>
        /// Fetches trades from Quantower API for the given date range and converts
        /// them to JournalTrade objects, grouping individual fills by PositionId.
        /// </summary>
        /// <param name="from">Start of the date range (inclusive)</param>
        /// <param name="to">End of the date range (inclusive)</param>
        /// <returns>FetchResult containing journal trades and any error information</returns>
        public FetchResult FetchAndConvertTrades(DateTime from, DateTime to)
        {
            var result = new FetchResult();

            try
            {
                var request = new TradesHistoryRequestParameters
                {
                    From = from,
                    To = to
                };

                var fills = Core.Instance.GetTrades(request, null);

                if (fills == null || fills.Count == 0)
                    return result;

                result.TotalFills = fills.Count;

                var allAccounts = Core.Instance.Accounts?.ToList();
                result.Trades = ConvertToJournalTrades(fills, allAccounts);
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[GetTradesService] Error fetching trades: {ex}");
            }

            return result;
        }

        /// <summary>
        /// Converts a list of Quantower Trade fills into JournalTrade objects,
        /// grouping fills that belong to the same position (by PositionId).
        /// </summary>
        private List<JournalTrade> ConvertToJournalTrades(IList<Trade> fills, List<Account> allAccounts)
        {
            var journalTrades = new List<JournalTrade>();

            // Separate fills that have a PositionId from those that don't
            var fillsWithPosition = fills
                .Where(t => t != null && !string.IsNullOrWhiteSpace(t.PositionId))
                .ToList();

            var ungroupedFills = fills
                .Where(t => t != null && string.IsNullOrWhiteSpace(t.PositionId))
                .ToList();

            // Group fills by PositionId and convert each group to one JournalTrade
            var positionGroups = fillsWithPosition
                .GroupBy(t => t.PositionId)
                .ToList();

            foreach (var group in positionGroups)
            {
                var sortedFills = group.OrderBy(t => t.DateTime).ToList();
                var journalTrade = ConvertFillsToJournalTrade(sortedFills, allAccounts);
                if (journalTrade != null)
                    journalTrades.Add(journalTrade);
            }

            // Each ungrouped fill becomes its own JournalTrade
            foreach (var fill in ungroupedFills)
            {
                var journalTrade = ConvertFillsToJournalTrade(new List<Trade> { fill }, allAccounts);
                if (journalTrade != null)
                    journalTrades.Add(journalTrade);
            }

            return journalTrades
                .OrderByDescending(t => t.Date)
                .ThenByDescending(t => t.EntryTime)
                .ToList();
        }

        /// <summary>
        /// Converts a group of fills that belong to the same position into a single JournalTrade.
        /// The first fill represents entry; the last fill represents exit.
        /// </summary>
        private JournalTrade ConvertFillsToJournalTrade(List<Trade> fills, List<Account> allAccounts)
        {
            if (fills == null || fills.Count == 0)
                return null;

            var firstFill = fills[0];
            var lastFill = fills[fills.Count - 1];

            var journalTrade = new JournalTrade();

            // Symbol name
            journalTrade.Symbol = firstFill.Symbol?.Name ?? "";

            // Account – store raw account Id; caller can remap to journal key if needed
            journalTrade.Account = firstFill.Account?.Id ?? "";

            // Dates and times
            journalTrade.Date = firstFill.DateTime.Date;
            journalTrade.EntryTime = firstFill.DateTime.ToString("h:mm:ss tt");
            journalTrade.ExitTime = lastFill.DateTime.ToString("h:mm:ss tt");

            // Trade direction from the opening fill
            journalTrade.TradeType = DetermineTradeType(firstFill.Side);

            // Prices
            journalTrade.EntryPrice = (decimal)firstFill.Price;
            journalTrade.ExitPrice = (decimal)lastFill.Price;

            // Contracts – use the absolute quantity of the opening fill
            journalTrade.Contracts = (int)Math.Abs(firstFill.Quantity);

            // Aggregate gross P/L and fees across all fills
            decimal totalGrossPL = 0m;
            decimal totalFees = 0m;

            foreach (var fill in fills)
            {
                totalGrossPL += (decimal)(fill.GrossPnl?.Value ?? 0.0);
                totalFees += (decimal)(fill.Fee?.Value ?? 0.0);
            }

            journalTrade.PL = totalGrossPL;
            journalTrade.Fees = Math.Abs(totalFees); // Store fees as a positive amount regardless of sign

            // Outcome is based on net P/L
            decimal netPL = totalGrossPL - Math.Abs(totalFees);
            journalTrade.Outcome = DetermineOutcome(netPL);

            // Notes and FollowedPlan are left empty/false so the user can fill them in the preview dialog
            journalTrade.Notes = "";
            journalTrade.FollowedPlan = false;

            return journalTrade;
        }

        private static string DetermineTradeType(Side side)
        {
            return side == Side.Buy ? "Long" : "Short";
        }

        private static string DetermineOutcome(decimal netPL)
        {
            if (netPL > 0) return "Win";
            if (netPL < 0) return "Loss";
            return "Breakeven";
        }
    }
}
