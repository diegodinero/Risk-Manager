# CSV Import Account Mapping Fix

## Problem
CSV imports were using simple account numbers (e.g., "FFN-25S951058292787") directly, but the Risk Manager system uses unique account identifiers constructed from multiple properties (e.g., "Rithmic_FFN-25S951058292787" or "Account_0_FFN-25S951058292787").

This caused:
- CSV imports to create separate journal entries that didn't match dropdown accounts
- Manual trade entries to save to wrong accounts if not carefully managed
- Inconsistency between CSV and manual trade account assignment

## Solution

### 1. Reverted Manual Trade Entry Changes
- **Removed** account ComboBox from TradeEntryDialog
- Manual trades now consistently use the dropdown-selected account
- Simpler, more predictable behavior

### 2. Fixed CSV Import Account Matching
Added `MapCsvAccountsToDropdownAccounts()` method that:

1. **Extracts CSV accounts**: Gets unique account numbers from CSV trades
2. **Matches to dropdown**: Iterates through Core.Instance.Accounts to find matching accounts
3. **Uses fuzzy matching**: Checks if CSV account is contained in or contains dropdown account ID/Name
4. **Maps to unique ID**: Uses GetUniqueAccountIdentifier to get the proper unique ID
5. **Updates trades**: Replaces simple CSV account with unique ID before import

### 3. Integration with ImportCsv_Click
The import process now:

```csharp
// 1. Parse CSV file
var result = csvService.ParseCsvFile(openDialog.FileName);

// 2. Show preview dialog
using (var previewDialog = new CsvImportPreviewDialog(...))
{
    if (previewDialog.ShowDialog() == DialogResult.OK)
    {
        var tradesToImport = previewDialog.SelectedTrades;
        
        // 3. MAP CSV ACCOUNTS TO DROPDOWN ACCOUNTS (NEW!)
        var accountMapping = MapCsvAccountsToDropdownAccounts(tradesToImport);
        
        // 4. UPDATE TRADE ACCOUNTS TO USE UNIQUE IDS (NEW!)
        foreach (var trade in tradesToImport)
        {
            if (!string.IsNullOrEmpty(trade.Account) && 
                accountMapping.ContainsKey(trade.Account))
            {
                trade.Account = accountMapping[trade.Account];
            }
        }
        
        // 5. Import with mapped accounts
        var importResults = TradingJournalService.Instance
            .ImportTradesToRespectiveAccounts(tradesToImport);
    }
}
```

## Example Flow

### CSV File Contains:
```csv
Account,Date/Time,Symbol,Side,...
FFN-25S951058292787 LilDee249,2/9/2026 9:44:56 AM,MNQH6,Buy,...
FFN-25S608685181937 Trader123,2/9/2026 10:15:30 AM,MESH6,Buy,...
```

### Dropdown Accounts (core.Accounts):
- Account 0: Rithmic connection, Id="FFN-25S951058292787", Name="LilDee249"
- Account 1: Rithmic connection, Id="FFN-25S608685181937", Name="Trader123"

### Mapping Process:
1. CSV account "FFN-25S951058292787" matches Account 0 (contains Id)
2. GetUniqueAccountIdentifier(Account 0, 0) returns "Rithmic_FFN-25S951058292787"
3. CSV account "FFN-25S608685181937" matches Account 1 (contains Id)
4. GetUniqueAccountIdentifier(Account 1, 1) returns "Rithmic_FFN-25S608685181937"

### Result:
- Trade 1: Account = "Rithmic_FFN-25S951058292787" (matches dropdown)
- Trade 2: Account = "Rithmic_FFN-25S608685181937" (matches dropdown)
- Both import to correct journal accounts that correspond to dropdown accounts

## Benefits

1. **Consistency**: CSV imports now use same account ID system as dropdown
2. **Correct Routing**: Trades import to proper accounts that match UI
3. **No Manual Work**: Automatic matching, no user intervention needed
4. **Robust Matching**: Fuzzy matching handles variations in account format
5. **Simple Manual Entry**: Manual trades use dropdown account (clear, predictable)

## Technical Details

### GetUniqueAccountIdentifier Logic
The system constructs unique IDs using multiple strategies:

```
Strategy 1: Connection.Name + Account.Name
  Example: "Rithmic_FFN-25S951058292787"

Strategy 2: Index-based with Name
  Example: "Account_0_FFN-25S951058292787"

Strategy 3: Fallback to Id or Name
  Example: "FFN-25S951058292787"
```

### Matching Algorithm
```csharp
// Check if CSV account matches dropdown account
if (accountId.Contains(csvAccount) || csvAccount.Contains(accountId) ||
    accountName.Contains(csvAccount) || csvAccount.Contains(accountName))
{
    // Match found!
    var uniqueId = GetUniqueAccountIdentifier(account, accountIndex);
    mapping[csvAccount] = uniqueId;
}
```

This handles:
- Exact matches: "FFN-25S951058292787" == "FFN-25S951058292787"
- Substring matches: "FFN-25S951058292787" contains "FFN-25S951058"
- Reverse matches: "LilDee249" is in "FFN-25S951058292787 LilDee249"

## Files Changed

### RiskManagerControl.cs
- Added `MapCsvAccountsToDropdownAccounts()` method
- Modified `ImportCsv_Click()` to call mapping before import
- Updated trade.Account properties before import

### TradeEntryDialog.cs (Reverted)
- Removed account ComboBox field
- Restored using _accountNumber from constructor
- Manual trades use dropdown-selected account

### TradingJournalService.cs (Reverted)
- Removed GetAllAccountNumbers() method
- No longer needed with dropdown-based approach

## Testing

### Manual Testing Required
1. **Manual Trade Entry**:
   - Select account in dropdown
   - Add new trade
   - Verify trade saved to correct account
   - Verify trade appears in Trade Log for that account

2. **CSV Import**:
   - Create CSV with known account numbers
   - Ensure those accounts exist in dropdown
   - Import CSV
   - Verify trades route to correct dropdown accounts
   - Check trade Account properties match unique IDs

3. **Multi-Account CSV**:
   - CSV with trades from 2+ accounts
   - All accounts exist in dropdown
   - Import and verify routing
   - Check success message shows correct breakdown

### Expected Behavior
- Manual trades: Always use dropdown account (simple)
- CSV imports: Map to dropdown accounts (automatic)
- No account mismatches
- No duplicate accounts created
- Clear success messages showing which accounts received trades

## Migration Notes
- Existing journal data unaffected (uses existing account IDs)
- Future CSV imports will map correctly
- Manual trades work as before (dropdown-based)
- No database changes needed
- No user action required

## Security
- CodeQL scan: No vulnerabilities found
- No SQL injection risks (no database)
- No path traversal (uses file dialogs)
- Account matching is read-only operation
- No credential exposure
