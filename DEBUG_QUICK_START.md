# Debug Quick Start - Get Button Visibility Info

## Quick Steps

1. **Run in Debug Mode**
   - Press F5 in Visual Studio

2. **Open Output Window**
   - View → Output (or Ctrl+Alt+O)
   - Select "Debug" from dropdown

3. **Navigate to Trade Log**
   - Open Risk Manager
   - Go to Trading Journal
   - Click "Trade Log" tab

4. **Copy Debug Output**
   - Look for lines starting with `===`
   - Copy ALL debug output
   - Paste in issue report

## What You'll See

If working correctly:
```
=== ShowJournalSection: Trade Log ===
=== CreateTradeLogPage CALLED ===
=== BUTTONS PANEL DEBUG ===
AddButton: Size={Width=120, Height=35}, Visible=True, Enabled=True
=== JOURNAL CARD DEBUG ===
JournalCard: Size={Width=600, Height=250}, MinimumSize={Width=0, Height=250}
=== PAGE PANEL DEBUG ===
PagePanel: Size={Width=600, Height=500}
```

If NOT working, you'll see:
- Missing lines (method not called)
- Visible=False (button hidden)
- Size={Width=0, Height=0} (collapsed)
- Wrong panel sizes

## Report Back

Copy the debug output and share it. We'll immediately see:
- Is the page being created?
- Are buttons being created?
- What sizes are actual?
- Is button visible?
- What's wrong?

Then we fix it!

## Example Report

```
Here's my debug output:

=== ShowJournalSection: Trade Log ===
=== CreateTradeLogPage CALLED ===
PagePanel created: AutoScroll=True
=== BUTTONS PANEL DEBUG ===
AddButton: Size={Width=120, Height=35}, Visible=True, Enabled=True, Text='➕ Add Trade'
ButtonsPanel: Size={Width=0, Height=50}, Visible=True, ControlCount=4  <- WIDTH IS 0!
...

And here's a screenshot: [attach image]
```

With this info, we'll know exactly what to fix!
