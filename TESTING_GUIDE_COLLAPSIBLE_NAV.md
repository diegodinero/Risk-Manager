# Testing Guide for Collapsible Navigation and Grid-to-Dropdown Sync

## Prerequisites
- Visual Studio or compatible C# IDE
- Quantower platform installed (for TradingPlatform DLLs)
- At least 2 trading accounts connected in Quantower

## Quick Start Testing

### 1. Build the Project
```bash
# Open the solution in Visual Studio
# Build the project (Ctrl+Shift+B)
# Ensure no compilation errors
```

### 2. Deploy to Quantower
```bash
# Copy the compiled DLL to Quantower's indicator directory
# Typical path: C:\Users\[Username]\Desktop\Quantower\TradingPlatform\v1.145.11\..\..\Settings\Scripts\Indicators\Risk Manager
```

### 3. Launch Risk Manager
1. Open Quantower
2. Open Risk Manager panel
3. You should see the new toggle button at the top of the left navigation

## Feature 1: Collapsible Navigation Testing

### Test Case 1: Basic Toggle Functionality
**Steps:**
1. Look at the top-left corner of the navigation panel
2. You should see a button with a left arrow emoji (⬅️)
3. Click the toggle button
4. Observe the navigation panel smoothly collapsing to ~50px width
5. Only emoji icons should be visible (no text labels)
6. The toggle button arrow should change to right arrow (➡️)
7. Click the toggle button again
8. Observe the panel smoothly expanding back to ~200px width
9. Both icons and text labels should now be visible
10. The toggle button arrow should change back to left arrow (⬅️)

**Expected Results:**
✓ Smooth animation (approximately 100ms duration)
✓ No flickering or jumping
✓ Icons remain centered when collapsed
✓ Text appears when expanded
✓ Toggle button arrow changes direction appropriately

### Test Case 2: State Persistence
**Steps:**
1. Collapse the navigation (click toggle until you see ➡️)
2. Close the Risk Manager panel completely
3. Close Quantower application
4. Relaunch Quantower
5. Open Risk Manager panel
6. Check the navigation panel state

**Expected Results:**
✓ Navigation panel should be collapsed (showing only icons)
✓ Toggle button should show ➡️
✓ Settings are loaded from: `%LocalAppData%\RiskManager\navigation_state.txt`

### Test Case 3: Navigation Functionality While Collapsed
**Steps:**
1. Collapse the navigation panel
2. Click on each icon in the collapsed navigation
3. Verify the corresponding page loads in the main content area
4. Check that the selected icon is highlighted

**Expected Results:**
✓ All navigation items remain clickable when collapsed
✓ Selected item is highlighted with different background color
✓ Correct page content displays for each icon clicked
✓ No functionality is lost in collapsed state

### Test Case 4: Content Area Adjustment
**Steps:**
1. Start with expanded navigation (200px)
2. Note the width available for content area
3. Collapse navigation to 50px
4. Observe the content area expanding

**Expected Results:**
✓ Content area gains approximately 150px of additional width
✓ Data grids in content area utilize the extra space
✓ No horizontal scrollbars appear unnecessarily
✓ Layout remains responsive and properly aligned

## Feature 2: Grid-to-Dropdown Sync Testing

### Test Case 1: Basic Grid Click Selection
**Steps:**
1. Navigate to "Accounts Summary" tab
2. Note the currently selected account in the dropdown at the top
3. Look at the accounts summary grid
4. Click on a different account row in the grid (any cell in the row)
5. Check the account dropdown at the top

**Expected Results:**
✓ Dropdown automatically updates to show the clicked account
✓ No need to manually select from dropdown
✓ Grid row remains highlighted
✓ No errors or exceptions thrown

### Test Case 2: Settings Synchronization
**Steps:**
1. Navigate to "Accounts Summary" tab
2. Click on Account A in the grid
3. Navigate to "Limits" tab
4. Note the loss/profit limit values shown
5. Navigate back to "Accounts Summary" tab
6. Click on Account B in the grid
7. Navigate back to "Limits" tab
8. Check the loss/profit limit values

**Expected Results:**
✓ Values in "Limits" tab change when different account is selected
✓ Settings are correctly loaded for the clicked account
✓ No settings from previous account persist
✓ All input fields update appropriately

