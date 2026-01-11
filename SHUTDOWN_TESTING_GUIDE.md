# Shutdown Testing Guide

## Purpose

This guide provides step-by-step instructions for testing the graceful shutdown mechanism implemented in the Risk Manager application.

## Prerequisites

- Risk Manager application with the graceful shutdown implementation
- Access to the application's debug output (Visual Studio Debug Output or DebugView)
- Trading platform running (if testing with actual accounts)

## Test Scenarios

### Test 1: Normal Shutdown Flow

**Objective**: Verify that the application shuts down gracefully under normal conditions.

**Steps**:
1. Launch the Risk Manager application
2. Wait for the application to fully load (all timers running)
3. Click the shutdown button (door icon) in the top-right corner
4. In the confirmation dialog, click "Yes"
5. Observe the 5-second countdown dialog
6. Wait for countdown to complete
7. Verify application closes

**Expected Results**:
- Confirmation dialog appears with proper message
- Countdown dialog appears showing countdown from 5 to 0
- All accounts are locked before shutdown
- Application closes completely
- No processes remain running in Task Manager
- Debug output shows all shutdown stages completing successfully

**Debug Output to Verify**:
```
[SHUTDOWN] Shutdown initiated by user
[SHUTDOWN] Stage 1: CancellationToken signaled
[SHUTDOWN] Stage 2: All timers stopped
[SHUTDOWN] Stage 3: All child forms closed
[SHUTDOWN] Stage 4: Resources disposed
[SHUTDOWN] Stage 5: Form.Close() completed
[SHUTDOWN] ===== Graceful Shutdown Process Complete =====
```

---

### Test 2: Cancelled Shutdown

**Objective**: Verify that shutdown can be cancelled and application continues normally.

**Steps**:
1. Launch the Risk Manager application
2. Click the shutdown button
3. In the confirmation dialog, click "Yes"
4. When the countdown dialog appears, click "Cancel Shutdown"
5. Verify the cancellation message appears
6. Click "OK" on the cancellation message
7. Verify application remains running normally

**Expected Results**:
- Countdown dialog is dismissed
- "Shutdown cancelled" message appears
- Application continues running normally
- All timers continue functioning
- Can interact with the application normally

**Debug Output to Verify**:
```
[SHUTDOWN] Shutdown initiated by user
[SHUTDOWN] User cancelled shutdown
```

---

### Test 3: Multiple Rapid Shutdown Attempts

**Objective**: Verify that multiple rapid shutdown button clicks are handled gracefully.

**Steps**:
1. Launch the Risk Manager application
2. Click the shutdown button multiple times rapidly
3. Confirm the first dialog
4. Observe behavior

**Expected Results**:
- Only one confirmation dialog appears
- Subsequent clicks are ignored (logged as "already in progress")
- Shutdown proceeds normally from the first attempt

**Debug Output to Verify**:
```
[SHUTDOWN] Shutdown initiated by user
[SHUTDOWN] Shutdown already in progress, ignoring duplicate request
[SHUTDOWN] Shutdown already in progress, ignoring duplicate request
...
```

---

### Test 4: Shutdown with Child Forms Open

**Objective**: Verify that child forms are closed during shutdown.

**Steps**:
1. Launch the Risk Manager application
2. Open any child dialogs or windows (if applicable)
3. Click the shutdown button
4. Confirm shutdown
5. Wait for countdown to complete
6. Verify all forms close

**Expected Results**:
- All child forms close before main form
- No forms remain open after shutdown
- Debug output shows child forms being closed

**Debug Output to Verify**:
```
[SHUTDOWN] Stage 3: Closing child forms
[SHUTDOWN] CloseAllChildForms: Found X owned forms
[SHUTDOWN] CloseAllChildForms: Closing form 'FormName'
...
[SHUTDOWN] CloseAllChildForms: Child form cleanup complete
```

---

### Test 5: Shutdown with Active Timers

**Objective**: Verify that all timers are properly stopped during shutdown.

**Steps**:
1. Launch the Risk Manager application
2. Navigate to different tabs to ensure all timers are active:
   - Accounts Summary (statsRefreshTimer)
   - Stats tab (statsDetailRefreshTimer)
   - Type tab (typeSummaryRefreshTimer)
3. Verify timers are actively updating (observe UI changes)
4. Click shutdown button
5. Confirm shutdown
6. Wait for countdown
7. Monitor debug output

**Expected Results**:
- All 6 timers are stopped during Stage 2
- No timer events fire after Stage 2 completes
- Debug output shows each timer being stopped

**Debug Output to Verify**:
```
[SHUTDOWN] Stage 2: Stopping all timers
[SHUTDOWN] StopAllTimers: statsRefreshTimer stopped
[SHUTDOWN] StopAllTimers: statsDetailRefreshTimer stopped
[SHUTDOWN] StopAllTimers: typeSummaryRefreshTimer stopped
[SHUTDOWN] StopAllTimers: lockExpirationCheckTimer stopped
[SHUTDOWN] StopAllTimers: pnlMonitorTimer stopped
[SHUTDOWN] StopAllTimers: badgeRefreshTimer stopped
[SHUTDOWN] StopAllTimers: Timer cleanup complete
```

---

### Test 6: Shutdown from Confirmation Dialog Cancel

**Objective**: Verify that cancelling at the confirmation stage works properly.

**Steps**:
1. Launch the Risk Manager application
2. Click the shutdown button
3. In the confirmation dialog, click "No"
4. Verify application continues running normally

**Expected Results**:
- Confirmation dialog is dismissed
- No countdown dialog appears
- Application continues running normally
- No shutdown stages are initiated

