# Settings Lock Feature

## Overview

The Settings Lock feature provides time-based protection for application settings, preventing any changes until 5:00 PM Eastern Time (ET). This feature is designed to ensure trading discipline by locking all configuration changes during active trading hours.

## Key Features

### 1. Manual Time-Based Settings Lock (Until 5 PM ET)

- **Automatic Duration Calculation**: When activated, the lock automatically calculates the time remaining until 5:00 PM ET
- **Timezone Handling**: Uses TimeZoneInfo for accurate Eastern Time conversion, handling Daylight Saving Time (DST) automatically
- **Auto-Unlock**: Lock automatically expires at 5:00 PM ET without manual intervention

### 2. Automated Daily Settings Lock (NEW)

- **Scheduled Lock Time**: Configure a specific time in Eastern Time when settings should automatically lock each day
- **Common Use Case**: Set to 9:30 AM ET for market open to prevent changes during trading hours
- **Per-Account Configuration**: Each account can have its own automated lock time
- **Enable/Disable Toggle**: Easy on/off control without losing the configured time
- **Persistent Settings**: Configuration saved with account settings and survives application restarts
- **Same Unlock Time**: Automated locks also unlock at 5:00 PM ET like manual locks

### 3. Complete Settings Protection

When settings are locked, the following controls are disabled:
- Daily Loss Limit settings
- Daily Profit Target settings
- Position Loss Limit settings
- Position Profit Target settings
- Weekly Loss Limit settings
- Weekly Profit Target settings
- Symbol Blacklist settings
- Contract Limits settings
- Trading Time Restrictions
- Feature Toggle settings

### 3. Trading Lock Remains Available

Even when settings are locked, users can still:
- Lock trading for the rest of the day (until 5 PM ET)
- Unlock trading if previously locked
- View all current settings (read-only)

### 4. Visual Indicators

- **Top Badge**: The "Settings Locked" badge in the title bar shows red when locked
- **Status Display**: Shows remaining lock time (e.g., "Settings Locked (3h 45m)")
- **Control State**: All disabled controls appear grayed out

## User Interface

### Lock Settings Panel

Located in: **⚙️ Settings Lock** tab

**Components:**

#### Manual Lock Section
1. **Status Display**: Shows current lock status with remaining time
2. **Lock Button**: "LOCK SETTINGS FOR REST OF DAY (Until 5 PM ET)" - Amber colored
3. **Unlock Button**: "UNLOCK SETTINGS" - Green colored

**Usage:**
1. Select an account from the dropdown
2. Click "LOCK SETTINGS FOR REST OF DAY" to activate the lock
3. Confirmation shows the exact duration (e.g., "Settings locked until 5:00 PM ET. Duration: 3h 45m")
4. To unlock early, click "UNLOCK SETTINGS"

#### Automated Daily Lock Section
5. **Enable Checkbox**: "Enable Automated Lock" - Toggle automated locking on/off
6. **Time Input**: Hour and minute fields for Eastern Time (e.g., 09:30)
7. **Save Button**: "SAVE AUTO-LOCK SETTINGS" - Blue colored

**Usage:**
1. Select an account from the dropdown
2. Check "Enable Automated Lock" to activate the feature
3. Set the desired lock time in Eastern Time (e.g., 09:30 for market open)
4. Click "SAVE AUTO-LOCK SETTINGS"
5. Settings will automatically lock every day at the configured time
6. Auto-lock uses the same 5 PM ET unlock time as manual locks

## Technical Implementation

### Backend (RiskManagerSettingsService.cs)

#### New Methods

**SetSettingsLock** - Enhanced with duration support
```csharp
public void SetSettingsLock(string accountNumber, bool isLocked, string? reason = null, TimeSpan? duration = null)
```

**AreSettingsLocked** - Checks lock status with auto-expiration
```csharp
public bool AreSettingsLocked(string accountNumber)
```

**GetSettingsLockStatusString** - Returns formatted status with time
```csharp
public string GetSettingsLockStatusString(string accountNumber)
```

**CalculateDurationUntil5PMET** - Static helper for ET timezone calculations
```csharp
public static TimeSpan CalculateDurationUntil5PMET()
```

#### Data Structure

The `LockInfo` class (already existed, now used for SettingsLock too) includes:
- `IsLocked`: Boolean lock state
- `LockTime`: When lock was activated (UTC)
- `LockDuration`: Duration of the lock (TimeSpan)
- `LockExpirationTime`: When lock will expire (UTC)
- `LockReason`: Reason for locking
- `LockDayOfWeek`: Day when locked

### Frontend (RiskManagerControl.cs)

#### UI Updates

**CreateLockSettingsDarkPanel()** - Redesigned panel with:
- Status label showing lock state and remaining time
- Lock button for rest-of-day locking
- Unlock button for manual override

**UpdateSettingsLockStatus()** - Enhanced to show remaining time
```csharp
private void UpdateSettingsLockStatus(Label lblSettingsStatus)
```

