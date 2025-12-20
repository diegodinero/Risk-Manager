# Copy Settings UI Layout

## Visual Structure

```
┌─────────────────────────────────────────────────────────────────┐
│  📋 Copy Settings                                      [Dark]    │
├─────────────────────────────────────────────────────────────────┤
│  Copy risk management settings from one account to multiple     │
│  target accounts:                                               │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Source Account (Copy From):                                    │
│  ┌────────────────────────────────────────────────────┐         │
│  │ [Select Source Account ▼]                          │         │
│  └────────────────────────────────────────────────────┘         │
│                                                                  │
│  Target Accounts (Copy To):                                     │
│  ┌────────────────────────────────────────────────────┐         │
│  │  ☐ Demo Account 2 (Demo_Account2)                  │         │
│  │  ☐ Live Account 1 (Interactive_LiveAccount1)       │         │
│  │  ☐ Live Account 2 (Interactive_LiveAccount2)       │         │
│  │  ☐ Paper Trading (Paper_Trading)                   │         │
│  └────────────────────────────────────────────────────┘         │
│                                                                  │
│  [ Select All ]  [ Deselect All ]                               │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  [   COPY SETTINGS TO SELECTED ACCOUNTS   ]   (Green Button)   │
└─────────────────────────────────────────────────────────────────┘
```

## Color Scheme (Dark Theme)

