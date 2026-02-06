# Calendar Quick Reference

## Overview
Visual calendar view for tracking trading activity by date with color-coded performance indicators.

## Quick Access
1. Open Risk Manager
2. Click "📓 Trading Journal"
3. Click "🗓  Calendar"

## Display Modes

### P&L Mode
- Shows daily profit/loss
- Green = Profit, Yellow = Breakeven, Red = Loss
- Displays: Net P/L amount + trade count

### Plan Mode
- Shows daily plan adherence %
- Green = ≥70%, Yellow = 50-69%, Red = <50%
- Displays: "X% followed" + trade count

## Controls

| Control | Action |
|---------|--------|
| ◀ Button | Previous month |
| ▶ Button | Next month |
| P&L Button | Switch to P/L mode |
| Plan Button | Switch to Plan mode |
| Click day cell | Go to Trade Log (future: filter by date) |

## Monthly Statistics

Shows at top of calendar:
- **Total Trades** - Number of trades in the month
- **Net P/L** - Total profit/loss for the month
- **Days Traded** - Count of unique trading days
- **Days Plan Followed** - Days with ≥70% plan adherence

## Colors

### P&L Mode
- 🟢 **Green (#6DE7B5)** - Positive P/L
- 🟡 **Yellow (#FCD44B)** - Zero P/L
- 🔴 **Red (#FDA4A5)** - Negative P/L
- ⬜ **Gray** - No trades

### Plan Mode
- 🟢 **Green (#6DE7B5)** - ≥70% followed plan
- 🟡 **Yellow (#FCD44B)** - 50-69% followed plan
- 🔴 **Red (#FDA4A5)** - <50% followed plan
- ⬜ **Gray** - No trades

## Day Cell Layout

```
┌─────────────┐
│ 15          │  ← Day number (top-left)
│             │
│  +$250.00   │  ← P/L or Plan % (center)
│             │
│         [8] │  ← Trade count badge (bottom-right)
└─────────────┘
```

## Calendar Grid

```
Sun  Mon  Tue  Wed  Thu  Fri  Sat
 1    2    3    4    5    6    7
 8    9   10   11   12   13   14
15   16   17   18   19   20   21
22   23   24   25   26   27   28
29   30   31
```

- 7 columns (days of week)
- 5-6 rows (weeks in month)
- Cells: 150px × 100px

## Key Features

✅ Month-by-month navigation
✅ Dual P&L/Plan display modes
✅ Color-coded performance indicators
✅ Trade count badges on each day
✅ Monthly statistics summary
✅ Click to navigate to Trade Log
✅ Per-account data isolation
✅ Automatic theme support

## Code Location

```
File: RiskManagerControl.cs
Methods:
  - CreateCalendarPage()
  - RefreshCalendarPage()
  - CreateCalendarStatsPanel()
  - CreateCalendarGrid()
  - CreateCalendarDayCell()
```

## Usage Tips

💡 **Best Practices**:
- Use P&L mode to track profitability patterns
- Use Plan mode to track discipline
- Look for patterns (day of week, time of month)
- Watch for clusters of red days
- Celebrate strings of green days

⚠️ **Notes**:
- Only days with trades are clickable
- Calendar shows trades from selected account only
- Colors update automatically when toggling modes
- Statistics calculate only for visible month

## Related Features

- **Trade Log** - Detailed list of all trades
- **Trading Models** - Strategy tracking
- **Notes** - Daily trading notes
- **Dashboard** - Overall performance metrics

## Keyboard Shortcuts
(Not yet implemented - planned for future)

---

**Version**: 1.0.0  
**Last Updated**: February 2026