**CheckExpiredLocks()** - Enhanced timer callback that:
- Checks both trading locks and settings locks
- Auto-unlocks expired settings locks
- Updates UI when locks expire

**UpdateSettingsControlsEnabledState()** - New method that:
- Checks if settings are locked
- Enables/disables all settings input controls accordingly

**SetAllSettingsControlsEnabled()** - Helper method that:
- Takes a boolean parameter (enabled/disabled)
- Applies state to all settings input controls

#### Save Button Protection

The save button now checks for settings lock before allowing changes:
```csharp
if (service.AreSettingsLocked(accountNumber))
{
    MessageBox.Show("Settings are currently locked...");
    return;
}
```

## Automated Daily Lock Implementation

### Overview

The automated daily lock feature allows users to configure settings to automatically lock at a specific time each day, eliminating the need to manually lock settings before trading hours.

### Data Model

**AccountSettings Properties:**
```csharp
public bool AutoLockSettingsEnabled { get; set; } = false;
public TimeSpan? AutoLockSettingsTime { get; set; }
```

These properties are persisted in the account's JSON settings file.

### Trigger Logic

**ShouldTriggerAutoLock Method:**
- Runs every second as part of the `CheckExpiredLocks()` timer
- Converts current UTC time to Eastern Time
- Checks if current time matches the configured auto-lock time (within 1-minute window)
- Returns true only once when the time window is reached
- Returns false if settings are already locked (prevents redundant locking)

**CheckExpiredLocks Enhancement:**
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
    }
}
```

### User Interface Controls

**Automated Lock Section Components:**
1. **Section Title**: "Automated Daily Lock"
2. **Description**: Explains the feature purpose
3. **Enable Checkbox**: Toggle for enabling/disabling automated lock
4. **Time Input Fields**:
   - Hour text box (00-23)
   - Minute text box (00-59)
   - Format hint label showing HH:MM in Eastern Time
5. **Save Button**: Persists configuration to account settings

**Dynamic Updates:**
- When account selection changes, `UpdateAutoLockControlsRecursive()` is called
- Finds auto-lock controls by their tags ("AutoLockEnabled", "AutoLockHour", "AutoLockMinute")
- Loads and displays the saved configuration for the selected account

### Configuration Workflow

1. User selects an account
2. User checks "Enable Automated Lock"
3. User enters desired time (e.g., 09:30 for market open)
4. User clicks "SAVE AUTO-LOCK SETTINGS"
5. Settings are validated and saved to account JSON
6. Every second thereafter, timer checks if it's time to trigger the lock
7. When time matches, settings automatically lock until 5 PM ET
8. Next day, the process repeats

### Example Scenarios

**Scenario 1: Market Open Lock**
- Configure: Enabled, 09:30 ET
- Behavior: Every trading day at 9:30 AM ET, settings automatically lock
- Unlock: Automatic at 5:00 PM ET

**Scenario 2: Pre-Market Lock**
- Configure: Enabled, 08:00 ET
- Behavior: Settings lock before market open for extra protection
- Unlock: Automatic at 5:00 PM ET

**Scenario 3: Disable After Hours Trading Changes**
- Configure: Enabled, 16:00 ET (4:00 PM)
- Behavior: Prevents end-of-day settings changes
- Unlock: Automatic at 5:00 PM ET (1 hour later)

### Timezone Handling

### Eastern Time (ET) Conversion

The system uses `TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")` which:
- Automatically handles EST (UTC-5) and EDT (UTC-4)
- Transitions correctly during DST changes
- Fallback mechanism if timezone data unavailable

### Fallback Logic

If Eastern Time zone is not found in the system:
1. Uses approximate DST detection (March-November = EDT, else EST)
2. Applies -4 hours offset for EDT, -5 hours for EST
3. Logs warning for debugging

## Edge Cases Handled

### 1. After 5 PM ET
If lock is activated after 5 PM ET, it locks until 5 PM ET the **next day**

### 2. Lock Expiration Check
- Timer checks every second (1000ms interval)
- Automatic unlock when expiration time reached
- UI updates automatically via recursive label search

### 3. Account Switching
- Lock state is per-account
- Switching accounts updates control states immediately
- Each account has independent lock status

### 4. Settings Load
When loading account settings, the system:
1. Loads all setting values
2. Checks lock status
3. Enables/disables controls accordingly

## Security Considerations

### Cannot Be Bypassed
- Lock check happens both in UI (control disabled) and backend (save validation)
- Attempting to save while locked shows error message
- Manual unlock requires explicit user action

### Audit Trail
All lock operations are logged with:
- Account identifier
- Action (lock/unlock)
- Timestamp (UTC)
- Reason
- Duration

Example log:
```
[ADMIN ACTION] Account: ACCOUNT123, Action: Manual Lock, Duration: 3h 45m, Timestamp: 2025-12-23 18:30:00 UTC
```

## Relationship with Trading Lock

**Settings Lock** and **Trading Lock** are independent:

| Feature | Purpose | Scope |
|---------|---------|-------|
| Settings Lock | Prevents configuration changes | UI settings only |
| Trading Lock | Prevents Buy/Sell actions | Trading platform API |

