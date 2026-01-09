# Shutdown Button Implementation

## Overview
A new shutdown button has been added to the Risk Manager application, positioned below the theme switcher button in the top panel. This button provides a safe way to lock all accounts and close the application.

## Features

### 1. Button Placement
- Located in the badges panel, below the theme switcher button (🎨)
- Positioned in the top-right corner of the application
- Size: 50px × 42px (larger than theme switcher for better visibility)

### 2. Button Icon
- Uses the `leave.png` image from the Resources folder
- Fallback to 🚪 emoji if resource is not available
- Red hover color (RGB: 231, 76, 60) to indicate shutdown action

### 3. Button Functionality
When clicked, the shutdown button performs the following actions:

1. **Confirmation Dialog**
   - Shows: "Are you sure you want to lock all accounts, settings, and close the application?"
   - User can choose "Yes" or "No"
   - If "No" is selected, the shutdown is cancelled

2. **Lock All Accounts**
   - Executes the same logic as the "Lock All Accounts" button
   - Locks all connected accounts (both trading and settings)
   - Locks until 5PM ET (same as regular lock)

3. **Audio Feedback**
   - Plays the "leave-get-out.wav" sound from Resources folder
   - Sound plays asynchronously without blocking

4. **Countdown Timer**
   - Shows a modal dialog with 5-second countdown
   - Dialog displays: "Application will close in X seconds..."
   - Countdown updates every second

5. **Cancel Option**
   - Red "Cancel Shutdown" button in the countdown dialog
   - Clicking cancel stops the timer and closes the dialog
   - Shows confirmation: "Shutdown cancelled."

6. **Application Closure**
   - After 5 seconds, the application closes
   - First tries to find and close the parent form
   - Falls back to Application.Exit() if form not found

## Visual Design

### Button Styling
- Transparent background with white foreground
- FlatStyle with no border
- Hand cursor on hover
- Red hover color to distinguish from theme button

### Countdown Dialog
- Fixed dialog window (400×180 pixels)
- Centered on screen
- Cannot be maximized or minimized
- Uses current theme colors (CardBackground and TextWhite)
- Large, centered countdown text
- Red cancel button (consistent with shutdown theme)

## Theme Compatibility
The shutdown button automatically adapts to all themes:
- Blue Theme (original)
- Black Theme (dark)
- White Theme (light)
- YellowBlueBlack Theme

The button background, colors, and the countdown dialog all inherit the current theme settings.

## Technical Implementation

### Fields Added
```csharp
private Button shutdownButton;
private Image shutdownButtonScaledImage;
private System.Windows.Forms.Timer shutdownTimer;
private Form shutdownCountdownForm;
private int shutdownCountdownSeconds;
```

### Methods Added
1. `ShutdownButton_Click(object sender, EventArgs e)` - Main click handler
2. `PlayShutdownSound()` - Plays the leave-get-out.wav sound
3. `ShowShutdownCountdown()` - Shows countdown dialog with cancel option

### Integration Points
- Added to `CreateTopPanel()` method in RiskManagerControl.cs
- Uses existing `BtnLockAllAccounts_Click()` method for locking logic
- Leverages existing theme system for consistent appearance
- Uses existing resource management for icon and sound

## Usage

1. Click the shutdown button (door icon) in the top-right corner
2. Confirm the shutdown action in the dialog
3. Wait for the countdown (or cancel if needed)
4. Application will lock all accounts and close after 5 seconds

## Safety Features

- **Double confirmation**: Initial dialog + countdown with cancel option
- **Audio feedback**: Sound plays to confirm action
- **Visual countdown**: Clear indication of time remaining
- **Easy cancellation**: Cancel button available throughout countdown
- **Locks before closing**: Ensures all accounts are locked before exit

## Resources Used

- **Icon**: `Resources/leave.png`
- **Sound**: `Resources/leave-get-out.wav`
- Both resources are already defined in `Properties/Resources.Designer.cs` and `Properties/Resources.resx`
