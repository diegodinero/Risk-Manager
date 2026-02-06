# Trading Journal Calendar - Implementation Guide

## Overview

The Trading Journal Calendar provides a visual month-by-month view of trading activity, showing trade performance through color-coded day cells. It's based on the Calendar component from the TradingJournalApp (@diegodinero/TradingJournalApp).

## Features

### Core Features
- ✅ **Monthly Calendar View** - Grid layout with 7 columns (Sun-Sat) and 5-6 rows
- ✅ **Month Navigation** - Previous/Next buttons to browse through months
- ✅ **Dual Display Modes** - Toggle between P&L and Plan views
- ✅ **Color-Coded Cells** - Visual indicators for performance
- ✅ **Trade Count Badges** - Shows number of trades per day
- ✅ **Monthly Statistics** - Summary of trades, P/L, and plan adherence
- ✅ **Interactive Cells** - Click to navigate to Trade Log for that date

### Display Modes

#### P&L Mode (Default)
Shows net profit/loss for each day:
- **Green cells** - Positive P/L (profit)
- **Yellow cells** - Zero P/L (breakeven)
- **Red cells** - Negative P/L (loss)
- Cell shows: Net P/L amount and trade count

#### Plan Mode
Shows plan adherence percentage for each day:
- **Green cells** - ≥70% of trades followed plan
- **Yellow cells** - 50-69% of trades followed plan
- **Red cells** - <50% of trades followed plan
- Cell shows: "X% followed" and trade count

## UI Layout

```
┌─────────────────────────────────────────────────────────────────┐
│  February 2026          ◀  ▶          [P&L] [Plan]              │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Monthly Summary                                                 │
│  Total Trades: 45 | Net P/L: +$2,450.00 | Days Traded: 15       │
│  Days Plan Followed (≥70%): 12                                   │
│                                                                  │
├─────────────────────────────────────────────────────────────────┤
│  Sun    Mon    Tue    Wed    Thu    Fri    Sat                  │
├────────┬───────┬───────┬───────┬───────┬───────┬───────┤
│        │       │       │       │       │   1   │   2   │
│        │       │       │       │       │ [G]   │ [R]   │
│        │       │       │       │       │+$150  │-$75   │
│        │       │       │       │       │   3   │   2   │
├────────┼───────┼───────┼───────┼───────┼───────┼───────┤
│   3    │   4   │   5   │   6   │   7   │   8   │   9   │
│  [Y]   │ [G]   │ [G]   │       │ [G]   │ [R]   │       │
│ $0.00  │+$200  │+$325  │       │+$425  │-$150  │       │
│   1    │   4   │   5   │       │   6   │   3   │       │
├────────┼───────┼───────┼───────┼───────┼───────┼───────┤
│  ...   │  ...  │  ...  │  ...  │  ...  │  ...  │  ...  │
└────────┴───────┴───────┴───────┴───────┴───────┴───────┘

Legend: [G] = Green, [R] = Red, [Y] = Yellow
Numbers in cells: Top = Day number, Middle = P/L or Plan %, Bottom = Trade count
```

## Technical Architecture

### File Structure

```
Risk-Manager/
├── RiskManagerControl.cs
│   ├── CreateCalendarPage()          - Main calendar page creation
│   ├── RefreshCalendarPage()         - Updates calendar when month/mode changes
│   ├── CreateCalendarStatsPanel()    - Monthly statistics panel
│   ├── CreateCalendarGrid()          - Calendar grid layout
│   └── CreateCalendarDayCell()       - Individual day cell rendering
└── Data/
    └── TradingJournalService.cs      - Data access for trades
```

### Class Fields

```csharp
// Calendar state tracking
private DateTime currentCalendarMonth = DateTime.Today;
private bool showPlanMode = false; // false = P&L mode, true = Plan mode
```

### Key Methods

#### 1. CreateCalendarPage()
**Purpose**: Creates the main calendar page UI
**Returns**: Panel containing the entire calendar interface
**Components**:
- Header with month/year label
- Previous/Next navigation buttons
- P&L/Plan toggle buttons
- Monthly statistics panel
- Calendar grid with day cells

#### 2. RefreshCalendarPage()
**Purpose**: Refreshes the calendar display when month or mode changes
**Triggers**: 
- Previous/Next month button clicks
- P&L/Plan toggle button clicks
**Actions**:
- Updates month/year label
- Updates toggle button colors
- Recreates calendar grid
- Refreshes statistics panel