**Key Difference:**
- Settings Lock: Can't change Daily Loss Limit, but can lock trading
- Trading Lock: Can't place trades, but can unlock settings (if not separately locked)

This allows emergency trading locks even when settings are locked for the day.

## User Experience Flow

### Normal Day Flow (Manual Lock)

1. **Morning (before trading)**: User configures risk settings
2. **9:30 AM**: User manually locks settings for rest of day
3. **During Trading Hours**: User can only lock/unlock trading, cannot modify settings
4. **5:00 PM ET**: Lock automatically expires
5. **After 5 PM**: User can modify settings for next trading day

### Normal Day Flow (Automated Lock)

1. **Previous Day or Setup**: User configures auto-lock for 9:30 AM ET and saves
2. **Next Day - Before 9:30 AM**: User can configure risk settings
3. **9:30 AM ET**: Settings automatically lock (no user action needed)
4. **During Trading Hours**: User can only lock/unlock trading, cannot modify settings
5. **5:00 PM ET**: Lock automatically expires
6. **After 5 PM**: User can modify settings for next trading day
7. **Following Days**: Process repeats daily at 9:30 AM automatically
3. **During Trading Hours**: User can only lock/unlock trading, cannot modify settings
4. **5:00 PM ET**: Lock automatically expires
5. **After 5 PM**: User can modify settings for next trading day

### Emergency Scenario

1. **Settings are locked** (e.g., 2 PM)
2. **Need to stop trading immediately**
3. **Solution**: Use "Lock Trading" button (still available)
4. **Result**: Trading stopped, but settings remain locked for protection

## Future Enhancements

Potential improvements could include:
- Multiple lock schedules per day
- Different unlock times (not just 5 PM)
- Weekend lock schedules
- Role-based lock override permissions
- Lock templates for different account types
- Email notifications when locks expire or trigger
- Holiday schedule support (skip auto-lock on market holidays)

## Testing Recommendations

### Manual Testing Checklist

1. **Basic Lock/Unlock**
   - [ ] Lock settings and verify all controls disabled
   - [ ] Verify save button shows error when locked
   - [ ] Unlock settings and verify controls enabled

2. **Time-Based Lock**
   - [ ] Lock before 5 PM and verify duration shown
   - [ ] Wait for lock to expire (or test with shorter duration)
   - [ ] Verify auto-unlock at expiration time

3. **Automated Daily Lock**
   - [ ] Configure auto-lock time (e.g., current time + 2 minutes)
   - [ ] Enable automated lock and save settings
   - [ ] Wait for configured time and verify automatic locking
   - [ ] Verify lock reason shows "Auto-locked at scheduled time"
   - [ ] Switch accounts and verify each has independent configuration
   - [ ] Disable auto-lock, save, and verify no automatic locking occurs
   - [ ] Close and reopen application, verify auto-lock settings persist

4. **Timezone Handling**
   - [ ] Test on systems in different timezones
   - [ ] Verify ET conversion is correct for both manual and auto-lock
   - [ ] Test during DST transition periods

5. **UI Updates**
   - [ ] Verify badge shows "Settings Locked" in red
   - [ ] Verify status label shows remaining time
   - [ ] Verify status updates every second
   - [ ] Verify auto-lock controls load correctly on account change

6. **Account Switching**
   - [ ] Lock settings on Account A
   - [ ] Switch to Account B (should be unlocked)
   - [ ] Switch back to Account A (should still be locked)

6. **Trading Lock Independence**
   - [ ] Lock settings
   - [ ] Verify trading lock button still works
   - [ ] Lock trading
   - [ ] Verify can unlock trading while settings locked

### Edge Case Testing

1. **After 5 PM Lock**
   - Lock at 5:30 PM, verify locks until next day at 5 PM
   
2. **System Time Change**
   - Change system time while lock active
   - Verify lock still expires at correct time

3. **Application Restart**
   - Lock settings
   - Close and reopen application
   - Verify lock persists correctly

## Troubleshooting

### Issue: Lock doesn't expire at 5 PM

**Check:**
- System timezone settings
- Windows timezone database updated
- Debug logs for timezone conversion errors

**Solution:**
- Verify `TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time")` works
- Check fallback logic is activated if needed

### Issue: Controls still disabled after unlock

**Check:**
- `UpdateSettingsControlsEnabledState()` is being called
- Account number is correct
- Cache hasn't expired

**Solution:**
- Refresh account selection
- Check logs for errors in control state update

### Issue: Can't save even when unlocked

**Check:**
- Settings service initialized
- Lock status query returning correct value
- No race condition between cache and actual settings

**Solution:**
- Clear settings cache: `settingsService.InvalidateCache(accountNumber)`
- Verify settings file on disk

## Conclusion

The Settings Lock feature provides robust protection for trading risk parameters while maintaining essential emergency controls. By implementing time-based locking with automatic expiration and proper timezone handling, it enforces trading discipline without compromising usability.
