# Risk Manager Title Bar Fix

## Problem Statement
The Risk Manager plugin did not display a title bar with minimize, maximize, and close buttons despite the `PluginInfo` configuration being set to allow these features. The window appeared embedded without the native window chrome controls.

## Root Cause
The issue was caused by using `NativeWindowParameters.Panel` mode when constructing the `NativeWindowParameters` object. Panel mode is designed for plugins that should be embedded within the host application's workspace without their own window chrome.

### Before (Problematic Code)
```csharp
WindowParameters = new NativeWindowParameters(NativeWindowParameters.Panel)
{
    BrowserUsageType = BrowserUsageType.None,
    WindowStyle = NativeWindowStyle.SingleBorderWindow,
    ResizeMode = NativeResizeMode.CanResizeWithGrip,
    AllowActionsButton = true,
    AllowCloseButton = true,
    AllowMaximizeButton = true,
    AllowFullScreenButton = true
}
```

The `NativeWindowParameters.Panel` parameter instructed the host application to embed the plugin without providing standalone window controls, effectively suppressing the title bar regardless of the `AllowCloseButton`, `AllowMaximizeButton` settings.

## Solution
Changed to use the default `NativeWindowParameters()` constructor which provides Dialog mode, ensuring a full standalone window with title bar and controls.

### After (Fixed Code)
```csharp
// Use default constructor (Dialog mode) instead of Panel to ensure title bar is shown
WindowParameters = new NativeWindowParameters()
{
    BrowserUsageType = BrowserUsageType.None,
    WindowStyle = NativeWindowStyle.SingleBorderWindow,
    ResizeMode = NativeResizeMode.CanResizeWithGrip,
    AllowActionsButton = true,
    AllowCloseButton = true,
    AllowMaximizeButton = true,
    AllowFullScreenButton = true,
    ShowInTaskbar = true
}
```

## Changes Applied

### 1. Window Parameters (Risk_Manager.cs & RiskManagerPanel.cs)
- **Changed**: `new NativeWindowParameters(NativeWindowParameters.Panel)` → `new NativeWindowParameters()`
- **Added**: `ShowInTaskbar = true` to explicitly request taskbar visibility
- **Result**: Plugin now runs as a standalone dialog window with full title bar controls

### 2. Debug Logging (Both Files)
Added comprehensive logging in the `Initialize()` method to help verify settings at runtime. **Note: This logging is only active in DEBUG builds** via `#if DEBUG` preprocessor directive.

```csharp
public override void Initialize()
{
    base.Initialize();
    
#if DEBUG
    // Debug logging to verify PluginInfo settings are applied
    try
    {
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        var log = System.IO.Path.Combine(desktop, "RiskManager_window_log.txt");
        var info = GetInfo();
        
        System.IO.File.AppendAllText(log, $"=== Risk_Manager Initialize - {DateTime.Now:O} ==={Environment.NewLine}");
        System.IO.File.AppendAllText(log, $"Plugin Title: {info.Title}{Environment.NewLine}");
        System.IO.File.AppendAllText(log, $"WindowStyle: {info.WindowParameters?.WindowStyle}{Environment.NewLine}");
        System.IO.File.AppendAllText(log, $"ResizeMode: {info.WindowParameters?.ResizeMode}{Environment.NewLine}");
        System.IO.File.AppendAllText(log, $"AllowCloseButton: {info.WindowParameters?.AllowCloseButton}{Environment.NewLine}");
        System.IO.File.AppendAllText(log, $"AllowMaximizeButton: {info.WindowParameters?.AllowMaximizeButton}{Environment.NewLine}");
        System.IO.File.AppendAllText(log, $"AllowFullScreenButton: {info.WindowParameters?.AllowFullScreenButton}{Environment.NewLine}");
        System.IO.File.AppendAllText(log, $"ShowInTaskbar: {info.WindowParameters?.ShowInTaskbar}{Environment.NewLine}");
        System.IO.File.AppendAllText(log, Environment.NewLine);
        
        System.Diagnostics.Debug.WriteLine($"Risk_Manager initialized with WindowStyle: {info.WindowParameters?.WindowStyle}");
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Risk_Manager: Logging error: {ex.Message}");
    }
#endif
    
    // ... rest of initialization
}
```

### Log Files
The debug logs are written to the user's Desktop for easy access **only in DEBUG builds**:
- **Risk_Manager.cs**: `RiskManager_window_log.txt`
- **RiskManagerPanel.cs**: `RiskManagerPanel_window_log.txt`

**Note**: In RELEASE builds, no log files will be created to avoid performance overhead.

## Files Modified
1. **Risk_Manager.cs**
   - Updated `GetInfo()` method's `WindowParameters`
   - Added debug logging to `Initialize()` method

2. **RiskManagerPanel.cs**
   - Updated `GetInfo()` method's `WindowParameters`
   - Added debug logging to `Initialize()` method

## Expected Behavior After Fix
1. ✅ Plugin window displays with a full title bar
2. ✅ Minimize button is visible and functional
3. ✅ Maximize button is visible and functional
4. ✅ Close button is visible and functional
5. ✅ Window can be resized using grip
6. ✅ Window appears in the taskbar
7. ✅ Debug logs confirm settings are applied correctly

## Verification Steps
1. Deploy the updated plugin to Quantower
2. Open the Risk Manager plugin
3. Verify the window has a title bar with all control buttons
4. Check the Desktop for log files to confirm settings were applied
5. Test minimize, maximize, and close functionality
6. Test window resizing

## Technical Notes
- **Dialog Mode vs Panel Mode**: Dialog mode creates a standalone window, while Panel mode embeds the content within the host application's layout
- **ShowInTaskbar**: Explicitly set to ensure the window appears in the Windows taskbar
- **WindowStyle**: `SingleBorderWindow` provides a standard window border with resize capability
- **ResizeMode**: `CanResizeWithGrip` allows users to resize the window
- All button permissions (`AllowCloseButton`, `AllowMaximizeButton`, etc.) are preserved and will now function properly with the dialog window

## Compatibility
- These changes maintain backward compatibility with existing settings and data
- The plugin will now behave as a standalone dialog rather than an embedded panel
- All existing functionality is preserved; only the window presentation mode has changed

## Additional Resources
- See `IMPLEMENTATION_SUMMARY.md` for overall plugin architecture
- Check Desktop log files for runtime verification of window settings
