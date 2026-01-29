# Feature Toggle Update Fix

## Problem Statement

User reported: "The greyed out looks good. However the functionality of enabled vs disabled is broken. It should technically behave just like the x overlay before. When a feature is disabled there was an x over the corresponding risk card. When it was enabled it was visible. This greyed out behavior should do the same."

### Expected Behavior
- **Feature ENABLED**: Card shows normally (no greying, no red X)
- **Feature DISABLED**: Card shows greyed out with red X in header

### Actual Behavior (Bug)
- Cards would get stuck in their initial state
- Once a card was disabled (greyed out), it would stay disabled even after the feature was re-enabled
- The refresh mechanism wasn't detecting cards that had been disabled before

## Root Cause

The bug was in the `RefreshLabelsInControl()` method at line 11428.

### The Issue

When a card is initially created, its Tag is set to a `Func<bool>` (the feature checker):
```csharp
Tag = () => IsFeatureEnabled(s => s.PositionsEnabled)
```

When the card is disabled, `SetCardDisabled()` wraps the Tag in an anonymous object:
```csharp
cardPanel.Tag = new { 
    FeatureChecker = featureChecker, 
    IsDisabled = true, 
    OriginalColors = originalColors 
};
```

The refresh code checked:
```csharp
if (control is Panel panel && panel.Tag is Func<bool>)
{
    UpdateCardOverlay(panel);
}
```

**Problem**: `panel.Tag is Func<bool>` returns FALSE for wrapped tags!

### Impact
1. Initial card creation → Tag is `Func<bool>` → UpdateCardOverlay called ✓
2. Feature disabled → Card disabled, Tag wrapped → UpdateCardOverlay called ✓
3. **Feature re-enabled → Refresh called → Tag is wrapped → Check fails → UpdateCardOverlay NOT called ✗**

Result: Card stays disabled forever!

## Solution

Updated `RefreshLabelsInControl()` to detect BOTH types of tags:

### Before (Broken)
```csharp
// Check if this is a card panel with feature overlay support
if (control is Panel panel && panel.Tag is Func<bool>)
{
    // Update the overlay state for this card
    UpdateCardOverlay(panel);
}
```

### After (Fixed)
```csharp
// Check if this is a card panel with feature overlay support
if (control is Panel panel && panel.Tag != null)
{
    // Check if Tag is directly a feature checker OR wrapped in an anonymous object
    bool hasFeatureChecker = false;
    
    if (panel.Tag is Func<bool>)
    {
        hasFeatureChecker = true;
    }
    else
    {
        // Check if Tag has FeatureChecker property (wrapped state)
        var featureCheckerProp = panel.Tag.GetType().GetProperty("FeatureChecker");
        if (featureCheckerProp != null && featureCheckerProp.GetValue(panel.Tag) is Func<bool>)
        {
            hasFeatureChecker = true;
        }
    }
    
    if (hasFeatureChecker)
    {
        // Update the overlay state for this card
        UpdateCardOverlay(panel);
    }
}
```

## Why UpdateCardOverlay Already Handled It

Note that `UpdateCardOverlay()` itself already correctly handled both tag types (lines 11074-11091):

```csharp
if (cardPanel.Tag != null)
{
    // Try to get FeatureChecker from anonymous object
    var featureCheckerProp = cardPanel.Tag.GetType().GetProperty("FeatureChecker");
    if (featureCheckerProp != null)
    {
        featureChecker = featureCheckerProp.GetValue(cardPanel.Tag) as Func<bool>;
        // ... check disabled state ...
    }
    else
    {
        // Tag is directly the feature checker (not disabled)
        featureChecker = cardPanel.Tag as Func<bool>;
    }
}
```

The problem was just that `RefreshLabelsInControl()` wasn't CALLING `UpdateCardOverlay()` for wrapped tags!

## Testing

### Test Case 1: Enable → Disable
1. Start with feature enabled (e.g., Positions)
2. Card shows normally ✓
3. Disable feature via checkbox
4. Card shows greyed out with red X ✓

### Test Case 2: Disable → Enable (The Bug Case)
1. Start with feature disabled
2. Card shows greyed out with red X ✓
3. Enable feature via checkbox
4. **Before fix**: Card stays greyed out with X ✗
5. **After fix**: Card shows normally ✓

### Test Case 3: Multiple Toggles
1. Enable → Disable → Enable → Disable
2. Card should update each time ✓

### Test Case 4: Multiple Cards
1. Disable multiple features
2. Enable one at a time
3. Each card should update independently ✓

## Technical Details

### State Transitions

#### Enabled → Disabled
1. User unchecks feature checkbox
2. Settings saved with feature = false
3. Refresh called → RefreshLabelsInControl
4. Tag detected (direct OR wrapped) → UpdateCardOverlay called
5. UpdateCardOverlay checks: shouldBeDisabled = !false = true
6. SetCardDisabled called → red X shown, opacity reduced, Tag wrapped

#### Disabled → Enabled
1. User checks feature checkbox
2. Settings saved with feature = true
3. Refresh called → RefreshLabelsInControl
4. **Before fix**: Wrapped tag not detected → UpdateCardOverlay not called → STUCK
5. **After fix**: Wrapped tag detected → UpdateCardOverlay called
6. UpdateCardOverlay checks: shouldBeDisabled = !true = false, currentlyDisabled = true
7. SetCardEnabled called → red X hidden, opacity restored, Tag unwrapped

### Tag State Machine

```
Initial Creation
    ↓
Tag = Func<bool>
    ↓
Feature Disabled
    ↓
Tag = { FeatureChecker, IsDisabled=true, OriginalColors }
    ↓
Feature Enabled (After Fix)
    ↓
Tag = Func<bool>  (unwrapped by SetCardEnabled)
    ↓
Feature Disabled Again
    ↓
Tag = { FeatureChecker, IsDisabled=true, OriginalColors }
```

## Related Code

### Files Modified
- **RiskManagerControl.cs** - `RefreshLabelsInControl()` method

### Related Methods
- `UpdateCardOverlay()` - Checks feature state and applies/removes disabled state
- `SetCardDisabled()` - Wraps Tag with disabled state info
- `SetCardEnabled()` - Unwraps Tag back to feature checker
- `RefreshLabelsInControl()` - Recursively refreshes labels and card states
- `RefreshRiskOverviewPanel()` - Entry point for panel refresh

## Conclusion

The fix ensures that `UpdateCardOverlay()` is called for ALL cards with feature checkers during refresh, regardless of whether they've been disabled before. This allows cards to properly transition between enabled and disabled states when feature toggles are changed.

The solution is minimal and surgical - it only adds the logic to detect wrapped tags in the refresh method, while all other card state management logic remains unchanged.
