# Settings Badge Account Switching - Debug Guide

## Purpose

This document explains the comprehensive debugging added to diagnose why the Settings Badge may not update correctly when switching between accounts.

## Issue Context

**Reported Issue:** Settings Badge is correct for the first account only, does not update when changing accounts in the dropdown.

**Previous Fix Attempt:** Changed from boolean state caching to full status string caching (commit 3d9b5d9), but issue persists.

**Current Investigation:** Added extensive debugging to identify the root cause.

## Debug Logging Added (Commit a64cd50)

### 1. Account Selection Change Tracking

**Location:** `AccountSelectorOnSelectedIndexChanged` method

**Logs Added:**
```
[AccountSelectorOnSelectedIndexChanged] === ACCOUNT CHANGE EVENT START ===
[AccountSelectorOnSelectedIndexChanged] Switching from Account: Id='XXX', Name='YYY'
[AccountSelectorOnSelectedIndexChanged] Switching to Account: Id='AAA', Name='BBB', Index=N
[AccountSelectorOnSelectedIndexChanged] Calling UpdateSettingsStatusLabelsRecursive...
[AccountSelectorOnSelectedIndexChanged] Calling UpdateManualLockStatusLabelsRecursive...
[AccountSelectorOnSelectedIndexChanged] Calling LoadAccountSettings...
[AccountSelectorOnSelectedIndexChanged] === ACCOUNT CHANGE EVENT END ===
```

**What This Reveals:**
- Confirms account selection change event fired
- Shows old vs new account details
- Tracks method call sequence

### 2. LoadAccountSettings Badge Update Tracking

**Location:** `LoadAccountSettings` method

**Logs Added:**
```
[LoadAccountSettings] About to call UpdateTradingStatusBadge...
[LoadAccountSettings] About to call UpdateSettingsStatusBadge...
[LoadAccountSettings] Badge updates completed.
```

**What This Reveals:**
- Confirms badge update methods are being called
- Shows call sequence during account load

### 3. UpdateSettingsStatusBadge Entry/Exit Tracking

**Location:** `UpdateSettingsStatusBadge` method

**Logs Added:**
```
[UpdateSettingsStatusBadge] === ENTRY === Caller=XXX
...
[UpdateSettingsStatusBadge] === EXIT (Reason) === Caller=XXX
```

**Exit Reasons:**
- `(No Account)` - No account selected
- `(Service Not Init)` - Settings service not initialized
- `(Status Unchanged)` - Status string matches cache, skipped update
- `(Success)` - Badge updated successfully
- `(Exception)` - Error occurred

**What This Reveals:**
- Whether method is being called
- Why method exits early (if it does)
- Who called the method (CallerMemberName)

### 4. Account Number Retrieval

**Logs Added:**
```
[UpdateSettingsStatusBadge] Account Number: 'XXXXX'
```

**What This Reveals:**
- The exact account identifier being used
- Whether different accounts generate different identifiers

### 5. Service Query Results

**Logs Added:**
```
[UpdateSettingsStatusBadge] Service returned: 'Locked (2h 30m)', IsLocked=True
```

**What This Reveals:**
- What the JSON settings service returns for this account
- Whether service returns correct status for different accounts

### 6. Cache State Dump

**Logs Added:**
```
[UpdateSettingsStatusBadge] === CACHE STATE DUMP ===
[UpdateSettingsStatusBadge] Cache Size: N entries
[UpdateSettingsStatusBadge] Cache Entry: Account='XXX' -> Status='YYY'
[UpdateSettingsStatusBadge] Cache Entry: Account='AAA' -> Status='BBB'
...
[UpdateSettingsStatusBadge] === END CACHE DUMP ===
```

**What This Reveals:**
- All cached status strings for all accounts
- Whether cache is growing correctly
- Whether account identifiers are consistent

### 7. Status String Comparison

**Logs Added:**
```
[UpdateSettingsStatusBadge] Cached Status: 'Locked (2h 30m)'
[UpdateSettingsStatusBadge] Status Comparison: Previous='Locked (2h 30m)' vs Current='Locked (1h 45m)' Match=False
```

**What This Reveals:**
- Previous cached status for this account
- Current status from service
- Whether comparison logic is working correctly

### 8. Cache Update Confirmation

**Logs Added:**
```
[UpdateSettingsStatusBadge] Cache Updated: Account='XXX' -> Status='YYY'
```

**What This Reveals:**
- What's being stored in cache
- Confirms cache write operation

### 9. Status Table Update

**Logs Added:**
```
[UpdateSettingsStatusBadge] Status Table Updated: 'Locked (2h 30m)'
```
OR
```
[UpdateSettingsStatusBadge] Status Table NOT updated (statusTableView=True/False, RowCount=N)
```

**What This Reveals:**
- Whether status table exists and has enough rows
- What value was written to table

### 10. Badge UI Update Confirmation

**Logs Added:**
```
[UpdateSettingsStatusBadge] Badge UI Updated:
  Old: Text='  Settings Locked (2h 30m)  ', Color=Red
  New: Text='  Settings Locked (1h 45m)  ', Color=Red
```
OR
```
[UpdateSettingsStatusBadge] Badge UI NOT updated (settingsStatusBadge is NULL)
```

**What This Reveals:**
- Whether badge control exists
- Old badge text/color before update
- New badge text/color after update
- Confirms UI was actually modified

## How to Use Debug Output

### Step 1: Enable Debug Output Window

**Visual Studio:**
1. Debug menu → Windows → Output
2. Select "Debug" from the "Show output from:" dropdown

**Rider:**
1. View → Tool Windows → Debug
2. Look at Console tab

**Other IDEs:**
1. Look for Debug Output, Console, or Trace window