- **Background**: Dark blue-gray (#2D3E50)
- **Card Background**: Slightly lighter (#37485A)
- **Text**: White (#FFFFFF)
- **Secondary Text**: Light gray (#BDC3C7)
- **Accent (Buttons)**: Green (#27AE60)
- **Checkbox Panel**: Darker gray (#233446)

## User Flow Visualization

### Step 1: Initial State
```
Source Dropdown: [Select Source Account ▼]
Target Panel: (Empty - waiting for source selection)
Copy Button: Enabled
```

### Step 2: Source Selected
```
Source Dropdown: [Demo Account 1 ▼]
Target Panel: Shows 3 other accounts with unchecked boxes
Copy Button: Enabled
```

### Step 3: Targets Selected
```
Source Dropdown: [Demo Account 1 ▼]
Target Panel: 
  ☐ Demo Account 2 (Demo_Account2)
  ☑ Live Account 1 (Interactive_LiveAccount1)  ← Checked
  ☑ Live Account 2 (Interactive_LiveAccount2)  ← Checked
  ☐ Paper Trading (Paper_Trading)
Copy Button: Enabled (ready to copy to 2 accounts)
```

### Step 4: Confirmation Dialog
```
┌─────────────────────────────────────────────┐
│  Confirm Copy Settings               [?]    │
├─────────────────────────────────────────────┤
│                                              │
│  Are you sure you want to copy settings     │
│  from:                                       │
│                                              │
│  Source: Demo Account 1 (Demo_Account1)     │
│                                              │
│  To 2 target account(s)?                    │
│                                              │
│  This will overwrite existing settings on   │
│  the target accounts.                       │
│                                              │
├─────────────────────────────────────────────┤
│               [ Yes ]  [ No ]               │
└─────────────────────────────────────────────┘
```

### Step 5: Success Dialog
```
┌─────────────────────────────────────────────┐
│  Success                             [i]    │
├─────────────────────────────────────────────┤
│                                              │
│  Copy operation completed:                  │
│                                              │
│  ✓ Successful: 2                            │
│  ✗ Failed: 0                                │
│                                              │
├─────────────────────────────────────────────┤
│                   [ OK ]                    │
└─────────────────────────────────────────────┘
```

### Step 5 (Alternative): Partial Failure Dialog
```
┌─────────────────────────────────────────────┐
│  Copy Completed with Errors          [!]    │
├─────────────────────────────────────────────┤
│                                              │
│  Copy operation completed:                  │
│                                              │
│  ✓ Successful: 1                            │
│  ✗ Failed: 1                                │
│                                              │
│  Failed accounts:                           │
│  - Interactive_LiveAccount2:                │
│    Error: Failed to create settings         │
│                                              │
├─────────────────────────────────────────────┤
│                   [ OK ]                    │
└─────────────────────────────────────────────┘
```

## Interactive Elements

### Source Account Dropdown
- **Type**: ComboBox (DropDownList style)
- **Population**: Auto-populated with all connected accounts
- **Event**: On selection change → Updates target accounts panel
- **Styling**: Dark background, white text, flat style

### Target Accounts Panel
- **Type**: FlowLayoutPanel with CheckBoxes
- **Population**: Dynamic based on source selection
- **Exclusion**: Source account is automatically excluded
- **Styling**: Darker background, white text for checkboxes
- **Empty State**: Shows "No other accounts available" when only 1 account exists

### Select All Button
- **Action**: Checks all checkboxes in target panel
- **Styling**: Medium gray background (#34495E), white text
- **Size**: 120px wide, 35px tall

### Deselect All Button
- **Action**: Unchecks all checkboxes in target panel
- **Styling**: Medium gray background (#34495E), white text
- **Size**: 120px wide, 35px tall

### Copy Button
- **Action**: Validates selections → Shows confirmation → Executes copy → Shows results
- **Styling**: Green background (#27AE60), white text, bold font
- **Size**: Full width, 50px tall
- **Position**: Bottom of panel (docked)

## Validation Messages

### No Source Selected
```
┌─────────────────────────────────────────────┐
│  Validation Error                    [!]    │
├─────────────────────────────────────────────┤
│  Please select a source account to copy     │
│  settings from.                             │
├─────────────────────────────────────────────┤
│                   [ OK ]                    │
└─────────────────────────────────────────────┘
```

### No Targets Selected
```
┌─────────────────────────────────────────────┐
│  Validation Error                    [!]    │
├─────────────────────────────────────────────┤
│  Please select at least one target          │
│  account to copy settings to.               │
├─────────────────────────────────────────────┤
│                   [ OK ]                    │
└─────────────────────────────────────────────┘
```

## Responsive Behavior

- **Minimum Width**: 600px (inherited from main control)
- **Minimum Height**: 400px (inherited from main control)
- **Scrolling**: Vertical scroll enabled for target accounts if many accounts exist
- **Auto-sizing**: Content area auto-scrolls, buttons remain docked at bottom

## Integration with Navigation

```
Left Sidebar:
┌────────────────────┐
│ 📊 Accounts Summary│
│ 📈 Stats           │
│ 📋 Type            │
│ ⚙️ Feature Toggles │
│ 📋 Copy Settings   │ ← New Tab
│ 📈 Positions       │
│ 📊 Limits          │
│ 🛡️ Symbols         │
│ 🕐 Allowed Trading │
│ 📉 Weekly Loss     │
│ 📈 Weekly Profit   │
│ 🔒 Lock Settings   │
│ 🔒 Manual Lock     │
└────────────────────┘
```

## Accessibility Features

- **Clear Labels**: All controls have descriptive labels
- **Visual Hierarchy**: Title → Subtitle → Controls → Action Button
- **Color Contrast**: High contrast between text and backgrounds
- **Error Messages**: Clear, actionable error messages
- **Confirmation**: Destructive action requires confirmation
- **Feedback**: Detailed success/failure feedback per account

## Technical Notes

- **Panel Type**: UserControl with docked layout
- **Layout Manager**: Combination of Dock and FlowLayoutPanel
- **Event Handling**: Event-driven updates (no polling)
- **State Management**: UI state cleared after successful operations
- **Thread Safety**: All operations on UI thread

## Testing Scenarios

1. **Single Account**: Shows "No other accounts available"
2. **Multiple Accounts**: Shows all except source
3. **Large Account List**: Vertical scrolling works correctly
4. **Quick Selection**: Select All/Deselect All work instantly
5. **Validation**: All validation messages display correctly
6. **Confirmation**: Confirmation shows correct details
7. **Success**: Success message displays correctly
8. **Partial Failure**: Failure details displayed for each failed account
9. **Account Changes**: Adding/removing accounts updates dropdowns correctly
10. **Navigation**: Tab switching preserves/resets state appropriately
