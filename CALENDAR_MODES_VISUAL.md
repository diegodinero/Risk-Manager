# Calendar Modes - Visual Comparison

## Side-by-Side Comparison

### Complete Calendar Layout

```
╔════════════════════════════════════════════════════════════════════════════╗
║                    P&L MODE                 vs                PLAN MODE     ║
╠════════════════════════════════════════════════════════════════════════════╣
║                                                                            ║
║  Weekly Stats Panel:                      Weekly Stats Panel:             ║
║  ┌──────────────────────┐                 ┌──────────────────────┐        ║
║  │ Trades: 15           │                 │ Trades: 15           │        ║
║  │ P&L: +$2,450.00      │                 │ Plan: 80%    ✓ 12/15 │        ║
║  │ W/L: 10/5            │                 │ W/L: 10/5            │        ║
║  │ Win%: 67%            │                 │                      │        ║
║  └──────────────────────┘                 └──────────────────────┘        ║
║                                                                            ║
║  Legend Panel:                            Legend Panel:                   ║
║  ┌─────────────────────────────┐          ┌─────────────────────────────┐ ║
║  │ Win Loss Ratio Legend:      │          │ Plan Followed Legend:       │ ║
║  │                             │          │                             │ ║
║  │ ● Profitable                │          │ ● ≥70% Followed             │ ║
║  │ ● Breakeven                 │          │ ● 50-69% Followed           │ ║
║  │ ● Loss                      │          │ ● <50% Followed             │ ║
║  │ ○ No Trades                 │          │ ○ No Trades                 │ ║
║  └─────────────────────────────┘          └─────────────────────────────┘ ║
║                                                                            ║
╚════════════════════════════════════════════════════════════════════════════╝
```

## Detailed Weekly Stats Comparison

### P&L Mode Weekly Stats

```
┌──────────────────────────────────────────────────────────┐
│                    Week 1 Stats                          │
├──────────────────────────────────────────────────────────┤
│                                                          │
│  Trades: 15            [Line 1, Left]                   │
│  P&L: +$2,450.00       [Line 2, Left]     Win%: 67%     │
│  W/L: 10/5             [Line 3, Left]                   │
│                                                          │
└──────────────────────────────────────────────────────────┘

Metrics Shown:
• Trades: Count of all trades this week
• P&L: Sum of NetPL (with currency formatting)
• W/L: Win count / Loss count
• Win%: (Win count / Total trades) × 100

Example Calculations:
- 15 trades total
- 10 wins, 5 losses
- Win% = (10/15) × 100 = 67%
- P&L = sum of all NetPL = +$2,450.00
```

### Plan Mode Weekly Stats

```
┌──────────────────────────────────────────────────────────┐
│                    Week 1 Stats                          │
├──────────────────────────────────────────────────────────┤
│                                                          │
│  Trades: 15            [Line 1, Left]                   │
│  Plan: 80%             [Line 2, Left]     ✓ 12/15       │
│  W/L: 10/5             [Line 3, Left]                   │
│                                                          │
└──────────────────────────────────────────────────────────┘

Metrics Shown:
• Trades: Count of all trades this week
• Plan: % of trades that followed plan
• W/L: Win count / Loss count
• Plan Ratio: Exact count with checkmark if ≥70%

Example Calculations:
- 15 trades total
- 12 followed plan, 3 did not
- Plan% = (12/15) × 100 = 80%
- Checkmark shows because 80% ≥ 70%
- Ratio displayed: "✓ 12/15"
```

## Legend Text Comparison

### P&L Mode Legend

```
═══════════════════════════════════════════════════════════
 Win Loss Ratio Legend:
───────────────────────────────────────────────────────────
 ● Profitable       = Day had positive net P&L (green)
 ● Breakeven        = Day had zero P&L (yellow)
 ● Loss             = Day had negative net P&L (pink)
 ○ No Trades        = No trades taken that day (gray)
═══════════════════════════════════════════════════════════
```

**Color Logic (Day Cells):**
```csharp
decimal netPL = dayTrades.Sum(t => t.NetPL);

if (netPL > 0)
    cellColor = Green;    // Profitable
else if (netPL == 0)
    cellColor = Yellow;   // Breakeven
else
    cellColor = Pink;     // Loss
```

### Plan Mode Legend

```
═══════════════════════════════════════════════════════════
 Plan Followed Legend:
───────────────────────────────────────────────────────────
 ● ≥70% Followed    = Most trades followed plan (green)
 ● 50-69% Followed  = Some trades followed plan (yellow)
 ● <50% Followed    = Few trades followed plan (pink)
 ○ No Trades        = No trades taken that day (gray)
═══════════════════════════════════════════════════════════
```

