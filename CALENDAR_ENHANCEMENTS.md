# Calendar Enhancements - Week Statistics and Theme Colors

## Overview

Enhanced the Trading Journal Calendar based on user feedback to include:
1. Theme-aware text colors
2. Color dot indicators for no-trade days with notes
3. Weekly statistics column

## Changes Made

### 1. Theme-Aware Text Colors

**Before:**
- Day numbers always used `Color.Black`
- P/L and plan labels always used `Color.Black`
- Did not adapt to different themes

**After:**
- Empty cells (no trades) use `TextWhite` for day numbers (theme-aware)
- Colored cells (with trades) use `Color.Black` for better contrast
- Adapts automatically to all 4 Risk Manager themes (Dark, Yellow, White, Blue)

**Code:**
```csharp
// Determine text color based on cell background
Color textColor = (tradeCount > 0) ? Color.Black : TextWhite;

var dayLabel = new Label
{
    Text = dayNumber.ToString(),
    ForeColor = textColor,  // Now theme-aware!
    // ...
};
```

### 2. Color Dot Indicators

**Purpose:** Show visual feedback on days with no trades but containing journal notes (indicating plan awareness/discipline even without trading).

**Implementation:**
- Green dot (●) appears on days with notes but no trades
- Indicates trader documented their thoughts/plan for the day
- Shows discipline and awareness even on non-trading days

**Code:**
```csharp
if (tradeCount == 0)
{
    var notes = TradingJournalService.Instance.GetNotes(accountNumber);
    var dayNotes = notes.Where(n => n.CreatedAt.Date == date.Date).ToList();
    
    if (dayNotes.Count > 0)
    {
        var dotLabel = new Label
        {
            Text = "●",
            ForeColor = Color.FromArgb(109, 231, 181), // Green
            // ...
        };
    }
}
```

**Visual:**
```
┌─────────────┐
│ 15          │  ← Day number (theme color)
│             │
│             │
│         ●   │  ← Green dot (has note, no trades)
└─────────────┘
```

### 3. Weekly Statistics Column

**New Feature:** An 8th column showing aggregated statistics for each week (calendar row).

**Metrics Displayed:**
- **Trades**: Total number of trades for that week
- **Plan**: Percentage of trades that followed the plan
- **W/L**: Win/Loss ratio (e.g., "5/2" = 5 wins, 2 losses)

**Visual Layout:**
```
┌─────────────────────────────────────────────────────────────────────────┐
│  Sun  Mon  Tue  Wed  Thu  Fri  Sat  │  Week Stats                      │
├──────────────────────────────────────┼──────────────────────────────────┤
│  1    2    3    4    5    6    7    │  Trades: 15                      │
│ [G]  [R]  [Y]  [G]  [G]  [R]        │  Plan: 80%                       │
│ +$150 -$75 $0  +$200 +$325 -$150    │  W/L: 10/5                       │
│  3    2    1    4    5    3         │                                  │
├──────────────────────────────────────┼──────────────────────────────────┤
│  8    9   10   11   12   13   14    │  Trades: 20                      │
│ [G]       [G]  [R]  [G]  [G]        │  Plan: 75%                       │
│ +$425     +$310 -$125 +$250 +$180   │  W/L: 14/6                       │
│  6         4    2    6    3         │                                  │
└──────────────────────────────────────┴──────────────────────────────────┘
```

**Code Structure:**
```csharp
// CreateCalendarGrid() now includes:
1. Track trades per week while creating day cells
2. Add "Week Stats" header column
3. Create weekly statistics panel for each row

// New method: CreateWeeklyStatsPanel()
- Calculates win/loss counts
- Calculates plan followed percentage
- Formats statistics display
- Uses theme colors (CardBackground, TextWhite)
```

