# Calendar Method Name Fixes

## Issue

The Calendar implementation had compilation errors due to using incorrect method names:

1. **`GetSelectedAccount()`** - This method doesn't exist in the codebase
2. **`GetTradesForAccount(account)`** - This method doesn't exist in TradingJournalService

## Root Cause

When implementing the Calendar feature, I used method names that seemed logical but didn't match the actual API in the codebase:
- The correct method to get the selected account is `GetSelectedAccountNumber()` (not `GetSelectedAccount()`)
- The correct method to get trades is `GetTrades(accountNumber)` (not `GetTradesForAccount(account)`)

## Solution

Fixed both occurrences in the Calendar implementation:

### Location 1: CreateCalendarStatsPanel() - Line 13600-13601

**Before:**
```csharp
var account = GetSelectedAccount();
var trades = TradingJournalService.Instance.GetTradesForAccount(account);
```

**After:**
```csharp
var accountNumber = GetSelectedAccountNumber();
var trades = TradingJournalService.Instance.GetTrades(accountNumber);
```

### Location 2: CreateCalendarGrid() - Line 13657-13658

**Before:**
```csharp
var account = GetSelectedAccount();
var trades = TradingJournalService.Instance.GetTradesForAccount(account);
```

**After:**
```csharp
var accountNumber = GetSelectedAccountNumber();
var trades = TradingJournalService.Instance.GetTrades(accountNumber);
```

## Verification

✅ **No more compilation errors** for these method names
✅ **Consistent with existing code** - Uses the same methods as Trade Log, Trading Models, and Notes pages
✅ **Variable naming updated** - Changed from `account` to `accountNumber` for clarity

## Correct API Reference

### GetSelectedAccountNumber()
```csharp
// Defined in RiskManagerControl.cs at line 9607
private string GetSelectedAccountNumber()
{
    // Returns unique account identifier
    // Used throughout the codebase (19+ occurrences)
}
```

### TradingJournalService.GetTrades()
```csharp
// Defined in Data/TradingJournalService.cs at line 143
public List<JournalTrade> GetTrades(string accountNumber)
{
    // Returns all trades for the specified account
    // Sorted by date descending
}
```

## Testing

The fix was verified by:
1. Searching for any remaining occurrences of incorrect method names (none found)
2. Running `dotnet build` to confirm these specific errors are resolved
3. Remaining build errors are only related to missing Quantower SDK (expected in CI environment)

## Impact

- **Calendar feature** now uses correct API methods
- **No functional changes** - the logic remains the same, only method names corrected
- **Ready for testing** in Quantower environment

---

**Status**: ✅ Fixed and committed
**Commit**: e76292c
**Files Changed**: RiskManagerControl.cs (4 lines, 2 locations)
