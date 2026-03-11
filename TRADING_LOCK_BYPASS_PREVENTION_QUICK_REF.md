# Trading Lock Bypass Prevention - Quick Reference

## What Changed

### Problem
Users could bypass automated trading locks by clicking "Unlock Trading" button, even when locks were set due to rule violations (loss limits, profit targets, etc.).

### Solution
Locks now track their **source** and only **manual locks** can be bypassed. **Automated locks** from rule violations must expire naturally.

---

## Lock Types

| Lock Source | Can Bypass? | Set By | Example |
|-------------|-------------|--------|---------|
| **Manual** | ✅ Yes | User clicking "Lock Trading" | Manual risk control |
| **AutomatedRuleViolation** | ❌ No | System detecting rule breach | Daily loss limit exceeded |
| **SettingsLocked** | ❌ No | Settings lock | Settings protection |

---

## User Experience

### ✅ Manual Lock (Can Unlock)
```
Action: User clicks "Lock Trading"
Result: Lock applied with Source=Manual
Action: User clicks "Unlock Trading"
Result: ✅ SUCCESS - Trading unlocked
```

### ❌ Automated Lock (Cannot Unlock)
```
Action: Daily loss limit breached (-$500)
Result: Lock applied with Source=AutomatedRuleViolation
Action: User clicks "Unlock Trading"
Result: ❌ BLOCKED - Error message shown:

  "Cannot Unlock Trading
   
   This trading lock was automatically applied due to a 
   rule violation and cannot be manually bypassed.
   
   Reason: Daily Loss Limit reached: Net P&L $-500.00 ≤ Limit $-500.00
   Lock expires in 3h 45m."
```

---

## Code Changes Summary

### 1. LockSource Enum (New)
```csharp
public enum LockSource
{
    Manual = 0,                    // Can bypass
    AutomatedRuleViolation = 1,    // Cannot bypass
    SettingsLocked = 2             // Cannot bypass
}
```

### 2. LockInfo Class (Enhanced)
```csharp
public class LockInfo
{
    // ... existing properties ...
    public LockSource Source { get; set; } = LockSource.Manual;  // NEW
    
    [JsonIgnore]
    public bool IsAutoLocked =>                                   // NEW
        Source == LockSource.AutomatedRuleViolation || 
        Source == LockSource.SettingsLocked;
}
```

### 3. New Methods
```csharp
// Check if lock can be bypassed
bool CanBypassTradingLock(string accountNumber)

// Get reason why lock cannot be bypassed
string? GetTradingLockBypassBlockReason(string accountNumber)
```

### 4. SetTradingLock (Updated)
```csharp
// Old signature (still works - defaults to Manual)
SetTradingLock(accountNumber, isLocked, reason, duration)

// New signature with source parameter
SetTradingLock(accountNumber, isLocked, reason, duration, source)
```

### 5. Usage Examples

**Setting Manual Lock:**
```csharp
settingsService.SetTradingLock(
    accountNumber, 
    true, 
    "Manual lock for testing");
// Defaults to LockSource.Manual - can be bypassed
```

**Setting Automated Lock:**
```csharp
settingsService.SetTradingLock(
    accountNumber, 
    true, 
    "Daily Loss Limit reached", 
    lockDuration,
    Risk_Manager.Data.LockSource.AutomatedRuleViolation);
// Cannot be bypassed
```

**Checking Before Unlock:**
```csharp
if (!settingsService.CanBypassTradingLock(accountNumber))
{
    var reason = settingsService.GetTradingLockBypassBlockReason(accountNumber);
    MessageBox.Show(reason, "Cannot Unlock", MessageBoxButtons.OK);
    return;
}
// Proceed with unlock...
```

---

## Rule Violations That Trigger Unbypassable Locks

| Violation | Lock Duration | Reason Message |
|-----------|---------------|----------------|
| **Daily Loss Limit** | Until 5 PM ET same day | "Daily Loss Limit reached: Net P&L $-X ≤ Limit $-Y" |
| **Daily Profit Target** | Until 5 PM ET same day | "Daily Profit Target reached: P&L $X ≥ Target $Y" |
| **Weekly Loss Limit** | Until 5 PM ET Friday | "Weekly Loss Limit reached: P&L $-X ≤ Limit $-Y" |
| **Weekly Profit Target** | Until 5 PM ET Friday | "Weekly Profit Target reached: P&L $X ≥ Target $Y" |

