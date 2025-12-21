# Risk Manager Title Bar Fix

## Problem Statement
The Risk Manager plugin did not display a title bar with minimize, maximize, and close buttons despite the `PluginInfo` configuration being set to allow these features. The window appeared embedded without the native window chrome controls.

## Root Cause Analysis
After investigation and testing, it was discovered that the issue was **not** related to `WindowParameters` settings (Panel vs Dialog mode), but rather the **base class** used for the plugin:

- `Plugin` base class does not display a title bar with window controls
- `TablePlugin` base class displays a full title bar with minimize, maximize, and close buttons

The original `RiskManagerPanel.cs` inherited from `Plugin`, which prevented the title bar from displaying regardless of the `WindowParameters` configuration.

## Solution
Changed `RiskManagerPanel` to inherit from `TablePlugin` instead of `Plugin`, while maintaining its custom control display functionality.

### Key Changes

1. **Base Class Change** (RiskManagerPanel.cs)
   - Changed from `public class RiskManagerPanel : Plugin` to `public class RiskManagerPanel : TablePlugin`
   - Added required `TablePlugin` members: `DefaultSize` and `AssociatedTableItem`
   - Note: `AssociatedTableItem` returns `null` since we use a custom control instead of table data

2. **Table Control Hiding**
   - Hide the default table control in `Initialize()` and `Populate()` methods
   - This allows the custom `RiskManagerControl` to be the primary content

3. **Custom Control Attachment**
   - Modified `Populate()` to properly attach the `RiskManagerControl` to the TablePlugin
   - Uses reflection to find and call `AddControl` method if available
   - Falls back to existing reflection-based attachment logic as a safety net

### Before (Problematic Code)
```csharp
public class RiskManagerPanel : Plugin
{
    // ... no DefaultSize or AssociatedTableItem required
    
    public override void Initialize()
    {
        base.Initialize();
    }
}
```

### After (Fixed Code)
```csharp
public class RiskManagerPanel : TablePlugin
{
    // Required by TablePlugin but not used - we display custom control instead
    public override Size DefaultSize => new Size(1200, 800);
    protected override TableItem AssociatedTableItem => null;

    public override void Initialize()
    {
        base.Initialize();
        
        // Hide the table since we're using a custom control
        if (this.table != null)
        {
            this.table.Visible = false;
        }
    }
    
    public override void Populate(PluginParameters args = null)
    {
        base.Populate(args);
        
        // Hide the table control
        if (this.table != null)
            this.table.Visible = false;

        // Attach custom control
        var addControlMethod = this.GetType().BaseType?.GetMethod("AddControl", ...);
        if (addControlMethod != null)
        {
            addControlMethod.Invoke(this, new object[] { _control });
        }
        else
        {
            // Fallback to reflection-based attachment
            AttachControlToHostWithLogging(_control, log);
        }
    }
}
```

## Files Modified
1. **RiskManagerPanel.cs**
   - Changed base class from `Plugin` to `TablePlugin`
   - Added required `DefaultSize` property
   - Added required `AssociatedTableItem` property (returns null)
   - Modified `Initialize()` to hide the table control
   - Modified `Populate()` to properly attach custom control and hide table

2. **Risk_Manager.cs**
   - Kept existing TablePlugin implementation
   - Added debug logging for window parameters (DEBUG builds only)
   - Added `ShowInTaskbar = true` for consistency

## Expected Results After Fix
✅ Title bar with plugin name displayed  
✅ Minimize button visible and functional  
✅ Maximize button visible and functional  
✅ Close button visible and functional  
✅ Window can be resized with grip  
✅ Window appears in taskbar  
✅ Custom RiskManagerControl content displays correctly  

## Verification Steps
1. Deploy the updated plugin to Quantower
2. Open the Risk Manager plugin
3. Verify the window has a title bar with all control buttons
4. Verify the custom control content (RiskManagerControl) is visible and functional
5. Test minimize, maximize, and close functionality
6. Test window resizing
7. Check that the table is hidden and only the custom control is shown

## Technical Notes
- **TablePlugin vs Plugin**: TablePlugin provides built-in window chrome support, while Plugin requires manual window management
- **Custom Control in TablePlugin**: By hiding the default table and using reflection to attach the custom control, we maintain the rich UI while gaining title bar functionality
- **AssociatedTableItem = null**: This is acceptable since we're not using the table functionality
- **Backwards Compatibility**: The change maintains all existing functionality while adding the missing title bar

## Troubleshooting
If the custom control doesn't display:
1. Check Desktop for `RiskManagerPanel_attach_log.txt` to see attachment attempts
2. Verify `table.Visible = false` is being executed
3. Ensure the fallback `AttachControlToHostWithLogging` method is working
4. Check that `RiskManagerControl` is being created successfully
