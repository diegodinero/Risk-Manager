# Trading Journal Tab - Visual Reference

## Tab Location
The Trading Journal tab appears in the left navigation panel, positioned between "Risk Overview" and "Feature Toggles":

```
📊 Accounts Summary
📈 Stats
📋 Type
🔍 Risk Overview
📓 Trading Journal     ← NEW TAB
⚙️ Feature Toggles
📋 Copy Settings
📈 Positions
📊 Limits
🛡️ Symbols
🕐 Allowed Trading Times
🔒 Lock Settings
🔒 Manual Lock
⚙️ General Settings
```

## Layout Overview

```
┌─────────────────────────────────────────────────────────────────────────┐
│ 📓 Trading Journal                                                       │
├─────────────────────────────────────────────────────────────────────────┤
│                                                                           │
│  ┌─ Journal Statistics ──────────────────────────────────────────────┐  │
│  │                                                                     │  │
│  │  Total Trades: 25 (W:18 L:6 BE:1)    Win Rate: 72.0%             │  │
│  │  Total P/L: $4,250.00                Avg P/L: $170.00             │  │
│  │                                                                     │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                           │
│  ┌─ Trade Log ────────────────────────────────────────────────────────┐  │
│  │                                                                     │  │
│  │  [➕ Add Trade]  [✏️ Edit]  [🗑️ Delete]                            │  │
│  │                                                                     │  │
│  │  ┌───────────────────────────────────────────────────────────────┐ │  │
│  │  │ Date       │Symbol│Type │Outcome│ P/L   │Net P/L│R:R │Model │ │ │  │
│  │  ├───────────────────────────────────────────────────────────────┤ │  │
│  │  │ 1/15/2024  │ ES   │Long │Win    │$250.00│$245.00│2.5 │OB    │ │ │  │
│  │  │ 1/15/2024  │ NQ   │Short│Loss   │-$100  │-$105  │2.0 │FVG   │ │ │  │
│  │  │ 1/14/2024  │ ES   │Long │Win    │$325.00│$320.00│3.0 │OB    │ │ │  │
│  │  │ 1/14/2024  │ ES   │Long │Breakev│$0.00  │-$5.00 │1.5 │FVG   │ │ │  │
│  │  │ ...        │ ...  │...  │...    │...    │...    │... │...   │ │ │  │
│  │  └───────────────────────────────────────────────────────────────┘ │  │
│  │                                                                     │  │
│  └─────────────────────────────────────────────────────────────────────┘  │
│                                                                           │
└─────────────────────────────────────────────────────────────────────────┘
```

## Statistics Card Details

The statistics card displays:
- **Total Trades**: Overall count with breakdown of Wins (W), Losses (L), and Breakevens (BE)
- **Win Rate**: Percentage of winning trades (color-coded)
- **Total P/L**: Sum of all net profit/loss (GREEN if positive, RED if negative)
- **Avg P/L**: Average net profit/loss per trade (GREEN if positive, RED if negative)

## Trade Log Grid

### Columns:
1. **Date**: Trade date (MM/DD/YYYY format)
2. **Symbol**: Trading symbol (e.g., ES, NQ, CL)
3. **Type**: Long or Short
4. **Outcome**: Win, Loss, or Breakeven (color-coded)
5. **P/L**: Profit/Loss before fees
6. **Net P/L**: Profit/Loss after fees
7. **R:R**: Risk:Reward ratio
8. **Model**: Trading model/strategy used
9. **Notes**: First 30 characters of notes (truncated with "...")

### Color Coding:
- **Win outcomes**: GREEN text
- **Loss outcomes**: RED text
- **Breakeven outcomes**: Default text color

### Selection:
- Single row selection
- Selected row is highlighted
- Must select a row to Edit or Delete

## Action Buttons

### ➕ Add Trade (Green)
- Opens the Trade Entry Dialog
- Requires an account to be selected
- Shows validation error if no account selected

### ✏️ Edit (Blue)
- Opens Trade Entry Dialog with selected trade data pre-filled
- Requires a row to be selected
- Shows info message if no trade selected

### 🗑️ Delete (Red)
- Removes selected trade after confirmation
- Shows confirmation dialog: "Are you sure you want to delete this trade?"
- Requires a row to be selected
- Shows info message if no trade selected

## Trade Entry Dialog

