# Automated Trading Lock Feature Documentation

## Overview

Added automated daily trading lock feature to the Manual Lock tab, allowing users to configure a specific time when trading will automatically lock each day. This feature uses an AM/PM picker interface similar to the Allowed Trading Times tab.

## Feature Components

### 1. Data Model

**File:** `Data/RiskManagerSettingsService.cs`

**New Properties in AccountSettings:**
```csharp
// Automated Trading Lock
public bool AutoLockTradingEnabled { get; set; } = false;
public TimeSpan? AutoLockTradingTime { get; set; }
```

**New Public Method:**
```csharp
public void UpdateAutoLockTrading(string accountNumber, bool enabled, TimeSpan? lockTime)
```

### 2. User Interface

**Location:** Manual Lock tab (🔒 Manual Lock)

**Section:** "Automated Daily Trading Lock"

**Controls:**
1. **Section Title** - "Automated Daily Trading Lock"
2. **Description** - "Automatically lock trading at a specific time each day."
3. **Enable Checkbox** - "Enable Automated Trading Lock"
4. **Time Picker:**
   - Hour dropdown (1-12 in 12-hour format)
   - Minute dropdown (00, 15, 30, 45)
   - AM/PM dropdown (AM, PM)
5. **Help Text** - "(e.g., 09:30 AM for market open)"
6. **Save Button** - "SAVE AUTO-LOCK SETTINGS" (blue color)

### 3. Logic Implementation

**Methods Added:**

#### UpdateAutoLockTradingControlsRecursive()
- Recursively searches for auto-lock trading controls in the UI
- Loads saved settings from the data model
- Updates controls to reflect current configuration
- Converts 24-hour time to 12-hour AM/PM format for display

#### FindAutoLockTradingControls()
- Helper method to locate controls by their tags
- Searches for: AutoLockTradingEnabled, AutoLockTradingHour, AutoLockTradingMinute, AutoLockTradingAmPm

#### Enhanced CheckExpiredLocks()
- Added check for automated trading lock trigger
- Runs every second as part of existing timer
- Triggers lock when current time matches configured time
- Only triggers if trading is not already locked
- Locks until 5:00 PM ET

## User Workflow

### Initial Configuration

1. User opens Manual Lock tab
2. Scrolls down to "Automated Daily Trading Lock" section
3. Checks "Enable Automated Trading Lock" checkbox
4. Selects time using AM/PM picker:
   - Hour: 09
   - Minute: 30
   - AM/PM: AM
5. Clicks "SAVE AUTO-LOCK SETTINGS"
6. Receives confirmation: "Automated trading lock enabled. Trading will lock daily at 9:30 AM ET."

### Daily Operation

1. Application runs with timer checking every second
2. At 9:30 AM ET, `ShouldTriggerAutoLock()` returns true
3. `CheckExpiredLocks()` triggers the trading lock
4. Trading locks with reason "Auto-locked trading at scheduled time"
5. Lock duration calculated until 5:00 PM ET
6. UI updates to show locked status
7. At 5:00 PM ET, lock automatically expires
8. Next day, process repeats at 9:30 AM

### Account Switching

1. User switches to different account
2. `UpdateLockAccountDisplay()` is called
3. `UpdateAutoLockTradingControlsRecursive()` loads new account's settings
4. UI controls update to show saved configuration
5. Each account maintains independent settings

## Technical Details

### Time Conversion

**Input Format:** 12-hour with AM/PM
- User selects: 09:30 AM
- Converts to: TimeSpan(9, 30, 0)

**Conversion Logic:**
```csharp
int hour = int.Parse(hourComboBox.SelectedItem.ToString());
string ampm = ampmComboBox.SelectedItem.ToString();

if (ampm == "PM" && hour != 12)
    hour += 12;
else if (ampm == "AM" && hour == 12)
    hour = 0;
```

**Storage Format:** 24-hour TimeSpan
- Stored as: TimeSpan(9, 30, 0) for 9:30 AM
- Stored as: TimeSpan(14, 30, 0) for 2:30 PM

**Display Format:** 12-hour with AM/PM
```csharp
int hour = time.Hours;
bool isPM = hour >= 12;

if (hour > 12) hour -= 12;
if (hour == 0) hour = 12;

// Display: 9:30 AM or 2:30 PM
```

### Minute Rounding

Minutes are rounded to nearest 15-minute interval:
```csharp
int roundedMinute = (minute / 15) * 15;
```

Options: 00, 15, 30, 45

### Trigger Check

**Method:** `ShouldTriggerAutoLock(TimeSpan autoLockTime)`

**Logic:**
1. Get current time in Eastern Time
2. Check if current time >= configured lock time
3. Check if current time < configured lock time + 1 minute
4. Returns true only within this 1-minute window

**Prevents:**
- Missing the exact second
- Re-triggering multiple times
- Double-locking

### Lock Enforcement

When automated trading lock triggers:
1. Sets trading lock in settings service
2. Calls Core API `LockAccount()` method
3. Locks until 5:00 PM ET
4. Updates UI if it's the selected account
5. Logs debug message

## Integration with Existing Features

### Independent from Settings Lock

**Settings Lock:**
- Purpose: Prevents configuration changes
- Location: Lock Settings tab
- Automated: AutoLockSettingsEnabled / AutoLockSettingsTime

**Trading Lock:**
- Purpose: Prevents Buy/Sell actions
- Location: Manual Lock tab
- Automated: AutoLockTradingEnabled / AutoLockTradingTime

Both can be enabled simultaneously and work independently.

### Coexists with Manual Lock

- Manual lock: User clicks button to lock immediately
- Automated lock: System locks at configured time
- Both use same locking mechanism
- Both unlock at 5:00 PM ET
- Automated lock only triggers if not already locked

