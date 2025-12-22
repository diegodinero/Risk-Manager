# Risk Management Quick Reference

## Quick Setup Guide

### 1. Allowed Trading Times
```
Navigate to: 🕐 Allowed Trading Times
1. Check the sessions you want to allow
2. Click SAVE SETTINGS
Result: Positions closed outside allowed times
```

### 2. Symbol Blacklist
```
Navigate to: 🛡️ Symbols (Blacklist section)
1. Enable "Symbol Blacklist" checkbox
2. Enter symbols: AAPL, MSFT, TSLA
3. Click SAVE SETTINGS
Result: Positions for these symbols closed immediately
```

### 3. Contract Limits
```
Navigate to: 🛡️ Symbols (Contract Limits section)
1. Enable "Symbol Contract Limits" checkbox
2. Set default limit: 10
3. Set specific limits: AAPL:5, MSFT:3
4. Click SAVE SETTINGS
Result: All positions closed when symbol exceeds limit
```

### 4. View All Settings
```
Navigate to: 🔍 Risk Overview
1. Select account from dropdown
2. View all current settings
3. Click REFRESH OVERVIEW to update
```

---

## Key Features

| Feature | Location | Enforcement Frequency | Action |
|---------|----------|----------------------|--------|
| Trading Times | 🕐 Allowed Trading Times | 500ms | Close positions outside hours |
| Symbol Blacklist | 🛡️ Symbols | 500ms | Close blacklisted positions |
| Contract Limits | 🛡️ Symbols | 500ms | Close all when limit exceeded |
| Position Limits | 📈 Positions | 500ms | Close position at limit |
| Daily Limits | 📊 Limits | 500ms | Lock account at limit |

---

## Common Scenarios

### Scenario 1: Block After-Hours Trading
```
Problem: Don't want positions overnight
Solution:
1. Go to 🕐 Allowed Trading Times
2. Enable only NY Session (8 AM - 5 PM)
3. Save settings
4. All positions closed at 5 PM daily
```

### Scenario 2: Limit High-Risk Stocks
```
Problem: Want to limit exposure to volatile stocks
Solution:
1. Go to 🛡️ Symbols
2. Enable Contract Limits
3. Set: TSLA:2, NVDA:2 (limit to 2 positions each)
4. Save settings
5. All positions closed if 3rd position opened
```

### Scenario 3: Avoid Specific Stocks
```
Problem: Want to completely avoid certain stocks
Solution:
1. Go to 🛡️ Symbols
2. Enable Symbol Blacklist
3. Enter: GME, AMC
4. Save settings
5. Any position in these stocks closed immediately
```

### Scenario 4: Review All Risk Settings
```
Problem: Want to verify current configuration
Solution:
1. Go to 🔍 Risk Overview
2. View all settings at a glance
3. Click Refresh to update
4. Verify all enabled/disabled statuses
```

---

## Monitoring Cycle

Every 500ms, the system checks (in order):

1. ✅ Is account locked? → Skip if yes
2. ✅ Trading time allowed? → Close positions if no
3. ✅ Symbol blacklisted? → Close position if yes
4. ✅ Contract limit exceeded? → Close all for symbol if yes
5. ✅ Daily limit reached? → Lock account if yes
6. ✅ Position limit reached? → Close position if yes

---

## Settings Storage

```
Location: %LocalAppData%\RiskManager\
File Format: {AccountNumber}.json
Example: 123456.json

Contents:
- Position limits
- Daily limits
- Blocked symbols
- Contract limits
- Trading time restrictions
- Lock status
```

---

## Troubleshooting

### Positions Not Closing?

```
✓ Check: Feature Toggle enabled (⚙️ tab)
✓ Check: Account not locked (🔒 tab)
✓ Check: Settings saved for correct account
✓ Check: Values are correct in Risk Overview
```

### Settings Not Saving?

```
✓ Check: Account selected in dropdown
✓ Check: SAVE SETTINGS clicked
✓ Check: File exists in %LocalAppData%\RiskManager\
✓ Check: File permissions allow writing
```

### Enforcement Not Working?

```
✓ Check: Timer running (check debug logs)
✓ Check: Position belongs to correct account
✓ Check: Feature Toggle is enabled
✓ Check: No conflicting settings
```

---

## Important Notes

⚠️ **All features work PER ACCOUNT**
- Settings saved separately for each account
- Monitoring checks each account independently
- No cross-account interference

⚠️ **Position closing is IMMEDIATE**
- When rule violated, position closed right away
- No grace period or warnings
- Monitor logs to see closures

⚠️ **Contract limit closes ALL positions**
- If you have 6 AAPL positions with limit of 5
- ALL 6 positions are closed, not just 1
- This ensures compliance with limit

⚠️ **Trading times use local time**
- System uses machine's local time
- EST times shown are Eastern Standard Time
- Adjust for your timezone if needed

---

## Best Practices

### 🎯 Daily Routine

1. Start of day:
   - Check 🔍 Risk Overview
   - Verify all settings correct
   - Ensure account unlocked

2. During trading:
   - Monitor positions
   - Watch for automatic closures
   - Check debug logs if issues

3. End of day:
   - Review 🔍 Risk Overview
   - Check any violations
   - Update settings as needed

### 🎯 Risk Management Strategy

1. **Conservative Approach:**
   ```
   - Trading Times: Single session only
   - Contract Limits: Low (5-10)
   - Blacklist: High-volatility stocks
   - Position Limits: Tight ($100-$500)
   ```

2. **Moderate Approach:**
   ```
   - Trading Times: Two sessions
   - Contract Limits: Medium (10-20)
   - Blacklist: Very high-volatility only
   - Position Limits: Moderate ($500-$1000)
   ```

3. **Aggressive Approach:**
   ```
   - Trading Times: All sessions
   - Contract Limits: High (20-50)
   - Blacklist: Minimal
   - Position Limits: Loose ($1000+)
   ```

### 🎯 Testing New Settings

1. Test with small positions first
2. Monitor for one full day
3. Review closure logs
4. Adjust as needed
5. Scale up gradually

---

## Keyboard Shortcuts

| Action | Shortcut |
|--------|----------|
| Switch Accounts | Click dropdown |
| Save Settings | Click SAVE SETTINGS |
| Refresh Overview | Click REFRESH OVERVIEW |
| Navigate Tabs | Click sidebar buttons |

---

## Support Checklist

Before contacting support, gather:

- [ ] Account number
- [ ] Current settings (from Risk Overview)
- [ ] Debug log output
- [ ] Steps to reproduce issue
- [ ] Expected vs. actual behavior
- [ ] Screenshots if applicable

---

## Version History

**Version 1.0** (December 22, 2024)
- Initial release
- Trading time restrictions
- Symbol blacklist
- Contract limits
- Risk overview tab

---

*For detailed information, see RISK_MANAGEMENT_ENHANCEMENTS.md*