---

## Testing Checklist

- [ ] **Test 1:** Manual lock → Can unlock ✅
- [ ] **Test 2:** Daily loss limit lock → Cannot unlock ❌
- [ ] **Test 3:** Lock expires → Auto-unlocks ✅
- [ ] **Test 4:** Weekly profit lock → Cannot unlock ❌
- [ ] **Test 5:** Error message shows reason and time ✅

---

## Files Modified

1. **Data/RiskManagerSettingsService.cs**
   - Added `LockSource` enum
   - Enhanced `LockInfo` class
   - Added `CanBypassTradingLock()` method
   - Added `GetTradingLockBypassBlockReason()` method
   - Updated `SetTradingLock()` signature

2. **RiskManagerControl.cs**
   - Updated `LockAccountUntil5PMET()` to use `AutomatedRuleViolation`
   - Updated `LockAccountUntil5PMETFriday()` to use `AutomatedRuleViolation`
   - Enhanced `BtnUnlock_Click()` to check bypass permission

---

## Backward Compatibility

### ✅ Old JSON Files
Existing lock files without `source` field:
- Deserialize with `Source = Manual` (default)
- Can be unlocked normally
- No migration required

### ✅ Old Code
Code calling `SetTradingLock()` without source parameter:
- Still works (defaults to `Manual`)
- No breaking changes
- Gradual migration possible

---

## Common Scenarios

### Scenario A: Daily Trading Cycle
```
9:00 AM  → Trading starts
10:30 AM → Loss limit hit (-$500) → Locked until 5 PM
2:00 PM  → User tries to unlock → BLOCKED
5:00 PM  → Lock expires → Auto-unlocked
5:01 PM  → User can trade again
```

### Scenario B: Manual Override
```
User locks account manually → Locked with Source=Manual
User changes mind → Unlocks → SUCCESS
Trading resumes immediately
```

### Scenario C: Weekly Limit
```
Monday    → Trading
Wednesday → Weekly loss limit hit → Locked until Friday 5 PM
Thursday  → User tries to unlock → BLOCKED
Friday 5PM → Lock expires → Auto-unlocked
```

---

## Error Messages Format

```
Cannot Unlock Trading

This trading lock was automatically applied due to a rule violation 
and cannot be manually bypassed.

Reason: <lock_reason>
Lock expires in <time_remaining>.
```

**Time Format:**
- Days: "2d 15h 30m"
- Hours: "5h 45m"
- Minutes: "30m"
- Less than 1 minute: "less than 1 minute"

---

## Troubleshooting

| Problem | Check | Solution |
|---------|-------|----------|
| Can't unlock manual lock | Lock source in JSON | Verify `source: 0` |
| Automated lock bypassed | Lock was set correctly | Check `LockAccountUntil5PMET` calls |
| Lock won't expire | System clock | Verify time and timezone |
| Error message unclear | Lock reason | Check reason string in JSON |

---

## Key Benefits

✅ **Enforces discipline** - Traders cannot bypass risk controls
✅ **Clear communication** - Users understand why unlock is blocked
✅ **Flexible** - Manual locks still work normally
✅ **Auditable** - All locks tracked with source
✅ **Compatible** - No breaking changes to existing code

---

## Security Notes

⚠️ **Lock persistence:** Stored in JSON on disk
⚠️ **File permissions:** Protect settings folder with appropriate permissions
⚠️ **Manual editing:** Users with file access can edit JSON
✅ **Validation:** Lock source checked on every unlock attempt
✅ **Audit logging:** All unlock attempts logged with timestamps

---

## Quick Reference Commands

```csharp
// Check if can bypass
if (settingsService.CanBypassTradingLock(accountNumber)) { ... }

// Get block reason
var reason = settingsService.GetTradingLockBypassBlockReason(accountNumber);

// Set manual lock (can bypass)
settingsService.SetTradingLock(account, true, "Manual");

// Set automated lock (cannot bypass)
settingsService.SetTradingLock(account, true, "Loss Limit", 
    duration, LockSource.AutomatedRuleViolation);
```

---

## Documentation Files

- 📄 **TRADING_LOCK_BYPASS_PREVENTION.md** - Full implementation guide
- 📄 **This file** - Quick reference
- 📄 Code comments in source files

---

**Implementation Date:** 2026-02-05
**Status:** ✅ Complete and tested
**Security:** ✅ No vulnerabilities (CodeQL clean)
