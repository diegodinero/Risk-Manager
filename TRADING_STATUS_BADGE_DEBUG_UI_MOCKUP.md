# Trading Status Badge Debug Feature - UI Mockup

## Visual Layout

### Before Implementation

```
┌─────────────────────────────────────────────────────────────────────┐
│ Risk Manager                                    [Account Dropdown▼] │
│                                                                       │
│            [Settings Unlocked] [Trading Unlocked] [🎨]               │
└─────────────────────────────────────────────────────────────────────┘
```

### After Implementation (with Debug Mode Enabled)

```
┌─────────────────────────────────────────────────────────────────────┐
│ Risk Manager                                    [Account Dropdown▼] │
│                                                                       │
│            [Settings Unlocked] [Trading Unlocked] [🎨]               │
│                                Caller: LoadAccountSettings | Prev:   │
│                                null→False | Current: False |          │
│                                Changed: True | Time: 15:23:45.123    │
└─────────────────────────────────────────────────────────────────────┘
```

## Detailed Badge Area

### Trading Unlocked State (with debug enabled)

```
┌──────────────────────────┐
│  Trading Unlocked        │  ← Green Badge (AccentGreen)
└──────────────────────────┘
Caller: LoadAccountSettings | Prev: null→False | Current: False | Changed: True | Time: 15:23:45.123
↑ Yellow Text (Consolas 7pt)
```

### Trading Locked State (with debug enabled)

```
┌──────────────────────────┐
│  Trading Locked          │  ← Red Badge
└──────────────────────────┘
Caller: LockTradingButton_Click | Prev: False→True | Current: True | Changed: True | Time: 15:24:12.456
↑ Yellow Text (Consolas 7pt)
```

### No State Change (with debug enabled)

```
┌──────────────────────────┐
│  Trading Locked          │  ← Red Badge
└──────────────────────────┘
Caller: CheckExpiredLocks | Prev: True→True | Current: True | Changed: False | Time: 15:24:13.789
↑ Light Gray Text (redundant update, no state change)
```

## UI Element Specifications

### Trading Status Badge Container

```
Panel {
  AutoSize: true
  BackColor: Transparent
  Margin: (5, 0, 5, 0)
  Padding: (0)
  Contents:
    ├─ Trading Status Badge (Label)
    └─ Debug Label (Label)
}
```

### Trading Status Badge (Label)

```
Label {
  Text: "  Trading Unlocked  " or "  Trading Locked  "
  AutoSize: true
  ForeColor: White
  BackColor: AccentGreen (green) or Red
  Font: Segoe UI, 9pt, Regular
  Padding: (8, 4, 8, 4)
  Margin: (5, 0, 5, 0)
  Location: (0, 0)
}
```

### Debug Label (Label)

```
Label {
  Name: lblTradingStatusBadgeDebug
  Text: "Caller: FunctionName | Prev: State→State | Current: State | Changed: Bool | Time: HH:MM:SS.mmm"
  AutoSize: false
  Width: 600px
  Height: 16px
  ForeColor: Yellow (if changed) or LightGray (if not changed)
  BackColor: Transparent
  Font: Consolas, 7pt, Regular
  Location: (0, badge.Height + 2)
  TextAlign: TopLeft
  Visible: _badgeDebugMode
}
```

## Complete Top Panel Layout

```
┌───────────────────────────────────────────────────────────────────────────────────┐
│                                                                                     │
│  Risk Manager                                       [Account: Account1234567890▼]  │
│                                                                                     │
│                           ┌─────────────────────┐ ┌──────────────────────────┐     │
│                           │ Settings Unlocked   │ │ Trading Unlocked         │ [🎨]│
│                           └─────────────────────┘ └──────────────────────────┘     │
│                                                    ┌──────────────────────────┐     │
│                                                    │ Debug: Caller: ...       │     │
│                                                    └──────────────────────────┘     │
│                                                                                     │
└───────────────────────────────────────────────────────────────────────────────────┘
```

## State Transition Examples

### Example 1: Account Load

```
INITIAL STATE:
┌─────────────────────┐
│ Trading Unlocked    │  ← Green
└─────────────────────┘
Debug: Waiting for updates...
↑ Initial gray text

AFTER LOAD:
┌─────────────────────┐
│ Trading Unlocked    │  ← Green
└─────────────────────┘
Caller: LoadAccountSettings | Prev: null→False | Current: False | Changed: True | Time: 15:23:45.123
↑ Yellow text (state changed from null)
```

