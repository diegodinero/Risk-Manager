# Automated Settings Lock Time - UI Layout

## Lock Settings Tab Layout

```
┌─────────────────────────────────────────────────────────────────────┐
│ 🔒 Lock Settings                                                     │
├─────────────────────────────────────────────────────────────────────┤
│ Prevent changes to settings until 5:00 PM ET.                       │
├─────────────────────────────────────────────────────────────────────┤
│ Account: ACCOUNT123                                                  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  Status: Settings Unlocked                    [Green text]           │
│                                                                       │
│  [ LOCK SETTINGS FOR REST OF DAY (Until 5 PM ET) ]  [Amber button]  │
│                                                                       │
│  [ UNLOCK SETTINGS ]                              [Green button]     │
│                                                                       │
│  ────────────────────────────────────────────────────────           │
│                                                                       │
│  Automated Daily Lock                        [Bold, larger text]     │
│                                                                       │
│  Automatically lock settings at a specific time each day.            │
│                                                                       │
│  ☐ Enable Automated Lock                                            │
│                                                                       │
│  Lock Time (ET):  [09] : [30]  (HH:MM in Eastern Time, e.g., 09:30  │
│                                  for market open)                    │
│                                                                       │
│  [ SAVE AUTO-LOCK SETTINGS ]                      [Blue button]      │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

## Component Details

### Manual Lock Section (Top)
Located at the top of the tab, this section contains the existing manual lock functionality:

**Status Display:**
- Label showing current lock state
- Green when unlocked: "Settings Unlocked"
- Red when locked: "Settings Locked (2h 30m)" with remaining time

**Lock Button:**
- Amber/orange colored button
- Text: "LOCK SETTINGS FOR REST OF DAY (Until 5 PM ET)"
- Triggers manual lock with duration until 5 PM ET

**Unlock Button:**
- Green colored button
- Text: "UNLOCK SETTINGS"
- Removes the lock early if user needs to make changes

### Separator
A subtle horizontal line separates the manual and automated sections for visual clarity.

### Automated Lock Section (Bottom - NEW)

**Section Title:**
- Large, bold text: "Automated Daily Lock"
- Indicates this is a separate feature from manual locking

**Description:**
- Smaller, gray text explaining the feature
- "Automatically lock settings at a specific time each day."

**Enable Checkbox:**
- Standard Windows checkbox
- Label: "Enable Automated Lock"
- Controls whether the automated lock feature is active

**Time Input:**
- Label: "Lock Time (ET):"
- Two text boxes for hour and minute entry:
  - Hour box: 2 digits (00-23), default "09"
  - Minute box: 2 digits (00-59), default "30"
  - Colon separator between boxes
- Help text in italics and gray: "(HH:MM in Eastern Time, e.g., 09:30 for market open)"

**Save Button:**
- Blue colored button
- Text: "SAVE AUTO-LOCK SETTINGS"
- Validates input and saves configuration

## Color Scheme

Following the existing dark theme:

- **Background**: Dark gray/black (#1E1E1E or similar)
- **Card Background**: Slightly lighter gray (#2D2D2D)
- **Text**: White (#FFFFFF)
- **Gray Text**: Light gray (#888888)
- **Buttons**:
  - Manual Lock: Amber/Orange (#FFA500 or similar)
  - Unlock: Green (#00AA00 or similar)
  - Auto-Lock Save: Blue (#0078D7 or similar)
- **Status Colors**:
  - Unlocked: Green
  - Locked: Red

## Interaction Flow

### Initial State
1. Account display shows selected account or "Not Selected"
2. Status shows current lock state
3. Checkbox reflects saved auto-lock enabled state
4. Time inputs show saved values or defaults (09:30)

### Enabling Auto-Lock
1. User checks "Enable Automated Lock"
2. User enters or adjusts time values
3. User clicks "SAVE AUTO-LOCK SETTINGS"
4. Validation occurs (0-23 for hour, 0-59 for minute)
5. Success message: "Automated lock enabled. Settings will lock daily at HH:MM ET."

### Disabling Auto-Lock
1. User unchecks "Enable Automated Lock"
2. User clicks "SAVE AUTO-LOCK SETTINGS"
3. Success message: "Automated lock disabled."

### Account Switching
1. User selects different account from dropdown (elsewhere in UI)
2. Account display updates to show new account
3. Auto-lock controls automatically load that account's configuration
4. Time inputs and checkbox update to show saved values

## Space Requirements

The automated lock section adds approximately 200 pixels of vertical space to the Lock Settings tab:

- Section title: 30px
- Description: 25px
- Checkbox: 25px
- Time input row: 25px
- Help text: 25px
- Save button: 40px
- Spacing/padding: ~30px
- **Total**: ~200px

The tab uses a scrollable panel, so this addition doesn't affect other tabs or require window resizing.

## Validation Messages

**Success Messages:**
- "Automated lock enabled. Settings will lock daily at HH:MM ET."
- "Automated lock disabled."

**Error Messages:**
- "Please select an account first." (No account selected)
- "Settings service is not initialized." (Service error)
- "Please enter a valid hour (00-23)." (Invalid hour)
- "Please enter a valid minute (00-59)." (Invalid minute)

All messages appear as standard Windows MessageBox dialogs with appropriate icons.

## Accessibility

- All controls have descriptive labels
- Tab order follows logical flow (checkbox → hour → minute → button)
- Help text provides format guidance
- Error messages are clear and actionable
- High contrast maintained in dark theme

## Responsive Behavior

- Controls maintain fixed positioning within the content area
- Content area is scrollable if needed
- Button widths are fixed for consistent appearance
- Text boxes are sized to accommodate 2 digits comfortably

## Technical Notes

- Text boxes restrict input to 2 characters (MaxLength = 2)
- Hour and minute boxes are centered text alignment
- Tags are used for programmatic access:
  - Checkbox: "AutoLockEnabled"
  - Hour box: "AutoLockHour"
  - Minute box: "AutoLockMinute"
- Controls are created in CreateLockSettingsDarkPanel() method
- Updates occur in UpdateAutoLockControlsRecursive() method
