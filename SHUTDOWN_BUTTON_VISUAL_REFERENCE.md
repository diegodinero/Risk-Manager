# Shutdown Button - Visual Reference

## Button Location

The shutdown button is located in the top-right corner of the Risk Manager application, positioned directly below the theme switcher button in a vertical layout:

```
┌─────────────────────────────────────────────────────────────┐
│ Risk Manager                    [Settings ✓] [Trading ✓]    │
│                                                              │
│                                                      [🎨]    │  ← Theme Switcher (44×36px)
│                                                      [🚪]    │  ← Shutdown Button (44×36px, 5px gap)
└─────────────────────────────────────────────────────────────┘
```

**Layout Structure:**
- A vertical `FlowLayoutPanel` (with `TopDown` direction) contains both buttons
- This buttons panel is added to the horizontal badges panel
- The shutdown button has a 5px top margin for visual separation

## Button Appearance

### Default State
- Size: 44px width × 36px height (matches theme switcher)
- Icon: leave.png (door icon)
- Background: Transparent
- Border: None (FlatStyle.Flat)

### Hover State
- Background: Red (#E74C3C / RGB: 231, 76, 60)
- Cursor: Hand pointer
- Visual feedback indicates shutdown action

### Fallback
- If leave.png not available, shows 🚪 emoji
- Font: Segoe UI Emoji, 14pt Bold

## Shutdown Flow

### 1. Initial Click
```
┌─────────────────────────────────────────────┐
│             Confirm Shutdown               │
│                                             │
│  Are you sure you want to lock all          │
│  accounts, settings, and close the          │
│  application?                               │
│                                             │
│            [  Yes  ]  [  No  ]             │
└─────────────────────────────────────────────┘
```

### 2. After Confirmation (Yes clicked)
- ✅ All accounts locked (trading and settings)
- 🔊 "leave-get-out.wav" sound plays
- ⏱️ Countdown dialog appears

### 3. Countdown Dialog
```
┌─────────────────────────────────────────────┐
│           Shutting Down...                  │
│                                             │
│                                             │
│  Application will close in 5 seconds...     │
│                                             │
│                                             │
│         [  Cancel Shutdown  ]              │
│                                             │
└─────────────────────────────────────────────┘
```

The countdown updates every second:
- 5 seconds... → 4 seconds... → 3 seconds... → 2 seconds... → 1 seconds... → Application closes

### 4. Cancel Option
Clicking "Cancel Shutdown" button:
- ⏹️ Stops the countdown timer
- ❌ Closes the countdown dialog
- ℹ️ Shows "Shutdown cancelled." confirmation message
- Application remains open with accounts still locked

### 5. Normal Completion
When countdown reaches 0:
- ⏹️ Timer stops automatically
- 🚪 Countdown dialog closes
- 🔒 All accounts remain locked
- 🛑 Application closes gracefully

## Theme Compatibility

The shutdown button adapts to all themes:

### Blue Theme (Original)
- Button background: Transparent
- Hover: Red (#E74C3C)
- Countdown dialog: Dark blue background (#37485A)

### Black Theme (Dark)
- Button background: Transparent
- Hover: Red (#E74C3C)
- Countdown dialog: Dark gray background (#1E1E1E)

### White Theme (Light)
- Button background: Transparent
- Hover: Red (#E74C3C)
- Countdown dialog: White background (#FFFFFF)
- Text: Dark gray (#1E1E1E)

### YellowBlueBlack Theme
- Button background: Transparent
- Hover: Red (#E74C3C)
- Countdown dialog: Dark gray background (#1E1E1E)
- Maintains special value colors (blue for positive, yellow for negative)

## Countdown Dialog Details

### Size & Position
- Width: 400px
- Height: 180px
- Position: Center screen
- Modal: No (uses Show() for responsiveness)
- TopMost: Yes (stays on top)

### Controls
1. **Title Bar**: "Shutting Down..."
2. **Countdown Label**:
   - Size: 360×60px
   - Font: Segoe UI, 11pt Regular
   - Alignment: Center
   - Updates every second

3. **Cancel Button**:
   - Size: 150×35px
   - Color: Red (#E74C3C)
   - Position: Centered horizontally, 90px from top
   - Font: Segoe UI, 10pt Bold

## User Experience Flow

```
Click Shutdown → Confirm? → Lock All → Play Sound → 5 sec countdown → Close App
    ↓              ↓           ↓           ↓              ↓
    🖱️             ✅          🔒          🔊             ⏱️
                   ↓                                      ↓
                   ❌ (No)                                ❌ (Cancel)
                   Stay open                              Stay open
```

## Error Handling

The implementation includes comprehensive error handling:

1. **Confirmation Dialog**: If user clicks "No", operation cancels immediately
2. **Lock Failure**: Errors during lock are caught and displayed, but shutdown continues
3. **Sound Playback**: If sound fails to play, error is logged but shutdown continues
4. **Countdown Timer**: Any errors stop the timer and cleanup resources
5. **Application Close**: Multiple fallbacks (Form.Close → Application.Exit)

## Safety Features

1. **Double Confirmation**: Dialog + Countdown with cancel
2. **Visual Countdown**: Clear time remaining display
3. **Easy Cancellation**: Prominent cancel button throughout countdown
4. **Auto-Lock**: Ensures accounts are locked before exit
5. **Audio Feedback**: Audible confirmation of action
6. **Resource Cleanup**: Proper disposal of all resources

## Technical Notes

### Code Location
- Main implementation: `RiskManagerControl.cs`
- Lines ~270-280: Field declarations
- Lines ~1904-1907: IconMap initialization
- Lines ~2261-2309: Button initialization in CreateTopPanel()
- Lines ~5272-5466: Event handlers and methods
- Lines ~10453-10486: Resource disposal in Dispose()

### Resources Required
- `Resources/leave.png` - Door icon (already exists)
- `Resources/leave-get-out.wav` - Exit sound (already exists)
- Both referenced in `Properties/Resources.Designer.cs`

### Dependencies
- Uses existing `BtnLockAllAccounts_Click()` for lock logic
- Integrates with existing theme system
- Uses existing IconMap infrastructure
- Follows existing pattern for resource disposal

## Testing Checklist

- [ ] Button appears below theme switcher
- [ ] Button displays door icon correctly
- [ ] Hover effect shows red background
- [ ] Click shows confirmation dialog
- [ ] Clicking "No" cancels operation
- [ ] Clicking "Yes" locks all accounts
- [ ] Sound plays after confirmation
- [ ] Countdown dialog appears and counts down
- [ ] Countdown updates every second (5→4→3→2→1)
- [ ] Cancel button stops countdown
- [ ] Cancelling shows "Shutdown cancelled" message
- [ ] Application closes after countdown reaches 0
- [ ] Button works in Blue theme
- [ ] Button works in Black theme
- [ ] Button works in White theme
- [ ] Button works in YellowBlueBlack theme
- [ ] Resources properly disposed on app close
- [ ] No crashes or errors during normal flow
- [ ] No crashes or errors during cancel flow
