# Account Mismatch Diagnosis Guide

## Issue: Files Exist with Data but Dashboard Shows 0

### Root Cause: Account Number Mismatch

The Trading Journal data is organized by **account number** in the JSON files. When TradeLog writes trades/models to one account and the Dashboard reads from a different account, the result is that the Dashboard shows 0 trades/models loaded despite the files existing and containing data.

## How Data is Organized

### JSON File Structure

Both `trading_journal.json` and `trading_models.json` use a Dictionary structure where:
- **Key** = Account Number (string)
- **Value** = List of trades or models for that account

```json
{
  "ACCOUNT456": [
    { "Id": "guid1", "Symbol": "ES", "NetPL": 500, ... },
    { "Id": "guid2", "Symbol": "NQ", "NetPL": 200, ... },
    ... (more trades)
  ],
  "ACCOUNT789": [
    { "Id": "guid3", "Symbol": "ES", "NetPL": -100, ... },
    ... (more trades)
  ]
}
```

### What This Means

- Data for **ACCOUNT456** is stored separately from **ACCOUNT789**
- Dashboard filters by specific account number
- If Dashboard uses **ACCOUNT123** but data exists for **ACCOUNT456**, it shows 0

## Diagnosis Steps

### Step 1: Check Dashboard Account

The debug label shows which account the Dashboard is using:
```
Dashboard READS from:
  Account Used: 'ACCOUNT123'
Trades Loaded: 0
Models Loaded: 0
```

### Step 2: Check What Accounts Exist in Files

**Manual Method:**
1. Navigate to: `%AppData%\RiskManager\Journal\`
2. Open `trading_journal.json` in a text editor
3. Look at the top-level keys - these are account numbers
4. Example:
```json
{
  "ACCOUNT456": [ ... ],  ← This account has data
  "ACCOUNT789": [ ... ]   ← This account has data
}
```

**Enhanced Debug Label Method** (Recommended):
The enhanced debug label should show:
```
ACCOUNTS IN FILES:
trading_journal.json:
  • ACCOUNT456 - 25 trades
  • ACCOUNT789 - 10 trades

trading_models.json:
  • ACCOUNT456 - 3 models
  • ACCOUNT789 - 2 models
```

### Step 3: Compare

**Dashboard Account:** ACCOUNT123
**Accounts in Files:** ACCOUNT456, ACCOUNT789

**Result:** ⚠️ MISMATCH! Dashboard account not found in files.

## Solutions

### Solution 1: Switch Dashboard to Existing Account

**Steps:**
1. Identify which account has data (e.g., ACCOUNT456)
2. Change Dashboard account selection to ACCOUNT456
3. Navigate away and back to Dashboard
4. Data should now appear

### Solution 2: Add Data for Current Dashboard Account

**Steps:**
1. Keep Dashboard on ACCOUNT123
2. Go to TradeLog page
3. Make sure ACCOUNT123 is selected
4. Add trades and models for ACCOUNT123
5. Return to Dashboard
6. Data should now appear

### Solution 3: Ensure Consistency

**Best Practice:**
- Always use the same account across all pages
- TradeLog, Dashboard, Reports should all use same account
- Verify account selection before adding data

## Enhanced Debug Label Specification

To make this diagnosis automatic, the debug label should include:

### Two Separate Lines

```
1. TradeLog WRITES to:
   File: trading_journal.json
   Location: C:\Users\...\AppData\Roaming\RiskManager\Journal

2. Dashboard READS from:
   File: trading_journal.json (SAME FILE ✅)
   Location: C:\Users\...\AppData\Roaming\RiskManager\Journal
   Account Used: 'ACCOUNT123'
```

### Account Diagnosis Section

```
ACCOUNTS FOUND IN FILES:
═══════════════════════════════════════════
trading_journal.json contains:
  • ACCOUNT456 - 25 trades
  • ACCOUNT789 - 10 trades

trading_models.json contains:
  • ACCOUNT456 - 3 models
  • ACCOUNT789 - 2 models
```

### Mismatch Warning

```
⚠️ WARNING: Account Mismatch Detected!

Dashboard is using account: 'ACCOUNT123'
No data found for this account in the files.

Accounts that HAVE data:
  • ACCOUNT456 (25 trades, 3 models)
  • ACCOUNT789 (10 trades, 2 models)

SOLUTION:
  1. Switch Dashboard to ACCOUNT456 or ACCOUNT789, OR
  2. Add trades/models for ACCOUNT123 via TradeLog
```

## Verification

After applying solution, verify:

1. **Debug Label Shows:**
   - Dashboard account matches an account in files
   - Trades Loaded > 0
   - Models Loaded > 0

2. **Dashboard Displays:**
   - Statistics populated
   - Filters show options
   - Performance sections display data

3. **No Warning:**
   - Account mismatch warning should not appear
   - Green checkmark or success indicator

## Common Scenarios

### Scenario 1: Multiple Accounts in Files

```
Files contain: ACCOUNT456, ACCOUNT789, ACCOUNT321
Dashboard using: ACCOUNT123
Result: Shows 0 (not in list)
Solution: Switch to one of the existing accounts
```

### Scenario 2: First Time User

```
Files contain: (empty or don't exist)
Dashboard using: ACCOUNT123
Result: Shows 0 (expected - no data yet)
Solution: Add trades via TradeLog for ACCOUNT123
```

### Scenario 3: Account Changed

```
Previously: Used ACCOUNT456 with data
Now: Switched to ACCOUNT789
Result: Shows 0 (ACCOUNT789 has no data yet)
Solution: Add data for ACCOUNT789 or switch back to ACCOUNT456
```

## Key Takeaways

1. **Data is per-account** - Each account has separate data
2. **Files are shared** - Same JSON files for all accounts
3. **Filtering matters** - Dashboard filters by account number
4. **Match accounts** - Ensure Dashboard and TradeLog use same account
5. **Debug label helps** - Shows exactly which accounts have data

## Summary

**Issue:** Files exist, have data, but Dashboard shows 0

**Why:** Dashboard account doesn't match accounts in files

**Solution:** Use matching account or add data for current account

**Prevention:** Always verify account selection consistency
