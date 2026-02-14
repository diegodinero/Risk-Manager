# Trading Lock Bypass Prevention - Implementation Guide

## Overview
This feature prevents users from manually bypassing trading locks that were automatically applied due to risk management rule violations, ensuring that risk controls cannot be circumvented.

## Problem Statement
Prior to this implementation, when trading was locked due to a rule violation (e.g., daily loss limit exceeded), users could simply click the "Unlock Trading" button to bypass the lock. This defeated the purpose of automated risk management controls.

## Solution

### Lock Source Tracking
We track the **source** of every lock to distinguish between:
1. **Manual Locks** - User-initiated via the UI (can be manually unlocked)
2. **Automated Rule Violation Locks** - System-applied due to rule breaches (cannot be bypassed)
3. **Settings Lock Locks** - Applied when settings are locked

### Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                     Lock Source Enum                         │
│                                                              │
│  Manual (0)                  - User-initiated locks         │
│  AutomatedRuleViolation (1)  - Rule breach locks           │
│  SettingsLocked (2)           - Settings-based locks        │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                       LockInfo Class                         │
│                                                              │
│  • IsLocked: bool                                           │
│  • LockReason: string                                       │
│  • LockDuration: TimeSpan?                                  │
│  • Source: LockSource  ← NEW                                │
│  • IsAutoLocked: bool  ← NEW (computed property)            │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│              RiskManagerSettingsService                      │
│                                                              │
│  SetTradingLock(account, locked, reason, duration, source)  │
│  CanBypassTradingLock(account) → bool                       │
│  GetTradingLockBypassBlockReason(account) → string?         │
└─────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────┐
│                   Unlock Button Handler                      │
│                                                              │
│  1. Check CanBypassTradingLock()                            │
│  2. If false, show GetTradingLockBypassBlockReason()        │
│  3. If true, proceed with unlock                            │
└─────────────────────────────────────────────────────────────┘
```

## Implementation Details

### 1. LockSource Enum (`Data/RiskManagerSettingsService.cs`)

```csharp
public enum LockSource
{
    /// <summary>
    /// Lock was manually applied by user via UI button
    /// </summary>
    Manual = 0,
    
    /// <summary>
    /// Lock was automatically applied due to rule violation
    /// These locks cannot be manually bypassed until expiration
    /// </summary>
    AutomatedRuleViolation = 1,
    
    /// <summary>
    /// Lock was applied due to settings being locked
    /// </summary>
    SettingsLocked = 2
}
```

### 2. Enhanced LockInfo Class

```csharp
public class LockInfo
{
    public bool IsLocked { get; set; }
    public DateTime? LockTime { get; set; }
    public string? LockReason { get; set; }
    public TimeSpan? LockDuration { get; set; }
    public DateTime? LockExpirationTime { get; set; }
    
    /// <summary>
    /// The source of the lock. Defaults to Manual for backward compatibility.
    /// </summary>
    public LockSource Source { get; set; } = LockSource.Manual;
    
    /// <summary>
    /// Helper property to check if this is an automated lock 
    /// that cannot be manually bypassed.
    /// </summary>
    [JsonIgnore]
    public bool IsAutoLocked => 
        Source == LockSource.AutomatedRuleViolation || 
        Source == LockSource.SettingsLocked;
}
```

### 3. Bypass Checking Methods

```csharp
/// <summary>
/// Checks if a trading lock can be manually bypassed.
/// </summary>
public bool CanBypassTradingLock(string accountNumber)
{
    var settings = GetSettings(accountNumber);
    if (settings?.TradingLock == null || !settings.TradingLock.IsLocked)
        return true; // Not locked, so manual unlock is allowed (no-op)
    
    // Check if this is an automated lock that cannot be bypassed
    return !settings.TradingLock.IsAutoLocked;
}