### Uses Existing Infrastructure

- Reuses `ShouldTriggerAutoLock()` method from settings lock
- Reuses `CheckExpiredLocks()` timer (runs every 1 second)
- Reuses `UpdateLockAccountDisplay()` pattern
- Reuses `CalculateDurationUntil5PMET()` method

## UI Design Patterns

### Follows Allowed Trading Times Style

The AM/PM picker design matches the Allowed Trading Times tab:
- Hour dropdown (1-12)
- Minute dropdown (00, 15, 30, 45)
- AM/PM dropdown
- Clear labeling
- Help text

### Follows Settings Lock Style

The section layout matches the automated settings lock:
- Separator line above section
- Section title (bold, large font)
- Description text (gray, smaller font)
- Enable checkbox
- Time input controls
- Save button (blue color)

## Data Persistence

### Storage Location

Settings saved in: `%LocalAppData%/RiskManager/[AccountNumber].json`

### JSON Format

```json
{
  "accountNumber": "ACCOUNT123",
  "autoLockTradingEnabled": true,
  "autoLockTradingTime": "09:30:00",
  ...
}
```

### Loading

- Automatically loads when account selected
- Called in `UpdateAutoLockTradingControlsRecursive()`
- Triggered by `UpdateLockAccountDisplay()`

## Examples

### Example 1: Market Open Lock

**Configuration:**
- Enable: ✓
- Time: 09:30 AM

**Behavior:**
- Trading locks at 9:30 AM ET every trading day
- Prevents all Buy/Sell buttons
- Unlocks at 5:00 PM ET
- Next day repeats

**Use Case:**
Trader wants to ensure no trading after market opens to enforce discipline.

### Example 2: Pre-Market Lock

**Configuration:**
- Enable: ✓
- Time: 08:00 AM

**Behavior:**
- Trading locks at 8:00 AM ET
- Ensures trading stops before market opens
- Allows reviewing positions
- Unlocks at 5:00 PM ET

**Use Case:**
Trader wants to review strategies before market open without trading.

### Example 3: Afternoon Lock

**Configuration:**
- Enable: ✓
- Time: 02:30 PM

**Behavior:**
- Trading locks at 2:30 PM ET
- Prevents late-day trading
- Unlocks at 5:00 PM ET (2.5 hours later)

**Use Case:**
Trader wants to stop trading mid-afternoon to avoid emotional decisions.

### Example 4: Disabled

**Configuration:**
- Enable: ✗
- Time: 09:30 AM (saved but not active)

**Behavior:**
- No automatic locking occurs
- Time setting preserved for future use
- Manual lock still available

**Use Case:**
Trader temporarily disables auto-lock without losing configuration.

## Benefits

### For Users

1. **Automation** - No need to remember to lock manually
2. **Consistency** - Same time every day
3. **Discipline** - Enforces trading rules automatically
4. **Flexibility** - Can be enabled/disabled as needed
5. **Per-Account** - Different accounts, different times
6. **Intuitive** - AM/PM picker is familiar to all users

### For System

1. **Clean Integration** - Uses existing timer and methods
2. **Independent** - Doesn't affect settings lock
3. **Minimal Code** - Reuses existing patterns
4. **Well-Documented** - Clear tags and comments
5. **Maintainable** - Follows established conventions

## Testing Recommendations

### Manual Testing

1. **Configuration Test**
   - Enable automated trading lock
   - Set time to current time + 2 minutes
   - Save settings
   - Wait 2-3 minutes
   - Verify trading locks

2. **AM/PM Test**
   - Set time to 09:30 AM
   - Save and verify message shows "9:30 AM"
   - Set time to 02:30 PM
   - Save and verify message shows "2:30 PM"

3. **Account Switch Test**
   - Configure Account A: 09:30 AM
   - Configure Account B: 02:30 PM
   - Switch between accounts
   - Verify UI shows correct settings

4. **Persistence Test**
   - Configure automated lock
   - Close application
   - Reopen application
   - Verify settings loaded correctly

5. **Disable Test**
   - Uncheck enable checkbox
   - Save settings
   - Wait for configured time
   - Verify no automatic locking

### Edge Cases

1. **Midnight** (12:00 AM)
   - Verify converts to 00:00 in 24-hour format
   
2. **Noon** (12:00 PM)
   - Verify stays as 12:00 in 24-hour format

3. **Already Locked**
   - Manually lock trading
   - Wait for auto-lock time
   - Verify no duplicate lock attempt

4. **After 5 PM**
   - Auto-lock triggers after 5 PM
   - Should lock until 5 PM next day

## Backward Compatibility

### No Breaking Changes

- New properties have default values (false, null)
- Existing settings files work without modification
- Feature is opt-in (disabled by default)
- No changes to existing lock functionality

### Migration

No migration required:
- Old settings files automatically gain new properties
- Default values ensure no unexpected behavior
- Users discover feature when ready

## Future Enhancements

Potential improvements:
1. Different lock times for different days of week
2. Multiple lock times per day
3. Different unlock times per trigger
4. Holiday schedule support
5. Notification when auto-lock triggers
6. Templates for common schedules

## Summary

The automated trading lock feature provides a convenient way to automatically lock trading at a specific time each day. The AM/PM picker interface makes it intuitive for users, while the implementation leverages existing infrastructure for reliability and maintainability.

**Key Points:**
- ✅ AM/PM picker like Allowed Trading Times
- ✅ Per-account configuration
- ✅ Automatic daily locking
- ✅ Independent from settings lock
- ✅ Backward compatible
- ✅ Well-integrated with existing code
