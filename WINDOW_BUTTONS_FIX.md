# Risk Manager Window Buttons Fix

## Problem Statement

The Risk Manager panel was not displaying the standard minimize, maximize, and close buttons, which is suboptimal and does not adhere to typical user expectations for windowed interfaces.

## Root Cause Analysis

After investigation, the issue was identified in the `WindowParameters` configuration in both `RiskManagerPanel.cs` and `Risk_Manager.cs`. The plugins were using:

```csharp
WindowParameters = new NativeWindowParameters(NativeWindowParameters.Panel)
```

The `NativeWindowParameters.Panel` mode is designed for embedded panel views and does not support window chrome buttons (minimize, maximize, close). While the button properties were correctly set to `true`:

- `AllowCloseButton = true`
- `AllowMaximizeButton = true`
- `AllowFullScreenButton = true`
- `AllowActionsButton = true`

These settings were not being applied because the underlying window mode (Panel) doesn't support window chrome.

## Solution Implemented

### 1. Window Mode Change

Changed the `WindowParameters` initialization in both files from:

```csharp
WindowParameters = new NativeWindowParameters(NativeWindowParameters.Panel)
```

To:

```csharp
WindowParameters = new NativeWindowParameters(NativeWindowParameters.Window)
```

This change enables proper window chrome with all standard window controls.

### 2. Diagnostic Logging

Added diagnostic logging in the `Initialize()` method of both plugins to help verify that window parameters are correctly applied at runtime. The logs include:

- Timestamp of initialization
- WindowParameters type
- AllowCloseButton setting
- AllowMaximizeButton setting
- AllowFullScreenButton setting
- AllowActionsButton setting
- WindowStyle setting
- ResizeMode setting

Log files are created on the desktop:
- `RiskManagerPanel_window_params_log.txt` for RiskManagerPanel
- `RiskManager_window_params_log.txt` for Risk_Manager

This logging is consistent with the existing debug logging pattern already present in the codebase.

## Files Modified

1. **RiskManagerPanel.cs**
   - Line 24: Changed `NativeWindowParameters.Panel` to `NativeWindowParameters.Window`
   - Lines 47-68: Added window parameter logging in `Initialize()` method

2. **Risk_Manager.cs**
   - Line 25: Changed `NativeWindowParameters.Panel` to `NativeWindowParameters.Window`
   - Lines 54-75: Added window parameter logging in `Initialize()` method

## Expected Behavior After Fix

With these changes, the Risk Manager panel should now display:
- ✓ Close button (X)
- ✓ Maximize button (□)
- ✓ Minimize button (-)
- ✓ Full-screen button support
- ✓ Actions button
- ✓ Proper window resizing with grip
- ✓ Single border window style

## Testing Recommendations

1. Open the Risk Manager panel in the Quantower trading platform
2. Verify that all window buttons (minimize, maximize, close) are visible and functional
3. Check the desktop log files to confirm window parameters are correctly applied
4. Test window resizing and maximize/restore functionality
5. Verify that the panel can be closed using the close button

## Technical Notes

- The fix maintains all other window parameters (WindowStyle, ResizeMode, etc.)
- The change is minimal and surgical, affecting only the window mode parameter
- No breaking changes to existing functionality
- Logging is non-intrusive and follows existing patterns in the codebase
- The fix applies to both plugin implementations in the repository

## References

- Problem Statement: Issue regarding missing window buttons in Risk Manager panel
- Solution Requirements: Verify WindowParameters configuration and add diagnostic logging
- Code Review: Passed with notes about logging consistency with existing code
- Security Scan (CodeQL): Passed with no alerts
