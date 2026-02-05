# Auto-Lock Position Closure Documentation

## Overview

This document describes the position and order closure behavior when automated trading lock is triggered.

## Feature Description

When the automated trading lock time is reached (e.g., 9:30 AM ET for market open), the system automatically:

1. **Cancels all pending/working orders** for the account
2. **Closes all open positions** for the account
3. **Locks the account** until 5:00 PM ET
4. **Disables trading buttons** (Buy/Sell)
5. **Creates audit log entries** for all actions

This ensures complete risk management and prevents positions from remaining open unintentionally.

---

## Behavior Consistency

### Manual Lock vs Auto-Lock

Both manual lock and automated lock now have identical behavior:

| Action | Manual Lock | Auto-Lock Trading |
|--------|-------------|-------------------|
| Cancel pending orders | ✅ Yes | ✅ Yes |
| Close open positions | ✅ Yes | ✅ Yes |
| Lock account | ✅ Yes | ✅ Yes |
| Disable trading | ✅ Yes | ✅ Yes |
| Audit logging | ✅ Yes | ✅ Yes |

**Consistency is critical** - users expect the same protective behavior whether they lock manually or rely on automation.

---

## Implementation Details

### Code Location

**File:** `RiskManagerControl.cs`
**Method:** `CheckExpiredLocks()`
**Lines:** ~8125-8150

### Code Flow

```csharp
// Check if automated trading lock should trigger
if (!isLocked && settings?.AutoLockTradingEnabled == true && settings?.AutoLockTradingTime.HasValue == true)
{
    // Check if we've reached the auto-lock time
    if (ShouldTriggerAutoLock(settings.AutoLockTradingTime.Value))
    {
        // STEP 1: Close all positions and cancel orders
        // This matches the behavior of manual lock
        CloseAllPositionsForAccount(account, core);
        
        // STEP 2: Set trading lock in settings service
        var duration = RiskManagerSettingsService.CalculateDurationUntil5PMET();
        settingsService.SetTradingLock(uniqueAccountId, true, "Auto-locked trading at scheduled time", duration);
        
        // STEP 3: Lock account in Core API
        var lockMethod = core.GetType().GetMethod("LockAccount");
        if (lockMethod != null)
        {
            lockMethod.Invoke(core, new object[] { account });
        }
    }
}
```

### Order of Operations

The order is critical for proper risk management:

1. **First: Cancel Orders**
   - Prevents new positions from opening
   - Done inside `CloseAllPositionsForAccount()`
   
2. **Second: Close Positions**
   - Exits all existing positions
   - Done inside `CloseAllPositionsForAccount()`
   
3. **Third: Lock Account**
   - Prevents all new trading activity
   - UI buttons disabled

---

## CloseAllPositionsForAccount() Function

This function handles both order cancellation and position closure:

```csharp
private void CloseAllPositionsForAccount(Account account, Core core)
{
    try
    {
        // STEP 1: Cancel all working orders for this account first
        CancelAllWorkingOrdersForAccount(account, core);
        
        // STEP 2: Get and close all positions for this account
        var accountPositions = core.Positions
            .Where(p => p.Account?.Id == account.Id)
            .ToList();
        
        foreach (var position in accountPositions)
        {
            try
            {
                core.ClosePosition(position);
                System.Diagnostics.Debug.WriteLine($"Closed position: {position.Symbol}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing position {position.Symbol}: {ex.Message}");
            }
        }
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error in CloseAllPositionsForAccount: {ex.Message}");
    }
}
```

### Error Handling

- Individual position close errors don't stop the process
- All positions attempted even if some fail
- All errors logged to debug log
- Continues to lock account even if closures fail

---

## Use Cases

### 1. Market Open Discipline

**Scenario:** Trader doesn't want overnight positions

**Configuration:**
- Auto-lock trading time: 9:30 AM ET

**What Happens:**
- 9:29:59 AM - Positions still open, orders active
- 9:30:00 AM - Auto-lock triggers:
  - All orders cancelled
  - All positions closed
  - Account locked until 5:00 PM
- Result: No overnight positions remain

### 2. End of Day Risk Management

**Scenario:** Trader wants to close all positions before market close

**Configuration:**
- Auto-lock trading time: 3:00 PM ET

**What Happens:**
- 3:00:00 PM - Auto-lock triggers:
  - All positions closed
  - All orders cancelled
  - Account locked until 5:00 PM
- Result: Clean exit with no late-day surprises

### 3. Strategy Time Window

**Scenario:** Trader only trades during specific hours

**Configuration:**
- Auto-lock trading time: 11:00 AM ET

**What Happens:**
- Trade from 9:30 AM - 11:00 AM
- 11:00 AM - All positions closed automatically
- Account locked until 5:00 PM
- Result: Enforced trading time window

---

## Audit Trail

### Debug Log Output

When auto-lock triggers, the following entries appear in the debug log:

```
[9:30:00.001] Canceled working order: ES 03-25 for account 123456
[9:30:00.015] Closed position: ES 03-25
[9:30:00.020] Closed 1 positions for account
[9:30:00.025] Auto-locked trading for account: 123456
```

### Settings Service Log

```
[9:30:00.030] Trading lock set: Account=123456, Locked=True, Reason="Auto-locked trading at scheduled time"
```

---

## Risk Management Benefits

### 1. Automatic Position Exit

**Before:** Positions could remain open unintentionally
**After:** All positions automatically closed at lock time

### 2. Order Protection

**Before:** Pending orders could execute after lock time
**After:** All orders cancelled before positions closed

### 3. Consistency

**Before:** Manual and auto-lock behaved differently
**After:** Identical behavior for both methods

### 4. Complete Protection

**Before:** Account locked but trading could still occur via orders
**After:** Orders cancelled, positions closed, then account locked

### 5. Peace of Mind

**Before:** Need to monitor and manually close positions
**After:** Automated, reliable position closure every day

---

## Testing Recommendations

### Manual Test Procedure

1. **Setup**
   - Configure auto-lock trading for current time + 2 minutes
   - Example: If it's 2:28 PM, set to 2:30 PM

2. **Create Test Positions**
   - Open 1-2 positions (any symbol)
   - Place 1-2 pending limit orders

3. **Wait for Trigger**
   - Monitor the clock
   - At configured time (2:30 PM), observe:

4. **Verify Results**
   - ✓ All positions closed
   - ✓ All orders cancelled
   - ✓ Account shows "Locked"
   - ✓ Trading buttons disabled
   - ✓ Lock status shows "Until 5:00 PM ET"

5. **Check Debug Log**
   - Verify order cancellation messages
   - Verify position closure messages
   - Verify lock trigger message

### Expected Timeline

```
2:29:59 PM - Pre-trigger state
  - 2 positions open
  - 1 pending order
  - Account unlocked
  - Trading enabled

2:30:00 PM - Auto-lock triggers
  - Orders cancelled (5-10ms)
  - Positions closed (10-20ms)
  - Account locked (20-25ms)
  - UI updated (25-30ms)

2:30:01 PM - Post-trigger state
  - 0 positions open
  - 0 pending orders
  - Account locked
  - Trading disabled
```

---

## Edge Cases

### 1. Position Close Fails

**Scenario:** Position close API call fails

**Behavior:**
- Error logged to debug log
- Other positions still attempted
- Account still locked
- User should manually verify

**Log Example:**
```
Error closing position ES 03-25: Connection timeout
Closed position: NQ 03-25 ← Next position succeeds
Auto-locked trading for account: 123456 ← Lock still happens
```

### 2. No Positions or Orders

**Scenario:** Account has no positions or orders at lock time

**Behavior:**
- `CloseAllPositionsForAccount()` called but does nothing
- Account locked normally
- No error messages

### 3. Already Locked

**Scenario:** Account already locked manually

**Behavior:**
- Auto-lock check: `if (!isLocked && ...)`
- Skips the entire trigger logic
- No position closure attempted
- No duplicate lock

### 4. Multiple Accounts

**Scenario:** Multiple accounts with auto-lock at same time

**Behavior:**
- Each account processed independently
- Position closure is account-specific
- All accounts locked at their configured times

---

## Comparison with Other Lock Types

### Settings Lock vs Trading Lock

| Feature | Settings Lock | Trading Lock |
|---------|---------------|--------------|
| Purpose | Prevent config changes | Prevent trading |
| Position Closure | ❌ No | ✅ Yes |
| Order Cancellation | ❌ No | ✅ Yes |
| UI Changes Blocked | ✅ Yes | ❌ No |
| Trading Blocked | ❌ No | ✅ Yes |

**Key Difference:** Only Trading Lock closes positions and cancels orders.

---

## Summary

### What Users Get

✅ **Complete Protection** - Positions and orders handled automatically
✅ **Consistency** - Same behavior for manual and auto-lock
✅ **Reliability** - Runs every day at configured time
✅ **Audit Trail** - Full logging of all actions
✅ **Risk Management** - Prevents unintended position exposure

### What Changed

Before this fix:
- Auto-lock would lock account but leave positions open ❌
- Inconsistent with manual lock behavior ❌
- Risk of unintended overnight positions ❌

After this fix:
- Auto-lock closes positions and cancels orders ✅
- Consistent with manual lock behavior ✅
- Complete risk management ✅

---

## Related Documentation

- `AUTOMATED_TRADING_LOCK_DOCUMENTATION.md` - Auto-lock trading feature overview
- `LOCK_DURATION_FEATURE.md` - Lock duration and expiration details
- `TRADING_TIMES_LOCK_ARCHITECTURE.md` - Overall lock architecture

---

**Last Updated:** 2026-02-05
**Version:** 1.0
**Status:** Implemented and Documented