### Step 2: Reproduce the Issue

1. Start the application in Debug mode
2. Note the first account's badge state
3. Switch to a different account using the dropdown
4. Observe whether badge updates

### Step 3: Analyze Debug Output

Look for the complete flow:

```
[AccountSelectorOnSelectedIndexChanged] === ACCOUNT CHANGE EVENT START ===
[AccountSelectorOnSelectedIndexChanged] Switching from Account: Id='123', Name='Account A'
[AccountSelectorOnSelectedIndexChanged] Switching to Account: Id='456', Name='Account B'
[AccountSelectorOnSelectedIndexChanged] Calling LoadAccountSettings...
[LoadAccountSettings] About to call UpdateSettingsStatusBadge...
[UpdateSettingsStatusBadge] === ENTRY === Caller=LoadAccountSettings
[UpdateSettingsStatusBadge] Account Number: '456_0'
[UpdateSettingsStatusBadge] Service returned: 'Locked (1h 45m)', IsLocked=True
[UpdateSettingsStatusBadge] === CACHE STATE DUMP ===
[UpdateSettingsStatusBadge] Cache Size: 2 entries
[UpdateSettingsStatusBadge] Cache Entry: Account='123_0' -> Status='Locked (2h 30m)'
[UpdateSettingsStatusBadge] Cache Entry: Account='456_0' -> Status='NULL' (or something)
[UpdateSettingsStatusBadge] === END CACHE DUMP ===
[UpdateSettingsStatusBadge] Cached Status: 'NULL'
[UpdateSettingsStatusBadge] Status Comparison: Previous='NULL' vs Current='Locked (1h 45m)' Match=False
[UpdateSettingsStatusBadge] Cache Updated: Account='456_0' -> Status='Locked (1h 45m)'
[UpdateSettingsStatusBadge] Badge UI Updated:
  Old: Text='  Settings Locked (2h 30m)  ', Color=Red
  New: Text='  Settings Locked (1h 45m)  ', Color=Red
[UpdateSettingsStatusBadge] === EXIT (Success) ===
```

## Possible Issues and How Debug Logs Identify Them

### Issue 1: UpdateSettingsStatusBadge Not Being Called

**Debug Evidence:**
- No `[UpdateSettingsStatusBadge] === ENTRY ===` log after account switch

**Indicates:**
- LoadAccountSettings not calling the method
- Event handler not firing

### Issue 2: Wrong Account Number Used

**Debug Evidence:**
```
[UpdateSettingsStatusBadge] Account Number: '123_0'  // Should be different after switch
```

**Indicates:**
- GetSelectedAccountNumber() returning stale value
- selectedAccount not being updated
- selectedAccountIndex not being updated

### Issue 3: Service Returning Wrong Status

**Debug Evidence:**
```
[UpdateSettingsStatusBadge] Service returned: 'Locked (2h 30m)', IsLocked=True
// But Account B should have different duration
```

**Indicates:**
- Service using wrong account number
- JSON file not updated for Account B
- Service caching issue

### Issue 4: Cache Comparison Failing

**Debug Evidence:**
```
[UpdateSettingsStatusBadge] Status Comparison: Previous='Locked (2h 30m)' vs Current='Locked (2h 30m)' Match=True
[UpdateSettingsStatusBadge] === EXIT (Status Unchanged) ===
```

**Indicates:**
- Cache contains stale value from previous account
- Account identifier collision (multiple accounts using same ID)

### Issue 5: Badge UI Not Updating

**Debug Evidence:**
```
[UpdateSettingsStatusBadge] Badge UI Updated:
  Old: Text='  Settings Locked (2h 30m)  ', Color=Red
  New: Text='  Settings Locked (2h 30m)  ', Color=Red
// Values are the same
```

**Indicates:**
- lockStatusString is stale
- Service query succeeded but returned old value

### Issue 6: Badge Control Null

**Debug Evidence:**
```
[UpdateSettingsStatusBadge] Badge UI NOT updated (settingsStatusBadge is NULL)
```

**Indicates:**
- Badge control not initialized
- Control disposed or garbage collected

## What to Report

When reporting debug output, include:

1. **Complete Flow:** All logs from account change event through badge update
2. **Account Details:** What accounts are being switched (names, IDs)
3. **Expected vs Actual:** What should happen vs what debug logs show
4. **Cache State:** The cache dump showing all entries
5. **Badge State:** Old and new badge text/colors

## Example Good Report

```
Switching from Account A (ID: 123, Locked 2h 30m) to Account B (ID: 456, Locked 1h 45m)

Expected: Badge should change from "Settings Locked (2h 30m)" to "Settings Locked (1h 45m)"
Actual: Badge still shows "Settings Locked (2h 30m)"

Debug Output:
[AccountSelectorOnSelectedIndexChanged] === ACCOUNT CHANGE EVENT START ===
[AccountSelectorOnSelectedIndexChanged] Switching from Account: Id='123', Name='Account A'
[AccountSelectorOnSelectedIndexChanged] Switching to Account: Id='456', Name='Account B'
...
[UpdateSettingsStatusBadge] Account Number: '123_0'  <-- PROBLEM: Should be '456_0'
...
```

This clearly shows the issue is in account number generation/retrieval.

## Next Steps

After reviewing debug output:
1. Identify which step in the flow fails
2. Add targeted fix for that specific issue
3. Re-test with debug output
4. Remove or reduce debug logging once issue is resolved

## Debug Logging Control

Debug logging can be controlled by modifying these statements:
- Comment out individual `System.Diagnostics.Debug.WriteLine()` calls
- Wrap sections in `if (_badgeDebugMode)` conditional (line 205 has flag)
- Remove entirely once issue is resolved (before merge)
