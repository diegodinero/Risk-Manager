using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Risk_Manager.Data
{
    /// <summary>
    /// Service for importing trades from CSV files (trading platform exports)
    /// </summary>
    public class CsvImportService
    {
        /// <summary>
        /// Represents a raw CSV row from the trading platform export
        /// </summary>
        private class CsvRow
        {
            public string Account { get; set; }
            public string DateTime { get; set; }
            public string Symbol { get; set; }
            public string Description { get; set; }
            public string SymbolType { get; set; }
            public string ExpirationDate { get; set; }
            public string StrikePrice { get; set; }
            public string Side { get; set; }
            public string OrderType { get; set; }
            public string Quantity { get; set; }
            public string Price { get; set; }
            public string GrossPL { get; set; }
            public string Fee { get; set; }
            public string NetPL { get; set; }
            public string TradeValue { get; set; }
            public string TradeID { get; set; }
            public string OrderID { get; set; }
            public string PositionID { get; set; }
            public string ConnectionName { get; set; }
            public string Comment { get; set; }
            public string Exchange { get; set; }
        }

        /// <summary>
        /// Result of CSV import operation
        /// </summary>
        public class ImportResult
        {
            public List<JournalTrade> Trades { get; set; } = new List<JournalTrade>();
            public List<string> Errors { get; set; } = new List<string>();
            public List<string> Warnings { get; set; } = new List<string>();
            public int TotalRowsParsed { get; set; }
            public int SuccessfulTrades { get; set; }
        }

        /// <summary>
        /// Parses a CSV file and converts to JournalTrade objects
        /// </summary>
        public ImportResult ParseCsvFile(string filePath)
        {
            var result = new ImportResult();

            try
            {
                if (!File.Exists(filePath))
                {
                    result.Errors.Add($"File not found: {filePath}");
                    return result;
                }

                var lines = File.ReadAllLines(filePath);
                if (lines.Length < 2)
                {
                    result.Errors.Add("CSV file is empty or contains no data rows");
                    return result;
                }

                // Parse header
                var header = ParseCsvLine(lines[0]);
                var columnMap = BuildColumnMap(header);

                if (!ValidateRequiredColumns(columnMap, result.Errors))
                {
                    return result;
                }

                // Parse data rows
                var csvRows = new List<CsvRow>();
                for (int i = 1; i < lines.Length; i++)
                {
                    try
                    {
                        var values = ParseCsvLine(lines[i]);
                        if (values.Length == 0 || string.IsNullOrWhiteSpace(values[0]))
                            continue; // Skip empty lines

                        var csvRow = MapRowToCsvRow(values, columnMap);
                        csvRows.Add(csvRow);
                        result.TotalRowsParsed++;
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add($"Error parsing row {i + 1}: {ex.Message}");
                    }
                }

                // Group by Position ID and convert to trades
                var trades = ConvertToJournalTrades(csvRows, result);
                result.Trades = trades;
                result.SuccessfulTrades = trades.Count;
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Error reading CSV file: {ex.Message}");
            }

            return result;
        }

        /// <summary>
        /// Parses a single CSV line, handling quoted fields properly
        /// </summary>
        private string[] ParseCsvLine(string line)
        {
            var values = new List<string>();
            var currentValue = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.ToString().Trim());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            // Add the last value
            values.Add(currentValue.ToString().Trim());

            return values.ToArray();
        }

        /// <summary>
        /// Builds a map of column names to indices
        /// </summary>
        private Dictionary<string, int> BuildColumnMap(string[] header)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < header.Length; i++)
            {
                var columnName = header[i].Trim();
                if (!string.IsNullOrWhiteSpace(columnName))
                {
                    map[columnName] = i;
                }
            }

            return map;
        }

        /// <summary>
        /// Validates that all required columns are present
        /// </summary>
        private bool ValidateRequiredColumns(Dictionary<string, int> columnMap, List<string> errors)
        {
            var requiredColumns = new[] { "Account", "Date/Time", "Symbol", "Side", "Quantity", "Price" };
            var missingColumns = new List<string>();

            foreach (var column in requiredColumns)
            {
                if (!columnMap.ContainsKey(column))
                {
                    missingColumns.Add(column);
                }
            }

            if (missingColumns.Any())
            {
                errors.Add($"Missing required columns: {string.Join(", ", missingColumns)}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Maps a CSV row to a CsvRow object
        /// </summary>
        private CsvRow MapRowToCsvRow(string[] values, Dictionary<string, int> columnMap)
        {
            var row = new CsvRow();

            row.Account = GetValue(values, columnMap, "Account");
            row.DateTime = GetValue(values, columnMap, "Date/Time");
            row.Symbol = GetValue(values, columnMap, "Symbol");
            row.Description = GetValue(values, columnMap, "Description");
            row.SymbolType = GetValue(values, columnMap, "Symbol type");
            row.ExpirationDate = GetValue(values, columnMap, "Expiration date");
            row.StrikePrice = GetValue(values, columnMap, "Strike price");
            row.Side = GetValue(values, columnMap, "Side");
            row.OrderType = GetValue(values, columnMap, "Order type");
            row.Quantity = GetValue(values, columnMap, "Quantity");
            row.Price = GetValue(values, columnMap, "Price");
            row.GrossPL = GetValue(values, columnMap, "Gross P/L");
            row.Fee = GetValue(values, columnMap, "Fee");
            row.NetPL = GetValue(values, columnMap, "Net P/L");
            row.TradeValue = GetValue(values, columnMap, "Trade value");
            row.TradeID = GetValue(values, columnMap, "Trade ID");
            row.OrderID = GetValue(values, columnMap, "Order ID");
            row.PositionID = GetValue(values, columnMap, "Position ID");
            row.ConnectionName = GetValue(values, columnMap, "Connection name");
            row.Comment = GetValue(values, columnMap, "Comment");
            row.Exchange = GetValue(values, columnMap, "Exchange");

            return row;
        }

        /// <summary>
        /// Gets a value from the CSV row by column name
        /// </summary>
        private string GetValue(string[] values, Dictionary<string, int> columnMap, string columnName)
        {
            if (columnMap.TryGetValue(columnName, out int index) && index < values.Length)
            {
                return values[index];
            }
            return string.Empty;
        }

        /// <summary>
        /// Converts CSV rows to JournalTrade objects, pairing buy/sell transactions
        /// </summary>
        private List<JournalTrade> ConvertToJournalTrades(List<CsvRow> csvRows, ImportResult result)
        {
            var trades = new List<JournalTrade>();

            // Group by Position ID (if available) or Trade ID
            var groups = csvRows
                .Where(r => !string.IsNullOrWhiteSpace(r.PositionID) || !string.IsNullOrWhiteSpace(r.TradeID))
                .GroupBy(r => string.IsNullOrWhiteSpace(r.PositionID) ? r.TradeID : r.PositionID)
                .ToList();

            // Process rows without Position ID or Trade ID separately
            var ungroupedRows = csvRows
                .Where(r => string.IsNullOrWhiteSpace(r.PositionID) && string.IsNullOrWhiteSpace(r.TradeID))
                .ToList();

            // Process grouped trades
            foreach (var group in groups)
            {
                try
                {
                    var trade = ProcessTradeGroup(group.ToList(), result);
                    if (trade != null)
                    {
                        trades.Add(trade);
                    }
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Error processing trade group {group.Key}: {ex.Message}");
                }
            }

            // Process ungrouped rows as individual trades
            foreach (var row in ungroupedRows)
            {
                try
                {
                    var trade = ProcessSingleRow(row, result);
                    if (trade != null)
                    {
                        trades.Add(trade);
                    }
                }
                catch (Exception ex)
                {
                    result.Warnings.Add($"Error processing single row: {ex.Message}");
                }
            }

            return trades;
        }

        /// <summary>
        /// Processes a group of related CSV rows (paired trades)
        /// </summary>
        private JournalTrade ProcessTradeGroup(List<CsvRow> rows, ImportResult result)
        {
            if (rows.Count == 0)
                return null;

            // Sort by date/time
            rows = rows.OrderBy(r => ParseDateTime(r.DateTime)).ToList();

            var firstRow = rows[0];
            var lastRow = rows[rows.Count - 1];

            var trade = new JournalTrade();

            // Basic info from first row
            trade.Symbol = firstRow.Symbol;
            trade.Account = ExtractAccountNumber(firstRow.Account);

            // Determine trade type from first row (entry)
            trade.TradeType = DetermineTradeType(firstRow.Side);

            // Date from first row
            var entryDate = ParseDateTime(firstRow.DateTime);
            trade.Date = entryDate.Date;
            trade.EntryTime = entryDate.ToString("h:mm:ss tt");

            // Exit time from last row
            var exitDate = ParseDateTime(lastRow.DateTime);
            trade.ExitTime = exitDate.ToString("h:mm:ss tt");

            // Entry and exit prices
            trade.EntryPrice = ParseDecimal(firstRow.Price);
            trade.ExitPrice = ParseDecimal(lastRow.Price);

            // Contracts (use absolute value)
            trade.Contracts = Math.Abs(ParseInt(firstRow.Quantity));

            // Calculate P/L and fees
            decimal totalGrossPL = 0;
            decimal totalFees = 0;
            decimal totalNetPL = 0;

            foreach (var row in rows)
            {
                totalGrossPL += ParseDecimal(row.GrossPL);
                totalFees += ParseDecimal(row.Fee);
                totalNetPL += ParseDecimal(row.NetPL);
            }

            trade.PL = totalGrossPL;
            trade.Fees = totalFees;

            // Determine outcome based on Net P/L
            trade.Outcome = DetermineOutcome(totalNetPL);

            // Combine order type and comment for notes
            var notes = new List<string>();
            if (!string.IsNullOrWhiteSpace(firstRow.OrderType))
                notes.Add($"Order Type: {firstRow.OrderType}");
            if (!string.IsNullOrWhiteSpace(firstRow.Comment))
                notes.Add(firstRow.Comment);
            trade.Notes = string.Join(" | ", notes);

            return trade;
        }

        /// <summary>
        /// Processes a single CSV row as an independent trade
        /// </summary>
        private JournalTrade ProcessSingleRow(CsvRow row, ImportResult result)
        {
            var trade = new JournalTrade();

            trade.Symbol = row.Symbol;
            trade.Account = ExtractAccountNumber(row.Account);
            trade.TradeType = DetermineTradeType(row.Side);

            var tradeDate = ParseDateTime(row.DateTime);
            trade.Date = tradeDate.Date;
            trade.EntryTime = tradeDate.ToString("h:mm:ss tt");
            trade.ExitTime = tradeDate.ToString("h:mm:ss tt");

            trade.EntryPrice = ParseDecimal(row.Price);
            trade.ExitPrice = ParseDecimal(row.Price);
            trade.Contracts = Math.Abs(ParseInt(row.Quantity));

            trade.PL = ParseDecimal(row.GrossPL);
            trade.Fees = ParseDecimal(row.Fee);

            var netPL = ParseDecimal(row.NetPL);
            trade.Outcome = DetermineOutcome(netPL);

            var notes = new List<string>();
            if (!string.IsNullOrWhiteSpace(row.OrderType))
                notes.Add($"Order Type: {row.OrderType}");
            if (!string.IsNullOrWhiteSpace(row.Comment))
                notes.Add(row.Comment);
            trade.Notes = string.Join(" | ", notes);

            return trade;
        }

        /// <summary>
        /// Parses date/time string (format: "M/d/yyyy h:mm:ss tt")
        /// </summary>
        private DateTime ParseDateTime(string dateTimeStr)
        {
            if (string.IsNullOrWhiteSpace(dateTimeStr))
                return DateTime.MinValue;

            try
            {
                // Try multiple formats
                var formats = new[]
                {
                    "M/d/yyyy h:mm:ss tt",
                    "M/d/yyyy hh:mm:ss tt",
                    "MM/dd/yyyy h:mm:ss tt",
                    "MM/dd/yyyy hh:mm:ss tt",
                    "M/d/yyyy H:mm:ss",
                    "MM/dd/yyyy H:mm:ss"
                };

                foreach (var format in formats)
                {
                    if (DateTime.TryParseExact(dateTimeStr, format, CultureInfo.InvariantCulture, 
                        DateTimeStyles.None, out DateTime result))
                    {
                        return result;
                    }
                }

                // Fallback to general parsing
                if (DateTime.TryParse(dateTimeStr, out DateTime fallbackResult))
                {
                    return fallbackResult;
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return DateTime.MinValue;
        }

        /// <summary>
        /// Parses a decimal value from string
        /// </summary>
        private decimal ParseDecimal(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0m;

            // Remove any currency symbols or commas
            value = value.Replace("$", "").Replace(",", "").Trim();

            if (decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }

            return 0m;
        }

        /// <summary>
        /// Parses an integer value from string
        /// </summary>
        private int ParseInt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return 0;

            value = value.Replace(",", "").Trim();

            if (int.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out int result))
            {
                return result;
            }

            return 0;
        }

        /// <summary>
        /// Extracts account number from account field (handles formats like "FFN-25S951058292787 LilDee249")
        /// </summary>
        private string ExtractAccountNumber(string accountField)
        {
            if (string.IsNullOrWhiteSpace(accountField))
                return string.Empty;

            // Take everything before the first space (if any)
            var parts = accountField.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length > 0 ? parts[0] : accountField;
        }

        /// <summary>
        /// Determines trade type from side (Buy = Long, Sell = Short)
        /// </summary>
        private string DetermineTradeType(string side)
        {
            if (string.IsNullOrWhiteSpace(side))
                return string.Empty;

            side = side.Trim().ToLower();

            if (side == "buy")
                return "Long";
            else if (side == "sell")
                return "Short";

            return string.Empty;
        }

        /// <summary>
        /// Determines outcome based on Net P/L
        /// </summary>
        private string DetermineOutcome(decimal netPL)
        {
            if (netPL > 0)
                return "Win";
            else if (netPL < 0)
                return "Loss";
            else
                return "Breakeven";
        }
    }
}
