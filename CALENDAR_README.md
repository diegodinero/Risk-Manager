# Trading Journal Calendar - README

## 📅 Overview

The Trading Journal Calendar is a visual month-by-month view of your trading activity, integrated into the Risk Manager application. It provides color-coded performance indicators and dual display modes to help you track profitability and plan adherence at a glance.

## 🎯 Quick Start

### Accessing the Calendar
1. Open Risk Manager in Quantower
2. Select your trading account from the dropdown
3. Click **"📓 Trading Journal"** in the left navigation
4. Click **"🗓  Calendar"** in the journal sidebar

### Using the Calendar

**View Monthly Performance**
- Current month displays automatically
- Use ◀ ▶ buttons to navigate between months
- Color-coded cells show performance at a glance

**Switch Display Modes**
- Click **[P&L]** to see profit/loss amounts
- Click **[Plan]** to see plan adherence percentages
- Colors update automatically

**View Trade Details**
- Click any colored day cell to navigate to Trade Log
- (Future: Will filter to show only that day's trades)

## 🎨 Color Meanings

### P&L Mode
| Color | Meaning | Example |
|-------|---------|---------|
| 🟢 Green | Profitable day | +$250.00 |
| 🟡 Yellow | Breakeven day | $0.00 |
| 🔴 Red | Loss day | -$150.00 |
| ⬜ Gray | No trades | (empty) |

### Plan Mode
| Color | Meaning | Example |
|-------|---------|---------|
| 🟢 Green | ≥70% followed plan | 85% followed |
| 🟡 Yellow | 50-69% followed plan | 60% followed |
| 🔴 Red | <50% followed plan | 40% followed |
| ⬜ Gray | No trades | (empty) |

## 📊 What You See

### Day Cell Information
Each day cell shows:
1. **Day number** (top-left) - 1-31
2. **Performance metric** (center):
   - P&L mode: Net profit/loss amount
   - Plan mode: Plan adherence percentage
3. **Trade count** (bottom-right) - Number of trades that day

### Monthly Statistics Panel
Displays at the top:
- **Total Trades** - Count of trades in the month
- **Net P/L** - Total profit/loss for the month
- **Days Traded** - Number of unique trading days
- **Days Plan Followed** - Days where ≥70% of trades followed the plan

## 🎓 Use Cases

### Track Profitability Patterns
- Identify your most profitable days of the week
- Spot winning/losing streaks
- Find patterns in monthly performance

### Monitor Trading Discipline
- Track plan adherence over time
- Identify days when discipline breaks down
- Correlate plan adherence with profitability

### Review Trading Frequency
- See how often you trade per month
- Identify overtrading periods
- Balance trading frequency with quality

### Analyze Seasonal Patterns
- Compare month-to-month performance
- Identify seasonal trends
- Plan around historically strong/weak periods

## 📚 Documentation

### Quick Reference
**File**: `CALENDAR_QUICK_REFERENCE.md`
- Controls and keyboard shortcuts
- Color meanings and cell layout
- Usage tips

### Technical Guide
**File**: `CALENDAR_IMPLEMENTATION.md`
- Architecture and code structure
- Integration details
- Testing checklist
- Future enhancements

### Visual Examples
**File**: `CALENDAR_VISUAL_COMPARISON.md`
- Before/after comparison
- Cell state examples
- Navigation flows
- Feature parity matrix

### Complete Summary
**File**: `CALENDAR_COMPLETE_SUMMARY.md`
- Implementation details
- Deliverables summary
- Quality assurance results
- Success criteria

## 🔧 Technical Details

### Data Source
- Uses `TradingJournalService` for trade data
- Filters by selected account automatically
- Calculates statistics in real-time

### Performance
- Lazy loading (only current month)
- Efficient refresh on month/mode changes
- Minimal recalculation

### Integration
- Seamlessly integrated into Trading Journal sidebar
- Uses existing theme system
- Respects account selector

## 💡 Tips & Tricks

### Best Practices
1. **Review weekly** - Check calendar each Sunday for the past week
2. **Track patterns** - Note which days are consistently green/red
3. **Use both modes** - Toggle between P&L and Plan for complete picture
4. **Compare months** - Navigate backward to compare with previous months
5. **Set goals** - Aim for more green days each month

### Common Patterns to Look For
- 🔴 Red Monday syndrome - Are you rushing weekend analysis?
- 🟢 Green Friday clusters - Are you more disciplined near weekends?
- 🟡 Yellow streaks - Are you breaking even too often?
- 🔴 Red after green - Are you overconfident after wins?

## ⚠️ Important Notes

- **Account-Specific**: Calendar shows trades from selected account only
- **Real-Time**: Statistics calculate from current trade data
- **Navigation**: Click any day with trades to go to Trade Log
- **Clickable Days**: Only days with trades (colored cells) are clickable
- **Empty Months**: Gray cells indicate no trading activity

## 🐛 Troubleshooting

### Calendar Not Showing Trades
- ✅ Verify correct account is selected
- ✅ Check if trades exist in selected month
- ✅ Try navigating to a different month

### Colors Not Updating
- ✅ Ensure toggle button is selected (blue background)
- ✅ Click toggle button again to force refresh

### Statistics Showing Zero
- ✅ This is normal if no trades in selected month
- ✅ Navigate to months with trading activity

## 🚀 Future Enhancements

Planned features for future releases:
1. **Date Filtering** - Filter Trade Log when clicking day cells
2. **Weekly Statistics** - Summary rows showing weekly performance
3. **Hover Tooltips** - Show trade details on hover
4. **Export Calendar** - Save calendar view as image
5. **Year View** - See all 12 months at once
6. **Keyboard Navigation** - Use arrow keys to navigate months
7. **Date Annotations** - Add notes to specific days

## 📞 Support

### Getting Help
1. Check the quick reference guide first
2. Review the implementation guide for technical details
3. Check the visual comparison for examples
4. Create a GitHub issue if problems persist

### Reporting Issues
When reporting issues, include:
- Screenshot of the calendar
- Selected account name
- Current month/year displayed
- Expected vs actual behavior
- Steps to reproduce

## 📜 Version History

### Version 1.0.0 (February 2026)
- ✅ Initial release
- ✅ Monthly calendar grid
- ✅ P&L and Plan display modes
- ✅ Month navigation
- ✅ Monthly statistics
- ✅ Color-coded cells
- ✅ Trade count badges
- ✅ Interactive navigation

## 🙏 Credits

- **Original Design**: @diegodinero/TradingJournalApp
- **Platform**: Quantower Trading Platform
- **Implementation**: GitHub Copilot
- **Version**: 1.0.0
- **Date**: February 2026

## 📄 License

Same license as Risk Manager project.

---

## 🎉 Start Using the Calendar Today!

The Trading Journal Calendar transforms your trading data into visual insights. Start exploring your trading patterns and improve your performance through data-driven decisions!

**Navigate to: Risk Manager → Trading Journal → Calendar** 📅

---

**Questions?** Check the documentation files or create a GitHub issue.