#### 3. CreateCalendarStatsPanel()
**Purpose**: Creates the monthly statistics summary panel
**Returns**: Panel with monthly metrics
**Calculates**:
- Total trades for the month
- Net P/L for the month
- Number of days traded
- Number of days with ≥70% plan adherence

#### 4. CreateCalendarGrid()
**Purpose**: Creates the calendar grid with all day cells
**Returns**: Panel containing the 7×6 grid
**Logic**:
1. Creates day-of-week headers (Sun, Mon, etc.)
2. Calculates first day of month and days in month
3. Creates day cells for each day of the month
4. Positions cells in correct row/column
5. Handles empty cells before/after the month

#### 5. CreateCalendarDayCell()
**Purpose**: Creates a single day cell with trade information
**Parameters**:
- `dayNumber` - Day of month (1-31)
- `dayTrades` - List of trades for this day
- `date` - DateTime for this day
**Returns**: Panel representing the day cell
**Logic**:
1. Determines cell color based on mode and trade data
2. Adds day number label
3. Adds P/L or plan % label (if trades exist)
4. Adds trade count badge
5. Adds click handler for navigation

## Data Flow

```
User Action → Event Handler → Update State → Refresh Display

Examples:
1. Click "Next Month"
   → currentCalendarMonth += 1 month
   → RefreshCalendarPage()
   → Recreate calendar grid

2. Click "Plan" toggle
   → showPlanMode = true
   → RefreshCalendarPage()
   → Recreate cells with plan data

3. Click day cell
   → Navigate to Trade Log
   → (Future: Filter by selected date)
```

## Color Scheme

### P&L Mode Colors
```csharp
// Positive P/L
Color.FromArgb(109, 231, 181)  // #6DE7B5 - Light Green

// Zero P/L
Color.FromArgb(252, 212, 75)   // #FCD44B - Yellow

// Negative P/L
Color.FromArgb(253, 164, 165)  // #FDA4A5 - Light Red

// No trades
CardBackground                  // Dark theme background
```

### Plan Mode Colors
```csharp
// ≥70% plan followed
Color.FromArgb(109, 231, 181)  // #6DE7B5 - Light Green

// 50-69% plan followed
Color.FromArgb(252, 212, 75)   // #FCD44B - Yellow

// <50% plan followed
Color.FromArgb(253, 164, 165)  // #FDA4A5 - Light Red

// No trades
CardBackground                  // Dark theme background
```

### UI Element Colors
```csharp
// Selected toggle button
Color.FromArgb(41, 128, 185)   // #2980B9 - Blue

// Unselected toggle button
CardBackground                  // Dark theme background

// Header background
Color.FromArgb(50, 50, 50)     // Dark gray

// Text
TextWhite                       // White for dark theme
Color.Black                     // Black for colored cells
```

## Integration with Existing Features

### Trading Journal Service
The calendar uses `TradingJournalService.Instance` to:
- Fetch all trades for the selected account
- Filter trades by date range (month/year)
- Calculate statistics (P/L, plan adherence)

```csharp
var account = GetSelectedAccount();
var trades = TradingJournalService.Instance.GetTradesForAccount(account);
var monthTrades = trades.Where(t => 
    t.Date.Year == currentCalendarMonth.Year && 
    t.Date.Month == currentCalendarMonth.Month
).ToList();
```

### Navigation System
The calendar integrates with the sidebar navigation:
- Listed as "🗓  Calendar" in the nav buttons
- Appears first in the list (before Trading Models)
- Updates selected state when clicked

### Theme Support
The calendar respects the current theme:
- Uses `DarkBackground` for main backgrounds
- Uses `CardBackground` for panels
- Uses `TextWhite` for text in dark areas
- Adapts to all theme changes automatically

## Usage Example

### Viewing the Calendar
1. Open Risk Manager
2. Select your trading account
3. Click "📓 Trading Journal" in left nav
4. Click "🗓  Calendar" in journal sidebar
5. View current month's trading activity

### Navigating Months
1. Click "◀" to go to previous month
2. Click "▶" to go to next month
3. Calendar updates immediately

### Switching Modes
1. Click "P&L" button to see profit/loss view
2. Click "Plan" button to see plan adherence view
3. Colors and cell contents update accordingly

