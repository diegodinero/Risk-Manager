# Calendar Color Coding Visual Guide

## Overview

This visual guide demonstrates the color-coded weekly statistics cells and reordered Plan mode monthly statistics with examples.

## Weekly Stats Cell Colors

### Plan Mode Color Coding

Weekly statistics cells are colored based on plan adherence percentage.

#### Excellent Adherence (Green - ≥70%)

```
┌─────────────────────┐
│                     │  Background: #6DE7B5 (Green)
│   Trades: 20        │  Text: TextWhite
│   Plan: 85%         │  Bold percentage
│   W/L: 15/5         │
│   ✓ 17/20           │  Checkmark shown
│                     │
└─────────────────────┘

Week Performance: 17 out of 20 trades followed plan (85%)
Visual Indicator: Green background + Checkmark
```

#### Moderate Adherence (Yellow - 50-69%)

```
┌─────────────────────┐
│                     │  Background: #FCD44B (Yellow)
│   Trades: 12        │  Text: TextWhite
│   Plan: 58%         │  Bold percentage
│   W/L: 6/6          │
│   7/12              │  No checkmark
│                     │
└─────────────────────┘

Week Performance: 7 out of 12 trades followed plan (58%)
Visual Indicator: Yellow background (warning)
```

#### Poor Adherence (Pink - <50%)

```
┌─────────────────────┐
│                     │  Background: #FDA4A5 (Pink)
│   Trades: 15        │  Text: TextWhite
│   Plan: 27%         │  Bold percentage
│   W/L: 4/11         │
│   4/15              │  No checkmark
│                     │
└─────────────────────┘

Week Performance: 4 out of 15 trades followed plan (27%)
Visual Indicator: Pink background (alert)
```

#### No Trades

```
┌─────────────────────┐
│                     │  Background: CardBackground
│                     │  (Theme-based color)
│     (Empty)         │
│                     │
│                     │
└─────────────────────┘

Week Performance: No trading activity
Visual Indicator: Default background (no coloring)
```

### P&L Mode (No Color Coding)

In P&L mode, weekly cells keep the default CardBackground:

```
┌─────────────────────┐
│                     │  Background: CardBackground
│   Trades: 15        │  Text: TextWhite
│   P&L: +$2,450.00   │  Regular font
│   W/L: 10/5         │
│   Win%: 67%         │  Bold percentage
│                     │
└─────────────────────┘

Note: Weekly cell coloring is Plan mode exclusive
```

## Monthly Stats Display

### Plan Mode - Before and After

#### Before (Original Layout)

```
Monthly stats: 15 Days Traded and 5 Days Followed
               ^^                 ^^
               Blue highlight     Blue highlight
               Volume first       Discipline second
```

**Issues**:
- Volume emphasized before discipline
- Days Followed not visually differentiated by performance
- Counter to trading psychology best practices

#### After (Enhanced Layout)

**Excellent Month (≥70% Adherence)**:
```
Monthly stats: 16 Days Followed then 20 Days Traded
               ^^                    ^^
               Green (#6DE7B5)       Blue (#2980B9)
               80% adherence         Volume metric
```

**Moderate Month (50-69% Adherence)**:
```
Monthly stats: 12 Days Followed then 20 Days Traded
               ^^                    ^^
               Yellow (#FCD44B)      Blue (#2980B9)
               60% adherence         Volume metric
```

**Poor Month (<50% Adherence)**:
```
Monthly stats: 6 Days Followed then 20 Days Traded
               ^^                   ^^
               Pink (#FDA4A5)       Blue (#2980B9)
               30% adherence        Volume metric
```

**No Adherence Data**:
```
Monthly stats: 0 Days Followed then 20 Days Traded
               ^^                   ^^
               White (TextWhite)    Blue (#2980B9)
               0% adherence         Volume metric
```

### P&L Mode (Unchanged)

```
Monthly stats: +$2,450.00 for the month then 15 Days Traded
               ^^^^^^^^^^                    ^^
               Green/Red based on value      Blue highlight
```

## Complete Calendar View Examples

### Excellent Trading Month (Plan Mode)

```
Trading Calendar                          [P&L] [Plan*]
◀  February 2026  ▶

Monthly stats: 18 Days Followed then 21 Days Traded
               ^^green                  ^^blue

┌────────────────────────────────────────────────────┐
│ Sun  Mon  Tue  Wed  Thu  Fri  Sat │ Week Stats     │
├────────────────────────────────────┼────────────────┤
│      1    2    3    4    5    6   │  Trades: 15    │
│ ○   🟢   🟢   🟢   🟢   🟢   ○   │  Plan: 80%     │ ← Green
│     $200 $325 $425 $180 $220      │  W/L: 10/5     │
│                                    │  ✓ 12/15       │
├────────────────────────────────────┼────────────────┤
│  7   8    9    10   11   12   13  │  Trades: 10    │
│ ○   🟢   🟢   🟢   🟢   🟢   ○   │  Plan: 90%     │ ← Green
│     $150 $400 $275 $185 $330      │  W/L: 8/2      │
│                                    │  ✓ 9/10        │
└────────────────────────────────────┴────────────────┘

Plan Followed Legend:
●  ≥70% Followed    ●  50-69% Followed    ●  <50% Followed    ○  No Trades
```

### Mixed Performance Month (Plan Mode)

