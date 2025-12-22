# Risk Management Enhancements

This document describes the new risk management features implemented for account-based trading control.

## Overview

All risk management features operate on an **account-by-account basis**, ensuring proper isolation between different trading accounts. The system continuously monitors positions and enforces rules in real-time.

## New Features

### 1. Allowed Trading Times Enforcement

**Location:** `🕐 Allowed Trading Times` tab

**Description:** Restricts trading to specific time windows. Positions are automatically closed if held outside allowed trading hours.

**Features:**
- Three predefined trading sessions:
  - **NY Session:** Monday-Friday, 8 AM - 5 PM EST
  - **London Session:** Monday-Friday, 3 AM - 12 PM EST
  - **Asia Session:** Sunday-Friday, 7 PM - 4 AM EST
- Sessions can be enabled/disabled individually
- Settings are saved per account
- Positions outside allowed times are closed automatically

**How It Works:**
1. Select the trading sessions you want to allow
2. Click "SAVE SETTINGS" to persist the configuration
3. The system monitors positions every 500ms
4. If current time is outside allowed windows, all positions are closed

**Technical Implementation:**
- Settings stored in `TradingTimeRestrictions` list
- Checked via `IsTradingAllowedNow()` method
- Enforcement in `CheckTradingTimeRestrictions()` method

---

### 2. Symbol Blacklist

**Location:** `🛡️ Symbols` tab (Blacklist section)

**Description:** Prevents trading of specific symbols. Any position opened for a blacklisted symbol is immediately closed.

**Features:**
- Comma-separated list of symbols to blacklist
- Case-insensitive matching
- Automatic normalization to uppercase
- Per-account configuration
- Real-time enforcement

**How It Works:**
1. Enter symbols to block (e.g., "AAPL, MSFT, TSLA")
2. Enable the "Symbol Blacklist" checkbox
3. Click "SAVE SETTINGS"
4. Any existing or new positions for these symbols are closed immediately

**Example:**
```
Symbols to block: AAPL, MSFT, TSLA
```

**Technical Implementation:**
- Settings stored in `BlockedSymbols` list
- Checked via `IsSymbolBlocked()` method
- Enforcement in `CheckSymbolBlacklist()` method

---

### 3. Symbol Contract Limits

**Location:** `🛡️ Symbols` tab (Contract Limits section)

**Description:** Limits the number of open positions per symbol. When exceeded, ALL positions for that symbol are closed.

**Features:**
- Default contract limit applies to all symbols
- Symbol-specific limits override the default
- Per-account configuration
- Real-time enforcement
- Groups positions by symbol before checking

**How It Works:**
1. Set a default contract limit (e.g., 10)
2. Optionally set symbol-specific limits (e.g., "AAPL:5, MSFT:3")
3. Enable the "Symbol Contract Limits" checkbox
4. Click "SAVE SETTINGS"
5. If position count exceeds limit, all positions for that symbol are closed

**Example:**
```
Default contract limit: 10
Symbol-specific limits: AAPL:5, MSFT:3, TSLA:2
```

This means:
- AAPL: Max 5 positions (closes all if exceeded)
- MSFT: Max 3 positions (closes all if exceeded)
- TSLA: Max 2 positions (closes all if exceeded)
- All other symbols: Max 10 positions (closes all if exceeded)

**Technical Implementation:**
- Settings stored in `DefaultContractLimit` and `SymbolContractLimits`
- Checked via `GetContractLimit()` method
- Enforcement in `CheckSymbolContractLimits()` method
- Positions grouped by symbol before counting

---

### 4. Risk Overview Tab

**Location:** `🔍 Risk Overview` tab

**Description:** Displays a comprehensive overview of all risk management settings for the currently selected account.

**Information Displayed:**
- **Position Loss Limit:** Maximum loss per position
- **Position Profit Target:** Target profit per position
- **Daily Loss Limit:** Maximum loss per day
- **Daily Profit Target:** Target profit per day
- **Symbols Blacklisted:** List of blocked symbols
- **Default Contract Limit:** Base position limit
- **Symbol Contract Limits:** Per-symbol position limits
- **Allowed Trading Times:** Active trading sessions
- **Trading Lock Status:** Current lock state
- **Settings Lock Status:** Whether settings are locked
- **Feature Toggle:** Master switch status

