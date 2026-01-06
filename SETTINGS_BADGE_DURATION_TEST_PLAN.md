# Settings Status Badge Duration Display - Test Plan

## Overview
This document describes the test scenarios to verify that the Settings Status Badge correctly displays lock duration information.

## Test Environment Setup
1. Build the application with the latest changes
2. Launch the application with at least 2 connected accounts
3. Ensure the Risk Manager panel is visible and functional

## Test Scenarios

### Test 1: Initial State - Unlocked Settings
**Objective:** Verify badge shows "Unlocked" for accounts without settings lock

**Steps:**
1. Select an account that has never been locked
2. Observe the Settings Status Badge at the top of the panel

**Expected Result:**
- Badge displays "Settings Unlocked"
- Badge background is Green
- Status table in Risk Overview shows "Unlocked" for Settings Status

---

### Test 2: Lock Settings with Duration
**Objective:** Verify badge displays duration when settings are locked

**Steps:**
1. Select an account
2. Navigate to "Lock Settings" tab
3. Click "LOCK SETTINGS" button (locks until 5 PM ET)
4. Observe the Settings Status Badge

**Expected Result:**
- Badge displays "Settings Locked (Xh Ym)" where X and Y represent hours and minutes until 5 PM ET
- Badge background is Red
- Status table in Risk Overview shows "Locked (Xh Ym)"
- Duration format matches one of:
  - "Locked (2h 30m)" - for hours and minutes
  - "Locked (45m)" - for minutes only
  - "Locked (<1m)" - for less than 1 minute
  - "Locked (1d 3h 15m)" - for multi-day locks

**Example:**
If locked at 2:30 PM ET:
- Expected: "Settings Locked (2h 30m)"

---

### Test 3: Duration Countdown
**Objective:** Verify duration updates as time passes

**Steps:**
1. Lock settings (from Test 2)
2. Wait 1-2 minutes
3. Observe if the duration updates

**Expected Result:**
- Duration decrements (e.g., "2h 30m" → "2h 29m" → "2h 28m")
- Badge continues to show Red background
- Updates occur every second (timer refresh)

**Note:** The badge may not update immediately every second due to state caching. It should update when:
- Account is switched
- Settings are saved
- Lock expires

---

### Test 4: Unlock Settings
**Objective:** Verify badge updates when settings are unlocked

**Steps:**
1. Lock settings (from Test 2)
2. Click "UNLOCK SETTINGS" button
3. Observe the Settings Status Badge

**Expected Result:**
- Badge displays "Settings Unlocked"
- Badge background changes from Red to Green
- Status table in Risk Overview shows "Unlocked"
- Duration is no longer displayed

---

### Test 5: Account Switching - Different Lock States
**Objective:** Verify badge updates correctly when switching between accounts with different lock states

**Steps:**
1. Lock Account A settings (should show "Locked (Xh Ym)")
2. Switch to Account B (unlocked settings)
3. Observe badge for Account B
4. Switch back to Account A
5. Observe badge for Account A

**Expected Result:**
- Account A badge: "Settings Locked (Xh Ym)" (Red)
- Account B badge: "Settings Unlocked" (Green)
- Badge updates immediately upon account switch
- Duration for Account A is accurate (not stale from when first locked)
- No flickering or intermediate states

---

### Test 6: Multiple Rapid Lock/Unlock Operations
**Objective:** Verify badge handles rapid state changes without errors

**Steps:**
1. Select an account
2. Lock settings → Observe badge
3. Immediately unlock settings → Observe badge
4. Immediately lock settings again → Observe badge
5. Repeat 2-3 times

**Expected Result:**
- Badge reflects current state after each operation
- No visual flickering
- No error messages or exceptions
- State caching prevents redundant updates
- Final state is accurate

---

### Test 7: Lock Expiration
**Objective:** Verify badge updates when lock expires automatically

**Setup:** This test requires waiting until the lock expiration time (5 PM ET) or temporarily modifying the lock duration for testing.

**Steps:**
1. Lock settings with a short duration (if testing - may need code modification)
2. Wait for the lock to expire
3. Observe the badge after expiration

**Expected Result:**
- Badge automatically updates to "Settings Unlocked" (Green)
- State change is logged in debug output
- Controls become enabled
- No manual intervention required

---

### Test 8: Application Restart with Active Lock
**Objective:** Verify lock duration persists across application restarts

**Steps:**
1. Lock settings (should show duration)
2. Note the current duration (e.g., "2h 30m")
3. Close the application
4. Reopen the application
5. Select the same account
6. Observe the badge

**Expected Result:**
- Badge displays updated duration (less than original, accounting for elapsed time)
- Lock state persisted in JSON file
- Duration continues to countdown from stored expiration time
- Badge shows Red background

---

### Test 9: Edge Case - No Account Selected
**Objective:** Verify badge handles edge case gracefully

