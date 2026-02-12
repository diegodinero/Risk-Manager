# Trade Log Filter Panel - Visual Layout

## Complete Filter Panel Mockup

```
┌─────────────────────────────────────────────────────────────────────────────┐
│ Risk Manager - Trading Journal                                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                              │
│  ┌─ 📊 Trading Statistics ─────────────────────────────────────────────┐   │
│  │  Total: 42 (W:28 L:12 BE:2)    Win Rate: 66.7%                      │   │
│  │  Total P/L: $8,450.00          Avg P/L: $201.19                     │   │
│  │  Best: $1,200.00               Worst: -$350.00                      │   │
│  │  Avg Win: $385.50              Avg Loss: -$145.25                   │   │
│  └──────────────────────────────────────────────────────────────────────┘   │
│                                                                              │
│  ┌─ 🔍 Filter & Search ─────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  Search:  [________________]    Outcome: [All        ▼]              │  │
│  │  ⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃                    ⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃               │  │
│  │  Width: 150px                  Width: 100px                          │  │
│  │                                                                        │  │
│  │  Symbol:  [__________]    From: [02/11/2026 ▼]  To: [02/11/2026 ▼]  [CLEAR]  │
│  │  ⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃               ⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃   ⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃  ⌃⌃⌃⌃⌃⌃⌃│  │
│  │  Width: 80px               Width: 120px       Width: 120px   80px   │  │
│  │                                                                        │  │
│  │  ← DATE PICKERS ARE HERE →                                            │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
│  ┌─ 📋 Trade Log ───────────────────────────────────────────────────────┐  │
│  │                                                                        │  │
│  │  [+ Add]  [✏️ Edit]  [🗑️ Delete]  [📤 Export CSV]                    │  │
│  │                                                                        │  │
│  │ ┌────────────────────────────────────────────────────────────────┐   │  │
│  │ │Date      │Symbol│Type │Outcome│P/L    │Net P/L│R:R  │Model   │   │  │
│  │ ├──────────┼──────┼─────┼───────┼───────┼───────┼─────┼────────┤   │  │
│  │ │02/11/2026│ES    │Long │Win    │$250.00│$245.00│2.1  │Trend   │   │  │
│  │ │02/11/2026│NQ    │Short│Loss   │-$85.00│-$90.00│1.5  │Scalp   │   │  │
│  │ │02/10/2026│ES    │Long │Win    │$420.00│$415.00│3.2  │Breakout│   │  │
│  │ │...       │...   │...  │...    │...    │...    │...  │...     │   │  │
│  │ └────────────────────────────────────────────────────────────────┘   │  │
│  │                                                                        │  │
│  └────────────────────────────────────────────────────────────────────────┘  │
│                                                                              │
└──────────────────────────────────────────────────────────────────────────────┘
```

## Filter Controls Breakdown

### Row 1: Search & Outcome
```
┌──────────┬──────────────────────┬──────────────┬─────────────────────────┐
│ Label    │ Control              │ Width        │ Purpose                 │
├──────────┼──────────────────────┼──────────────┼─────────────────────────┤
│ Search:  │ [TextBox]            │ 150px        │ Global text search      │
│ Outcome: │ [ComboBox Dropdown]  │ 100px        │ Filter by Win/Loss/BE   │
└──────────┴──────────────────────┴──────────────┴─────────────────────────┘
```

### Row 2: Symbol, Date Pickers & Clear
```
┌──────────┬──────────────────────┬──────────────┬──────────────────────────────┐
│ Label    │ Control              │ Width        │ Purpose                      │
├──────────┼──────────────────────┼──────────────┼──────────────────────────────┤
│ Symbol:  │ [TextBox]            │ 80px         │ Filter by symbol             │
│ From:    │ [DateTimePicker]     │ 120px        │ Start date (1 month ago)     │
│ To:      │ [DateTimePicker]     │ 120px        │ End date (today)             │
│ (none)   │ [CLEAR Button]       │ 80px         │ Reset all filters            │
└──────────┴──────────────────────┴──────────────┴──────────────────────────────┘
```

## Date Picker Detail View