### Viewing Trade Details
1. Click on any day cell with trades (colored cell)
2. Navigates to Trade Log page
3. (Future enhancement: Could filter to show only that day's trades)

## Comparison with TradingJournalApp

### What's the Same
✅ Monthly calendar grid layout
✅ Dual P&L/Plan display modes
✅ Color-coded cells (green/yellow/red)
✅ Trade count badges
✅ Month navigation
✅ Monthly statistics summary
✅ Click navigation to trade details

### What's Different
- **Technology**: Windows Forms (C#) instead of WPF (XAML)
- **Layout**: Simpler grid with Panel controls instead of UniformGrid
- **Styling**: Uses Risk Manager's existing theme system
- **Navigation**: Integrated into sidebar instead of standalone page
- **Weekly Stats**: Not implemented (could be added in future)

### Missing Features (Could Add Later)
- Weekly statistics rows
- Animated transitions
- Corner radius on cells
- Hover effects
- Date filtering on Trade Log navigation

## Performance Considerations

### Optimization Strategies
1. **Lazy Loading**: Calendar only loads data for current month
2. **Efficient Refresh**: Only recreates affected components
3. **Minimal Recalculation**: Statistics calculated once per refresh
4. **Control Reuse**: Disposes old controls before creating new ones

### Potential Improvements
1. Cache calculated statistics for visited months
2. Implement virtual scrolling for many trades per day
3. Add loading indicators for large datasets
4. Debounce rapid month changes

## Testing Checklist

### Functional Tests
- [ ] Calendar displays current month on first load
- [ ] Previous month button works correctly
- [ ] Next month button works correctly
- [ ] P&L mode shows correct colors and amounts
- [ ] Plan mode shows correct colors and percentages
- [ ] Trade count badges show correct numbers
- [ ] Monthly statistics calculate correctly
- [ ] Empty days show gray background
- [ ] Days with trades are clickable
- [ ] Clicking day navigates to Trade Log
- [ ] Account switching refreshes calendar

### Visual Tests
- [ ] Layout matches design specifications
- [ ] Colors are consistent with theme
- [ ] Text is readable on all backgrounds
- [ ] Grid aligns properly (7 columns)
- [ ] Cells are properly sized
- [ ] No clipping or overflow
- [ ] Statistics panel displays correctly

### Edge Cases
- [ ] Month with 28 days (February non-leap)
- [ ] Month with 29 days (February leap year)
- [ ] Month with 31 days
- [ ] Month starting on Sunday
- [ ] Month starting on Saturday
- [ ] Day with 0 trades
- [ ] Day with 1 trade
- [ ] Day with 50+ trades
- [ ] All trades followed plan (100%)
- [ ] No trades followed plan (0%)
- [ ] Empty month (no trades)

### Data Integrity Tests
- [ ] Correct trades displayed per day
- [ ] P/L calculations match Trade Log
- [ ] Plan adherence matches individual trades
- [ ] Account isolation works correctly
- [ ] Date boundaries respected (not showing adjacent months)

## Future Enhancements

### Potential Additions
1. **Date Filtering**: Click day → filter Trade Log to that date
2. **Weekly Statistics**: Add rows below calendar with weekly summaries
3. **Hover Tooltips**: Show detailed stats on hover
4. **Export Calendar**: Save calendar view as image
5. **Print Support**: Print calendar for physical records
6. **Date Range Selection**: Select multiple days for analysis
7. **Annotations**: Add notes to specific days
8. **Comparison Mode**: Compare two months side-by-side
9. **Year View**: Show all 12 months in miniature
10. **Performance Graphs**: Show trend line overlaid on calendar

### Code Improvements
1. Extract calendar logic into separate class
2. Add unit tests for calculation methods
3. Create reusable calendar control component
4. Implement undo/redo for navigation
5. Add keyboard navigation (arrow keys)

## Troubleshooting

### Common Issues

**Issue**: Calendar not showing trades
- **Cause**: Wrong account selected
- **Solution**: Check account selector at top of Risk Manager

**Issue**: Colors not updating when toggling modes
- **Cause**: RefreshCalendarPage() not called
- **Solution**: Ensure toggle buttons call RefreshCalendarPage()

**Issue**: Day cells not clickable
- **Cause**: No trades for that day
- **Solution**: Only days with trades are clickable (by design)

**Issue**: Month navigation not working
- **Cause**: currentCalendarMonth not updating
- **Solution**: Check button click handlers update the field

**Issue**: Statistics showing zero
- **Cause**: No trades in selected month
- **Solution**: Expected behavior - navigate to months with trades

## Credits

- **Original Design**: TradingJournalApp by @diegodinero
- **Platform**: Quantower Trading Platform
- **Implementation**: Adapted for Risk Manager (Windows Forms)
- **Version**: 1.0.0
- **Date**: February 2026

---

**The Trading Journal Calendar is now ready for use! Start tracking your trading patterns visually today.** 📅