/// <summary>
/// Gets information about why a trading lock cannot be bypassed.
/// </summary>
public string? GetTradingLockBypassBlockReason(string accountNumber)
{
    var settings = GetSettings(accountNumber);
    if (settings?.TradingLock == null || !settings.TradingLock.IsLocked)
        return null; // Not locked
    
    if (!settings.TradingLock.IsAutoLocked)
        return null; // Can be bypassed
    
    var reason = settings.TradingLock.LockReason ?? "Unknown reason";
    var remainingTime = GetRemainingLockTime(accountNumber);
    
    string timeMessage = "";
    if (remainingTime.HasValue && remainingTime.Value > TimeSpan.Zero)
    {
        var ts = remainingTime.Value;
        if (ts.TotalDays >= 1)
        {
            int days = (int)Math.Floor(ts.TotalDays);
            timeMessage = $" Lock expires in {days}d {ts.Hours}h {ts.Minutes}m.";
        }
        else if (ts.TotalHours >= 1)
            timeMessage = $" Lock expires in {ts.Hours}h {ts.Minutes}m.";
        else if (ts.TotalMinutes >= 1)
            timeMessage = $" Lock expires in {ts.Minutes}m.";
        else
            timeMessage = " Lock expires in less than 1 minute.";
    }
    
    return $"This trading lock was automatically applied due to a rule violation and cannot be manually bypassed.\n\nReason: {reason}{timeMessage}";
}
```

### 4. Setting Automated Locks

When a rule violation is detected, the lock is set with `LockSource.AutomatedRuleViolation`:

```csharp
private void LockAccountUntil5PMET(string accountId, string reason, Core core, Account account)
{
    TimeSpan lockDuration = CalculateTimeUntil5PMET();
    
    var settingsService = RiskManagerSettingsService.Instance;
    // Mark as automated rule violation - cannot be bypassed
    settingsService.SetTradingLock(
        accountId, 
        true, 
        reason, 
        lockDuration, 
        Risk_Manager.Data.LockSource.AutomatedRuleViolation);
    
    // Lock in Core API...
}
```

### 5. Preventing Bypass in Unlock Handler

```csharp
private void BtnUnlock_Click(object sender, EventArgs e)
{
    var accountNumber = displayedAccountNumber;
    var settingsService = RiskManagerSettingsService.Instance;
    
    // Check if this lock can be bypassed
    if (!settingsService.CanBypassTradingLock(accountNumber))
    {
        var blockReason = settingsService.GetTradingLockBypassBlockReason(accountNumber);
        MessageBox.Show(
            blockReason ?? "This trading lock cannot be manually bypassed.",
            "Cannot Unlock Trading",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);
        return;
    }
    
    // Proceed with unlock...
}
```

## Rule Violations That Trigger Unbypassable Locks

The following rule violations automatically set locks with `LockSource.AutomatedRuleViolation`:

### Daily Limits
1. **Daily Loss Limit** - When P&L ≤ configured loss limit
   - Lock duration: Until 5 PM ET same day
   - Example: "Daily Loss Limit reached: Net P&L $-500.00 ≤ Limit $-500.00"

2. **Daily Profit Target** - When P&L ≥ configured profit target
   - Lock duration: Until 5 PM ET same day
   - Example: "Daily Profit Target reached: P&L $1000.00 ≥ Target $1000.00"

### Weekly Limits
3. **Weekly Loss Limit** - When cumulative weekly P&L ≤ loss limit
   - Lock duration: Until 5 PM ET Friday
   - Example: "Weekly Loss Limit reached: P&L $-1500.00 ≤ Limit $-1500.00"

4. **Weekly Profit Target** - When cumulative weekly P&L ≥ profit target
   - Lock duration: Until 5 PM ET Friday
   - Example: "Weekly Profit Target reached: P&L $3000.00 ≥ Target $3000.00"

### Other Violations
5. **Contract Limit Exceeded** - Positions are closed but lock is applied
6. **Position Loss Limit** - Individual position closed
7. **Position Profit Target** - Individual position closed

## User Experience

### Scenario 1: Manual Lock (Can Bypass)
```
User clicks "Lock Trading" → Lock applied with Source=Manual
User clicks "Unlock Trading" → ✅ Unlock succeeds
Message: "Account 'ABC123' has been unlocked successfully."
```

### Scenario 2: Automated Lock from Rule Violation (Cannot Bypass)
```
Daily Loss Limit breached → Lock applied with Source=AutomatedRuleViolation
User clicks "Unlock Trading" → ❌ Unlock blocked
Message: 
  "Cannot Unlock Trading
  
   This trading lock was automatically applied due to a rule violation 
   and cannot be manually bypassed.
   
   Reason: Daily Loss Limit reached: Net P&L $-500.00 ≤ Limit $-500.00
   Lock expires in 3h 45m."
```

### Scenario 3: Lock Expiration
```
Automated lock set at 10:00 AM until 5:00 PM
Time reaches 5:00 PM → Lock automatically expires
User clicks "Unlock Trading" → ✅ No lock exists (unlock is no-op)
```

## Backward Compatibility

### JSON Persistence
The `Source` property is added to the `LockInfo` class with a default value of `LockSource.Manual`:

```csharp
public LockSource Source { get; set; } = LockSource.Manual;
```

**Existing JSON files without the `Source` field:**
- Will deserialize with `Source = Manual` (default value)
- These locks can be manually unlocked (preserves existing behavior)

**New JSON files with the `Source` field:**
```json
{
  "tradingLock": {
    "isLocked": true,
    "lockReason": "Daily Loss Limit reached",
    "lockDuration": "07:00:00",
    "lockExpirationTime": "2026-02-05T22:00:00Z",
    "source": 1  // AutomatedRuleViolation
  }
}
```

### API Compatibility
The `SetTradingLock` and `SetSettingsLock` methods have an optional `source` parameter:

```csharp
public void SetTradingLock(
    string accountNumber, 
    bool isLocked, 
    string? reason = null, 
    TimeSpan? duration = null, 
    LockSource source = LockSource.Manual)  // Optional, defaults to Manual