### From Date Picker
```
Label: "From:"
┌─────────────────────┐
│ 01/11/2026      ▼  │  ← Default: 1 month ago
└─────────────────────┘
     │
     ├─ Click to open calendar
     ├─ Arrow keys to navigate
     ├─ Enter to select
     └─ Type date directly
```

### To Date Picker
```
Label: "To:"
┌─────────────────────┐
│ 02/11/2026      ▼  │  ← Default: Today
└─────────────────────┘
     │
     ├─ Click to open calendar
     ├─ Arrow keys to navigate
     ├─ Enter to select
     └─ Type date directly
```

## Calendar Dropdown (When Clicked)

```
┌─────────────────────┐
│ 01/11/2026      ▼  │ ← Click here
└─────────────────────┘
          │
          ▼
┌──────────────────────────┐
│   February 2026          │
├──────────────────────────┤
│ Su Mo Tu We Th Fr Sa     │
│                    1     │
│  2  3  4  5  6  7  8     │
│  9 10 [11]12 13 14 15    │  ← Today highlighted
│ 16 17 18 19 20 21 22     │
│ 23 24 25 26 27 28        │
│                          │
│ ◄ Prev    Next ►         │
│ [Today]                  │
└──────────────────────────┘
```

## Filter Panel Specifications

### Panel Properties
```
Panel: filterCard
├─ Dock: Top
├─ Height: 160px (specifically sized for date pickers)
├─ Background: CardBackground color
├─ Padding: 10px
└─ Margin: 0, 0, 0, 10

FlowLayoutPanel: filterPanel
├─ Dock: Fill
├─ Flow Direction: LeftToRight
├─ Wrap Contents: True (wraps to second row)
├─ Padding: 5px
└─ Background: CardBackground
```

### Control Order (Left to Right)
```
Row 1:
  1. Search Label
  2. Search TextBox
  3. Outcome Label
  4. Outcome ComboBox

Row 2:
  5. Symbol Label
  6. Symbol TextBox
  7. From Date Label    ← DATE FILTER
  8. From DateTimePicker ← DATE FILTER
  9. To Date Label      ← DATE FILTER
  10. To DateTimePicker  ← DATE FILTER
  11. Clear Button
```

## Control Dimensions

### All Filter Controls
```
┌─────────────────┬─────────┬──────────┬────────────────────────┐
│ Control         │ Width   │ Height   │ Margin                 │
├─────────────────┼─────────┼──────────┼────────────────────────┤
│ All Labels      │ AutoSize│ AutoSize │ 5px top, 8px others    │
│ Search Box      │ 150px   │ 25px     │ 5px all sides          │
│ Outcome Combo   │ 100px   │ (auto)   │ 5px all sides          │
│ Symbol Box      │ 80px    │ 25px     │ 5px all sides          │
│ From Picker     │ 120px   │ (auto)   │ 5px all sides          │
│ To Picker       │ 120px   │ (auto)   │ 5px all sides          │
│ Clear Button    │ 80px    │ 28px     │ 10px left, 5px others  │
└─────────────────┴─────────┴──────────┴────────────────────────┘

Total Row 1 Width: ~275px (Search + Outcome + margins)
Total Row 2 Width: ~535px (Symbol + Dates + Clear + margins)
```

## Visual States

### Normal State (Default)
```
From: [01/11/2026 ▼]  To: [02/11/2026 ▼]
      ⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃      ⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃
      1 month ago          Today
```

### After User Selection
```
From: [01/15/2026 ▼]  To: [01/31/2026 ▼]
      ⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃      ⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃
      Custom start         Custom end
```

### After Calendar Click (Same Day)
```
From: [02/08/2026 ▼]  To: [02/08/2026 ▼]
      ⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃      ⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃
      Clicked date         Same date (single day view)
```

### After Clear Button
```
From: [01/11/2026 ▼]  To: [02/11/2026 ▼]
      ⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃      ⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃
      Reset to default     Reset to default
```

## Color Scheme

### Filter Panel Colors
```
Background:     CardBackground (dark gray)
Labels:         White text, Segoe UI 10pt Bold
Controls:       Standard Windows control colors
Clear Button:   Gray background (100, 100, 100), White text
```

### DateTimePicker Colors
```
Normal:     Standard Windows theme
Focused:    Blue highlight (Windows default)
Disabled:   Gray (not used - always enabled)
```

