# Risk Overview Tab - UI Mockup

## Visual Representation

```
┌────────────────────────────────────────────────────────────────────────┐
│ Risk Manager                                      [Settings] [Trading]  │
│                                                                          │
│ Account: [Account Selector ▼]  [⚠️ EMERGENCY FLATTEN ⚠️]    🎨         │
├────────────┬────────────────────────────────────────────────────────────┤
│            │                                                             │
│ Navigation │ 🔍 Risk Overview                                           │
│            │ Comprehensive risk settings overview for the selected...   │
│ 📊 Accounts│ ┌──────────────────────────────────────────────────────┐  │
│ 📈 Stats   │ │ 🔒 Account Status                                    │  │
│ 📋 Type    │ │                                                       │  │
│ 🔍 Risk    │ │ Lock Status:        🔓 Unlocked                      │  │
│   Overview │ │ Settings Lock:      🔓 Unlocked                      │  │
│ ⚙️ Feature │ └──────────────────────────────────────────────────────┘  │
│ 📋 Copy    │                                                             │
│ 📈 Positio │ ┌──────────────────────────────────────────────────────┐  │
│ 📊 Limits  │ │ 📈 Position Limits                                   │  │
│ 🛡️ Symbols │ │                                                       │  │
│ 🕐 Trading │ │ Loss Limit:         💵 $500.00 per position          │  │
│ 🔒 Lock    │ │ Profit Target:      💵 $1,000.00 per position        │  │
│ 🔒 Manual  │ └──────────────────────────────────────────────────────┘  │
│            │                                                             │
│            │ ┌──────────────────────────────────────────────────────┐  │
│            │ │ 📊 Daily Limits                                      │  │
│            │ │                                                       │  │
│            │ │ Loss Limit:         💵 $1,000.00 per day             │  │
│            │ │ Profit Target:      💵 $2,000.00 per day             │  │
│            │ └──────────────────────────────────────────────────────┘  │
│            │                                                             │
│            │ ┌──────────────────────────────────────────────────────┐  │
│            │ │ 🛡️ Symbol Restrictions                               │  │
│            │ │                                                       │  │
│            │ │ Blacklisted Symbols:    ⛔ ES, NQ, YM                │  │
│            │ │ Default Contract Limit: 📊 10 contracts              │  │
│            │ │ Symbol-Specific Limits: 📊 ES:5, NQ:3                │  │
│            │ └──────────────────────────────────────────────────────┘  │
│            │                                                             │
│            │ ┌──────────────────────────────────────────────────────┐  │
│            │ │ 🕐 Allowed Trading Times                             │  │
│            │ │                                                       │  │
│            │ │ Trading Time Status:  ✅ 3 time slot(s) configured   │  │
│            │ └──────────────────────────────────────────────────────┘  │
│            │                                                             │
└────────────┴────────────────────────────────────────────────────────────┘
```

## Color Scheme

