# Lock Duration Feature - Visual Changes

## Manual Lock Tab Changes

### Before
The Manual Lock Tab only had two buttons:
- **LOCK TRADING** button (Amber/Orange)
- **UNLOCK TRADING** button (Green)

The lock was always indefinite and required manual unlock.

### After
The Manual Lock Tab now includes:
- **Lock Duration** dropdown selector with 7 options:
  - 5 Minutes
  - 15 Minutes
  - 1 Hour
  - 2 Hours
  - 4 Hours
  - All Day (Until 5PM ET)
  - All Week (Until 5PM ET Friday)
- **LOCK TRADING** button (shows confirmation dialog before locking)
- **UNLOCK TRADING** button (manual unlock)

**Layout:**
```
Lock Duration: [Dropdown with 7 options]

[LOCK TRADING]  [UNLOCK TRADING]
```

**Important Changes:**
- **Indefinite option removed** - All locks must have a specific duration
- **Confirmation dialog** - "Are you sure?" confirmation before locking
- **Eastern Time** - All time calculations use Eastern Time (ET)
- **All Week changed** - Now locks until 5 PM ET Friday (not Sunday)
- **5 PM ET Auto-Unlock** - Short duration locks (5 min, 15 min, 1 hour, 4 hours) automatically unlock at 5 PM ET if their normal expiration would be after 5 PM ET

---

## Accounts Summary Tab Changes

### Before
The **Lock Status** column showed:
- `Unlocked` - Account is not locked
- `Locked` - Account is locked

### After
The **Lock Status** column now shows:
- `Unlocked` - Account is not locked
- `Locked (2h 30m)` - Account locked with 2 hours 30 minutes remaining
- `Locked (45m)` - Account locked with 45 minutes remaining
- `Locked (<1m)` - Account locked with less than 1 minute remaining
- `Locked (1d 5h 30m)` - Account locked with 1 day, 5 hours, 30 minutes remaining

The remaining time updates automatically as time passes. All times are based on Eastern Time (ET).

---

## Stats Tab Changes

### Before
The **Trading Lock Status** row showed:
- `Unlocked` - Account is not locked
- `Locked` - Account is locked

### After
The **Trading Lock Status** row now shows the same enhanced format as the Accounts Summary Tab:
- `Unlocked`
- `Locked (Xh Ym)` - with remaining time
- `Locked (Xm)` - with remaining minutes
- `Locked (<1m)` - less than 1 minute
- `Locked (Xd Yh Zm)` - with days, hours, and minutes

All times are based on Eastern Time (ET).

---

## Behavioral Changes

### Confirmation Dialog
**New Feature:**
- When clicking "LOCK TRADING", a confirmation dialog appears
- Dialog shows: "Are you sure you want to lock account '[account]' for [duration]?"
- User must click "Yes" to proceed
- Clicking "No" cancels the lock operation

### Lock Behavior
**Before:**
- Locks were always indefinite
- Required manual unlock via UNLOCK TRADING button
- No indication of lock duration

**After:**
- **Cannot choose indefinite** - Indefinite option removed
- Must choose specific duration from dropdown (default: 5 Minutes)
- **Confirmation required** - Must confirm "Are you sure?" before locking
- Automatically unlocks when duration expires
- Shows remaining time in status displays
- Background timer checks for expired locks every 30 seconds
- **Eastern Time** - All time calculations use ET

### All Week Option
**Before:**
- "All Week" locked until Sunday 11:59 PM

**After:**
- "All Week (Until 5PM ET Friday)" locks until 5 PM Eastern Time on Friday
- More aligned with typical trading week end

### Lock Expiration
**New Feature:**
- When a lock duration expires:
  1. Account automatically unlocks in Core API (buy/sell buttons re-enabled)
  2. Trading status changes to "Allowed"
  3. Lock status badge changes from "Trading Locked" (red) to "Trading Unlocked" (green)
  4. Manual Lock button becomes un-greyed (enabled) in the Manual Lock Tab
  5. UI refreshes to show "Unlocked" status in all tabs
  6. Debug log shows: "Auto-unlocked account: [account_id]"

