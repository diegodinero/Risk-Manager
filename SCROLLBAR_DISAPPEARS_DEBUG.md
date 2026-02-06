# Scrollbar Disappears Debug Guide

## Problem
User reports: "I still don't see the trades in the trade log. I do see a scroll bar for a second and then it disappears"

## Symptom Analysis

**Scrollbar appears** → Grid loads with data (rows added, scrollbar shows)  
**Scrollbar disappears** → Grid cleared or reset (rows removed, scrollbar gone)

This is a **timing/sequencing issue** where the grid is being cleared or refreshed with empty data immediately after loading.

## Possible Causes

### 1. Multiple Refresh Calls
```
Timeline:
13:15:30.123 - RefreshJournalData called with 2 trades → Loads data
13:15:30.156 - RefreshJournalData called with 0 trades → Clears grid
```

**Result**: Scrollbar appears briefly then disappears

### 2. Event Chain
```
- Add Trade button clicked
- Trade saved
- RefreshJournalData called → Loads data
- Some event fires (layout change, resize, etc.)
- RefreshJournalData called again → Empty or wrong data
```

### 3. Data Race Condition
```
- First call: Retrieves trades from service → 2 trades
- Service state changes
- Second call: Retrieves trades from service → 0 trades
```

## Debug Tracking Added

### What's Being Tracked

**Every RefreshJournalData call logs:**
1. Call number (sequential counter)
2. Timestamp (HH:mm:ss.fff precision)
3. Account number
4. Number of trades from service
5. Grid rows before clear
6. Grid rows after operations
7. Final row count

### Example Output

**Scenario 1: Multiple calls clearing grid**
```
=== RefreshJournalData CALL #1 at 13:15:30.123 ===
Account=Alpaca_PA3330LZLKJW, TradeCount=2, GridRows=0
Clearing grid (had 0 rows)
Grid cleared (now 0 rows)
Grid updated with 2 rows

=== RefreshJournalData CALL #2 at 13:15:30.156 ===
Account=Alpaca_PA3330LZLKJW, TradeCount=0, GridRows=2
Clearing grid (had 2 rows)
Grid cleared (now 0 rows)
Grid updated with 0 rows
```

**Diagnosis**: Second call 33ms later with 0 trades clears the grid!

**Scenario 2: Single call, styling issue**
```
=== RefreshJournalData CALL #1 at 13:15:30.123 ===
Account=Alpaca_PA3330LZLKJW, TradeCount=2, GridRows=0
Clearing grid (had 0 rows)
Grid cleared (now 0 rows)
Grid updated with 2 rows
```

**Diagnosis**: Only one call, data loads correctly. Problem is visibility/styling.

## How to Use This Debug Info

### Step 1: Open Debug Output
1. In Visual Studio, press `Ctrl+Alt+O`
2. Select "Debug" from dropdown
3. Navigate to Trade Log

### Step 2: Check MessageBox
When Trade Log opens, you'll see a MessageBox showing:
- Current call number
- Recent call history (last 5 calls)
- Timestamps and trade counts

### Step 3: Analyze Call Pattern

**If you see multiple calls:**
- Note the time between calls
- Note if trade counts differ
- Second call with 0 trades = problem identified

**If you see single call:**
- Grid is loading correctly
- Problem is rendering/visibility
- Need to check styling/colors

### Step 4: Report Findings

Share:
1. Full MessageBox text
2. Debug Output window content (all "RefreshJournalData" lines)
3. Approximate time between seeing scrollbar appear/disappear
4. Any actions taken before the issue (click button, navigate, etc.)

## Expected Solutions

### If Multiple Calls Found
**Solution**: Prevent redundant refresh calls
- Add flag to prevent concurrent refreshes
- Debounce refresh calls
- Fix event handlers calling refresh unnecessarily

### If Single Call Only
**Solution**: Fix rendering
- Ensure row height > 0
- Ensure colors have contrast
- Ensure grid has proper size
- Check if rows are actually invisible vs not created

## Testing After Fix

After fix is applied, MessageBox should show:
- Only one call per user action
- Trade count matches actual trades
- Grid rows = trade count
- No rapid consecutive calls

## Files Modified

- `RiskManagerControl.cs` - Added call tracking to RefreshJournalData method

## Next Steps

1. User tests with tracking enabled
2. User shares MessageBox + Debug Output
3. Analyze call pattern
4. Apply appropriate fix based on findings
5. Re-test to confirm resolution
