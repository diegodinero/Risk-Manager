# Automated Settings Lock Time - Testing Guide

## Test Environment Requirements

- Windows OS (7, 10, 11, or Server)
- .NET 8.0 Runtime
- Risk Manager application with TradingPlatform integration
- Multiple trading accounts configured for multi-account testing

## Pre-Test Setup

1. Ensure the application builds successfully
2. Launch the Risk Manager application
3. Verify at least one trading account is connected
4. Navigate to the "🔒 Lock Settings" tab

## Test Cases

### Test Case 1: Basic UI Verification

**Objective:** Verify all new UI components are present and properly labeled

**Steps:**
1. Open Lock Settings tab
2. Scroll down past the manual lock section
3. Locate the separator line
4. Find "Automated Daily Lock" section

**Expected Results:**
- [ ] Section title "Automated Daily Lock" is visible and bold
- [ ] Description text explains the feature
- [ ] "Enable Automated Lock" checkbox is present
- [ ] "Lock Time (ET):" label is visible
- [ ] Hour text box shows "09" by default
- [ ] Minute text box shows "30" by default
- [ ] Colon separator between hour and minute boxes
- [ ] Format help text is visible and readable
- [ ] "SAVE AUTO-LOCK SETTINGS" button is blue and visible

---

### Test Case 2: Enable Automated Lock

**Objective:** Configure and save automated lock settings

**Steps:**
1. Select an account from the dropdown
2. Check the "Enable Automated Lock" checkbox
3. Verify hour shows "09" and minute shows "30"
4. Click "SAVE AUTO-LOCK SETTINGS"

**Expected Results:**
- [ ] Success message appears: "Automated lock enabled. Settings will lock daily at 09:30 ET."
- [ ] Settings are saved (will verify in next test)
- [ ] No errors occur

---

### Test Case 3: Settings Persistence

**Objective:** Verify settings persist after application restart

**Steps:**
1. Enable automated lock with time 09:30
2. Click "SAVE AUTO-LOCK SETTINGS"
3. Close the Risk Manager application completely
4. Relaunch the application
5. Navigate to Lock Settings tab
6. Verify checkbox and time values

**Expected Results:**
- [ ] Checkbox is still checked
- [ ] Hour shows "09"
- [ ] Minute shows "30"
- [ ] Account selection maintains configuration

---

### Test Case 4: Custom Time Configuration

**Objective:** Test configuring different lock times

**Steps:**
1. Clear the hour box and enter "14"
2. Clear the minute box and enter "45"
3. Click "SAVE AUTO-LOCK SETTINGS"

**Expected Results:**
- [ ] Success message: "Automated lock enabled. Settings will lock daily at 14:45 ET."
- [ ] Values are accepted and saved
- [ ] After reload, values persist

---

### Test Case 5: Input Validation - Invalid Hour

**Objective:** Verify validation prevents invalid hours

**Steps:**
1. Enter "25" in the hour box
2. Click "SAVE AUTO-LOCK SETTINGS"

**Expected Results:**
- [ ] Error message: "Please enter a valid hour (00-23)."
- [ ] Settings are NOT saved
- [ ] User can correct the input

**Additional invalid hour tests:**
- [ ] Test with "99" - should fail
- [ ] Test with "ab" - should fail
- [ ] Test with "-5" - should fail

---

### Test Case 6: Input Validation - Invalid Minute

**Objective:** Verify validation prevents invalid minutes

**Steps:**
1. Set hour to valid value "09"
2. Enter "75" in the minute box
3. Click "SAVE AUTO-LOCK SETTINGS"

**Expected Results:**
- [ ] Error message: "Please enter a valid minute (00-59)."
- [ ] Settings are NOT saved
- [ ] User can correct the input

**Additional invalid minute tests:**
- [ ] Test with "99" - should fail
- [ ] Test with "ab" - should fail
- [ ] Test with "-5" - should fail

---

### Test Case 7: Disable Automated Lock

**Objective:** Verify disabling the feature works correctly

**Steps:**
1. With automated lock already enabled
2. Uncheck "Enable Automated Lock" checkbox
3. Click "SAVE AUTO-LOCK SETTINGS"

**Expected Results:**
- [ ] Success message: "Automated lock disabled."
- [ ] Time values remain in boxes (not cleared)
- [ ] Setting is saved
- [ ] No automatic locking occurs

