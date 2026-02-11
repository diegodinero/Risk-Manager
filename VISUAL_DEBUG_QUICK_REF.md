# Visual Debug Quick Reference

## What You Should See

Run the application and navigate to Trade Log. Look for these colored panels:

### ✅ If All Working Correctly

```
┌─────────────────────────────────────┐
│ 🟢 GREEN - "TRADE LOG PAGE LOADED" │ ← Page loads
├─────────────────────────────────────┤
│ 🔴 RED - "FILTER CARD TEST PANEL"  │ ← filterCard renders
├─────────────────────────────────────┤
│ 🟠 ORANGE BACKGROUND                │ ← filterCard background
│   🔍 Filter & Search                │ ← Header
│ ┌─────────────────────────────────┐ │
│ │ 🟡 YELLOW                       │ │ ← filterPanel renders
│ │ DEBUG: FilterPanel loaded       │ │
│ │ Search: [___] Outcome: [▼]      │ │ ← Controls visible!
│ │ From: [date] To: [date]         │ │
│ └─────────────────────────────────┘ │
└─────────────────────────────────────┘
```

---

## Quick Diagnosis

### See GREEN only?
**Issue**: filterCard not rendering  
**Check**: Z-order, control addition

### See GREEN + RED only?
**Issue**: filterPanel not rendering  
**Check**: filterPanel.Dock, filterPanel.Size

### See GREEN + RED + ORANGE only?
**Issue**: filterPanel background not showing  
**Check**: filterPanel.BackColor, filterPanel.Visible

### See all colors but no filter controls?
**Issue**: Controls not added to filterPanel  
**Check**: filterPanel.Controls.Count

### See all colors + controls?
**SUCCESS!** Everything rendering correctly.

---

## Color Legend

| Color | Element | Purpose |
|-------|---------|---------|
| 🟢 **GREEN** | pageDebugPanel | Page load test |
| 🔴 **RED** | debugTestPanel | Filter card test |
| 🟠 **ORANGE** | filterCard | Card background |
| 🟡 **YELLOW** | filterPanel | Panel background |

---

## Debug Console - Key Values

Look for these in Output/Debug window:

```
=== FILTER CARD DEBUG ===
filterCard: Size={X, Y}     ← Should NOT be (0, 0)
filterCard: Height=180      ← Correct height
filterCard: Visible=True    ← Must be True
filterCard: Dock=Top        ← Correct docking
filterPanel: Size={X, Y}    ← Should NOT be (0, 0)
filterPanel: Visible=True   ← Must be True
```

---

## Next Steps Based on Results

### ❌ No colors visible
1. Check if Trade Log tab is selected
2. Verify CreateTradeLogPage() is called
3. Check navigation/tab switching code

### ✅ Some colors visible
1. Note which colors you see
2. Check Debug console output
3. Refer to VISUAL_DEBUG_GUIDE.md for detailed diagnosis

### ✅ All colors visible
1. SUCCESS! Filters should be visible
2. If still having issues, check control dimensions
3. May need to adjust heights or layout

---

**Quick Test**: Navigate to Trading Journal → Trade Log → Look for GREEN panel at top

If you see GREEN, page is loading! ✅  
If you see RED, filterCard is rendering! ✅  
If you see YELLOW, filterPanel is rendering! ✅
