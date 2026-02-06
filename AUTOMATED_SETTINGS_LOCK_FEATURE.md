# Automated Settings Lock Time Feature

## Overview

This feature adds the ability to configure a specific time each day when settings will automatically lock, eliminating the need to manually lock settings at the start of trading hours.

## Key Changes

### Data Model (RiskManagerSettingsService.cs)

Added two new properties to the `AccountSettings` class:

```csharp
// Automated Settings Lock
public bool AutoLockSettingsEnabled { get; set; } = false;
public TimeSpan? AutoLockSettingsTime { get; set; }
```

**Properties:**
- `AutoLockSettingsEnabled`: Boolean flag to enable/disable the automated lock feature
- `AutoLockSettingsTime`: TimeSpan representing the time of day in Eastern Time when settings should automatically lock

### User Interface (RiskManagerControl.cs)

Added a new "Automated Daily Lock" section to the Lock Settings tab with the following controls:

1. **Section Title and Description**
   - Clear labeling to distinguish from manual lock controls
   - Explanation of the feature's purpose

2. **Enable Checkbox**
   - Allows users to toggle the automated lock on/off
   - Tagged as "AutoLockEnabled" for programmatic access

3. **Time Input Fields**
   - Hour text box (00-23, 2-digit format)
   - Minute text box (00-59, 2-digit format)
   - Format hint showing "HH:MM in Eastern Time"
   - Tagged as "AutoLockHour" and "AutoLockMinute"

4. **Save Button**
   - Validates input (hour 0-23, minute 0-59)
   - Saves configuration to account settings
   - Shows confirmation message with status

### Logic Implementation (RiskManagerControl.cs)

#### New Method: `ShouldTriggerAutoLock(TimeSpan autoLockTime)`

Checks if the automated lock should be triggered based on the configured time.

**Features:**
- Converts current UTC time to Eastern Time
- Checks if current time is within configured lock time (1-minute window)
- Handles timezone conversion with DST support
- Fallback logic for systems without Eastern Time zone data

**Returns:** `true` when the current time matches the configured lock time

#### Enhanced Method: `CheckExpiredLocks()`

Extended to check for automated lock triggers in addition to expired locks.

**New Logic:**
```csharp
// Check if automated lock should trigger (only if not already locked)
if (!isSettingsLocked && settings?.AutoLockSettingsEnabled == true 
    && settings?.AutoLockSettingsTime.HasValue == true)
{
    if (ShouldTriggerAutoLock(settings.AutoLockSettingsTime.Value))
    {
        // Trigger auto-lock until 5 PM ET
        var duration = RiskManagerSettingsService.CalculateDurationUntil5PMET();
        settingsService.SetSettingsLock(accountNumber, true, 
            "Auto-locked at scheduled time", duration);
        
        // Update UI if this is the selected account
        if (selectedAccountNumber == uniqueAccountId)
        {
            selectedAccountChanged = true;
        }
    }
}
```

#### New Method: `UpdateAutoLockControlsRecursive(Control parent)`

Recursively searches for and updates auto-lock controls when the account changes.

**Features:**
- Finds controls by their tags
- Loads configuration from account settings
- Updates UI controls to reflect saved values
- Sets default values if no configuration exists

#### New Helper Method: `FindAutoLockControls(...)`

Helper method to locate auto-lock controls in the control tree by their tags.

### Updated Method: `UpdateLockAccountDisplay(Label lockAccountDisplay)`

Enhanced to call `UpdateAutoLockControlsRecursive()` when the account display changes, ensuring auto-lock settings are reloaded when switching accounts.

## User Workflow

### Initial Configuration

1. User selects an account from the dropdown
2. User navigates to the "Lock Settings" tab
3. User scrolls to the "Automated Daily Lock" section
4. User checks "Enable Automated Lock"
5. User enters desired time (e.g., 09:30 for market open)
6. User clicks "SAVE AUTO-LOCK SETTINGS"
7. System validates input and saves to account settings
8. Confirmation message shows the saved configuration

### Daily Operation

