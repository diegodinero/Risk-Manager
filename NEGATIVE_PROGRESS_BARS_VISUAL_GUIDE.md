# Visual Comparison: Progress Bar Colors for Negative Values

## Before the Change

When showing losses approaching the limit:

```
Percentage of Loss Limit Reached:
┌─────────────────────────────────────────┐
│ 0% ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 100% │
│ 🟢━━━━━━━━━━🟡━━━━━━🟠━━━━━━━━━━🔴       │
│ Green    Yellow  Orange       Red       │
│ 0-50%   50-70%  70-90%       90-100%    │
└─────────────────────────────────────────┘
```

**Problem:** Small losses showed green, which is confusing since green typically means "good" or "success"

## After the Change

When showing losses approaching the limit:

```
Percentage of Loss Limit Reached:
┌─────────────────────────────────────────┐
│ 0% ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━ 100% │
│ 🟡━━━━━━━━━━━━🟠━━━━━━━━━━━━━━━🔴        │
│ Yellow         Orange           Red      │
│ 0-40%         40-70%           70-100%   │
└─────────────────────────────────────────┘
```

**Improvement:** All losses now show warning colors (yellow/orange/red), making it immediately clear that any loss is a warning condition

## Example Scenarios

### Scenario 1: Small Loss (10% of limit)
- **Before:** 🟢 Green progress bar
- **After:** 🟡 Yellow progress bar
- **Impact:** Clearer warning that there is a loss

### Scenario 2: Moderate Loss (60% of limit)
- **Before:** 🟡 Yellow progress bar
- **After:** 🟠 Orange progress bar
- **Impact:** Earlier escalation to orange warning level

### Scenario 3: Large Loss (80% of limit)
- **Before:** 🟠 Orange progress bar
- **After:** 🔴 Red progress bar (critical)
- **Impact:** Earlier critical warning when approaching limit

### Scenario 4: Critical Loss (95% of limit)
- **Before:** 🔴 Red progress bar (critical)
- **After:** 🔴 Red progress bar (critical)
- **Impact:** No change - already at critical level

## Color Specifications

### Yellow (Warning)
- RGB: `Color.FromArgb(255, 193, 7)`
- Usage: 0-40% of loss limit
- Meaning: Initial warning - small loss detected

### Orange (Elevated Warning)
- RGB: `Color.FromArgb(255, 133, 27)`
- Usage: 40-70% of loss limit
- Meaning: Moderate loss - attention required

### Red (Critical)
- RGB: `Color.FromArgb(220, 53, 69)`
- Usage: 70-100% of loss limit
- Meaning: Significant loss - immediate action needed

## Applies To

This color scheme applies to all negative P&L progress bars in:

1. **Account Statistics Grid**
   - Gross P&L column (when losses approach Daily Loss Limit)
   - Open P&L column (when losses approach Position Loss Limit)

2. **Type Summary Grid**
   - Total P&L column (aggregated Daily Loss Limits)
   - Open P&L column (aggregated Position Loss Limits)

## Note on Positive Values

Progress bars for **positive** values (profits) remain unchanged:
- Light Green → Medium Green → Bright Green
- This continues to indicate progress toward profit targets