**Features:**
- Read-only display
- Real-time refresh capability
- Shows enabled/disabled status for each setting
- Values displayed with appropriate formatting
- Refresh button to update display

**How It Works:**
1. Select an account from the dropdown
2. Navigate to "🔍 Risk Overview" tab
3. View all configured risk settings
4. Click "REFRESH OVERVIEW" to update

---

## Monitoring and Enforcement

### Continuous Monitoring

The system uses a timer-based monitoring approach:

```
Timer Interval: 500ms (twice per second)
Location: MonitorPnLLimits() method
```

**Monitoring Order:**
1. Check if account is locked (skip if locked)
2. Check trading time restrictions
3. Check symbol blacklist
4. Check contract limits
5. Check daily P&L limits
6. Check position P&L limits

### Account-Based Operation

All features operate independently per account:
- Settings are stored with unique account identifier
- Monitoring loops through all connected accounts
- Each account checked separately
- No cross-account interference

**Account Identification:**
```csharp
var uniqueAccountId = GetUniqueAccountIdentifier(account, index);
var settings = settingsService.GetSettings(uniqueAccountId);
```

### Position Closing

When a rule violation is detected:
1. Relevant positions are identified
2. `ClosePosition()` method is called
3. Position is closed via Quantower API
4. Action is logged to debug output

**Example Log Output:**
```
Position closed (outside trading hours): AAPL
Position closed (blacklisted symbol): MSFT for account 123456
Position closed (contract limit exceeded): TSLA for account 123456
```

---

## Configuration

### Settings Storage

All settings are stored in JSON files:
- **Location:** `%LocalAppData%\RiskManager\`
- **Format:** `{AccountNumber}.json`
- **Backup:** Automatically created on save
- **Cache:** 30-second cache for performance

### Settings Structure

```json
{
  "accountNumber": "123456",
  "featureToggleEnabled": true,
  "blockedSymbols": ["AAPL", "MSFT"],
  "defaultContractLimit": 10,
  "symbolContractLimits": {
    "TSLA": 5,
    "NVDA": 3
  },
  "tradingTimeRestrictions": [
    {
      "name": "NY Session",
      "dayOfWeek": "Monday",
      "startTime": "08:00:00",
      "endTime": "17:00:00",
      "isAllowed": true
    }
  ]
}
```

---

## Best Practices

### Trading Time Restrictions

1. **Enable only the sessions you actively trade**
   - Reduces risk of overnight positions
   - Ensures positions are closed outside active hours

2. **Consider overlapping sessions**
   - NY + London overlap: 8 AM - 12 PM EST
   - Higher liquidity during overlaps

3. **Test with small positions first**
   - Verify closing logic works as expected
   - Monitor logs for position closures

### Symbol Blacklist

1. **Use for high-risk symbols**
   - Volatile stocks
   - Low liquidity instruments
   - Symbols you want to avoid

2. **Regular review recommended**
   - Market conditions change
   - Update blacklist accordingly

3. **Case-insensitive matching**
   - "AAPL", "aapl", "Aapl" all match
   - No need for exact case

### Contract Limits

1. **Start with conservative limits**
   - Default limit of 5-10 positions
   - Reduce risk exposure per symbol

2. **Use symbol-specific limits for concentration control**
   - Higher limits for stable stocks
   - Lower limits for volatile stocks

3. **All positions closed when exceeded**
   - Ensures compliance with limit
   - Prevents gradual limit creep

### Risk Overview

1. **Check before each trading session**
   - Verify settings are correct
   - Ensure no accidental changes

2. **Use refresh button regularly**
   - Settings may change from other sources
   - Always see current configuration

3. **Review after any changes**
   - Confirm changes were saved
   - Verify expected values

---

## Troubleshooting

### Positions Not Closing

**Check:**
1. Feature Toggle is enabled (⚙️ Feature Toggles tab)
2. Account is not locked (🔒 Manual Lock tab)
3. Settings saved for correct account
4. Timer is running (check logs)

**Debug:**
- Check debug output for log messages
- Verify `MonitorPnLLimits()` is being called
- Confirm position belongs to correct account

### Settings Not Persisting

**Check:**
1. Account selected in dropdown
2. "SAVE SETTINGS" button clicked
3. Settings service initialized
4. File write permissions exist

**Debug:**
- Check `%LocalAppData%\RiskManager\` folder
- Look for `{AccountNumber}.json` file
- Verify file contains expected values

### Trading Times Not Working

**Check:**
1. Sessions enabled in UI
2. Settings saved for account
3. Current time within/outside session
4. Timezone configured correctly

**Debug:**
- Log current time and session windows
- Verify `IsTradingAllowedNow()` return value
- Check `TradingTimeRestrictions` list

---

## API Reference

### Key Methods

#### CheckTradingTimeRestrictions
```csharp
private void CheckTradingTimeRestrictions(
    Account account, 
    string accountId, 
    AccountSettings settings, 
    RiskManagerSettingsService settingsService, 
    Core core)