### Blue Theme (Default)
- **Background**: Dark blue-gray (#2D3E50)
- **Cards**: Lighter blue-gray (#37485A)
- **Text**: White (#FFFFFF)
- **Secondary Text**: Light gray (#BDC3C7)
- **Accents**: Green (#27AE60), Amber (#F39C12)

### Black Theme
- **Background**: Very dark gray (#141414)
- **Cards**: Dark gray (#1E1E1E)
- **Text**: White (#FFFFFF)
- **Secondary Text**: Medium gray (#A0A0A0)
- **Accents**: Bright green (#00C853), Bright amber (#FFB900)

### White Theme
- **Background**: Light gray (#F5F5F5)
- **Cards**: White (#FFFFFF)
- **Text**: Dark gray (#1E1E1E)
- **Secondary Text**: Medium gray (#5A5A5A)
- **Accents**: Green (#27AE60), Amber (#F39C12)

## Emoji Reference

| Emoji | Name | Usage | Color |
|-------|------|-------|-------|
| 🔍 | Magnifying Glass | Tab title, search/overview | Full color |
| 🔒 | Locked | Locked status | Full color |
| 🔓 | Unlocked | Unlocked status | Full color |
| 📈 | Chart Increasing | Position limits, growth | Full color |
| 📊 | Bar Chart | Daily limits, statistics | Full color |
| 🛡️ | Shield | Symbol restrictions, protection | Full color |
| 🕐 | Clock | Trading times | Full color |
| 💵 | Dollar Banknote | Monetary values | Full color |
| ⛔ | No Entry | Blocked/restricted symbols | Full color |
| ✅ | Check Mark | Active/enabled features | Full color |
| ❌ | Cross Mark | Disabled features | Full color |
| ⚠️ | Warning | Warnings, no selection | Full color |

## Interactive States

### Hover State
- Card background lightens slightly
- Cursor remains default (no action on hover)
- Tooltip may appear for truncated text

### Disabled State (NEW)
When a feature is disabled in account settings, the corresponding card displays:
- **Red X Indicator**: A red ✖ symbol appears in the top-right corner of the card header
- **Reduced Opacity**: Card content is displayed at 40% opacity (semi-transparent)
- **No Cursor**: Mouse cursor changes to "No" symbol (🚫) when hovering over the card
- **Non-Interactive**: Card Enabled property is set to false, preventing all user interaction
- **No Overlay**: Content remains visible without an obscuring overlay panel

```
┌──────────────────────────────────────────────────────┐
│ 📈 Position Limits                              ✖   │
│                                                      │
│ Loss Limit:         💵 $500.00 per position         │  [40% opacity, not clickable]
│ Profit Target:      💵 $1,000.00 per position       │  [40% opacity, not clickable]
└──────────────────────────────────────────────────────┘
```

### Loading State
```
┌──────────────────────────────────────────────────────┐
│ 🔍 Risk Overview                                    │
│ Loading risk settings...                            │
└──────────────────────────────────────────────────────┘
```

### No Account Selected State
```
┌──────────────────────────────────────────────────────┐
│ 🔍 Risk Overview                                    │
│                                                      │
│ ⚠️ No account selected                              │
│ Please select an account from the dropdown above    │
└──────────────────────────────────────────────────────┘
```

### Empty Settings State
```
┌──────────────────────────────────────────────────────┐
│ 📈 Position Limits                                  │
│                                                      │
│ Loss Limit:         ❌ Not enabled                   │
│ Profit Target:      ❌ Not enabled                   │
└──────────────────────────────────────────────────────┘
```

## Responsive Behavior

### Narrow Window
- Cards maintain 700px width
- Horizontal scrollbar appears if needed
- Vertical scrollbar for overflow content
- Sidebar collapses (existing behavior)

### Wide Window
- Cards remain 700px width (centered)
- Additional whitespace on right
- No horizontal scrolling needed
- Better readability with consistent width

## Accessibility

### Keyboard Navigation
- Tab key moves between cards
- Arrow keys scroll content
- Enter key can expand/collapse (future enhancement)

### Screen Readers
- Card titles announced with emoji descriptions
- Label-value pairs read in sequence
- Status indicators clearly described

### High Contrast Mode
- Emoji colors adjust for visibility
- Text maintains sufficient contrast
- Borders added for card separation

## Future UI Enhancements

### Phase 2
- [ ] Inline edit buttons
- [ ] Copy to clipboard buttons
- [ ] Expand/collapse cards
- [ ] Card reordering

### Phase 3
- [ ] Historical data charts
- [ ] Real-time update indicators
- [ ] Quick action buttons
- [ ] Tooltips with details

### Phase 4
- [ ] Export to PDF button
- [ ] Print layout optimization
- [ ] Share via email
- [ ] Comparison mode

## Mobile/Tablet View (Future)

```
┌────────────────────────────┐
│ 🔍 Risk Overview          │
│ ┌──────────────────────┐  │
│ │ 🔒 Account Status    │  │
│ │ Lock: 🔓 Unlocked    │  │
│ └──────────────────────┘  │
│ ┌──────────────────────┐  │
│ │ 📈 Position Limits   │  │
│ │ Loss: $500           │  │
│ └──────────────────────┘  │
│ [Scroll down...]          │
└────────────────────────────┘
```

## Implementation Notes

1. **Emoji Rendering**: Uses Segoe UI Emoji font with GDI+ custom painting for full color support
2. **Theme Integration**: Dynamically updates with theme changes
3. **Performance**: Cached data prevents unnecessary reloads
4. **Scalability**: Modular card design allows easy addition of new settings
5. **Maintainability**: Consistent with existing UI patterns
6. **Disabled State**: Non-overlay approach with red X and reduced opacity for disabled features
   - Original colors are preserved and restored when re-enabled
   - Card interaction is fully disabled (Enabled = false) to prevent unintended actions

## Testing Checklist

- [ ] All emojis display correctly in all themes
- [ ] Cards render properly at different window sizes
- [ ] Scrolling works smoothly
- [ ] Text is readable in all themes
- [ ] No account selected state displays
- [ ] Empty settings display correctly
- [ ] Values update when account changes
- [ ] Theme changes apply immediately
- [ ] Performance is acceptable with many settings
- [ ] No visual glitches or artifacts
- [ ] Disabled cards show red X in top-right corner
- [ ] Disabled cards display content at 40% opacity
- [ ] Disabled cards show "No" cursor on hover
- [ ] Disabled cards are fully non-interactive (Enabled = false)
- [ ] Enabling/disabling features updates card state correctly
- [ ] Original colors are preserved and restored correctly when re-enabled
