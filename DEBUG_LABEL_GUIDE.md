# Visible Debug Label Guide

## Overview

The dashboard now displays a **prominent yellow debug label** at the top that shows exactly where trading data is written and read from. This helps diagnose why the dashboard might be showing zeros.

## What It Looks Like

At the top of your dashboard, you'll see a bright yellow panel with black text that looks like this:

```
════════════════════════════════════════════════════════════════════════════════
DEBUG INFO - DATA FILE LOCATIONS
════════════════════════════════════════════════════════════════════════════════
Data Directory (TradeLog WRITES here / Dashboard READS here):
  C:\Users\YourName\AppData\Roaming\RiskManager\Journal

Trading Journal File:
  Path: trading_journal.json
  Exists: True
  Size: 15234 bytes

Trading Models File:
  Path: trading_models.json
  Exists: True
  Size: 2456 bytes

Current Account: 'ACCOUNT123'
Trades Loaded: 25
Models Loaded: 3
════════════════════════════════════════════════════════════════════════════════
```

## Information Displayed

### 1. Data Directory
- **Shows:** The complete path where data files are stored
- **Important:** TradeLog writes TO this location, Dashboard reads FROM this location
- **Same Path:** Confirms both use the exact same directory

### 2. Trading Journal File
- **Path:** trading_journal.json (contains all your trades)
- **Exists:** True if file exists, False if not
- **Size:** File size in bytes
  - 0 bytes = no file
  - 2 bytes = empty file (just `{}`)
  - 100+ bytes = has data

### 3. Trading Models File
- **Path:** trading_models.json (contains your trading strategies)
- **Exists:** True if file exists, False if not
- **Size:** File size in bytes

### 4. Current Account
- **Shows:** Which account number the dashboard is using
- **Important:** Must match the account you used when adding trades

### 5. Data Counts
- **Trades Loaded:** How many trades were loaded from JSON for this account
- **Models Loaded:** How many models were loaded from JSON for this account

## Common Scenarios and Solutions

### Scenario 1: No Files Exist
```
Trading Journal File:
  Exists: False
  Size: 0 bytes
Trades Loaded: 0
```

**Diagnosis:** Files haven't been created yet (first time use)

**Solution:**
1. Navigate to Trade Log page
2. Click "Add Trade"
3. Fill in all fields
4. Click Save
5. Return to Dashboard

### Scenario 2: Files Empty
```
Trading Journal File:
  Exists: True
  Size: 2 bytes
Trades Loaded: 0
```

**Diagnosis:** Files exist but contain no data (just empty `{}`)

**Solution:**
1. Add trades via Trade Log page
2. Make sure to fill in all fields
3. Save the trades
4. Return to Dashboard

### Scenario 3: Wrong Account
```
Trading Journal File:
  Size: 15234 bytes  ← Has data!
Trades Loaded: 0     ← But not for this account
Current Account: 'ACCOUNT456'
```

**Diagnosis:** Data exists but for a different account number

**Solution:**
1. Check which account you used when adding trades
2. Either switch to that account in the dashboard
3. Or add trades for the current account

### Scenario 4: Working Correctly
```
Trading Journal File:
  Exists: True
  Size: 15234 bytes
Trades Loaded: 25
Models Loaded: 3
```

**Diagnosis:** Everything is working - data is loaded

**Expected Result:** Dashboard should display statistics

**If still showing zeros:** The issue is in calculations, not data loading

## How to Interpret

### File Existence
- **Exists: True** ✅ File is there
- **Exists: False** ❌ File doesn't exist yet

### File Size
- **0 bytes:** No file
- **2 bytes:** Empty file (just `{}`)
- **100-1000 bytes:** Small amount of data (a few trades)
- **1000+ bytes:** Good amount of data
- **10000+ bytes:** Lots of data

### Loaded Counts
- **0:** No data loaded (check file size and account)
- **1-10:** Small dataset
- **10+:** Good dataset for statistics

## Verification Steps

Use this debug label to verify:

1. **✅ Same Path:** TradeLog and Dashboard use same directory
2. **✅ Files Exist:** Both JSON files present
3. **✅ Files Have Data:** Size > 100 bytes
4. **✅ Correct Account:** Matches account used for data entry
5. **✅ Data Loaded:** Counts > 0

## Taking Action

Based on what you see:

**If files don't exist:**
→ Add data via Trade Log

**If files are empty:**
→ Add more trades and models

**If wrong account:**
→ Check account selection or add data for current account

**If everything looks good but dashboard shows zeros:**
→ Report the debug label output for further diagnosis
→ Issue is likely in calculation logic, not data loading

## Benefits

- **Immediate visibility:** No need to check debug output or logs
- **Complete information:** All diagnostic data in one place
- **Clear status:** Easy to understand what's working and what's not
- **Path verification:** Confirms TradeLog and Dashboard use same files
- **Account verification:** Shows which account is active
- **Quick diagnosis:** Can immediately see the problem

## Next Steps

1. Look at the yellow debug label on your dashboard
2. Identify which scenario matches your situation
3. Follow the solution for that scenario
4. If issues persist after adding data, share the debug label output

## Technical Details

**File Locations:**
- Windows: `C:\Users\<username>\AppData\Roaming\RiskManager\Journal\`
- Mac/Linux: `~/.config/RiskManager/Journal/`

**Files:**
- trading_journal.json: All trade log data
- trading_models.json: Trading models/strategies

**Data Organization:**
- JSON files organized by account number
- Dictionary format: `{ "ACCOUNT": [items] }`
- Each account has its own array of trades/models

## Summary

The yellow debug label provides instant visibility into where data is stored and whether it's being loaded correctly. Use it to quickly diagnose why the dashboard might be showing zeros and take appropriate action.