**Debug Output to Verify**:
- No shutdown-related debug output (shutdown was cancelled before initiation)

---

### Test 7: Resource Disposal Verification

**Objective**: Verify that all resources are properly disposed during shutdown.

**Steps**:
1. Launch the Risk Manager application
2. Play some sounds (if there's functionality that uses sound players)
3. Interact with various UI elements
4. Click shutdown button
5. Confirm and complete shutdown
6. Check debug output for resource disposal

**Expected Results**:
- All sound players are disposed
- CancellationTokenSource is disposed
- Tooltips are disposed
- All managed resources are released

**Debug Output to Verify**:
```
[SHUTDOWN] Stage 4: Releasing resources
[SHUTDOWN] DisposeResources: alertSoundPlayer disposed
[SHUTDOWN] DisposeResources: shutdownSoundPlayer disposed
[SHUTDOWN] DisposeResources: shutdownCancellationTokenSource disposed
[SHUTDOWN] DisposeResources: titleToolTip disposed
[SHUTDOWN] DisposeResources: Resource cleanup complete
```

---

### Test 8: Fallback Mechanism Testing

**Objective**: Verify that fallback mechanisms work if primary shutdown methods fail.

**Note**: This test requires code modification to simulate failures and should only be performed in a development/test environment.

**Steps**:
1. Temporarily modify `PerformGracefulShutdown` to throw an exception in Stage 5 (Form.Close)
2. Launch the application
3. Initiate shutdown
4. Observe that Stage 6 (Application.Exit) is triggered
5. Restore code and test Stage 6 failure similarly

**Expected Results**:
- If Form.Close() fails, Application.Exit() is called
- If Application.Exit() fails, Environment.Exit(0) is called
- Application eventually closes regardless of failures

**Debug Output to Verify**:
```
[SHUTDOWN] Stage 5: Error closing parent form: [Error message]
[SHUTDOWN] Stage 6: Attempting Application.Exit()
[SHUTDOWN] Stage 6: Application.Exit() called
```

or

```
[SHUTDOWN] Stage 6: Application.Exit() failed: [Error message]
[SHUTDOWN] Stage 7: Forcing shutdown with Environment.Exit(0)
```

---

## Regression Testing Checklist

After confirming all test scenarios pass, verify that existing functionality still works:

- [ ] Application launches successfully
- [ ] All tabs and navigation work correctly
- [ ] Timers update data properly
- [ ] Account selection and settings work
- [ ] Lock/unlock functionality works
- [ ] Theme switching works
- [ ] All existing buttons and controls function properly

---

## Performance Testing

### Memory Leak Check

**Steps**:
1. Launch application
2. Note initial memory usage in Task Manager
3. Use the application normally for 5-10 minutes
4. Shut down using the shutdown button
5. Verify process is completely terminated
6. Launch application again
7. Verify memory usage is similar to initial launch

**Expected Results**:
- No significant memory growth over time
- Process terminates completely
- No memory leaks detected
- Subsequent launches use similar memory

### Response Time

**Steps**:
1. Launch application
2. Click shutdown button
3. Time from button click to application closure
4. Repeat 5 times

**Expected Results**:
- Shutdown completes within 6-8 seconds (5 second countdown + 1-3 seconds for cleanup)
- Consistent timing across multiple attempts
- No hanging or freezing

---

## Error Handling Testing

### Simulated Errors

Test error handling by temporarily introducing errors in the code:

1. **Timer Stop Error**: Make a timer throw an exception when Stop() is called
   - Expected: Other timers still stop, shutdown continues

2. **Form Close Error**: Make a child form throw on Close()
   - Expected: Other forms close, shutdown continues

3. **Resource Disposal Error**: Make a resource throw on Dispose()
   - Expected: Other resources dispose, shutdown continues

---

## Logging Verification

For each test scenario, verify that:

1. All major stages are logged with [SHUTDOWN] prefix
2. Error conditions are logged with clear error messages
3. Timing information is available from logs
4. Log sequence matches expected shutdown flow

---

## Sign-off Checklist

Before considering the implementation complete, verify:

- [ ] All test scenarios pass
- [ ] No regressions in existing functionality
- [ ] Performance is acceptable
- [ ] Error handling works as expected
- [ ] Logging is comprehensive and useful
- [ ] Documentation is complete and accurate
- [ ] Code follows project conventions
- [ ] No TODO or FIXME comments remain in code

---

## Troubleshooting Common Issues

### Issue: Application doesn't close

**Check**:
- Debug output for error messages
- Task Manager for remaining processes
- Event Viewer for application crashes

**Solutions**:
- Verify all fallback mechanisms are working
- Check for infinite loops in timer events
- Ensure no modal dialogs are blocking shutdown

### Issue: Timers continue firing after shutdown initiated

**Check**:
- Debug output for "StopAllTimers" completion
- Verify timer references are not null

**Solutions**:
- Ensure StopAllTimers is called early in shutdown
- Check for timer recreation after stop
- Verify timer event handlers check for shutdown state

### Issue: Resources not released

**Check**:
- Debug output for DisposeResources stage
- Memory profiler for resource leaks

**Solutions**:
- Verify all Dispose calls complete successfully
- Check for resource references held elsewhere
- Ensure Dispose is called before Environment.Exit

---

## Contact for Support

If you encounter issues not covered in this guide:
1. Check the debug output for detailed error messages
2. Review GRACEFUL_SHUTDOWN_IMPLEMENTATION.md for implementation details
3. Raise an issue in the repository with debug output and steps to reproduce