### Test Case 3: Badge Updates
**Steps:**
1. Ensure you have at least one locked and one unlocked account
2. Navigate to "Accounts Summary" tab
3. Click on the unlocked account row
4. Check the "Settings Status" and "Trading Status" badges at the top
5. Click on the locked account row
6. Check the badges again

**Expected Results:**
✓ Badges update immediately when account is clicked
✓ Lock status reflects the selected account's state
✓ Badge colors change appropriately (green for unlocked, red for locked)
✓ No delay in badge updates

### Test Case 4: Multiple Rapid Clicks
**Steps:**
1. Navigate to "Accounts Summary" tab
2. Rapidly click different account rows (5-10 clicks in quick succession)
3. Verify the dropdown shows the last clicked account
4. Navigate to other tabs to verify settings loaded correctly

**Expected Results:**
✓ No errors or crashes
✓ Dropdown updates to reflect the final clicked account
✓ No race conditions or inconsistent state
✓ Settings panels show data for the final selected account

### Test Case 5: Edge Cases
**Steps:**
1. Click on the header row of the grid (should not select)
2. Click on an account row, then immediately click the dropdown
3. Click on an account row that's already selected in dropdown
4. Try clicking while settings are loading

**Expected Results:**
✓ Header row clicks are ignored (no selection change)
✓ Manual dropdown interaction still works
✓ Re-clicking same account doesn't cause issues
✓ Concurrent operations don't cause conflicts

## Performance Testing

### Test Case 1: Animation Smoothness
**Steps:**
1. Rapidly toggle navigation 10 times
2. Observe the animation quality
3. Check CPU usage during animation

**Expected Results:**
✓ Animation remains smooth
✓ No visible stuttering or lag
✓ CPU usage remains reasonable (<5% spike)

### Test Case 2: Large Account Lists
**Steps:**
1. Test with 10+ connected accounts
2. Click different accounts in quick succession
3. Measure response time

**Expected Results:**
✓ Response time < 100ms per click
✓ No noticeable delay in dropdown update
✓ UI remains responsive

## Verification Checklist

After completing all tests, verify:

- [ ] Navigation collapses/expands smoothly
- [ ] Toggle button arrow changes appropriately
- [ ] Navigation state persists after restart
- [ ] All navigation items work when collapsed
- [ ] Grid clicks update dropdown selector
- [ ] Settings load correctly for clicked account
- [ ] Status badges update when account changes
- [ ] No errors in debug output or logs
- [ ] Performance is acceptable
- [ ] No regression in existing functionality

## Troubleshooting

### Navigation doesn't toggle
- Check if toggle button is visible at top of navigation
- Verify button click event is firing (add breakpoint)
- Check `leftPanel` reference is not null

### Grid clicks don't update dropdown
- Ensure you're clicking on the data grid, not the header
- Verify accounts exist in both grid and dropdown
- Check `accountSelector` reference is not null
- Look for event handler exceptions in debug output

### State doesn't persist
- Check file permissions for `%LocalAppData%\RiskManager\`
- Verify `navigation_state.txt` is being created
- Check file content (should be "True" or "False")

### Animation is choppy
- Close other resource-intensive applications
- Check if running on remote desktop (can affect animation)
- Verify Timer interval is set to 10ms

## Debug Logging

Look for these debug messages in Visual Studio Output window:

```
Navigation collapse state saved: True
Navigation collapse state loaded: True
Theme preference loaded: Blue
```

## Success Criteria

Both features pass when:
1. ✓ Navigation toggles smoothly between states
2. ✓ State persists across application restarts
3. ✓ Grid clicks update dropdown immediately
4. ✓ Settings load correctly for clicked account
5. ✓ No errors or exceptions occur
6. ✓ Performance remains acceptable
7. ✓ All existing functionality still works

## Reporting Issues

If you encounter any issues, please provide:
1. Steps to reproduce
2. Expected vs actual behavior
3. Screenshots or screen recording
4. Debug output logs
5. System specifications (OS, .NET version, Quantower version)

## Screenshots to Capture

For documentation purposes, please capture:
1. Navigation in expanded state
2. Navigation in collapsed state
3. Toggle button in both states
4. Grid selection updating dropdown
5. Settings panels after grid click
6. Status badges updating

These screenshots will help verify the implementation and can be added to the documentation.