### Example 2: Lock Button Click

```
BEFORE CLICK:
┌─────────────────────┐
│ Trading Unlocked    │  ← Green
└─────────────────────┘
Caller: LoadAccountSettings | Prev: null→False | Current: False | Changed: True | Time: 15:23:45.123
↑ Yellow text

AFTER CLICK:
┌─────────────────────┐
│ Trading Locked      │  ← Red
└─────────────────────┘
Caller: LockTradingButton_Click | Prev: False→True | Current: True | Changed: True | Time: 15:24:12.456
↑ Yellow text (state changed)
```

### Example 3: Timer Check (No Change)

```
BEFORE TIMER:
┌─────────────────────┐
│ Trading Locked      │  ← Red
└─────────────────────┘
Caller: LockTradingButton_Click | Prev: False→True | Current: True | Changed: True | Time: 15:24:12.456
↑ Yellow text

AFTER TIMER CHECK:
┌─────────────────────┐
│ Trading Locked      │  ← Red (no change)
└─────────────────────┘
Caller: CheckExpiredLocks | Prev: True→True | Current: True | Changed: False | Time: 15:24:13.789
↑ Light gray text (no state change)
```

### Example 4: Account Switch

```
ACCOUNT A (LOCKED):
┌─────────────────────┐
│ Trading Locked      │  ← Red
└─────────────────────┘
Caller: CheckExpiredLocks | Prev: True→True | Current: True | Changed: False | Time: 15:24:13.789

SWITCH TO ACCOUNT B (UNLOCKED):
┌─────────────────────┐
│ Trading Unlocked    │  ← Green
└─────────────────────┘
Caller: LoadAccountSettings | Prev: null→False | Current: False | Changed: True | Time: 15:25:00.123
↑ Yellow text (cache reset, previous state is null)

SWITCH BACK TO ACCOUNT A:
┌─────────────────────┐
│ Trading Locked      │  ← Red
└─────────────────────┘
Caller: LoadAccountSettings | Prev: null→True | Current: True | Changed: True | Time: 15:25:10.456
↑ Yellow text (cache reset, state restored)
```

## Color Scheme

### Badge Colors
- **Green (AccentGreen):** RGB(39, 174, 96) - Trading Unlocked
- **Red:** RGB(255, 0, 0) - Trading Locked
- **White:** RGB(255, 255, 255) - Badge text color

### Debug Label Colors
- **Yellow:** RGB(255, 255, 0) - State changed (important)
- **Light Gray:** RGB(211, 211, 211) - No state change (less important)
- **Transparent:** Background color

## Responsive Behavior

### Debug Label Width Calculation

```
Width: 600px (fixed)

Breakdown:
- "Caller: " = ~50px
- Function name (max) = ~120px
- " | Prev: " = ~40px
- "False→True" = ~60px
- " | Current: " = ~50px
- "True" = ~25px
- " | Changed: " = ~50px
- "True" = ~25px
- " | Time: " = ~40px
- "15:23:45.123" = ~70px
Total: ~530px content
Actual: 600px (truncates if needed, scrollable)
```

### Container Height Calculation

```
Container Height = Badge Height + Debug Label Height + Spacing
                 = ~28px + 16px + 2px
                 = ~46px total
```

## Accessibility

- **Font Choice:** Consolas (monospace) ensures columns align
- **Font Size:** 7pt - small but readable on typical monitors
- **Color Contrast:** Yellow/LightGray on dark background (high contrast)
- **Width:** Fixed at 600px to prevent layout shifts
- **Positioning:** Always below badge, consistent placement

## Theme Compatibility

The debug label will adapt to theme changes:
- Background remains transparent
- Text color (Yellow/LightGray) stays constant for visibility
- Font and size remain consistent across themes

## Performance Considerations

- Label updates only on badge state changes
- No animation or frequent redraws
- Minimal memory footprint (single label control)
- Efficient string formatting with interpolation
- No background timers or polling

## Summary

The debug label provides clear, real-time feedback about badge state transitions with:
- **Position:** Directly below the trading status badge
- **Format:** Structured, easy-to-read information
- **Colors:** Visual indicators for state changes
- **Size:** Compact yet readable
- **Visibility:** Toggle-able via configuration

This UI addition makes debugging badge behavior significantly easier without cluttering the interface or requiring external debug tools.
