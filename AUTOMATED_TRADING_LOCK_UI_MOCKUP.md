# Automated Trading Lock - Visual UI Mockup

## Manual Lock Tab Layout

```
┌─────────────────────────────────────────────────────────────────────┐
│ 🔒 Manual Lock                                                       │
├─────────────────────────────────────────────────────────────────────┤
│ Manually lock or unlock trading with optional duration.             │
├─────────────────────────────────────────────────────────────────────┤
│ Account: ACCOUNT123                                                  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  Status: Unlocked                                  [Green text]      │
│                                                                       │
│  Lock Duration:  [5 Minutes                     ▼]                   │
│                                                                       │
│  [ LOCK TRADING ]                              [Amber button]        │
│  [ UNLOCK TRADING ]                            [Green button]        │
│                                                                       │
│  ──────────────────────────────────────────────────────              │
│                                                                       │
│  Automated Daily Trading Lock               [Bold, larger text]      │
│                                                                       │
│  Automatically lock trading at a specific time each day.             │
│                                                                       │
│  ☐ Enable Automated Trading Lock                                    │
│                                                                       │
│  Lock Time (ET): [09 ▼] : [30 ▼] [AM ▼]                            │
│                  (e.g., 09:30 AM for market open)                    │
│                                                                       │
│  [ SAVE AUTO-LOCK SETTINGS ]                   [Blue button]         │
│                                                                       │
└─────────────────────────────────────────────────────────────────────┘
```

## Component Details

### Manual Lock Section (Top - Existing)

**Status Display:**
- Shows "Unlocked" in green or "Locked (X hours remaining)" in red

**Lock Duration Dropdown:**
- Options: 5 Minutes, 15 Minutes, 1 Hour, 2 Hours, 4 Hours
- All Day (Until 5:00 PM ET)
- All Week (Until 5:00 PM ET Friday)

**Lock/Unlock Buttons:**
- Lock Trading: Amber/orange colored
- Unlock Trading: Green colored

### Automated Trading Lock Section (Bottom - NEW)

