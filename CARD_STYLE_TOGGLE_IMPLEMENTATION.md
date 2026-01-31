# Card Style Toggle Implementation

## Overview
This implementation adds the ability to toggle between two different visual styles for disabled Risk Overview cards using the "Use Greyed Out Style for Disabled Cards" checkbox in General Settings.

## Problem Statement
Previously, the Risk Manager had two separate implementations:
1. **Master branch**: Greyed out text with x in header (reduced opacity approach)
2. **copilot/fix-feature-toggle-functionality branch**: Overlay with large centered x

The user wanted to be able to toggle between these two styles using a checkbox in General Settings, but the cards were generated differently in each approach.

## Solution
Added conditional logic (if-else) in the `SetCardDisabled` method that checks the `UseGreyedOutCardStyle` setting and applies the appropriate style.

## Implementation Details

### 1. Two Style Approaches

#### Style A: Greyed Out Style (Default - Checked)
**When to use**: Default style, works well with dark themes
**Visual characteristics**:
- Small red ✖ appears in the top-right corner of the card header (28pt font)
- Card content reduced to 40% opacity
- Content remains visible but clearly disabled
- No overlay panel

**Implementation**: `ApplyGreyedOutStyle()` method
```csharp
private void ApplyGreyedOutStyle(Panel cardPanel)
{
    // Show X in header
    var header = cardPanel.Controls.OfType<CustomCardHeaderControl>().FirstOrDefault();
    if (header != null)
    {
        header.SetDisabled(true);
    }
    
    // Reduce opacity of content to 40%
    foreach (Control control in cardPanel.Controls)
    {
        if (control != header)
        {
            SetControlOpacity(control, 0.4);
        }
    }
}
```

#### Style B: Overlay Style (Unchecked)
**When to use**: Recommended for White theme for better visibility
**Visual characteristics**:
- Semi-transparent dark overlay covers the entire card (40% opacity)
- Large red ✖ centered on the overlay (72pt font, bright red)
- More dramatic visual indication
- Content partially obscured by overlay

**Implementation**: `ApplyOverlayStyle()` method
```csharp
private void ApplyOverlayStyle(Panel cardPanel)
{
    // Create semi-transparent overlay panel
    var overlayPanel = new Panel
    {
        Name = "DisabledOverlay",
        Dock = DockStyle.Fill,
        BackColor = Color.FromArgb(102, 45, 62, 80), // 40% opacity (alpha=102, calculated as 0.4 × 255) with dark background
        Cursor = Cursors.No
    };
    
    // Create large centex
    var largeXLabel = new Label
    {
        Text = "✖",
        Font = new Font("Segoe UI", 72, FontStyle.Bold),
        ForeColor = Color.FromArgb(220, 50, 50),
        BackColor = Color.Transparent,
        TextAlign = ContentAlignment.MiddleCenter,
        Dock = DockStyle.Fill,
        UseCompatibleTextRendering = false
    };
    
    overlayPanel.Controls.Add(largeXLabel);
    cardPanel.Controls.Add(overlayPanel);
    overlayPanel.BringToFront();
}
```

### 2. If-Else Statement Implementation

The `SetCardDisabled` method now checks the current setting and chooses the appropriate style:

```csharp
private void SetCardDisabled(Panel cardPanel)
{
    if (cardPanel == null) return;
    
    // ... check if already disabled ...
    
    // Get the current card style setting
    var accountNumber = GetSelectedAccountNumber();
    bool useGreyedOutStyle = true; // Default to greyed out style
    
    if (!string.IsNullOrEmpty(accountNumber))
    {
        var settingsService = RiskManagerSettingsService.Instance;
        if (settingsService.IsInitialized)
        {
            var settings = settingsService.GetSettings(accountNumber);
            if (settings != null)
            {
                useGreyedOutStyle = settings.UseGreyedOutCardStyle;
            }
        }
    }
    
    // Apply the appropriate style based on setting
    if (useGreyedOutStyle)
    {
        // Greyed out style: X in header + reduced opacity
        ApplyGreyedOutStyle(cardPanel);
    }
    else
    {
        // Overlay style: semi-transparent overlay with large centered x
        ApplyOverlayStyle(cardPanel);
    }
    
    // Disable card interaction
    cardPanel.Enabled = false;
    cardPanel.Cursor = Cursors.No;
    
    // Store disabled state in Tag
    var featureChecker = cardPanel.Tag as Func<bool>;
    if (featureChecker != null)
    {
        cardPanel.Tag = new { FeatureChecker = featureChecker, IsDisabled = true };
    }
}
```

### 3. Cleanup in SetCardEnabled

The `SetCardEnabled` method was updated to properly clean up both styles:

```csharp
private void SetCardEnabled(Panel cardPanel)
{
    if (cardPanel == null) return;
    
    // Remove any overlay panel if it exists (for overlay style)
    var overlayPanel = cardPanel.Controls.OfType<Panel>()
        .FirstOrDefault(p => p.Name == "DisabledOverlay");
    if (overlayPanel != null)
    {
        cardPanel.Controls.Remove(overlayPanel);
        overlayPanel.Dispose();
    }
    
    // Hide X in header (for greyed out style)
    var header = cardPanel.Controls.OfType<CustomCardHeaderControl>().FirstOrDefault();
    if (header != null)
    {
        header.SetDisabled(false);
    }
    
    // Re-enable card interaction
    cardPanel.Enabled = true;
    cardPanel.Cursor = Cursors.Default;
    
    // Restore Tag to just the feature checker
    // ... restore tag logic ...
    
    // Reset opacity to full
    foreach (Control control in cardPanel.Controls)
    {
        if (control != header)
        {
            SetControlOpacity(control, 1.0);
        }
    }
    
    // Force refresh of labels to restore proper colors
    RefreshLabelsInCardOnly(cardPanel);
}
```