1. Application runs with timer checking every second
2. At the configured time (e.g., 9:30 AM ET), `ShouldTriggerAutoLock()` returns true
3. `CheckExpiredLocks()` triggers the lock automatically
4. Settings lock with reason "Auto-locked at scheduled time"
5. Lock duration calculated until 5:00 PM ET
6. UI updates to show locked status
7. At 5:00 PM ET, lock automatically expires
8. Next day, process repeats at the same time

### Account Switching

1. User switches to a different account
2. `UpdateLockAccountDisplay()` is called
3. `UpdateAutoLockControlsRecursive()` loads the new account's configuration
4. UI controls update to show the new account's settings
5. Each account maintains independent configuration

## Technical Details

### Timer Integration

The existing `lockExpirationCheckTimer` (runs every 1 second) is used to check for automated lock triggers. This eliminates the need for a separate timer and ensures consistent behavior with other time-based features.

### Timezone Handling

Automated lock times are specified in Eastern Time (ET) to align with US market hours:
- Uses `TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")`
- Automatically handles DST transitions
- Fallback logic approximates DST if timezone data unavailable
- Consistent with existing manual lock timezone handling

### Data Persistence

Configuration is stored in the account's JSON settings file:
```json
{
  "accountNumber": "ACCOUNT123",
  "autoLockSettingsEnabled": true,
  "autoLockSettingsTime": "09:30:00",
  ...
}
```

### Trigger Window

The 1-minute window prevents missing the exact trigger second:
- Checks if current time >= configured time
- Checks if current time < configured time + 1 minute
- Only triggers once within the window (lock check prevents re-triggering)

## Common Use Cases

### Market Open Lock (9:30 AM ET)
Configure auto-lock for 9:30 AM to prevent settings changes once market opens.

### Pre-Market Lock (8:00 AM ET)
Lock settings before market open to ensure configurations are final.

### After-Hours Protection (4:00 PM ET)
Lock an hour before standard unlock time to prevent end-of-day changes.

## Benefits

1. **Consistency**: Settings automatically lock at the same time every day
2. **Convenience**: No need to remember to manually lock settings
3. **Discipline**: Enforces trading discipline without user intervention
4. **Flexibility**: Can be enabled/disabled and time can be changed as needed
5. **Per-Account**: Different accounts can have different lock times
6. **Non-Intrusive**: Works alongside existing manual lock functionality

## Testing

See the updated "Testing Recommendations" section in SETTINGS_LOCK_FEATURE.md for comprehensive testing procedures including:
- Auto-lock configuration
- Trigger verification
- Account switching behavior
- Settings persistence
- UI updates

## Backward Compatibility

- New properties have default values (false and null)
- Existing account settings files work without modification
- Feature is opt-in (disabled by default)
- No breaking changes to existing lock functionality
- Manual lock still works independently

## Future Enhancements

Possible improvements:
- Multiple lock times per day
- Different lock times for different days of week
- Holiday schedule support
- Custom unlock times per trigger
- Notification when auto-lock triggers
- Templates for common lock schedules

## Files Modified

1. **Data/RiskManagerSettingsService.cs**
   - Added `AutoLockSettingsEnabled` and `AutoLockSettingsTime` properties

2. **RiskManagerControl.cs**
   - Added UI controls for automated lock configuration
   - Implemented `ShouldTriggerAutoLock()` method
   - Enhanced `CheckExpiredLocks()` to trigger automated locks
   - Added `UpdateAutoLockControlsRecursive()` and `FindAutoLockControls()` methods
   - Enhanced `UpdateLockAccountDisplay()` to reload auto-lock settings

3. **SETTINGS_LOCK_FEATURE.md**
   - Updated to document automated lock feature
   - Added new testing procedures
   - Updated user experience flows

4. **AUTOMATED_SETTINGS_LOCK_FEATURE.md** (NEW)
   - This comprehensive feature documentation

## Summary

The automated settings lock time feature provides a convenient and reliable way to ensure settings are locked at a specific time each day, supporting trading discipline without requiring manual intervention. The implementation integrates seamlessly with existing lock functionality while maintaining per-account configuration flexibility.
