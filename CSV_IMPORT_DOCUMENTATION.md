# CSV Import Feature

This document describes the CSV import feature for the Trading Journal.

## Overview

The CSV import feature allows traders to import their trading history from trading platform exports (like Rithmic, NinjaTrader, etc.) directly into the Risk Manager trading journal.

## Features

### 1. CsvImportService
- **Location**: `Data/CsvImportService.cs`
- **Purpose**: Parses CSV files and converts them to JournalTrade objects
- **Key Features**:
  - Handles CSV files with complex formats (quoted fields, multiple columns)
  - Supports duplicate column names (uses first occurrence)
  - Groups trades by Position ID or Trade ID for paired buy/sell transactions
  - Calculates outcomes (Win/Loss/Breakeven) based on Net P/L
  - Extracts account numbers from complex formats (e.g., "FFN-25S951058292787 LilDee249")
  - Validates required columns before processing
  - Provides detailed error and warning messages

### 2. Import Preview Dialog
- **Location**: `CsvImportPreviewDialog.cs`
- **Purpose**: Shows a preview of trades before importing
- **Features**:
  - DataGridView showing all parsed trades with Account column
  - Summary statistics (total trades, unique accounts, win rate, total P/L)
  - Checkbox column to select which trades to import
  - "Select All" checkbox for bulk selection
  - Color-coded outcomes (green for wins, red for losses)
  - Displays errors and warnings from parsing
  - Shows which account each trade will be imported to
  - "Import Selected" and "Cancel" buttons

### 3. Import Button in Trade Log
- **Location**: `RiskManagerControl.cs` (Trade Log page)
- **UI Integration**: Button added to existing toolbar
- **Workflow**:
  1. User clicks "IMPORT CSV" button (no account selection required)
  2. File dialog opens to select CSV file
  3. Progress message shows while parsing
  4. Preview dialog displays parsed trades with account information
  5. User selects trades to import
  6. Trades are automatically routed to their respective accounts from CSV
  7. Duplicate detection prevents importing existing trades (per account)
  8. Success message shows trades imported per account

### 4. ImportTrades Methods
- **Location**: `Data/TradingJournalService.cs`
- **Purpose**: Imports trades into the journal with duplicate detection
- **Methods**:
  - `ImportTrades(trades, accountNumber)`: Imports all trades to a single account
  - `ImportTradesToRespectiveAccounts(trades)`: **NEW** - Routes trades to their respective accounts based on the Account property in each trade
- **Features**:
  - Checks for duplicates based on: Date, Symbol, Entry Time, P/L, and Contracts
  - Only imports new trades
  - Returns count of imported trades (or dictionary of counts per account)
  - Automatically saves journal after import
  - Supports multi-account CSV imports

## CSV Format

### Required Columns
- `Account`: Trading account identifier
- `Date/Time`: Trade date and time (format: "M/d/yyyy h:mm:ss tt")
- `Symbol`: Trading symbol (e.g., MNQH6, MESH6)
- `Side`: Buy or Sell
- `Quantity`: Number of contracts
- `Price`: Execution price

### Optional Columns (Recommended)
- `Gross P/L`: Profit/Loss in dollars
- `Fee`: Trading fees
- `Net P/L`: Net profit/loss after fees
- `Position ID`: Links related entry/exit trades
- `Trade ID`: Alternative linking method
- `Order type`: Market, Limit, Stop, etc.
- `Comment`: User notes about the trade

### Example CSV Format
```csv
Account,Date/Time,Symbol,Side,Quantity,Price,Gross P/L,Fee,Net P/L,Position ID,Order type,Comment
FFN-25S951058292787 LilDee249,2/9/2026 9:44:56 AM,MNQH6,Buy,1,25148,0,0.82,-0.82,P001,Market,
FFN-25S951058292787 LilDee249,2/9/2026 9:45:01 AM,MNQH6,Sell,-1,25158,20,0.82,19.18,P001,Market,
```

## Mapping Logic

### Trade Pairing
1. Trades with the same Position ID are grouped together
2. If no Position ID, Trade ID is used
3. Grouped trades are sorted by date/time
4. First trade is entry, last trade is exit
5. P/L and fees are summed across all trades in the group

### Field Mapping
- **Date**: Parsed from "Date/Time" column
- **Symbol**: Direct copy from "Symbol" column
- **TradeType**: "Buy" → "Long", "Sell" → "Short"
- **PL**: Sum of "Gross P/L" for all trades in group
- **Fees**: Sum of "Fee" for all trades in group
- **EntryPrice**: "Price" from first trade in group
- **ExitPrice**: "Price" from last trade in group
- **Contracts**: Absolute value of "Quantity"
- **Account**: Extracted from "Account" field (takes first part before space)
- **Notes**: Combines "Order type" and "Comment"
- **EntryTime**: Time portion of first trade
- **ExitTime**: Time portion of last trade
- **Outcome**: Calculated from Net P/L (positive = Win, negative = Loss, zero = Breakeven)

## Error Handling

### Validation
- Checks for file existence
- Verifies required columns are present
- Validates date/time formats
- Handles malformed CSV rows gracefully

