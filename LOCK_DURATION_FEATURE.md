# Lock Duration Feature

## Overview
This feature allows users to lock trading accounts for specified durations in the Manual Lock Tab. After the lock duration expires, the account automatically unlocks.

## Lock Duration Options
The following lock duration options are available in the Manual Lock Tab:

1. **Indefinite** - Lock remains until manually unlocked
2. **5 Minutes** - Lock for 5 minutes
3. **15 Minutes** - Lock for 15 minutes
4. **1 Hour** - Lock for 1 hour
5. **2 Hours** - Lock for 2 hours
6. **4 Hours** - Lock for 4 hours
7. **All Day (Until 5PM)** - Lock until 5 PM today (or tomorrow if after 5 PM)
8. **All Week** - Lock until end of week (Sunday 11:59 PM)

## How to Use

### Locking an Account with Duration
1. Navigate to the **Manual Lock Tab** (🔒 Manual Lock)
2. Select an account from the account dropdown at the top
3. Choose a lock duration from the **Lock Duration** dropdown
4. Click the **LOCK TRADING** button
5. A confirmation message will show the account is locked

### Viewing Lock Status
Lock status with remaining time is displayed in two locations:

#### Accounts Summary Tab (📊 Accounts Summary)
- The **Lock Status** column shows:
  - `Unlocked` - Account is not locked
  - `Locked` - Account is locked indefinitely
  - `Locked (Xh Ym)` - Account is locked with X hours and Y minutes remaining
  - `Locked (Xm)` - Account is locked with X minutes remaining
  - `Locked (Xd Yh Zm)` - Account is locked with X days, Y hours, and Z minutes remaining

#### Stats Tab (📈 Stats)
- The **Trading Lock Status** row shows the same format as above

### Unlocking an Account
Accounts can be unlocked in two ways:

1. **Manual Unlock**
   - Navigate to the **Manual Lock Tab**
   - Click the **UNLOCK TRADING** button
   - Account will be unlocked immediately

2. **Automatic Unlock**
   - When the lock duration expires, the account automatically unlocks
   - A background timer checks every 30 seconds for expired locks
   - The UI updates to reflect the unlocked status
   - The lock reason will show "Auto-unlocked after duration expired"

## Technical Details

### Data Model
The `LockInfo` class in `RiskManagerSettingsService.cs` has been extended with:
- `LockDuration` (TimeSpan?) - The duration of the lock
- `LockExpirationTime` (DateTime?) - The time when the lock expires

### Auto-Unlock Mechanism
Two mechanisms ensure locks are auto-unlocked:

1. **On-Demand Check**: Every time `IsTradingLocked()` is called, it checks if the lock has expired and auto-unlocks if necessary.

2. **Background Timer**: A background timer (`lockExpirationCheckTimer`) runs every 30 seconds to check all accounts for expired locks and process auto-unlocks.

### Lock Status Display
The `GetLockStatusString()` method in the settings service formats the lock status with remaining time:
- If unlocked: "Unlocked"
- If locked indefinitely: "Locked"
- If locked with duration: "Locked (Xh Ym)" or "Locked (Xm)" or "Locked (Xd Yh Zm)"

## Files Modified
1. `Data/RiskManagerSettingsService.cs`
   - Extended `LockInfo` class
   - Updated `SetTradingLock()` to support duration
   - Added `GetRemainingLockTime()` and `GetLockStatusString()` methods
   - Modified `IsTradingLocked()` to check expiration

2. `RiskManagerControl.cs`
   - Added lock duration dropdown to Manual Lock Tab
   - Implemented `GetSelectedLockDuration()` helper method
   - Added `CheckExpiredLocks()` method and background timer
   - Updated lock status displays in Accounts Summary and Stats tabs

## Benefits
- **Flexibility**: Multiple duration options for different trading scenarios
- **Automation**: No need to manually unlock after a set period
- **Transparency**: Clear display of remaining lock time
- **Safety**: Prevents accidental trading during high-risk periods
- **Convenience**: "All Day" and "All Week" options for planned breaks
