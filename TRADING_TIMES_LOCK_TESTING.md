# Trading Times Lock Feature - Testing Guide

## Overview
This document provides comprehensive testing scenarios for the Allowed Trading Times Lock feature that automatically locks and unlocks accounts based on configured trading time windows.

## Feature Summary
The system now automatically:
1. **Locks accounts** when the current time falls outside any configured allowed trading time window
2. **Respects existing locks** - Day-level locks (5 PM ET) take precedence over time-based unlocking
3. **Calculates lock duration** - Locks until the next allowed trading time OR 5 PM ET (whichever is sooner)
4. **Uses EST consistently** - All time calculations use Eastern Standard Time for consistency
5. **Updates in real-time** - Status badges update every 500ms via the monitoring timer

## Implementation Details

### Backend (RiskManagerSettingsService.cs)

#### 1. IsTradingAllowedNow() - Enhanced
- Now uses EST timezone for all comparisons
- Converts UTC time to EST before checking restrictions
- Falls back to local time if EST timezone not found

#### 2. GetTradingLockDuration() - New Method
Returns the duration to lock an account if trading is not allowed:
- Returns `null` if trading is currently allowed
- Searches for next allowed trading window (today or up to 7 days ahead)
- Calculates duration until 5 PM ET
- Returns the shorter of: next allowed window OR 5 PM ET

#### 3. CheckAndEnforceTradeTimeLocks() - New Method
Enforces trading time locks:
- **Locking Logic**:
  - If outside allowed times AND not locked → Lock with calculated duration
  - Lock reason: "Outside allowed trading times"
  
- **Unlocking Logic**:
  - If inside allowed times AND currently locked → Check lock reason
  - Only unlocks if lock reason contains "Outside allowed trading times" or "Trading times"
  - Does NOT unlock day-level locks (5 PM ET) or manual locks
  - Verifies lock expiration time before unlocking

### Frontend (RiskManagerControl.cs)

#### EnforceTradingTimeLocks() - New Method
- Calls `CheckAndEnforceTradeTimeLocks()` for each account
- Updates trading status badge if current account is affected
- Runs every 500ms as part of the existing P&L monitoring timer

#### Integration into MonitorPnLLimits()
- Added `EnforceTradingTimeLocks()` call before checking other limits
- Runs for all connected accounts with feature toggle enabled
- Skips locked accounts for further processing

## Test Scenarios

### Scenario 1: Lock Outside Trading Hours
**Configuration:**
- NY Session enabled: 8 AM - 5 PM EST
- All other sessions disabled
- Current time: 6:00 PM EST

**Expected Behavior:**
1. Account is automatically locked
2. Lock reason: "Outside allowed trading times"
3. Lock duration: Until 8:00 AM EST next day
4. Trading status badge shows "Locked" with remaining time
5. All open positions are closed (handled by existing CheckTradingTimeRestrictions)

**Verification Steps:**
1. Configure NY session only (8 AM - 5 PM EST)
2. Wait until 6:00 PM EST or simulate time
3. Check trading status badge - should show "Locked"
4. Check lock reason in settings - should contain "Outside allowed trading times"
5. Verify lock expiration time is set to 8:00 AM EST next day

### Scenario 2: Respect Existing Day Lock
**Configuration:**
- Asia session enabled: 7 PM - 4 AM EST
- Account locked until 5 PM ET (daily lock from P&L limit)
- Current time: 10:00 PM EST (within allowed Asia time)

**Expected Behavior:**
1. Account remains locked despite being in allowed trading time
2. Lock reason does NOT contain "Trading times" (it contains "Daily Loss Limit")
3. System respects the 5 PM ET daily lock
4. Auto-unlock logic is skipped for this account

**Verification Steps:**
1. Configure Asia session (7 PM - 4 AM EST)
2. Trigger a daily loss limit to lock account until 5 PM ET
3. Wait until 10:00 PM EST (within Asia session)
4. Verify account remains locked
5. Check lock reason - should be "Daily Loss Limit" not "Trading times"
6. Verify lock expiration is still 5 PM ET

### Scenario 3: Multi-Session Support
**Configuration:**
- London enabled: 8 AM - 4 PM EST
- NY enabled: 1 PM - 9 PM EST
- Current time: 12:00 PM EST (between sessions)

**Expected Behavior:**
1. Account is locked at 12:00 PM EST (outside both windows)
2. Lock duration: Until 1:00 PM EST (next allowed time)
3. Account auto-unlocks at 1:00 PM EST
4. Account remains unlocked from 1:00 PM - 4:00 PM (overlap period)
5. Account remains unlocked from 4:00 PM - 9:00 PM (NY only)
6. Account locks again at 9:00 PM EST until 8:00 AM EST next day

**Verification Steps:**
1. Configure London (8 AM - 4 PM) and NY (1 PM - 9 PM) sessions
2. Test at 12:00 PM EST - verify locked until 1:00 PM
3. Test at 1:00 PM EST - verify unlocked (overlap period)
4. Test at 4:30 PM EST - verify still unlocked (NY session)
5. Test at 9:30 PM EST - verify locked until 8:00 AM next day

### Scenario 4: No Restrictions (24/7 Trading)
**Configuration:**
- No trading time restrictions configured
- Current time: Any

**Expected Behavior:**
1. Account never locked due to trading times
2. IsTradingAllowedNow() always returns true
3. GetTradingLockDuration() always returns null
4. Trading status badge shows "Unlocked"

**Verification Steps:**
1. Clear all trading time restrictions
2. Test at various times throughout the day
3. Verify account is never locked due to trading times
4. Manual locks and P&L locks should still work normally