---

### Test Case 8: Account Switching

**Objective:** Verify each account has independent configuration

**Preconditions:** Multiple accounts available

**Steps:**
1. Select Account A
2. Enable auto-lock with time 09:30
3. Save settings
4. Switch to Account B
5. Verify Account B's auto-lock settings
6. Enable auto-lock with time 10:00 for Account B
7. Save settings
8. Switch back to Account A
9. Verify Account A's settings

**Expected Results:**
- [ ] Account A shows: Enabled, 09:30
- [ ] Account B shows: Enabled, 10:00
- [ ] Switching accounts updates UI correctly
- [ ] Each account maintains independent settings

---

### Test Case 9: Trigger Automated Lock (Near-Future Test)

**Objective:** Verify automated lock triggers at configured time

**Note:** This test requires setting a time just a few minutes in the future

**Steps:**
1. Check current time in Eastern Time
2. Set auto-lock time to current time + 2 minutes
3. Enable automated lock
4. Save settings
5. Wait and observe for 2-3 minutes
6. Check lock status

**Expected Results:**
- [ ] At the configured time (within 1 minute), settings automatically lock
- [ ] Status changes to "Settings Locked"
- [ ] Lock reason shows "Auto-locked at scheduled time"
- [ ] Duration shows time remaining until 5 PM ET
- [ ] All settings controls are disabled
- [ ] Debug log shows: "Auto-locked settings for selected account: [account]"

---

### Test Case 10: Trigger Automated Lock (Exact Time)

**Objective:** Verify lock triggers at exact configured time

**Setup:** Use system clock manipulation if needed

**Steps:**
1. Set auto-lock for specific time (e.g., 11:00)
2. Enable and save
3. Use Task Scheduler or wait until 11:00 ET
4. Verify lock triggers between 11:00:00 and 11:00:59

**Expected Results:**
- [ ] Lock triggers within the 1-minute window
- [ ] Triggers only once (not multiple times)
- [ ] Status updates immediately
- [ ] If selected account, UI reflects locked state

---

### Test Case 11: No Trigger When Already Locked

**Objective:** Verify auto-lock doesn't interfere with existing locks

**Steps:**
1. Enable auto-lock for current time + 2 minutes
2. Save settings
3. Manually lock settings using "LOCK SETTINGS FOR REST OF DAY" button
4. Wait for the configured auto-lock time to pass
5. Verify lock state

**Expected Results:**
- [ ] Manual lock remains active
- [ ] No duplicate lock attempt
- [ ] No error messages
- [ ] Status shows manual lock reason, not auto-lock

---

### Test Case 12: No Trigger When Disabled

**Objective:** Verify disabled auto-lock doesn't trigger

**Steps:**
1. Configure auto-lock for current time + 2 minutes
2. Keep checkbox unchecked (disabled)
3. Save settings
4. Wait for 2-3 minutes past configured time

**Expected Results:**
- [ ] No automatic locking occurs
- [ ] Settings remain unlocked and editable
- [ ] No log messages about auto-lock

---

### Test Case 13: Edge Case - After 5 PM Configuration

**Objective:** Test configuring lock time after 5 PM ET

**Note:** This test should be run after 5 PM ET

**Steps:**
1. When current time is after 5 PM ET
2. Configure auto-lock for 9:30 AM ET
3. Enable and save
4. Note the current day

**Expected Results:**
- [ ] Settings save successfully
- [ ] Next day at 9:30 AM ET, lock should trigger
- [ ] Lock duration should be until 5 PM ET of the next day

---

### Test Case 14: Timezone Handling

**Objective:** Verify Eastern Time conversion works correctly

**Steps:**
1. Note your local timezone
2. Convert 9:30 AM ET to your local time
3. Configure auto-lock for 9:30 AM ET
4. At the equivalent local time, verify trigger

**Expected Results:**
- [ ] Lock triggers at correct local time equivalent to 9:30 AM ET
- [ ] Handles DST transitions correctly
- [ ] System in any timezone works properly

**Additional timezone tests:**
- [ ] Test during DST transition week (March/November)
- [ ] Test with system timezone set to different zones

---

### Test Case 15: Unlock After Auto-Lock

**Objective:** Verify manual unlock works after auto-lock

