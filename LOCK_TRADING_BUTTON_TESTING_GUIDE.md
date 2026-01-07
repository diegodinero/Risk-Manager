# Lock Trading Button Testing Guide

## Test Plan

This document provides a comprehensive testing plan for validating the Lock Trading button centering and sizing changes.

---

## Pre-Testing Setup

### Build Requirements
1. Ensure all dependencies are installed
2. Build the project: `dotnet build "Risk Manager.csproj"`
3. Launch the application
4. Navigate to the **Accounts Summary** tab

---

## Test Cases

### TC1: Button Dimensions
**Objective**: Verify button dimensions match Emergency Flatten button

**Steps**:
1. Open the application
2. Navigate to top panel to view Emergency Flatten button
3. Navigate to Accounts Summary tab
4. Compare the two buttons visually

**Expected Result**:
- Lock Trading button width: 250 pixels
- Lock Trading button height: 26 pixels
- Dimensions match Emergency Flatten button exactly

**Pass Criteria**: ✅ Buttons have identical dimensions

---

### TC2: Horizontal Centering
**Objective**: Verify button is centered between header text and right edge

**Steps**:
1. Open the application
2. Navigate to Accounts Summary tab
3. Observe the Lock Trading button position
4. Measure distance from "Accounts Summary" text to button left edge
5. Measure distance from button right edge to window right edge

**Expected Result**:
- Button appears visually centered in available space
- Approximately equal spacing on both sides (accounting for text width)

**Pass Criteria**: ✅ Button is horizontally centered with balanced spacing

---

### TC3: Vertical Centering
**Objective**: Verify button is vertically centered in header

**Steps**:
1. Open the application
2. Navigate to Accounts Summary tab
3. Observe the Lock Trading button vertical alignment
4. Compare with header text vertical position

**Expected Result**:
- Button is vertically centered within the header
- Top and bottom margins are equal

**Pass Criteria**: ✅ Button is vertically aligned with header content

---

### TC4: Window Resize - Large to Small
**Objective**: Verify centering is maintained when shrinking window

**Steps**:
1. Open the application at maximum window size
2. Navigate to Accounts Summary tab
3. Note the button position (should be centered)
4. Gradually resize window smaller
5. Observe button position throughout resize

**Expected Result**:
- Button remains centered at all window sizes
- No visual glitches during resize
- Button position updates smoothly

**Pass Criteria**: ✅ Button maintains centering during resize

---

### TC5: Window Resize - Small to Large
**Objective**: Verify centering is maintained when expanding window

**Steps**:
1. Open the application at minimum window size
2. Navigate to Accounts Summary tab
3. Note the button position (should be centered)
4. Gradually resize window larger
5. Observe button position throughout resize

**Expected Result**:
- Button remains centered at all window sizes
- Spacing increases proportionally on both sides
- Button position updates smoothly

**Pass Criteria**: ✅ Button maintains centering during resize

---

### TC6: Icon Sizing
**Objective**: Verify icons fit properly within button

**Steps**:
1. Open the application
2. Navigate to Accounts Summary tab
3. Examine the lock icons on both sides of button text
4. Verify icons are not clipped or distorted

**Expected Result**:
- Lock icons are 20×20 pixels
- Icons fit within button height (26px)
- Icons are centered vertically
- Icons are not clipped or distorted

**Pass Criteria**: ✅ Icons display correctly at proper size

---

### TC7: Text Readability
**Objective**: Verify button text is readable and properly sized

**Steps**:
1. Open the application
2. Navigate to Accounts Summary tab
3. Read the "Lock Trading" text on the button
4. Verify text is centered and legible

**Expected Result**:
- Text uses Segoe UI, 10pt Bold font
- Text is horizontally and vertically centered
- Text is fully visible and readable
- Text doesn't overflow or get clipped

**Pass Criteria**: ✅ Text is properly sized and readable

---

### TC8: Click Functionality
**Objective**: Verify button remains functional after changes

**Steps**:
1. Open the application
2. Navigate to Accounts Summary tab
3. Click on the Lock Trading button
4. Verify expected action occurs (lock confirmation dialog)

**Expected Result**:
- Button responds to click
- Confirmation dialog appears
- Lock functionality works as before

**Pass Criteria**: ✅ Button functionality is preserved

---

### TC9: Hover State
**Objective**: Verify button responds to mouse hover

**Steps**:
1. Open the application
2. Navigate to Accounts Summary tab
3. Move mouse over Lock Trading button
4. Observe cursor change and any visual feedback

**Expected Result**:
- Cursor changes to hand pointer
- Button provides visual feedback if implemented
- Hover state is consistent with Emergency Flatten button

**Pass Criteria**: ✅ Hover behavior works correctly

---

