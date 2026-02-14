# Unbypassable App - Testing Guide

## Overview
This document provides a comprehensive testing plan for the unbypassable application feature. Follow these tests to verify that all bypass prevention mechanisms are working correctly.

## Prerequisites
- Build and run the Risk Manager application
- Have at least one trading account configured
- Ensure the application is running in standalone mode (Program.cs entry point)

## Test Cases

### Test 1: X Button Close Prevention ✅
**Objective:** Verify that clicking the window's close (X) button does not close the application

**Steps:**
1. Launch the Risk Manager application
2. Locate the X (close) button in the window's title bar
3. Click the X button

**Expected Result:**
- A message box appears with the title "Cannot Close Application"
- Message text reads: "This application cannot be closed directly. Use the 🚪 Shutdown button in the top-right corner to lock accounts and close the application safely."
- Application remains open after clicking OK
- Application window stays visible and functional

**Pass Criteria:**
- [ ] Message box appears with correct text
- [ ] Application does not close
- [ ] Application remains fully functional after the warning

---

### Test 2: Alt+F4 Key Prevention ✅
**Objective:** Verify that pressing Alt+F4 does not close the application

**Steps:**
1. Launch the Risk Manager application
2. Ensure the application window has focus
3. Press Alt+F4 keys simultaneously

**Expected Result:**
- A message box appears with the title "Cannot Close Application"
- Message text reads: "Alt+F4 is disabled. Use the 🚪 Shutdown button in the top-right corner to lock accounts and close the application safely."
- Application remains open after clicking OK
- Application window stays visible and functional

**Pass Criteria:**
- [ ] Message box appears with correct text
- [ ] Application does not close
- [ ] Application remains fully functional after the warning

---

### Test 3: Taskbar Right-Click Close Prevention ✅
**Objective:** Verify that right-clicking the taskbar icon and selecting Close does not close the application

**Steps:**
1. Launch the Risk Manager application
2. Locate the application icon in the Windows taskbar
3. Right-click on the taskbar icon
4. Select "Close window" from the context menu

**Expected Result:**
- A message box appears with the title "Cannot Close Application"
- Message text reads: "This application cannot be closed directly. Use the 🚪 Shutdown button in the top-right corner to lock accounts and close the application safely."
- Application remains open after clicking OK
- Application window stays visible and functional

**Pass Criteria:**
- [ ] Message box appears with correct text
- [ ] Application does not close
- [ ] Application remains fully functional after the warning

---

### Test 4: Proper Shutdown Flow ✅
**Objective:** Verify that the shutdown button properly closes the application after locking accounts

**Steps:**
1. Launch the Risk Manager application with at least one account connected
2. Locate the 🚪 Shutdown button in the top-right corner (below theme switcher)
3. Click the shutdown button
4. In the confirmation dialog, click "Yes"
5. Wait for the 5-second countdown (do not cancel)

**Expected Result:**
1. Confirmation dialog appears asking: "Are you sure you want to lock all accounts, settings, and FORCEFULLY close the application?"
2. After clicking Yes:
   - All accounts are locked
   - A notification sound plays (leave-get-out.wav)
   - A countdown dialog appears: "Application will close in X seconds..."
   - Countdown decrements from 5 to 0
3. After countdown reaches 0:
   - Application closes gracefully
   - No error messages appear

**Pass Criteria:**
- [ ] Confirmation dialog appears with correct message
- [ ] Accounts are locked before shutdown
- [ ] Sound plays (if sound resources are available)
- [ ] Countdown dialog appears and counts down correctly
- [ ] Application closes after countdown completes
- [ ] No error messages during shutdown

---

### Test 5: Shutdown Cancellation ✅
**Objective:** Verify that the shutdown process can be cancelled during the countdown

**Steps:**
1. Launch the Risk Manager application
2. Click the 🚪 Shutdown button
3. Click "Yes" in the confirmation dialog
4. When the countdown dialog appears, click "Cancel Shutdown" button

**Expected Result:**
- Countdown stops immediately
- Countdown dialog closes
- A message box appears: "Shutdown cancelled."
- Application remains open and functional
- Accounts remain locked (from step 3)

**Pass Criteria:**
- [ ] Countdown stops when cancel is clicked
- [ ] Cancellation confirmation message appears
- [ ] Application does not close
- [ ] Application remains fully functional

---

### Test 6: Multiple Close Attempts ✅
**Objective:** Verify consistent behavior across multiple close attempts

**Steps:**
1. Launch the Risk Manager application
2. Try closing via X button (should be blocked)
3. Click OK on the warning message
4. Try closing via Alt+F4 (should be blocked)
5. Click OK on the warning message
6. Try closing via taskbar right-click (should be blocked)
7. Click OK on the warning message
8. Use the shutdown button to close properly

**Expected Result:**
- Each close attempt is blocked with the appropriate warning message
- Application remains stable after multiple blocked attempts
- Shutdown button works correctly after all blocked attempts

**Pass Criteria:**
- [ ] All three close methods are consistently blocked
- [ ] Warning messages appear each time
- [ ] Application remains stable and responsive
- [ ] Shutdown button still works correctly