### Error Messages
- **Missing required columns**: Lists which columns are missing
- **File not found**: Shows file path
- **Parsing errors**: Shows row number and error message
- **No valid trades**: Displays when CSV has no usable data

### Warnings
- Trade group processing errors (e.g., unpaired trades)
- Single row processing errors
- Duplicate trade notifications

## Duplicate Detection

Trades are considered duplicates if they match on ALL of:
- Date (same day)
- Symbol
- Entry Time
- P/L
- Contracts

This allows:
- Same symbol traded multiple times per day (different entry times)
- Same time but different outcomes (different P/L)
- Re-importing without creating duplicates

## Usage Instructions

### For End Users
1. Navigate to Trading Journal → Trade Log
2. Click "IMPORT CSV" button
3. Select your CSV export file
4. Review trades in preview dialog
5. Uncheck any trades you don't want to import
6. Click "Import Selected"
7. Verify import success message

### For Developers
```csharp
// Parse CSV file
var csvService = new CsvImportService();
var result = csvService.ParseCsvFile("path/to/file.csv");

// Check for errors
if (result.Errors.Count > 0) {
    // Handle errors
}

// Import trades to respective accounts (recommended for CSV imports)
var importResults = TradingJournalService.Instance.ImportTradesToRespectiveAccounts(
    result.Trades
);

// Check results per account
foreach (var kvp in importResults) {
    Console.WriteLine($"Account {kvp.Key}: {kvp.Value} trades imported");
}

// Or import all trades to a specific account (legacy method)
var importedCount = TradingJournalService.Instance.ImportTrades(
    result.Trades, 
    accountNumber
);
```

## Multi-Account Support

The CSV import feature fully supports importing trades from multiple accounts in a single CSV file.

### How It Works
1. Each trade row in the CSV contains an Account field
2. The parser extracts the account number (e.g., "FFN-25S951058292787" from "FFN-25S951058292787 LilDee249")
3. Trades are automatically grouped by account during import
4. Each group is imported to its respective account in the journal
5. No need to select an account before importing - routing is automatic

### Preview Dialog
- Shows Account column for each trade
- Displays count of unique accounts in summary
- Example: "Total Trades: 4 | Accounts: 2 | Wins: 3 | Losses: 1"

### Import Success Message
When importing from a multi-account CSV, the success message shows a breakdown:
```
Successfully imported 4 trade(s) to 2 account(s).

Breakdown by account:
  • FFN-25S951058292787: 2 trade(s)
  • FFN-25S608685181937: 2 trade(s)

1 duplicate(s) skipped.
```

### Benefits
- Import CSV exports from trading platform without pre-filtering by account
- One CSV file can contain trades from multiple sub-accounts
- Trades are automatically organized in the correct journal account
- Duplicate detection works per-account (same trade in different accounts is not a duplicate)

## Testing

### Unit Tests Performed
1. **CSV Parsing**: Verified correct parsing of 6 row CSV file into 3 paired trades
2. **Trade Pairing**: Confirmed Position ID grouping works correctly
3. **P/L Calculation**: Validated Gross P/L and fees are summed correctly
4. **Duplicate Detection**: Verified same trade is not imported twice
5. **Duplicate Column Names**: Confirmed first occurrence is used when column names repeat
6. **Multi-Account Import**: Verified 8 rows from 2 accounts creates 4 trades routed correctly

### Test Results
- ✓ All 6 CSV rows parsed successfully
- ✓ 3 paired trades created correctly
- ✓ P/L values match CSV data
- ✓ Duplicate detection prevents re-importing
- ✓ Outcome calculation (Win/Loss) is accurate
- ✓ Trade notes preserved from CSV comments
- ✓ Multi-account CSV (8 rows, 2 accounts) imports correctly
- ✓ Account routing works (2 trades per account)
- ✓ Account property preserved in all trades

## Known Limitations

1. **Time Zone**: Uses local time zone for date parsing
2. **Currency**: Assumes USD for all amounts
3. **Partial Fills**: Not explicitly tracked, summed into single trade
4. **Multiple Exits**: Summed into single exit, individual exits not preserved
5. **Unsupported Formats**: Only CSV files supported, not Excel or other formats

## Future Enhancements

Potential improvements for future versions:
1. Support for Excel file imports (.xlsx)
2. Custom column mapping dialog
3. Import templates for different platforms
4. Batch import (multiple files at once)
5. Trade editing after import
6. Import history/audit log
7. Rollback imported trades feature
8. Advanced duplicate detection options

## Troubleshooting

### CSV Won't Parse
- Check file encoding (UTF-8 recommended)
- Verify column headers match expected names
- Ensure Date/Time format is "M/d/yyyy h:mm:ss tt"

### Wrong P/L Values
- Verify "Gross P/L" column contains dollar amounts, not ticks
- Check that duplicate column names are handled (first occurrence used)

### Trades Not Grouping
- Ensure Position ID or Trade ID is present and consistent
- Check that entry and exit trades have same Position ID

### Import Button Not Visible
- Verify you're on the "Trade Log" page
- Check that account is selected
- Ensure window is not minimized or overlapping

## Support

For issues or questions:
1. Check error messages in preview dialog
2. Review CSV file format against requirements
3. Consult this documentation
4. Contact development team