**Color Logic (Day Cells):**
```csharp
int yesCount = dayTrades.Count(t => t.FollowedPlan);
double planPct = (yesCount * 100.0) / tradeCount;

if (planPct >= 70.0)
    cellColor = Green;    // ≥70% followed
else if (planPct >= 50.0)
    cellColor = Yellow;   // 50-69% followed
else
    cellColor = Pink;     // <50% followed
```

## Use Case Examples

### Use Case 1: Profitability Analysis

**Goal**: Find which weeks were profitable

**Steps**:
1. Click [P&L] button
2. Look at weekly stats P&L column
3. Identify weeks with positive P&L
4. Check Win% to see win rate

**What You See**:
```
Week 1: P&L: +$2,450.00, Win%: 67%  ✓ Good
Week 2: P&L: -$320.00,   Win%: 40%  ✗ Poor
Week 3: P&L: +$1,890.00, Win%: 75%  ✓ Excellent
Week 4: P&L: $0.00,      Win%: 50%  ≈ Break-even
```

### Use Case 2: Discipline Analysis

**Goal**: Check plan adherence over time

**Steps**:
1. Click [Plan] button
2. Look at weekly stats Plan % column
3. Look for checkmarks (≥70% adherence)
4. Count how many weeks had good discipline

**What You See**:
```
Week 1: Plan: 80%,  ✓ 12/15  ✓ Good discipline
Week 2: Plan: 45%,    5/11   ✗ Poor discipline
Week 3: Plan: 72%,  ✓ 13/18  ✓ Good discipline
Week 4: Plan: 60%,    9/15   ≈ Needs improvement
```

## Metric Formulas

### P&L Mode Metrics

| Metric | Formula | Example |
|--------|---------|---------|
| Trades | Count of all trades | 15 trades |
| P&L | Σ(NetPL) | +$2,450.00 |
| W/L | Win count / Loss count | 10/5 |
| Win% | (Wins / Total) × 100 | 67% |

### Plan Mode Metrics

| Metric | Formula | Example |
|--------|---------|---------|
| Trades | Count of all trades | 15 trades |
| Plan | (Followed / Total) × 100 | 80% |
| W/L | Win count / Loss count | 10/5 |
| Ratio | "✓ Followed / Total" (if ≥70%) | ✓ 12/15 |

## Color Codes

### Standard Colors (Used in Both Modes)

```
Green  = Color.FromArgb(109, 231, 181)  // #6DE7B5
Yellow = Color.FromArgb(252, 212, 75)   // #FCD44B
Pink   = Color.FromArgb(253, 164, 165)  // #FDA4A5
```

### When Each Color Appears

**P&L Mode:**
- Green: netPL > 0 (profitable)
- Yellow: netPL == 0 (breakeven)
- Pink: netPL < 0 (loss)

**Plan Mode:**
- Green: planPct >= 70% (good adherence)
- Yellow: 50% <= planPct < 70% (moderate adherence)
- Pink: planPct < 50% (poor adherence)

## Toggle Behavior

### Switching from Plan to P&L

**What Changes:**
1. Legend title: "Plan Followed Legend" → "Win Loss Ratio Legend"
2. Legend green: "≥70% Followed" → "Profitable"
3. Legend yellow: "50-69% Followed" → "Breakeven"
4. Legend pink: "<50% Followed" → "Loss"
5. Weekly stats line 2 left: "Plan: 80%" → "P&L: +$2,450.00"
6. Weekly stats line 2 right: "✓ 12/15" → "Win%: 67%"

**What Stays the Same:**
1. Legend "No Trades" text
2. Weekly stats "Trades: 15"
3. Weekly stats "W/L: 10/5"
4. Day cell colors (same color scheme)

### Switching from P&L to Plan

**What Changes:**
1. Legend title: "Win Loss Ratio Legend" → "Plan Followed Legend"
2. Legend green: "Profitable" → "≥70% Followed"
3. Legend yellow: "Breakeven" → "50-69% Followed"
4. Legend pink: "Loss" → "<50% Followed"
5. Weekly stats line 2 left: "P&L: +$2,450.00" → "Plan: 80%"
6. Weekly stats line 2 right: "Win%: 67%" → "✓ 12/15"

**What Stays the Same:**
1. Legend "No Trades" text
2. Weekly stats "Trades: 15"
3. Weekly stats "W/L: 10/5"
4. Day cell colors (same color scheme)

## Best Practices

### When to Use P&L Mode
- Analyzing profitability
- Checking win rates
- Calculating earnings
- Comparing profitable vs unprofitable periods
- Tracking financial goals

### When to Use Plan Mode
- Reviewing discipline
- Tracking plan adherence
- Identifying when you deviate from strategy
- Building better trading habits
- Improving consistency

---

**Visual Reference Version**: 1.3.0  
**Date**: February 2026  
**Status**: Complete ✅