```
Checks if trading is allowed at current time and closes positions if not.

#### CheckSymbolBlacklist
```csharp
private void CheckSymbolBlacklist(
    Account account, 
    string accountId, 
    AccountSettings settings, 
    RiskManagerSettingsService settingsService, 
    Core core)
```
Checks if any positions are for blacklisted symbols and closes them.

#### CheckSymbolContractLimits
```csharp
private void CheckSymbolContractLimits(
    Account account, 
    string accountId, 
    AccountSettings settings, 
    RiskManagerSettingsService settingsService, 
    Core core)
```
Checks if position count per symbol exceeds limits and closes excess positions.

#### PopulateRiskOverview
```csharp
private void PopulateRiskOverview(DataGridView grid)
```
Populates the Risk Overview grid with current account settings.

### Settings Service Methods

#### SetTradingTimeRestrictions
```csharp
public void SetTradingTimeRestrictions(
    string accountNumber, 
    IEnumerable<TradingTimeRestriction> restrictions)
```
Saves trading time restrictions for an account.

#### IsTradingAllowedNow
```csharp
public bool IsTradingAllowedNow(string accountNumber)
```
Checks if trading is currently allowed based on time restrictions.

#### IsSymbolBlocked
```csharp
public bool IsSymbolBlocked(string accountNumber, string symbol)
```
Checks if a symbol is blacklisted for an account.

#### GetContractLimit
```csharp
public int? GetContractLimit(string accountNumber, string symbol)
```
Gets the contract limit for a symbol (symbol-specific or default).

---

## Security Considerations

### Input Validation

- All symbol names sanitized
- Contract limits validated (must be positive)
- Time ranges validated
- Account numbers sanitized for file paths

### File Security

- Settings stored in user-specific location
- No sensitive data in settings files
- JSON serialization with null handling
- File locks prevent concurrent access

### Access Control

- Settings lock prevents unauthorized changes
- Trading lock prevents trading
- Per-account isolation
- No cross-account access

---

## Performance

### Monitoring Overhead

- Timer runs every 500ms
- Efficient position enumeration
- Cached settings (30-second TTL)
- Minimal impact on trading performance

### Optimization

- Settings loaded once per monitoring cycle
- Position grouping for contract limits
- Early exit conditions
- Logging only when needed

---

## Future Enhancements

Potential improvements for future versions:

1. **Custom Trading Sessions**
   - User-defined time windows
   - Multiple windows per day
   - Holiday schedules

2. **Advanced Contract Limits**
   - Separate long/short limits
   - Notional value limits
   - Percentage-based limits

3. **Alerting**
   - Email notifications
   - Sound alerts
   - Push notifications

4. **Reporting**
   - Position closure history
   - Rule violation reports
   - Performance analytics

---

## Support

For questions or issues:
1. Check debug logs in Output window
2. Verify settings in Risk Overview tab
3. Review this documentation
4. Contact support with specific details

---

*Last Updated: December 22, 2024*
*Version: 1.0*