### TC10: Multiple Accounts
**Objective**: Verify button works with different account configurations

**Steps**:
1. Open the application with multiple accounts
2. Navigate to Accounts Summary tab
3. Verify button appears and is centered
4. Switch between accounts
5. Verify button remains functional

**Expected Result**:
- Button displays correctly with any number of accounts
- Button position doesn't change when switching accounts
- Functionality works for all accounts

**Pass Criteria**: ✅ Button works across all account configurations

---

### TC11: Theme Changes
**Objective**: Verify button appearance across different themes

**Steps**:
1. Open the application
2. Navigate to Accounts Summary tab
3. Change theme (Blue, Black, White, YellowBlueBlack)
4. Verify button appearance in each theme

**Expected Result**:
- Button maintains red background in all themes
- Text remains white and readable
- Icons display correctly in all themes
- Centering is maintained across themes

**Pass Criteria**: ✅ Button displays correctly in all themes

---

### TC12: High DPI Displays
**Objective**: Verify button scales correctly on high DPI displays

**Steps**:
1. Open application on high DPI display (150%, 200% scaling)
2. Navigate to Accounts Summary tab
3. Verify button appearance and centering

**Expected Result**:
- Button scales proportionally
- Centering is maintained
- Icons and text remain crisp
- No pixelation or distortion

**Pass Criteria**: ✅ Button scales correctly at different DPI settings

---

## Visual Inspection Checklist

Use this checklist for quick visual validation:

- [ ] Button width is 250 pixels
- [ ] Button height is 26 pixels
- [ ] Button is horizontally centered
- [ ] Button is vertically centered
- [ ] Icons are 20×20 pixels
- [ ] Icons fit within button bounds
- [ ] Text is Segoe UI, 10pt Bold
- [ ] Text is centered and readable
- [ ] Button has red background
- [ ] Button has white text
- [ ] Cursor changes to hand on hover
- [ ] Spacing is balanced on both sides
- [ ] No visual glitches or artifacts
- [ ] Matches Emergency Flatten button size

---

## Regression Testing

Verify these existing features still work:

- [ ] Accounts Summary grid displays correctly
- [ ] Account data refreshes every second
- [ ] Row selection works
- [ ] Lock Status column updates
- [ ] Other header elements (text, icon) display correctly
- [ ] Page navigation works
- [ ] Window dragging works
- [ ] Other tabs are unaffected

---

## Performance Testing

- [ ] No performance degradation when resizing window
- [ ] Button re-centers quickly (<16ms for 60fps)
- [ ] No memory leaks from resize event handler
- [ ] Application starts up normally
- [ ] No lag when switching tabs

---

## Edge Cases

### Minimum Window Size
- [ ] Button displays correctly at minimum window size (600×400)
- [ ] Button doesn't overlap with header text
- [ ] Button remains clickable

### Maximum Window Size
- [ ] Button displays correctly at maximum window size
- [ ] Centering is maintained
- [ ] Spacing scales appropriately

### Rapid Resizing
- [ ] Button handles rapid window resize events
- [ ] No visual flickering
- [ ] No performance issues

---

## Bug Reporting Template

If issues are found, use this template:

```markdown
### Bug Report

**Test Case**: [TC number and name]
**Severity**: [Critical/High/Medium/Low]
**Environment**: [OS, .NET version, screen resolution, DPI scaling]

**Steps to Reproduce**:
1. [Step 1]
2. [Step 2]
3. [Step 3]

**Expected Result**:
[What should happen]

**Actual Result**:
[What actually happened]

**Screenshots**:
[Attach screenshots showing the issue]

**Additional Notes**:
[Any other relevant information]
```

---

## Sign-Off

Testing completed by: ___________________  
Date: ___________________  
All tests passed: [ ] Yes [ ] No  
Issues found: [ ] None [ ] See bug reports  
Ready for deployment: [ ] Yes [ ] No  

---

## Notes for Testers

1. **Visual Comparison**: Take screenshots before and after for side-by-side comparison
2. **Measurements**: Use browser developer tools or screen rulers to verify pixel dimensions
3. **Multiple Runs**: Test multiple times to catch intermittent issues
4. **Different Configurations**: Test with different account types and counts
5. **Documentation**: Document any unexpected behavior even if not a bug

---

## Success Criteria Summary

All test cases must pass with ✅ to consider the implementation successful:

- ✅ Button dimensions match Emergency Flatten (250×26)
- ✅ Button is horizontally centered
- ✅ Button is vertically centered
- ✅ Centering maintained on resize
- ✅ Icons sized correctly (20×20)
- ✅ Text sized correctly (10pt)
- ✅ Functionality preserved
- ✅ Visual consistency maintained
- ✅ No regressions introduced
- ✅ Performance acceptable