```
Trading Calendar                          [P&L] [Plan*]
◀  February 2026  ▶

Monthly stats: 12 Days Followed then 21 Days Traded
               ^^yellow                 ^^blue

┌────────────────────────────────────────────────────┐
│ Sun  Mon  Tue  Wed  Thu  Fri  Sat │ Week Stats     │
├────────────────────────────────────┼────────────────┤
│      1    2    3    4    5    6   │  Trades: 15    │
│ ○   🟢   🟢   🔴   🟢   🟢   ○   │  Plan: 60%     │ ← Yellow
│     $200 $325 -$150 $180 $220     │  W/L: 10/5     │
│                                    │  9/15          │
├────────────────────────────────────┼────────────────┤
│  7   8    9    10   11   12   13  │  Trades: 8     │
│ ○   🟢   🔴   🔴   🔴   🟢   ○   │  Plan: 38%     │ ← Pink
│     $150 -$200 -$175 -$125 $330   │  W/L: 3/5      │
│                                    │  3/8           │
└────────────────────────────────────┴────────────────┘

Plan Followed Legend:
●  ≥70% Followed    ●  50-69% Followed    ●  <50% Followed    ○  No Trades
```

### Challenging Month (Plan Mode)

```
Trading Calendar                          [P&L] [Plan*]
◀  February 2026  ▶

Monthly stats: 6 Days Followed then 21 Days Traded
               ^^pink                   ^^blue

┌────────────────────────────────────────────────────┐
│ Sun  Mon  Tue  Wed  Thu  Fri  Sat │ Week Stats     │
├────────────────────────────────────┼────────────────┤
│      1    2    3    4    5    6   │  Trades: 15    │
│ ○   🔴   🔴   🔴   🔴   🟢   ○   │  Plan: 33%     │ ← Pink
│     -$150 -$200 -$125 -$75 $220   │  W/L: 5/10     │
│                                    │  5/15          │
├────────────────────────────────────┼────────────────┤
│  7   8    9    10   11   12   13  │  Trades: 10    │
│ ○   🔴   🔴   🔴   🔴   🔴   ○   │  Plan: 20%     │ ← Pink
│     -$100 -$200 -$175 -$225 -$150 │  W/L: 2/8      │
│                                    │  2/10          │
└────────────────────────────────────┴────────────────┘

Plan Followed Legend:
●  ≥70% Followed    ●  50-69% Followed    ●  <50% Followed    ○  No Trades
```

## Color Palette Reference

### Plan Adherence Colors

```
Green (Excellent - ≥70%):
─────────────────────────
RGB: (109, 231, 181)
Hex: #6DE7B5
Use: Day cells, weekly cells, monthly "Days Followed"

Yellow (Moderate - 50-69%):
─────────────────────────
RGB: (252, 212, 75)
Hex: #FCD44B
Use: Day cells, weekly cells, monthly "Days Followed"

Pink (Poor - <50%):
─────────────────────────
RGB: (253, 164, 165)
Hex: #FDA4A5
Use: Day cells, weekly cells, monthly "Days Followed"
```

### Highlight Colors

```
Blue (Volume Metrics):
─────────────────────────
RGB: (41, 128, 185)
Hex: #2980B9
Use: Navigation arrows, toggle buttons, "Days Traded" highlight
```

### P&L Colors

```
Green (Profit):
─────────────────────────
RGB: (110, 231, 183)
Hex: #6DE7B7
Use: Positive P&L amounts

Red (Loss):
─────────────────────────
RGB: (253, 164, 165)
Hex: #FDA4A5
Use: Negative P&L amounts
```

## Quick Reference Chart

| Element | Plan Mode | P&L Mode |
|---------|-----------|----------|
| **Day Cells** | Colored by plan % | Colored by P&L |
| **Weekly Cells** | Colored by plan % | CardBackground |
| **Monthly "Days Followed"** | Colored by plan % | N/A |
| **Monthly "Days Traded"** | Blue highlight | Blue highlight |
| **Monthly P&L** | N/A | Green/Red by value |

## Interpretation Guide

### Reading Weekly Colors

**All Green Weeks**:
- Excellent consistency
- Plan discipline maintained
- Focus on maintaining standards

**Mix of Green and Yellow**:
- Generally good adherence
- Some inconsistency
- Identify yellow weeks for improvement

**Presence of Pink Weeks**:
- Significant plan deviations
- Review trading logs for those weeks
- Identify root causes

**Mostly Pink Weeks**:
- Poor overall adherence
- Review trading plan viability
- Consider plan adjustments or stricter discipline

### Reading Monthly Stats Color

**Green "Days Followed"**:
- Excellent monthly discipline (≥70%)
- Trading plan is working well
- Continue current approach

**Yellow "Days Followed"**:
- Moderate monthly discipline (50-69%)
- Room for improvement
- Review plan adherence strategies

**Pink "Days Followed"**:
- Poor monthly discipline (<50%)
- Requires immediate attention
- Review plan viability and commitment

## Usage Tips

### For Quick Scanning

1. **Scan Weekly Column**: Look for dominant colors
   - Mostly green = good month
   - Mostly yellow = moderate month
   - Mostly pink = problematic month

2. **Check Monthly Stat**: Confirm overall performance
   - Color matches weekly pattern
   - Single metric for month overview

3. **Compare Months**: Track color trends over time
   - Improving = pink → yellow → green
   - Declining = green → yellow → pink

### For Detailed Analysis

1. **Identify Patterns**: Which weeks tend to be problematic?
   - First week of month?
   - After big wins/losses?
   - Specific market conditions?

2. **Correlate with Results**: Do green weeks show better W/L?
   - Calculate average W/L for each color
   - Validate that plan adherence helps performance

3. **Track Improvement**: Use as motivation tool
   - Goal: Turn all weeks green
   - Celebrate green week streaks
   - Analyze what went right in green weeks

## Summary

The color-coded calendar provides instant visual feedback:

✅ **Weekly cells** show adherence at a glance
✅ **Monthly stats** show overall performance
✅ **Consistent colors** across all elements
✅ **Self-documenting** interface
✅ **Actionable insights** for improvement

Use the colors to identify patterns, maintain discipline, and improve trading performance.