### 4. Settings Integration

#### Settings Service (RiskManagerSettingsService.cs)
```csharp
// Property in AccountSettings class
public bool UseGreyedOutCardStyle { get; set; } = true; // Default to greyed out

// Method to update the setting
public void UpdateCardDisplayStyle(string accountNumber, bool useGreyedOut)
{
    var settings = GetOrCreateSettings(accountNumber);
    if (settings != null)
    {
        settings.UseGreyedOutCardStyle = useGreyedOut;
        SaveSettings(settings);
    }
}
```

#### General Settings UI (RiskManagerControl.cs)
```csharp
// Checkbox in CreateGeneralSettingsPanel()
var cardStyleCheckBox = new CheckBox
{
    Text = "Use Greyed Out Style for Disabled Cards",
    Checked = true, // Default
    // ... styling ...
};

// Load current setting
if (accountSelector != null && accountSelector.SelectedItem is Account currentAcc)
{
    var accountNumber = GetAccountIdentifier(currentAcc);
    var settings = RiskManagerSettingsService.Instance.GetSettings(accountNumber);
    if (settings != null)
    {
        cardStyleCheckBox.Checked = settings.UseGreyedOutCardStyle;
    }
}

// Handle checkbox changes
cardStyleCheckBox.CheckedChanged += (s, e) =>
{
    if (accountSelector != null && accountSelector.SelectedItem is Account currentAccount)
    {
        var accountNumber = GetAccountIdentifier(currentAccount);
        RiskManagerSettingsService.Instance.UpdateCardDisplayStyle(
            accountNumber, 
            cardStyleCheckBox.Checked
        );
        
        // Rebuild Risk Overview panel to apply the new style
        RefreshRiskOverviewIfVisible();
    }
};
```

## How It Works

1. **User opens General Settings tab**
   - Checkbox shows current style setting (checked = greyed out, unchecked = overlay)

2. **User toggles the checkbox**
   - Setting is saved immediately via `UpdateCardDisplayStyle()`
   - `RefreshRiskOverviewIfVisible()` is called
   - Entire Risk Overview panel is rebuilt with new style

3. **Card becomes disabled** (feature toggle turned off)
   - `SetCardDisabled()` is called
   - Method checks `UseGreyedOutCardStyle` setting
   - Conditional logic applies appropriate style:
     - If true → `ApplyGreyedOutStyle()` (X in header + reduced opacity)
     - If false → `ApplyOverlayStyle()` (overlay with large centered x)

4. **Card becomes enabled** (feature toggle turned on)
   - `SetCardEnabled()` is called
   - Removes overlay panel if present
   - Hides X in header if present
   - Restores full opacity
   - Re-enables interaction

## Visual Comparison

### Greyed Out Style (Checked - Default)
```
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ 📈 Position Limits          ✖  ┃  ← Small X in header
┠─────────────────────────────────┨
┃ Max Loss per Position: $500    ┃  ← 40% opacity
┃ Max Profit per Position: $1000 ┃  ← 40% opacity
┗━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┛
```

### Overlay Style (Unchecked)
```
┏━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━┓
┃ 📈 Position Limits             ┃
╠═════════════════════════════════╣
║█████████████████████████████████║  ← Semi-transparent overlay
║████████████   ✖   █████████████║  ← Large centex (72pt)
║█████████████████████████████████║
╚═════════════════════════════════╝
```

## Benefits

1. **Flexibility**: Users can choose the style that works best with their chosen theme
2. **Backwards Compatible**: Default behavior matches the current implementation (greyed out)
3. **Clean Code**: Separate methods for each style make the code easy to maintain
4. **Proper Cleanup**: SetCardEnabled works for both styles
5. **Immediate Feedback**: Changes apply immediately when toggling the checkbox
6. **Per-Account Setting**: Each account can have its own style preference

## Files Modified

1. **RiskManagerControl.cs**
   - Modified `SetCardEnabled()` to clean up both styles
   - Modified `SetCardDisabled()` to use if-else conditional logic
   - Added `ApplyGreyedOutStyle()` method
   - Added `ApplyOverlayStyle()` method
   - Updated info label text to correctly describe default

2. **Data/RiskManagerSettingsService.cs**
   - Updated comments to reflect correct default (true = greyed out)
   - Updated XML documentation

## Testing Recommendations

1. **Test Greyed Out Style (Default)**
   - Verify checkbox is checked by default
   - Disable a feature (e.g., Position Limits)
   - Confirm X appears in card header
   - Confirm card content is at reduced opacity
   - Enable the feature
   - Confirm X disappears and opacity is restored

2. **Test Overlay Style**
   - Uncheck the "Use Greyed Out Style" checkbox
   - Disable a feature
   - Confirm semi-transparent overlay appears over card
   - Confirm large X is centered on overlay
   - Enable the feature
   - Confirm overlay is removed

3. **Test Toggle Behavior**
   - With a feature disabled:
     - Toggle checkbox from checked to unchecked
     - Confirm style changes from greyed out to overlay
     - Toggle back
     - Confirm style changes from overlay to greyed out

4. **Test Theme Compatibility**
   - Test greyed out style with dark themes
   - Test overlay style with white theme
   - Verify both styles are clearly visible in their recommended themes

5. **Test Multiple Cards**
   - Disable multiple features
   - Confirm all disabled cards use the same style
   - Toggle the style
   - Confirm all cards update to the new style

## Known Limitations

1. Style changes require rebuilding the Risk Overview panel (unavoidable due to how cards are generated)
2. Cannot mix styles (all disabled cards use the same style)
3. Default style (greyed out) may be less visible on white themes (this is why we provide the toggle)