**Implementation Details:**
```csharp
private Panel CreateWeeklyStatsPanel(List<JournalTrade> weekTrades)
{
    int tradeCount = weekTrades.Count;
    int winCount = weekTrades.Count(t => t.Outcome == "Win");
    int lossCount = weekTrades.Count(t => t.Outcome == "Loss");
    int planFollowedCount = weekTrades.Count(t => t.FollowedPlan);
    double planPct = (planFollowedCount * 100.0) / tradeCount;
    string winLossRatio = $"{winCount}/{lossCount}";
    
    // Display: Trades, Plan%, W/L ratio
}
```

## Layout Changes

### Before (7 columns):
```
Width: 7 × 150px = 1,050px
Columns: Sun, Mon, Tue, Wed, Thu, Fri, Sat
```

### After (8 columns):
```
Width: (7 × 150px) + 200px = 1,250px
Columns: Sun, Mon, Tue, Wed, Thu, Fri, Sat, Week Stats
```

## Color Scheme

### Day Cell Colors (unchanged):
- **Green (#6DE7B5)**: Profit or ≥70% plan followed
- **Yellow (#FCD44B)**: Breakeven or 50-69% plan followed
- **Red (#FDA4A5)**: Loss or <50% plan followed
- **Gray (CardBackground)**: No trades

### Indicator Dot:
- **Green (#6DE7B5)**: Note exists on day with no trades

### Weekly Stats Panel:
- **Background**: CardBackground (theme-aware)
- **Text**: TextWhite (theme-aware)

## Use Cases

### 1. Weekly Performance Tracking
Traders can quickly see:
- Which weeks were most active
- Which weeks had best plan adherence
- Which weeks had best win/loss ratio

### 2. Pattern Recognition
- Identify weeks with high trading volume
- Spot weeks with poor plan adherence
- Correlate plan adherence with win rates

### 3. Discipline Monitoring
- Green dots show days when trader documented thoughts even without trading
- Helps identify periods of good vs. poor discipline
- Encourages daily journaling practice

## Testing Checklist

- [ ] Calendar displays correctly in Dark theme
- [ ] Calendar displays correctly in Yellow theme
- [ ] Calendar displays correctly in White theme
- [ ] Calendar displays correctly in Blue theme
- [ ] Week Stats column shows correct trade counts
- [ ] Week Stats column shows correct plan percentages
- [ ] Week Stats column shows correct W/L ratios
- [ ] Green dots appear on days with notes but no trades
- [ ] Text colors are readable in all themes
- [ ] Weekly stats panels align with calendar rows
- [ ] Month navigation updates weekly stats correctly

## API Changes

### Modified Methods:
1. **CreateCalendarGrid()** - Now creates 8 columns instead of 7
2. **CreateCalendarDayCell()** - Now checks for notes and adds dot indicators

### New Methods:
1. **CreateWeeklyStatsPanel(List<JournalTrade>)** - Creates weekly statistics display

### No Breaking Changes:
- All existing functionality preserved
- Calendar still works with existing data
- No database/JSON format changes required

## Performance Considerations

### Additional Computations:
- **Per week**: Calculate win/loss counts and plan percentage (minimal cost)
- **Per day**: Check for notes (one additional query per day)

### Optimizations:
- Weekly stats calculated once per month change
- Note queries filtered by date for efficiency
- No impact on existing calendar performance

## Future Enhancements

Potential additions based on weekly stats:
1. Click weekly stats to filter Trade Log to that week
2. Highlight best/worst week of the month
3. Show weekly P/L in addition to W/L ratio
4. Add weekly average trade size
5. Show weekly volatility metrics
6. Export weekly stats to CSV

## Documentation Updates Needed

Files to update:
1. CALENDAR_IMPLEMENTATION.md - Add weekly stats section
2. CALENDAR_QUICK_REFERENCE.md - Add week stats column info
3. CALENDAR_VISUAL_COMPARISON.md - Update visual examples
4. CALENDAR_README.md - Mention weekly statistics feature

---

**Version**: 1.1.0  
**Date**: February 2026  
**Status**: Implemented ✅