## Responsive Behavior

### Wide Window (>800px)
```
Row 1: [Search] [Outcome]
Row 2: [Symbol] [From Date] [To Date] [Clear]
       ← All visible on same line
```

### Narrow Window (<800px)
```
Row 1: [Search] [Outcome]
Row 2: [Symbol] [From Date]
Row 3: [To Date] [Clear]
       ← Wraps to additional rows
```

### FlowLayoutPanel Wrapping
- `WrapContents = true` enables automatic wrapping
- Controls flow left-to-right
- Wraps to next row when panel width exceeded
- Maintains vertical alignment

## Integration with Other Components

### Filter Interactions
```
┌──────────────────────────────────────────────────┐
│                                                  │
│  Search Filter ────┐                             │
│                    │                             │
│  Outcome Filter ───┼─→ Combined with AND logic  │
│                    │                             │
│  Symbol Filter ────┤                             │
│                    │                             │
│  Date Filters ─────┘                             │
│  (From & To)                                     │
│                                                  │
│  ↓                                               │
│                                                  │
│  Filtered Trade List                             │
│                                                  │
└──────────────────────────────────────────────────┘
```

### Clear Button Behavior
```
Click [CLEAR]
     │
     ├─→ Search Box = ""
     ├─→ Outcome = "All"
     ├─→ Symbol Box = ""
     ├─→ From Date = Today - 1 month
     ├─→ To Date = Today
     └─→ Call FilterTrades()
```

## User Interaction Flow

### Changing From Date
```
1. User clicks From date picker
   ↓
2. Calendar dropdown opens
   ↓
3. User navigates to desired date
   ↓
4. User clicks date or presses Enter
   ↓
5. DateTimePicker updates
   ↓
6. ValueChanged event fires
   ↓
7. FilterTrades() executes
   ↓
8. Grid updates with filtered trades
```

### Changing To Date
```
Same flow as From Date (above)
```

## Code References

### Control Creation
```csharp
File: RiskManagerControl.cs
Lines: 13375-13398

- dateFromLabel creation: 13376
- dateFromPicker creation: 13377-13385
- dateFromPicker event: 13386
- dateToLabel creation: 13388
- dateToPicker creation: 13389-13397
- dateToPicker event: 13398
```

### Control Addition to UI
```csharp
File: RiskManagerControl.cs
Lines: 13430-13433

filterPanel.Controls.Add(dateFromLabel);
filterPanel.Controls.Add(dateFromPicker);
filterPanel.Controls.Add(dateToLabel);
filterPanel.Controls.Add(dateToPicker);
```

### Filter Application
```csharp
File: RiskManagerControl.cs
Lines: 17698-17699, 17728-17734

var dateFromPicker = FindControlByName(contentPanel, "DateFromPicker");
var dateToPicker = FindControlByName(contentPanel, "DateToPicker");

// Later in method:
if (dateFromPicker != null && dateToPicker != null)
{
    var fromDate = dateFromPicker.Value.Date;
    var toDate = dateToPicker.Value.Date;
    filteredTrades = filteredTrades.Where(t => 
        t.Date.Date >= fromDate && 
        t.Date.Date <= toDate
    );
}
```

## Summary

### Key Points
✅ Date pickers are in Row 2 of the filter panel  
✅ Located after Symbol filter, before Clear button  
✅ Both default to reasonable values (1 month ago to today)  
✅ Standard Windows DateTimePicker controls  
✅ Width: 120px each (adequate for date display)  
✅ Real-time filtering on value change  
✅ Integrated with Clear button  
✅ Part of FlowLayoutPanel with automatic wrapping  

### Visual Location
```
Filter Panel (Height: 160px)
├─ Row 1: Search, Outcome
└─ Row 2: Symbol, [FROM DATE], [TO DATE], Clear
                    ⌃⌃⌃⌃⌃⌃⌃⌃⌃⌃  ⌃⌃⌃⌃⌃⌃⌃⌃⌃
                    HERE!      HERE!
```

---

**Document Purpose**: Visual reference for date filter location and behavior  
**Status**: Reflects current implementation in RiskManagerControl.cs  
**Last Updated**: February 11, 2026
