# Calendar Mode-Specific UI - Implementation Guide

## Overview

The Trading Journal Calendar now displays different metrics and legend text based on whether the user is in P&L mode or Plan mode, providing context-appropriate information.

## Changes Implemented

### 1. Dynamic Weekly Statistics Panel

The weekly statistics panel now shows different metrics depending on the selected mode.

#### P&L Mode (showPlanMode = false)

Shows financial performance metrics:

```
┌──────────────────────┐
│ Trades: 15           │  ← Total trades
│ P&L: +$2,450.00      │  ← Weekly P&L total (NEW!)
│ W/L: 10/5            │  ← Win/Loss ratio
│ Win%: 67%            │  ← Win percentage (NEW!)
└──────────────────────┘
```

**Metrics Shown:**
- **Trades**: Total number of trades for the week
- **P&L**: Sum of all trade NetPL for the week (formatted as currency)
- **W/L**: Win count / Loss count ratio
- **Win%**: Percentage of trades that were winners

#### Plan Mode (showPlanMode = true)

Shows discipline and plan adherence metrics:

```
┌──────────────────────┐
│ Trades: 15           │  ← Total trades
│ Plan: 80%    ✓ 12/15 │  ← Plan % with checkmark and ratio
│ W/L: 10/5            │  ← Win/Loss ratio
└──────────────────────┘
```

**Metrics Shown:**
- **Trades**: Total number of trades for the week
- **Plan**: Percentage of trades that followed the plan
- **W/L**: Win count / Loss count ratio
- **Plan Ratio**: Exact count with checkmark if ≥70% (e.g., "✓ 12/15")

### 2. Dynamic Legend Panel

The legend panel title and descriptions change based on the mode to explain what the calendar colors represent.

#### P&L Mode Legend

```
Win Loss Ratio Legend:

● Profitable    ● Breakeven    ● Loss    ○ No Trades
```

**Meaning:**
- **Green (●)**: Day had net positive P&L (profitable)
- **Yellow (●)**: Day had zero P&L (breakeven)
- **Pink (●)**: Day had net negative P&L (loss)
- **Empty (○)**: No trades taken that day

#### Plan Mode Legend

```
Plan Followed Legend:

● ≥70% Followed    ● 50-69% Followed    ● <50% Followed    ○ No Trades
```

**Meaning:**
- **Green (●)**: ≥70% of trades followed the plan
- **Yellow (●)**: 50-69% of trades followed the plan
- **Pink (●)**: <50% of trades followed the plan
- **Empty (○)**: No trades taken that day

## Technical Implementation

### Modified Methods

#### CreateWeeklyStatsPanel()

**Before:** Always showed plan-related metrics
**After:** Conditionally shows metrics based on `showPlanMode`

```csharp
private Panel CreateWeeklyStatsPanel(List<JournalTrade> weekTrades)
{
    // Calculate all needed statistics
    int winCount = weekTrades.Count(t => t.Outcome == "Win");
    int lossCount = weekTrades.Count(t => t.Outcome == "Loss");
    decimal weeklyPL = weekTrades.Sum(t => t.NetPL);
    double winPct = (winCount * 100.0) / tradeCount;
    int planFollowedCount = weekTrades.Count(t => t.FollowedPlan);
    double planPct = (planFollowedCount * 100.0) / tradeCount;
    
    // Show different labels based on mode
    if (showPlanMode)
    {
        // Plan mode: Plan %, W/L, Plan ratio
    }
    else
    {
        // P&L mode: P&L total, W/L, Win%
    }
}
```

**New Calculations:**
- `weeklyPL`: Sum of NetPL for all trades in the week
- `winPct`: Percentage of trades that are wins

**Label Positioning:**
- Line 1 (Y=10): Trades count (always shown)
- Line 2 (Y=35): P&L total OR Plan % (mode-dependent)
- Line 3 (Y=60): W/L ratio (always shown)
- Line 2 Right (X=100, Y=35): Win% OR Plan ratio (mode-dependent)

#### CreateCalendarLegendPanel()

**Before:** Always showed "Plan Followed Legend"
**After:** Changes title and descriptions based on `showPlanMode`