### Scenario 5: Single Session with Gap
**Configuration:**
- NY Session enabled: 8 AM - 12 PM EST (morning only)
- Current time scenarios:
  - 7:00 AM EST (before session)
  - 10:00 AM EST (during session)
  - 1:00 PM EST (after session)

**Expected Behavior:**
- **7:00 AM**: Locked until 8:00 AM EST
- **10:00 AM**: Unlocked (within session)
- **1:00 PM**: Locked until 5:00 PM EST (day lock time, since no more sessions today)

**Verification Steps:**
1. Configure single NY session (8 AM - 12 PM)
2. Test before session - locked until session start
3. Test during session - unlocked
4. Test after session - locked until 5 PM ET (or 8 AM next day if after 5 PM)

### Scenario 6: Weekend Trading
**Configuration:**
- Monday-Friday: NY Session 8 AM - 5 PM EST
- Saturday-Sunday: No restrictions
- Current time: Saturday 10:00 AM EST

**Expected Behavior:**
1. If no Saturday restrictions configured, trading is allowed 24/7 on weekends
2. Account unlocked all day Saturday and Sunday
3. Monday 8:00 AM EST - account unlocks if locked
4. Monday 5:00 PM EST - account locks

**Verification Steps:**
1. Configure weekday sessions only (Mon-Fri)
2. Test on Saturday - verify no restrictions apply
3. Test on Monday morning - verify session restrictions apply
4. Test transition from Friday 5 PM to Saturday - verify unlocking behavior

### Scenario 7: Lock Duration Calculation
**Configuration:**
- London: 8 AM - 4 PM EST
- NY: 1 PM - 9 PM EST
- Test at different times to verify duration calculations

**Test Cases:**
| Current Time | Expected Lock Duration | Notes |
|--------------|------------------------|-------|
| 6:00 AM EST | 2 hours (until 8 AM) | Lock until London session |
| 12:30 PM EST | 30 min (until 1 PM) | Lock until NY session |
| 4:30 PM EST | Still unlocked | NY session active until 9 PM |
| 9:30 PM EST | 10.5 hours (until 8 AM) | Lock until London next day |
| 10:00 PM EST | 10 hours (until 8 AM) or until 5 PM ET next day | Shorter of next window or 5 PM ET |

**Verification Steps:**
1. Configure London and NY sessions
2. For each test case, verify lock duration matches expected
3. Check debug logs for duration calculations
4. Verify lock expiration time is set correctly

## Debug Logging

The implementation includes comprehensive debug logging:

### RiskManagerSettingsService.cs
```
GetTradingLockDuration: No next allowed time found, locking until 5 PM ET. Duration={duration}
GetTradingLockDuration: Next window={time}, 5PM={time}, Lock duration={duration}
CheckAndEnforceTradeTimeLocks: Locking account {id} - {reason}, Duration={duration}
CheckAndEnforceTradeTimeLocks: Unlocking account {id} - entering allowed trading time
CheckAndEnforceTradeTimeLocks: Account {id} lock not yet expired, keeping locked
CheckAndEnforceTradeTimeLocks: Account {id} has day/manual lock, not unlocking despite allowed time
```

### RiskManagerControl.cs
```
Error enforcing trading time locks for account {id}: {message}
```

## Testing Tools

### Time Simulation
Since testing time-based features is challenging, consider these approaches:

1. **System Clock Adjustment**: Temporarily adjust system time to test different scenarios
2. **Debug Breakpoints**: Set breakpoints in enforcement logic to inspect state
3. **Log Analysis**: Review debug logs to verify correct behavior
4. **Wait Periods**: Use actual time passage for critical tests (e.g., wait for lock to expire)

### Test Account Setup
1. Create test accounts with different configurations
2. Use demo/paper trading accounts for safety
3. Configure short time windows for faster testing (e.g., 5-minute sessions)

## Known Limitations

1. **Timezone Fallback**: If EST timezone is not found, system falls back to local time
2. **7-Day Search Limit**: Next allowed window search is limited to 7 days ahead
3. **Lock Precision**: Locks expire based on UTC time comparison, not exact to the millisecond
4. **Badge Update Delay**: Status badges update every 500ms, not instantly

## Troubleshooting

### Account Won't Lock
- Verify trading time restrictions are configured (not empty)
- Check that at least one restriction is NOT allowed (IsAllowed = false)
- Ensure feature toggle is enabled for the account
- Check debug logs for enforcement calls

### Account Won't Unlock
- Verify current time is within an allowed window
- Check lock reason - may be a day-level or manual lock
- Verify lock expiration time has passed
- Check that lock reason contains "Trading times"

### Incorrect Lock Duration
- Verify all sessions are configured with correct EST times
- Check for multiple sessions on the same day
- Verify 5 PM ET calculation is correct
- Review debug logs for duration calculations

### Badge Not Updating
- Verify monitoring timer is running (every 500ms)
- Check that account is selected in dropdown
- Ensure no exceptions in EnforceTradingTimeLocks()
- Verify UpdateTradingStatusBadge() is being called

## Success Criteria

The feature is working correctly if:
- [ ] Accounts lock automatically when outside allowed trading times
- [ ] Lock duration is calculated correctly (next window or 5 PM ET, whichever is sooner)
- [ ] Accounts unlock automatically when entering allowed trading times
- [ ] Day-level locks (5 PM ET) are respected and NOT unlocked by time-based logic
- [ ] Manual locks are respected and NOT unlocked by time-based logic
- [ ] Multiple sessions are supported with correct lock/unlock timing
- [ ] All time calculations use EST consistently
- [ ] Status badges update in real-time (within 500ms)
- [ ] Debug logs show correct lock/unlock decisions
- [ ] No exceptions or errors in monitoring timer