---

### Test 7: Windows Shutdown/Restart ✅
**Objective:** Verify that Windows shutdown/restart is allowed (not blocked)

**Steps:**
1. Launch the Risk Manager application
2. Initiate a Windows shutdown or restart (Start → Power → Shut down)
3. Observe application behavior

**Expected Result:**
- Application does not show any "Cannot Close" warning
- Application allows Windows to close it
- Windows shutdown/restart proceeds normally

**Pass Criteria:**
- [ ] No warning messages appear
- [ ] Application closes when Windows shuts down
- [ ] Windows shutdown is not prevented or delayed by the application

**Note:** This test may log out your Windows session. Save all work before testing.

---

### Test 8: Thread Safety Verification ✅
**Objective:** Verify thread-safe operation of the AllowClose flag

**Steps:**
1. Launch the Risk Manager application
2. Start the shutdown process (click shutdown button, confirm)
3. Immediately try to close the window via X button or Alt+F4 during the countdown
4. Allow the countdown to complete

**Expected Result:**
- During countdown: Close attempts are still blocked
- After countdown completes: Application closes normally
- No race conditions or unexpected behavior

**Pass Criteria:**
- [ ] Close attempts during countdown are blocked
- [ ] Application closes properly after countdown
- [ ] No freezing or unexpected behavior

---

### Test 9: Application State After Blocked Close ✅
**Objective:** Verify that the application remains fully functional after a blocked close attempt

**Steps:**
1. Launch the Risk Manager application
2. Try to close via X button or Alt+F4
3. Click OK on the warning message
4. Test various application features:
   - Change theme
   - Select different account
   - Open settings
   - View risk overview cards
   - Test any trading functionality (if available)

**Expected Result:**
- All features continue to work normally
- No degradation in performance
- No error messages
- UI remains responsive

**Pass Criteria:**
- [ ] All tested features work correctly
- [ ] No error messages appear
- [ ] UI is responsive and stable
- [ ] No memory leaks or performance issues

---

## Automated Testing Considerations

For automated testing (if applicable):

### Unit Tests
- Test `AllowClose` property getter/setter
- Test `FormClosing` event handler logic
- Test `ProcessCmdKey` method with Alt+F4 input

### Integration Tests
- Mock FormClosingEventArgs with different CloseReasons
- Verify proper handling of `CloseReason.WindowsShutDown`
- Test timer completion and flag setting

### UI Automation Tests
- Use UI automation framework (e.g., TestStack.White, FlaUI)
- Automate button clicks and key presses
- Verify message box appearance and text

## Known Limitations

The following cannot be prevented and should not be tested as failures:

❌ **Cannot Prevent:**
1. Task Manager → End Task (OS-level termination)
2. Command line: `taskkill /f /im Risk_Manager.exe`
3. Power loss or hardware failure
4. Debugging tools (breakpoints, process termination)

## Test Environment

**Operating System:** Windows 10/11 or Windows Server 2016+
**Framework:** .NET 8.0 or later
**Application Mode:** Standalone (via Program.cs)

## Test Results Template

```
Test Date: _______________
Tester: _______________
Build Version: _______________

Test 1 (X Button): ☐ Pass ☐ Fail - Notes: _______________
Test 2 (Alt+F4): ☐ Pass ☐ Fail - Notes: _______________
Test 3 (Taskbar): ☐ Pass ☐ Fail - Notes: _______________
Test 4 (Shutdown Flow): ☐ Pass ☐ Fail - Notes: _______________
Test 5 (Cancellation): ☐ Pass ☐ Fail - Notes: _______________
Test 6 (Multiple Attempts): ☐ Pass ☐ Fail - Notes: _______________
Test 7 (Windows Shutdown): ☐ Pass ☐ Fail - Notes: _______________
Test 8 (Thread Safety): ☐ Pass ☐ Fail - Notes: _______________
Test 9 (App State): ☐ Pass ☐ Fail - Notes: _______________

Overall Result: ☐ All Tests Passed ☐ Some Failures

Issues Found:
_______________________________________________
_______________________________________________
_______________________________________________
```

## Troubleshooting

### Issue: Warning message doesn't appear
- **Check:** Ensure application is built with latest changes
- **Check:** Verify Program.cs includes the FormClosing event handler
- **Check:** Confirm RiskManagerControl.cs includes ProcessCmdKey override

### Issue: Application still closes despite blocked attempts
- **Check:** Verify `AllowClose` flag is initialized to `false`
- **Check:** Ensure FormClosing event handler checks the flag correctly
- **Check:** Confirm CloseReason is not WindowsShutDown

### Issue: Shutdown button doesn't close application
- **Check:** Verify `AllowClose` is set to `true` after countdown
- **Check:** Confirm no exceptions in shutdown logic
- **Check:** Review debug logs for any errors

## Conclusion

All tests should pass for the unbypassable feature to be considered fully functional. If any test fails, investigate the specific issue and re-test after fixes are applied.