```csharp
private Panel CreateCalendarLegendPanel()
{
    // Dynamic title
    Text = showPlanMode ? "Plan Followed Legend:" : "Win Loss Ratio Legend:"
    
    // Dynamic green text
    Text = showPlanMode ? "≥70% Followed" : "Profitable"
    
    // Dynamic yellow text
    Text = showPlanMode ? "50-69% Followed" : "Breakeven"
    
    // Dynamic pink text
    Text = showPlanMode ? "<50% Followed" : "Loss"
    
    // "No Trades" stays the same in both modes
}
```

## Usage Examples

### Scenario 1: Analyzing Profitability

User wants to see which weeks were profitable:

1. Click **[P&L]** button
2. Calendar shows:
   - Day cells colored by P&L (green=profit, yellow=break-even, red=loss)
   - Weekly stats show P&L totals and Win%
   - Legend explains: "Win Loss Ratio Legend"
3. User can quickly identify profitable weeks and days

### Scenario 2: Analyzing Discipline

User wants to see how well they followed their trading plan:

1. Click **[Plan]** button
2. Calendar shows:
   - Day cells colored by plan adherence (green=≥70%, yellow=50-69%, red=<50%)
   - Weekly stats show Plan % and plan ratios
   - Legend explains: "Plan Followed Legend"
3. User can track discipline and see which weeks had good plan adherence

## Color Consistency

The calendar day cell colors remain consistent with the legend:

### P&L Mode
```csharp
if (netPL > 0)
    cellColor = Color.FromArgb(109, 231, 181); // Green - Profitable
else if (netPL == 0)
    cellColor = Color.FromArgb(252, 212, 75); // Yellow - Breakeven
else
    cellColor = Color.FromArgb(253, 164, 165); // Red - Loss
```

### Plan Mode
```csharp
if (planPct >= 70.0)
    cellColor = Color.FromArgb(109, 231, 181); // Green - ≥70%
else if (planPct >= 50.0)
    cellColor = Color.FromArgb(252, 212, 75); // Yellow - 50-69%
else
    cellColor = Color.FromArgb(253, 164, 165); // Red - <50%
```

## Benefits

### User Experience
- **Contextual Information**: Legend and stats always explain current view
- **No Confusion**: Clear what colors mean in each mode
- **Better Analysis**: Right metrics for the task at hand
- **Seamless Switching**: Instant update when changing modes

### For Traders
- **P&L Mode**: Focus on profitability analysis
  - Which weeks made money?
  - What's my win rate?
  - How much did I make/lose?

- **Plan Mode**: Focus on discipline analysis
  - Am I following my plan?
  - Which weeks had good discipline?
  - How many trades followed the plan?

## Testing Checklist

### P&L Mode Testing
- [ ] Toggle to P&L mode
- [ ] Verify legend title: "Win Loss Ratio Legend:"
- [ ] Verify legend items: Profitable, Breakeven, Loss, No Trades
- [ ] Verify weekly stats show: Trades, P&L, W/L, Win%
- [ ] Verify P&L formatting (currency with +/- signs)
- [ ] Verify Win% calculation is correct
- [ ] Verify day cells colored by P&L

### Plan Mode Testing
- [ ] Toggle to Plan mode
- [ ] Verify legend title: "Plan Followed Legend:"
- [ ] Verify legend items: ≥70%, 50-69%, <50%, No Trades
- [ ] Verify weekly stats show: Trades, Plan, W/L, Plan ratio
- [ ] Verify checkmark appears when Plan ≥70%
- [ ] Verify plan ratio format (e.g., "✓ 12/15")
- [ ] Verify day cells colored by plan adherence

### Mode Switching
- [ ] Switch between modes multiple times
- [ ] Verify legend updates immediately
- [ ] Verify weekly stats update immediately
- [ ] Verify no visual glitches
- [ ] Verify colors remain consistent with legend

## Future Enhancements

### Potential Additions
1. **Tooltip on hover**: Show detailed breakdown on weekly stats hover
2. **Click stats to filter**: Click weekly stats to filter Trade Log
3. **More metrics**: Additional metrics based on user feedback
4. **Export**: Export weekly stats to CSV/Excel
5. **Comparison**: Compare weeks side-by-side

---

**Version**: 1.3.0  
**Date**: February 2026  
**Status**: Complete ✅  
**Testing**: Ready for validation in Quantower