```
┌─ Add New Trade / Edit Trade ───────────────────────┐
│                                                     │
│  Date:              [MM/DD/YYYY  ▼]                │
│                                                     │
│  Symbol:            [________]                      │
│                                                     │
│  Outcome:           [Win          ▼]               │
│                     (Win / Loss / Breakeven)        │
│                                                     │
│  Type:              [Long         ▼]               │
│                     (Long / Short)                  │
│                                                     │
│  Model/Strategy:    [________]                      │
│                                                     │
│  Session:           [________]                      │
│                                                     │
│  P/L ($):           [________]                      │
│                                                     │
│  Risk:Reward:       [________]                      │
│                                                     │
│  Entry Time:        [________]                      │
│                                                     │
│  Exit Time:         [________]                      │
│                                                     │
│  Entry Price:       [________]                      │
│                                                     │
│  Exit Price:        [________]                      │
│                                                     │
│  Contracts:         [________]                      │
│                                                     │
│  Fees ($):          [________]                      │
│                                                     │
│  [✓] Followed Trading Plan                         │
│                                                     │
│  Emotions:          [Confident    ▼]               │
│                     (Confident / Nervous / Excited) │
│                     (Fearful / Greedy / Disciplined)│
│                     (Impulsive / Neutral)           │
│                                                     │
│  Notes:             ┌──────────────────┐           │
│                     │                  │           │
│                     │                  │           │
│                     └──────────────────┘           │
│                                                     │
│              [  Save  ]  [ Cancel ]                │
│                                                     │
└─────────────────────────────────────────────────────┘
```

### Dialog Features:
- **Dark theme** consistent with Risk Manager
- **White text** on dark background
- **Date picker** for easy date selection
- **Dropdowns** for predefined options (Outcome, Type, Emotions)
- **Numeric validation** for P/L, prices, fees
- **Multiline notes** field with scrollbar
- **Save button** (green) validates and saves
- **Cancel button** (gray) closes without saving

### Validation:
- **Symbol**: Required, shows warning if empty
- **Outcome**: Required, shows warning if not selected
- **Other fields**: Optional, defaults to 0 or empty string

## Theme Support

The Trading Journal tab adapts to all Risk Manager themes:

### Dark Theme (Default)
- Background: #1E1E1E
- Cards: #2A2A2A
- Text: White
- Wins: Lime Green
- Losses: Red

### Yellow Theme
- Background: Yellow tinted
- Cards: Lighter yellow
- Text: Black/Dark Gray
- Wins: Dark Green
- Losses: Dark Red

### White Theme
- Background: White
- Cards: Light Gray
- Text: Black
- Wins: Green
- Losses: Red

### Blue Theme
- Background: Dark Blue
- Cards: Medium Blue
- Text: White
- Wins: Cyan
- Losses: Orange

## Account Integration

The Trading Journal is **account-specific**:

1. **Account Selection Required**: User must select an account from the dropdown at the top
2. **Per-Account Data**: Each account has its own separate journal
3. **Account Switching**: When switching accounts:
   - Statistics update to show selected account's data
   - Trade log refreshes to show selected account's trades
   - New trades are associated with the currently selected account

## Data Persistence

- **Storage Location**: `%AppData%\RiskManager\Journal\trading_journal.json`
- **Format**: JSON with account numbers as keys
- **Auto-Save**: All changes saved immediately
- **Load on Startup**: Journal data loaded when Risk Manager starts
- **Backup**: Consider manual backup of JSON file for safety

## Example Usage Scenario

1. Trader selects "Account 12345" from dropdown
2. Clicks on "📓 Trading Journal" tab
3. Sees 25 existing trades with 72% win rate and $4,250 total P/L
4. Clicks "➕ Add Trade" to log today's winning ES trade
5. Fills in trade details:
   - Date: Today
   - Symbol: ES
   - Outcome: Win
   - Type: Long
   - P/L: $250
   - Entry: 4750.00
   - Exit: 4755.00
   - Model: "Order Block"
   - Emotions: "Disciplined"
   - Notes: "Perfect entry at OB, took profit at resistance"
6. Clicks "Save"
7. Statistics automatically update to show 26 trades, updated win rate and P/L
8. New trade appears at top of the grid

## Benefits

- **Centralized**: No need to switch to separate journaling app
- **Account-Aware**: Automatically associated with trading account
- **Real-Time Stats**: See performance metrics immediately
- **Easy Access**: One click from any Risk Manager view
- **Consistent UI**: Matches Risk Manager's look and feel
- **Persistent**: All data saved automatically
- **Lightweight**: No heavy dependencies or complex setup

## Notes for Users

- Always select an account before using the journal
- Use consistent model names for better analysis
- Be honest about emotions and plan adherence for learning
- Review statistics regularly to track improvement
- Back up the journal JSON file periodically
- Use notes field to capture key insights from each trade