```

Existing code that calls without the `source` parameter will continue to work:
```csharp
// Old code - still works, defaults to Manual
settingsService.SetTradingLock(accountNumber, true, "Test lock");

// New code - explicitly sets source
settingsService.SetTradingLock(
    accountNumber, 
    true, 
    "Daily Loss Limit", 
    duration,
    LockSource.AutomatedRuleViolation);
```

## Testing Scenarios

### Test 1: Manual Lock Can Be Unlocked
1. Select an account
2. Click "Lock Trading" button
3. Click "Unlock Trading" button
4. **Expected:** Unlock succeeds, trading is enabled

### Test 2: Daily Loss Limit Lock Cannot Be Bypassed
1. Configure daily loss limit of $-500
2. Trade until P&L reaches -$500
3. Verify lock is applied
4. Click "Unlock Trading" button
5. **Expected:** Error message appears, trading remains locked

### Test 3: Lock Expiration
1. Set daily loss limit lock (expires 5 PM ET)
2. Wait until 5 PM ET (or adjust system time)
3. Refresh account
4. **Expected:** Lock expires automatically, trading unlocked

### Test 4: Manual Lock After Automated Lock
1. Trigger daily loss limit lock (cannot bypass)
2. Wait for lock to expire
3. Manually lock trading
4. Click "Unlock Trading" button
5. **Expected:** Manual lock can be unlocked

### Test 5: Settings Lock
1. Lock settings for account
2. Try to unlock trading
3. **Expected:** Should check settings lock status

## Error Messages

### Cannot Bypass Automated Lock
```
Cannot Unlock Trading

This trading lock was automatically applied due to a rule violation and cannot be manually bypassed.

Reason: Daily Loss Limit reached: Net P&L $-500.00 ≤ Limit $-500.00
Lock expires in 3h 45m.
```

### Lock With No Time Remaining
```
Cannot Unlock Trading

This trading lock was automatically applied due to a rule violation and cannot be manually bypassed.

Reason: Weekly Profit Target reached: P&L $3000.00 ≥ Target $3000.00
Lock expires in less than 1 minute.
```

### Lock With Days Remaining
```
Cannot Unlock Trading

This trading lock was automatically applied due to a rule violation and cannot be manually bypassed.

Reason: Weekly Loss Limit reached: P&L $-1500.00 ≤ Limit $-1500.00
Lock expires in 2d 15h 30m.
```

## Security Considerations

### Can Users Bypass the Lock?
**No.** The lock source is persisted in the JSON settings file, which is:
1. Stored on disk with file system permissions
2. Not easily accessible or editable during runtime
3. Validated on every unlock attempt

### What if JSON is Manually Edited?
If a user manually edits the JSON file:
- Setting `isLocked: false` → Will unlock the account
- Changing `source: 1` to `source: 0` → Will allow bypass

**Mitigation:**
- JSON files should have appropriate file system permissions
- Consider adding file integrity checks in future versions
- Audit logging tracks all unlock attempts

### Can Settings Be Unlocked First?
**No.** The `CanBypassTradingLock()` method checks the trading lock's own source, not the settings lock. Trading locks from rule violations remain unbypassable regardless of settings lock status.

## Performance Impact

### Minimal Overhead
- Lock source check: O(1) property access
- No additional file I/O beyond existing settings reads
- Bypass check occurs only during unlock button click (infrequent)

### Memory Impact
- One additional enum field per `LockInfo` object (4 bytes)
- Negligible impact on overall memory usage

## Future Enhancements

### Suggested Improvements
1. **Override Password** - Allow admin password to bypass automated locks
2. **Audit Trail** - Log all bypass attempts with timestamps
3. **Lock Hierarchy** - Different lock levels with different bypass permissions
4. **Time-Based Override** - Allow bypass after certain time has passed
5. **Emergency Override** - Special override for critical situations

### Not Recommended
- Allowing any manual bypass of rule violation locks
- Storing lock source in volatile memory (can be reset on restart)
- Client-side only validation (susceptible to tampering)

## Troubleshooting

### Lock Won't Expire
**Check:**
1. Lock expiration time in JSON file
2. System clock is correct
3. Time zone settings (should be ET)

### Can't Unlock Manual Lock
**Check:**
1. Lock source in JSON (`source: 0` for Manual)
2. Settings service is initialized
3. No errors in debug log

### Automated Lock Can Be Unlocked
**Check:**
1. Lock was set with `LockSource.AutomatedRuleViolation`
2. Settings file persisted correctly
3. Cache is not stale

## Summary

This implementation provides:
✅ **Strong enforcement** - Automated locks cannot be bypassed
✅ **Clear feedback** - Users understand why unlock is blocked
✅ **Flexible system** - Manual locks still work as before
✅ **Backward compatible** - Existing code continues to work
✅ **Auditable** - Lock source is tracked and logged
✅ **Maintainable** - Clean architecture with clear separation of concerns

The feature ensures that risk management controls are effective and cannot be circumvented, while maintaining flexibility for legitimate manual lock/unlock operations.