### Lock Enforcement
**New Feature:**
- Background timer runs **every second** (not every 30 seconds) to:
  1. Check for expired locks and auto-unlock
  2. Enforce active locks to prevent manual override
  3. Update all UI elements (buttons, badges, status displays)
- This prevents users from bypassing the lock system
- Ensures lock status is always current and accurate

### 5 PM ET Auto-Unlock for Short Durations
**New Feature:**
- Short duration locks (5 min, 15 min, 1 hour, 4 hours) automatically unlock at 5 PM ET if their normal expiration would be after 5 PM ET
- This ensures accounts are unlocked at market close time
- **Examples:**
  - Lock at 4:50 PM ET for 15 minutes → Unlocks at 5:00 PM ET (not 5:05 PM ET)
  - Lock at 4:30 PM ET for 1 hour → Unlocks at 5:00 PM ET (not 5:30 PM ET)
  - Lock at 2:00 PM ET for 1 hour → Unlocks at 3:00 PM ET (normal expiration, before 5 PM)
- **2 Hours option does NOT have this behavior** - it will lock for exactly 2 hours regardless of 5 PM ET

---

## User Experience Improvements

1. **Safety**: Confirmation dialog prevents accidental locks
2. **Consistency**: All locks use Eastern Time for predictable behavior
3. **Flexibility**: Choose from 7 different lock durations (removed indefinite)
4. **Automation**: No need to manually unlock after a set period
5. **Transparency**: Always know how much time remains on a lock
6. **Convenience**: "All Day" and "All Week" options for planned breaks
7. **Weekly Alignment**: "All Week" now ends at trading week close (5 PM ET Friday)
8. **Market Close Protection**: Short duration locks auto-unlock at 5 PM ET to align with market close
9. **Real-time Enforcement**: Lock status enforced every second to prevent override
10. **Complete UI Updates**: All UI elements update automatically when lock expires

---

## Example Use Cases

### Example 1: Short Break
- Select "15 Minutes" from dropdown
- Click "LOCK TRADING"
- Confirm "Yes" in dialog
- Status shows "Locked (15m)"
- Take a 15-minute break
- Account automatically unlocks when time expires

### Example 1a: Short Break Before Market Close
- At 4:50 PM ET, select "15 Minutes" from dropdown
- Click "LOCK TRADING"
- Confirm "Yes" in dialog
- Status shows "Locked (10m)" (will unlock at 5 PM ET, not 5:05 PM ET)
- Account automatically unlocks at 5 PM ET

### Example 2: Lunch Break
- Select "1 Hour" from dropdown
- Click "LOCK TRADING"
- Confirm "Yes" in dialog
- Status shows "Locked (1h 0m)"
- Go to lunch
- Returns to find account unlocked and ready to trade

### Example 3: End of Trading Day
- At 2 PM ET, select "All Day (Until 5PM ET)"
- Click "LOCK TRADING"
- Confirm "Yes" in dialog
- Status shows "Locked (3h 0m)"
- Account stays locked until 5 PM ET
- At 5 PM ET, automatically unlocks for next day

### Example 4: Trading Week Lock
- Wednesday morning, select "All Week (Until 5PM ET Friday)"
- Click "LOCK TRADING"
- Confirm "Yes" in dialog
- Status shows "Locked (2d 7h 30m)" (example - actual time varies)
- Account stays locked until Friday 5 PM ET
- Friday at 5 PM ET, automatically unlocks

---

## Technical Notes

- Lock status updates automatically every second via background enforcement timer
- Background timer runs **every second** to check expired locks and enforce active locks
- Expiration checking also happens on-demand when status is queried
- **All date/time calculations use Eastern Time (ET) for consistency**
- Lock duration calculations account for Eastern Time zone regardless of user's local time
- Lock settings are persisted to disk and survive application restart
- Lock enforcement prevents manual override attempts by re-applying lock status every second
- UI elements (buy/sell buttons, badges, Manual Lock button) update automatically when lock expires