**Separator Line:**
- Subtle horizontal line for visual separation
- Color: Dark gray (#3C3C3C)

**Section Title:**
- Text: "Automated Daily Trading Lock"
- Font: Segoe UI, 11pt, Bold
- Color: White

**Description:**
- Text: "Automatically lock trading at a specific time each day."
- Font: Segoe UI, 9pt, Regular
- Color: Gray

**Enable Checkbox:**
- Text: "Enable Automated Trading Lock"
- Standard checkbox
- Unchecked by default

**Time Picker:**

Hour Dropdown:
```
┌──────┐
│ 09 ▼ │
├──────┤
│ 01   │
│ 02   │
│ 03   │
│ ...  │
│ 09   │ ← Selected
│ 10   │
│ 11   │
│ 12   │
└──────┘
```

Minute Dropdown:
```
┌──────┐
│ 30 ▼ │
├──────┤
│ 00   │
│ 15   │
│ 30   │ ← Selected
│ 45   │
└──────┘
```

AM/PM Dropdown:
```
┌──────┐
│ AM ▼ │
├──────┤
│ AM   │ ← Selected
│ PM   │
└──────┘
```

**Help Text:**
- Text: "(e.g., 09:30 AM for market open)"
- Font: Segoe UI, 8pt, Italic
- Color: Light gray

**Save Button:**
- Text: "SAVE AUTO-LOCK SETTINGS"
- Color: Blue (#0078D7 or AccentBlue)
- Width: 250px
- Height: 40px
- Font: Segoe UI, 10pt, Bold

## Interaction Flow

### Step 1: Initial State

User opens Manual Lock tab and sees:
```
☐ Enable Automated Trading Lock

Lock Time (ET): [09 ▼] : [30 ▼] [AM ▼]
                (e.g., 09:30 AM for market open)

[ SAVE AUTO-LOCK SETTINGS ]
```

### Step 2: User Configuration

User checks the enable box and selects time:
```
☑ Enable Automated Trading Lock

Lock Time (ET): [09 ▼] : [30 ▼] [AM ▼]
                (e.g., 09:30 AM for market open)

[ SAVE AUTO-LOCK SETTINGS ]  ← User clicks here
```

### Step 3: Confirmation Message

```
┌─────────────────────────────────────────────────┐
│  Auto-Lock Trading Settings Saved               │
├─────────────────────────────────────────────────┤
│                                                  │
│  Automated trading lock enabled. Trading will   │
│  lock daily at 9:30 AM ET.                      │
│                                                  │
│                          [ OK ]                  │
└─────────────────────────────────────────────────┘
```

### Step 4: Daily at 9:30 AM

Status updates automatically:
```
Status: Trading Locked (7h 30m remaining)  [Red text]
```

## Color Scheme

**Dark Theme (Default):**
- Background: Dark gray (#1E1E1E)
- Card Background: Slightly lighter (#2D2D2D)
- Separator: Medium gray (#3C3C3C)
- Text: White (#FFFFFF)
- Gray Text: Light gray (#888888)
- Enable Checkbox: White text
- Dropdowns: Dark background with white text
- Save Button: Blue (#0078D7)

**Control Colors:**
- Hour/Minute/AM-PM Dropdowns: DarkerBackground with white text
- Help Text: TextGray (light gray)
- Section Title: TextWhite (white)

## Spacing and Layout

### Vertical Spacing:
- Status to Duration: 50px
- Duration to Buttons: 50px
- Buttons to Separator: 60px
- Separator to Section Title: 20px
- Section Title to Description: 5px
- Description to Checkbox: 10px
- Checkbox to Time Label: 10px
- Time controls: Same row (285px from top)
- Time to Save Button: 40px

### Horizontal Spacing:
- Hour dropdown: Left = 130px, Width = 60px
- Colon label: Left = 195px, Width = 10px
- Minute dropdown: Left = 210px, Width = 60px
- AM/PM dropdown: Left = 275px, Width = 60px
- Help text: Left = 340px

## Comparison with Allowed Trading Times

### Similarities:
- Same dropdown style (hour, minute, AM/PM)
- Same 12-hour format
- Same minute options (00, 15, 30, 45)
- Same hour range (1-12)
- Same AM/PM options

### Differences:

**Allowed Trading Times:**
- Multiple rows (day + start time + end time)
- Add/Delete buttons
- Day of week selector
- Two time pickers per row (start and end)

**Automated Trading Lock:**
- Single row (just one time)
- Enable checkbox
- No day selector (applies every day)
- Save button for configuration

## Before and After Examples

### Example 1: Morning Lock

**Before Save:**
```
Lock Time (ET): [09 ▼] : [30 ▼] [AM ▼]
```

**After Save:**
```
Message: "Automated trading lock enabled. Trading will lock daily at 9:30 AM ET."
```

**Display Time:**
- Input: 09:30 AM
- Stored: TimeSpan(9, 30, 0)
- Display: 9:30 AM

### Example 2: Afternoon Lock

**Before Save:**
```
Lock Time (ET): [02 ▼] : [30 ▼] [PM ▼]
```

**After Save:**
```
Message: "Automated trading lock enabled. Trading will lock daily at 2:30 PM ET."
```

**Display Time:**
- Input: 02:30 PM
- Stored: TimeSpan(14, 30, 0)
- Display: 2:30 PM

### Example 3: Disabled

**Before Save:**
```
☐ Enable Automated Trading Lock
Lock Time (ET): [09 ▼] : [30 ▼] [AM ▼]
```

**After Save:**
```
Message: "Automated trading lock disabled."
```

**Display Time:**
- Time saved but not active

## Account Switching Behavior

### Account A Configuration:
```
☑ Enable Automated Trading Lock
Lock Time (ET): [09 ▼] : [30 ▼] [AM ▼]
```

### Switch to Account B (unconfigured):
```
☐ Enable Automated Trading Lock
Lock Time (ET): [09 ▼] : [30 ▼] [AM ▼]  (defaults)
```

### Switch to Account C (different config):
```
☑ Enable Automated Trading Lock
Lock Time (ET): [02 ▼] : [30 ▼] [PM ▼]
```

### Switch back to Account A:
```
☑ Enable Automated Trading Lock
Lock Time (ET): [09 ▼] : [30 ▼] [AM ▼]  (restored)
```

## Validation Messages

### Success:
```
┌─────────────────────────────────────────────────┐
│  Auto-Lock Trading Settings Saved               │
├─────────────────────────────────────────────────┤
│  Automated trading lock enabled. Trading will   │
│  lock daily at 9:30 AM ET.                      │
│                          [ OK ]                  │
└─────────────────────────────────────────────────┘
```

### No Account Selected:
```
┌─────────────────────────────────────────────────┐
│  No Account Selected                            │
├─────────────────────────────────────────────────┤
│  Please select an account first.                │
│                          [ OK ]                  │
└─────────────────────────────────────────────────┘
```

### Service Error:
```
┌─────────────────────────────────────────────────┐
│  Service Error                                  │
├─────────────────────────────────────────────────┤
│  Settings service is not initialized.           │
│                          [ OK ]                  │
└─────────────────────────────────────────────────┘
```

### Invalid Time:
```
┌─────────────────────────────────────────────────┐
│  Invalid Time                                   │
├─────────────────────────────────────────────────┤
│  Please select hour, minute, and AM/PM.         │
│                          [ OK ]                  │
└─────────────────────────────────────────────────┘
```

## Responsive Behavior

- Section expands vertically to accommodate all controls
- Scrollable if needed (contentArea has AutoScroll = true)
- Fixed control positions ensure consistent layout
- No horizontal scrolling required

## Accessibility

- All dropdowns have clear labels
- Keyboard navigation supported (Tab key)
- Help text provides format guidance
- Error messages are descriptive
- High contrast maintained in dark theme

## Summary

The automated trading lock UI provides an intuitive AM/PM picker interface that matches the Allowed Trading Times tab style while maintaining consistency with the automated settings lock layout. Users can easily configure when trading should automatically lock each day using familiar 12-hour format.

**Key UI Features:**
- ✅ Clear section separation with line
- ✅ Descriptive title and description
- ✅ Enable/disable checkbox
- ✅ AM/PM picker (hour, minute, AM/PM)
- ✅ Helpful example text
- ✅ Prominent save button
- ✅ Clear confirmation messages
- ✅ Per-account persistence