**Steps:**
1. Launch application with no accounts connected or selected
2. Observe the badge

**Expected Result:**
- Badge displays a default/empty state or is hidden
- No error messages or exceptions
- Debug log shows "No account selected, skipping update"

---

### Test 10: Status Table Consistency
**Objective:** Verify status table in Risk Overview matches badge

**Steps:**
1. Navigate to "Risk Overview" tab
2. Lock settings
3. Compare badge text with "Settings Status" row in the table
4. Unlock settings
5. Compare again

**Expected Result:**
- Badge and table always show the same state
- Duration format is consistent between badge and table
- Colors match (Red for locked, Green for unlocked)

---

## Debug Output Verification

During testing, monitor the debug console for structured logging from `UpdateSettingsStatusBadge`:

### Expected Log Entries

**On Account Load (Unlocked):**
```
[UpdateSettingsStatusBadge] Caller=LoadAccountSettings, Account='123456', IsLocked=False, PreviousState=null
[UpdateSettingsStatusBadge] Caller=LoadAccountSettings, Account='123456', IsLocked=False - State changed, updating UI
```

**On Lock:**
```
[UpdateSettingsStatusBadge] Caller=UpdateSettingsLockStatus, Account='123456', IsLocked=True, PreviousState=False
[UpdateSettingsStatusBadge] Caller=UpdateSettingsLockStatus, Account='123456', IsLocked=True - State changed, updating UI
```

**On Redundant Call (State Unchanged):**
```
[UpdateSettingsStatusBadge] Caller=CheckExpiredLocks, Account='123456', IsLocked=True, PreviousState=True
[UpdateSettingsStatusBadge] Caller=CheckExpiredLocks, Account='123456', IsLocked=True - State unchanged, skipping UI update to prevent redundant refresh
```

**On Account Switch:**
```
[UpdateSettingsStatusBadge] Caller=LoadAccountSettings, Account='789012', IsLocked=False, PreviousState=null
[UpdateSettingsStatusBadge] Caller=LoadAccountSettings, Account='789012', IsLocked=False - State changed, updating UI
```

---

## Known Limitations

1. **Timer Precision**: Badge updates occur on timer ticks (every second), not real-time
2. **State Caching**: Badge may not update immediately if called multiple times with unchanged state
3. **Duration Format**: Very short durations (<1m) show as "Locked (<1m)" rather than seconds
4. **Timezone Display**: Duration countdown is based on server time, not local time

---

## Regression Tests

Ensure existing functionality still works:

1. ✅ Trading Status Badge still updates correctly
2. ✅ Lock Settings panel controls remain functional
3. ✅ Settings save operation works
4. ✅ Account selector dropdown functions
5. ✅ Navigation tabs are enabled/disabled based on lock state
6. ✅ Manual Lock panel continues to work independently

---

## Success Criteria

All tests pass with:
- ✅ Correct duration display format
- ✅ Accurate duration countdown
- ✅ Proper color coding (Red/Green)
- ✅ No visual flickering
- ✅ No error messages or exceptions
- ✅ Consistent state between badge and status table
- ✅ Structured debug logging present
- ✅ State caching prevents redundant updates

---

## Troubleshooting

### Issue: Badge shows "Locked" without duration
**Possible Causes:**
- Lock was created without duration parameter
- Old JSON settings file from before duration feature

**Solution:**
- Re-lock settings using the Lock Settings button
- Verify JSON file contains `lockDuration` and `lockExpirationTime`

### Issue: Badge not updating after account switch
**Possible Causes:**
- State cache not cleared on account change
- Account selector event not firing

**Solution:**
- Check debug logs for account switch event
- Verify `_accountSettingsLockStateCache` is per-account

### Issue: Duration not counting down
**Possible Causes:**
- Timer not running
- State caching preventing updates

**Solution:**
- Check if `badgeRefreshTimer` is started
- Verify timer interval (should be 1000ms)
- Review state change detection logic

---

## Test Results Template

| Test # | Test Name | Result | Notes | Tester | Date |
|--------|-----------|--------|-------|--------|------|
| 1 | Initial State - Unlocked | ⬜ | | | |
| 2 | Lock with Duration | ⬜ | | | |
| 3 | Duration Countdown | ⬜ | | | |
| 4 | Unlock Settings | ⬜ | | | |
| 5 | Account Switching | ⬜ | | | |
| 6 | Rapid Lock/Unlock | ⬜ | | | |
| 7 | Lock Expiration | ⬜ | | | |
| 8 | Application Restart | ⬜ | | | |
| 9 | No Account Selected | ⬜ | | | |
| 10 | Status Table Consistency | ⬜ | | | |

Legend: ⬜ Not Tested | ✅ Pass | ❌ Fail | ⚠️ Partial/Warning