**Steps:**
1. Configure and trigger auto-lock (use near-future time)
2. Wait for lock to activate
3. Click "UNLOCK SETTINGS" button
4. Verify settings are editable

**Expected Results:**
- [ ] Manual unlock removes auto-lock
- [ ] Settings become editable
- [ ] Status shows "Settings Unlocked"
- [ ] Next day, auto-lock will trigger again at configured time

---

### Test Case 16: Integration with Manual Lock

**Objective:** Verify automated and manual locks work together

**Steps:**
1. Enable auto-lock for current time + 2 minutes
2. Save settings
3. Before auto-lock triggers, manually lock settings
4. Wait for auto-lock time to pass
5. At 5 PM ET, verify unlock
6. Next day, verify auto-lock triggers again

**Expected Results:**
- [ ] Manual lock takes precedence before auto-lock time
- [ ] No conflict or error when auto-lock time passes
- [ ] Both unlock at 5 PM ET
- [ ] Auto-lock re-enables next day

---

### Test Case 17: No Account Selected

**Objective:** Verify proper handling when no account selected

**Steps:**
1. Deselect all accounts (if possible) or start with no account
2. Try to save auto-lock settings

**Expected Results:**
- [ ] Error message: "Please select an account first."
- [ ] Settings are NOT saved
- [ ] No crash or unexpected behavior

---

### Test Case 18: Boundary Time Values

**Objective:** Test edge values for time input

**Test Cases:**
- [ ] Hour: 00, Minute: 00 (midnight) - should save
- [ ] Hour: 23, Minute: 59 (11:59 PM) - should save
- [ ] Hour: 12, Minute: 00 (noon) - should save
- [ ] Hour: 17, Minute: 00 (5 PM, same as unlock) - should save

**Expected Results:**
- All valid time combinations save successfully
- Lock triggers at the exact configured time
- Duration until 5 PM ET calculated correctly

---

## Performance Tests

### Test Case 19: Timer Performance

**Objective:** Verify auto-lock checking doesn't impact performance

**Steps:**
1. Enable auto-lock on multiple accounts
2. Monitor application performance
3. Check CPU usage over 5-10 minutes
4. Verify UI remains responsive

**Expected Results:**
- [ ] No noticeable performance degradation
- [ ] CPU usage remains normal
- [ ] UI remains smooth and responsive
- [ ] Timer checks execute quickly (< 1ms)

---

## Regression Tests

### Test Case 20: Existing Manual Lock Still Works

**Objective:** Verify manual lock functionality unchanged

**Steps:**
1. Ignore auto-lock settings
2. Use manual "LOCK SETTINGS FOR REST OF DAY" button
3. Verify lock activates
4. Verify unlock works

**Expected Results:**
- [ ] Manual lock works exactly as before
- [ ] No interference from auto-lock feature
- [ ] All existing functionality preserved

---

### Test Case 21: Settings Still Saved When Unlocked

**Objective:** Verify other settings save correctly

**Steps:**
1. Configure auto-lock but keep disabled
2. Modify other settings (Daily Loss Limit, etc.)
3. Save settings
4. Verify changes persist

**Expected Results:**
- [ ] All settings save normally
- [ ] No conflicts or errors
- [ ] Auto-lock configuration doesn't interfere

---

## Test Summary Template

```
Test Date: ___________
Tester: ___________
Environment: ___________
Build Version: ___________

Passed Tests: __ / 21
Failed Tests: __ / 21

Critical Issues Found: ___________
Minor Issues Found: ___________

Notes:
_________________________________
_________________________________
_________________________________
```

## Known Limitations

1. Requires Windows OS for timezone handling
2. Requires TradingPlatform SDK for full testing
3. Time trigger uses 1-minute window (not exact second)
4. Eastern Time assumption (US markets)

## Post-Test Validation

After all tests:
- [ ] Review debug logs for errors
- [ ] Check settings JSON files for corruption
- [ ] Verify no memory leaks (long-running test)
- [ ] Confirm UI still functions after extended use
- [ ] Test with production-like account configurations

## Bug Reporting Template

If issues found:

```
Test Case #: ___________
Issue Description: ___________
Steps to Reproduce:
1. ___________
2. ___________
3. ___________

Expected: ___________
Actual: ___________
Severity: [Critical/High/Medium/Low]
Screenshots: [Attach if applicable]
Logs: [Include relevant debug output]
```
