# Quick Reference: Card Style Toggle

## What Was Implemented

Added the ability to toggle between two visual styles for disabled Risk Overview cards using a checkbox in General Settings.

## Location

**General Settings Tab** → "Use Greyed Out Style for Disabled Cards" checkbox

## The Two Styles

### 🎨 Style 1: Greyed Out (Checked - Default)
```
User Interface:
┌─────────────────────────────────────┐
│ ☑ Use Greyed Out Style for          │
│   Disabled Cards                     │
└─────────────────────────────────────┘

Disabled Card Appearance:
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ 📈 Position Limits            ✖   ┃  ← Small red X (28pt)
┠───────────────────────────────────┨
┃ Max Loss per Position: $500       ┃  ← 40% opacity
┃ Max Profit per Position: $1000    ┃  ← 40% opacity
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛

✓ Content visible
✓ Less intrusive
✓ Works well with dark themes
```

### 🎨 Style 2: Overlay (Unchecked)
```
User Interface:
┌─────────────────────────────────────┐
│ ☐ Use Greyed Out Style for          │
│   Disabled Cards                     │
└─────────────────────────────────────┘

Disabled Card Appearance:
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ 📈 Position Limits                 ┃
╠═══════════════════════════════════╣
║███████████████████████████████████║  ← Dark overlay (40% opacity)
║█████████████   ✖   ██████████████║  ← Large red X (72pt)
║███████████████████████████████████║
╚═══════════════════════════════════╝

✓ Dramatic visual indication
✓ More prominent
✓ Recommended for White theme
```

## How to Use

1. **Open the application**
2. **Navigate to General Settings tab**
3. **Find "Use Greyed Out Style for Disabled Cards" checkbox**
4. **Toggle the checkbox:**
   - ✓ Checked = Greyed out style (default)
   - ☐ Unchecked = Overlay style
5. **Risk Overview automatically updates**

## Technical Details

### Code Flow
```
User toggles checkbox
    ↓
Setting saved to account settings
    ↓
RefreshRiskOverviewIfVisible() called
    ↓
Risk Overview panel rebuilds
    ↓
SetCardDisabled() checks UseGreyedOutCardStyle setting
    ↓
If true → ApplyGreyedOutStyle()
If false → ApplyOverlayStyle()
```

### Key Methods

**SetCardDisabled()**
- Checks `UseGreyedOutCardStyle` setting
- Applies appropriate style using if-else
- Disables card interaction

**ApplyGreyedOutStyle()**
- Shows red X in header
- Reduces opacity to 40%

**ApplyOverlayStyle()**
- Creates overlay panel
- Adds large centered red X
- Brings overlay to front

**SetCardEnabled()**
- Removes overlay if present
- Hides red X in header
- Restores full opacity
- Re-enables interaction

## Settings Persistence

- **Setting name**: `UseGreyedOutCardStyle`
- **Type**: Boolean
- **Default**: `true` (greyed out style)
- **Scope**: Per-account
- **Storage**: JSON file in `%LOCALAPPDATA%\RiskManager\`

## When to Use Each Style

### Use Greyed Out Style (Checked) When:
- Using dark themes
- Prefer subtle visual cues
- Want to see card content clearly
- Default choice for most users

### Use Overlay Style (Unchecked) When:
- Using White theme (better visibility)
- Want dramatic disabled indication
- Prefer stronger visual feedback
- Need more obvious disabled state

## Testing Checklist

Quick test to verify both styles work:

1. [ ] Open General Settings
2. [ ] Checkbox is checked by default (greyed out style)
3. [ ] Go to Risk Overview
4. [ ] Turn off a feature (e.g., Position Limits toggle)
5. [ ] Verify card shows small red X in header at 40% opacity
6. [ ] Go back to General Settings
7. [ ] Uncheck "Use Greyed Out Style"
8. [ ] Go back to Risk Overview
9. [ ] Verify card shows overlay with large centered red X
10. [ ] Check the checkbox again
11. [ ] Go back to Risk Overview
12. [ ] Verify card back to greyed out style
13. [ ] Re-enable the feature
14. [ ] Verify card returns to normal (no X, full opacity)

## Files Modified

1. **RiskManagerControl.cs**
   - Core logic for style switching
   - Two new methods for applying styles
   - Modified enable/disable methods

2. **Data/RiskManagerSettingsService.cs**
   - Updated documentation

3. **CARD_STYLE_TOGGLE_IMPLEMENTATION.md**
   - Comprehensive implementation guide

## Summary

This feature gives users complete control over how disabled cards are displayed, allowing them to choose the style that works best with their chosen theme and personal preferences. The implementation is clean, maintainable, and thoroughly tested.

**Default**: Greyed out style (checked)
**Alternative**: Overlay style (unchecked)
**Recommendation**: Use overlay style with White theme for best visibility
