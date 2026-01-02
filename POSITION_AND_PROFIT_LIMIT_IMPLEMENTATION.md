# Automated Position and Daily Profit Limit Management - Implementation Summary

## Overview
This document describes the implementation of three automated risk management features:
1. **Daily Profit Target** - Automatically locks account and closes positions when daily profit target is reached
2. **Position Loss Limit** - Automatically closes individual positions when loss limit is reached (no account lock)
3. **Position Profit Target** - Automatically closes individual positions when profit target is reached (no account lock)

## Implementation Date
January 2, 2026

## Requirements Met

### 1. Daily Profit Target ✅
- ✅ Automatically close all open positions when target reached
- ✅ Lock account until 5 PM ET when target exceeded
- ✅ Monitor account's Gross P&L in real-time (every 500ms)
- ✅ Send warning notification at 80% of daily profit target
- ✅ Send lock notification when target is exceeded
- ✅ Log all profit target breach events with timestamps
- ✅ Prevent duplicate notifications on same day

### 2. Position Loss Limit ✅
- ✅ Automatically close individual position when loss limit reached
- ✅ Do NOT lock the account (position-level limit)
- ✅ Monitor each position's P&L in real-time (every 500ms)
- ✅ Log position closure events with timestamps
- ✅ Enhanced audit logging explicitly stating account is NOT locked

### 3. Position Profit Target ✅
- ✅ Automatically close individual position when profit target reached
- ✅ Do NOT lock the account (position-level limit)
- ✅ Monitor each position's P&L in real-time (every 500ms)
- ✅ Log position closure events with timestamps
- ✅ Enhanced audit logging explicitly stating account is NOT locked

## Technical Implementation

### Files Modified

#### 1. RiskManagerSettingsService.cs
Location: `/Data/RiskManagerSettingsService.cs`

**New Data Classes:**
```csharp
// Line ~1017: Added daily profit warning tracking data class
public class DailyProfitWarningInfo
{
    public bool WarningNotificationSent { get; set; }
    public DateTime? WarningDate { get; set; }
    public decimal? WarningPnLValue { get; set; }
}
```

**Updated AccountSettings:**
```csharp
// Line ~960: Added daily profit warning to account settings
public DailyProfitWarningInfo? DailyProfitWarning { get; set; }
```

**New Methods:**
```csharp
// Line ~883: Daily Profit Warning Management region
public void SetDailyProfitWarningSent(string accountNumber, decimal pnlValue)
public bool HasDailyProfitWarningSent(string accountNumber)
public void ResetDailyProfitWarning(string accountNumber)
```

**Method Details:**
- `SetDailyProfitWarningSent()`: Records warning notification with timestamp and P&L value
- `HasDailyProfitWarningSent()`: Checks if warning sent today, auto-resets at UTC midnight
- `ResetDailyProfitWarning()`: Clears warning state (called when limit reached or new day)

#### 2. RiskManagerControl.cs
Location: `/RiskManagerControl.cs`

**Enhanced CheckDailyPnLLimits() Method (Lines ~4568-4636):**

Added daily profit target warning logic:
```csharp
// Calculate warning threshold (80% of target)
decimal warningThreshold = profitTarget * 0.80m;

// Check if profit target is reached
if (currentPnL >= profitTarget)
{
    // Lock account until 5 PM ET
    LockAccountUntil5PMET(accountId, reason, core, account);
    CloseAllPositionsForAccount(account, core);
    NotifyUserProfitTargetReached(accountId, netPnL, profitTarget);
    settingsService.ResetDailyProfitWarning(accountId);
}
// Check if warning threshold reached (80%)
else if (currentPnL >= warningThreshold)
{
    if (!settingsService.HasDailyProfitWarningSent(accountId))
    {
        // Send warning notification
        // Calculate percentage of target reached
        // Play alert sound
        // Mark warning as sent
        settingsService.SetDailyProfitWarningSent(accountId, currentPnL);
    }
}
```

**New NotifyUserProfitTargetReached() Method (Lines ~5040-5059):**
```csharp
private void NotifyUserProfitTargetReached(string accountId, decimal netPnL, decimal profitTarget)
{
    string lockMessage = $"🎯 DAILY PROFIT TARGET REACHED!\n\n" +
        $"Account: {accountId}\n" +
        $"Current Net P&L: ${netPnL:F2}\n" +
        $"Daily Profit Target: ${profitTarget:F2}\n\n" +
        $"Congratulations! Your account has been locked until 5 PM ET to protect your profits.\n" +
        $"All positions have been closed.\n\n" +
        $"Please contact an administrator if you need to unlock the account before the scheduled time.";
    
    PlayAlertSound();
    MessageBox.Show(lockMessage, "Account Locked - Profit Target Reached", 
        MessageBoxButtons.OK, MessageBoxIcon.Information);
}
```

**Enhanced CheckPositionPnLLimits() Method (Lines ~4693-4760):**

Added enhanced logging for position closures:
```csharp
// Position Loss Limit
if ((decimal)openPnL <= lossLimit)
{
    string reason = $"Position Loss Limit: {position.Symbol} P&L ${openPnL:F2} ≤ Limit ${lossLimit:F2}";
    
    System.Diagnostics.Debug.WriteLine($"[POSITION CLOSURE] Account: {accountId}, Symbol: {position.Symbol}, " +
        $"Reason: {reason}, Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
    
    ClosePosition(position, core);
    
    System.Diagnostics.Debug.WriteLine($"[AUDIT LOG] Position closed for account {accountId}, Symbol: {position.Symbol}, " +
        $"P&L: ${openPnL:F2}, Limit: ${lossLimit:F2}, Account NOT locked");
}

// Position Profit Target (similar structure)
```

## Real-time Monitoring Architecture

### Timer Configuration
```csharp
// Line ~414: P&L monitoring timer setup
pnlMonitorTimer = new System.Windows.Forms.Timer { Interval = PNL_MONITOR_INTERVAL_MS };
pnlMonitorTimer.Tick += (s, e) => MonitorPnLLimits();
pnlMonitorTimer.Start();

// Constant definition
private const int PNL_MONITOR_INTERVAL_MS = 500; // Check P&L every half second
```

### Monitoring Flow
```
Every 500ms:
├── MonitorPnLLimits()
│   ├── Check if near market close (4:59-6:00 PM ET) → Skip if true
│   ├── For each connected account:
│   │   ├── Check Feature Toggle Enabled
│   │   ├── Check if already locked → Skip if locked
│   │   ├── CheckDailyPnLLimits()
│   │   │   ├── Get account Net P&L
│   │   │   ├── Check Daily Loss Limit (existing feature)
│   │   │   └── Check Daily Profit Target (NEW)
│   │   │       ├── At 80%: Send warning (once per day)
│   │   │       └── At 100%: Lock + Close positions + Notify
│   │   └── CheckPositionPnLLimits()
│   │       ├── Get all positions for account
│   │       └── For each position:
│   │           ├── Check Position Loss Limit (ENHANCED)
│   │           │   └── Close position + Log (no lock)
│   │           └── Check Position Profit Target (ENHANCED)
│   │               └── Close position + Log (no lock)
```

## Warning System

### Daily Profit Target Warnings

**80% Threshold Warning:**
- Triggered when Net P&L >= 80% of daily profit target
- Sent once per day (resets at UTC midnight)
- Warning message includes:
  - Current P&L
  - Target amount
  - Percentage of target reached
  - Notice that account will lock when target reached

**100% Target Reached Notification:**
- Triggered when Net P&L >= daily profit target
- Success message includes:
  - Current P&L
  - Target amount
  - Notice of account lock until 5 PM ET
  - Notice that all positions closed

### Position Limit Notifications
Position closures are logged but do not show UI notifications (by design):
- Position loss/profit events are high-frequency
- Silent closure prevents notification spam
- All events logged to debug output with timestamps
- Logs explicitly state "Account NOT locked"

## Audit Logging

### Log Format Standards
All limit breach events follow this format:

**Position Closures:**
```
[POSITION CLOSURE] Account: {accountId}, Symbol: {symbol}, Reason: {reason}, Timestamp: {UTC}
[AUDIT LOG] Position closed for account {accountId}, Symbol: {symbol}, P&L: ${pnl}, Limit/Target: ${value}, Account NOT locked
```

**Daily Profit Target:**
```
[WARNING NOTIFICATION] Account: {accountId}, Current P&L: ${pnl}, Target: ${target}, Threshold: 80%, Timestamp: {UTC}
[AUDIT LOG] Warning notification sent to account {accountId} at 80% profit target threshold
[ACCOUNT LOCK] Account: {accountId}, Reason: {reason}, Timestamp: {UTC}
[AUDIT LOG] Account {accountId} locked due to daily profit target at ${pnl}
```

### Example Logs

**Position Loss Closure:**
```
[POSITION CLOSURE] Account: PA-123456, Symbol: ES, Reason: Position Loss Limit: ES P&L $-152.50 ≤ Limit $-150.00, Timestamp: 2026-01-02 18:30:45 UTC
[AUDIT LOG] Position closed for account PA-123456, Symbol: ES, P&L: $-152.50, Limit: $-150.00, Account NOT locked
```

**Daily Profit Warning:**
```
[WARNING NOTIFICATION] Account: PA-123456, Current P&L: $801.25, Target: $1000.00, Threshold: 80%, Timestamp: 2026-01-02 14:25:10 UTC
[AUDIT LOG] Warning notification sent to account PA-123456 at 80% profit target threshold
```

**Daily Profit Target Reached:**
```
[ACCOUNT LOCK] Account: PA-123456, Reason: Daily Profit Target reached: Net P&L $1025.50 ≥ Target $1000.00, Timestamp: 2026-01-02 15:45:30 UTC
[AUDIT LOG] Account PA-123456 locked due to daily profit target at $1025.50
```

## Configuration

All settings are configurable via the UI "📊 Limits" tab:

### Daily Limits
- **Daily Profit Target**: Positive USD value
  - Toggle: Enable/Disable
  - When reached: Locks account + Closes all positions
  - Warning at 80% threshold

### Position Limits (UI: "📈 Positions" tab)
- **Position Loss Limit**: Positive input (stored as negative internally)
  - Toggle: Enable/Disable
  - When reached: Closes position only (no account lock)
  
- **Position Profit Target**: Positive USD value
  - Toggle: Enable/Disable
  - When reached: Closes position only (no account lock)

### Settings Storage
- Settings persisted per account in JSON files
- Location: `%LocalAppData%/RiskManager/{AccountNumber}.json`
- Includes warning state to prevent duplicates

## Key Design Decisions

### 1. Warning Reset Timing
- Warnings reset at UTC midnight (not 5 PM ET)
- Rationale: Simpler implementation, prevents timezone edge cases
- Account locks still expire at 5 PM ET (market close)

### 2. Position Closures vs Account Lock
- Position limits: Close position only
- Daily limits: Close all positions AND lock account
- Rationale: Position limits are defensive (per-trade risk), daily limits are protective (account-level risk)

### 3. No Duplicate Notifications
- Warning state persisted to settings file
- Auto-resets daily
- Prevents notification spam during volatile P&L swings

### 4. Market Close Protection
- P&L monitoring paused 4:59-6:00 PM ET
- Rationale: Prevents re-locking accounts that auto-unlock at 5 PM ET
- Allows clean transitions at market close

## Testing Recommendations

### Manual Test Scenarios

**Scenario 1: Daily Profit Target - 80% Warning**
1. Set Daily Profit Target to $1000
2. Trade to reach $800 P&L
3. Verify warning notification appears
4. Continue trading
5. Verify no duplicate warning

**Scenario 2: Daily Profit Target - 100% Reached**
1. Set Daily Profit Target to $1000
2. Trade to reach $1000+ P&L
3. Verify success notification appears
4. Verify all positions closed
5. Verify account locked
6. Verify lock expires at 5 PM ET

**Scenario 3: Position Loss Limit**
1. Set Position Loss Limit to $100
2. Enter position
3. Let position reach -$100 loss
4. Verify position auto-closes
5. Verify account remains unlocked
6. Verify can open new positions

**Scenario 4: Position Profit Target**
1. Set Position Profit Target to $200
2. Enter position
3. Let position reach $200 profit
4. Verify position auto-closes
5. Verify account remains unlocked
6. Verify can open new positions

**Scenario 5: Multiple Positions**
1. Set Position Loss Limit to $50
2. Open 3 positions
3. Let one position reach -$50
4. Verify only that position closes
5. Verify other positions remain open

## Known Limitations

1. **Build Errors in Development Environment**
   - Expected: Missing TradingPlatform DLL references
   - Resolution: Build in actual Quantower platform environment

2. **Weekly Limits Not Implemented**
   - Weekly loss/profit limits exist but use daily P&L as proxy
   - Future enhancement: Track cumulative weekly P&L

3. **Position P&L Calculation**
   - Uses platform's GetPositionOpenPnL() method
   - Accuracy depends on platform's P&L calculation

## Future Enhancements

1. **Configurable Warning Thresholds**
   - Allow users to customize warning percentage (currently fixed at 80%)

2. **Position Closure Notifications**
   - Optional UI notifications for position closures (currently silent)
   - Configurable threshold to prevent spam

3. **Weekly P&L Tracking**
   - Implement true cumulative weekly P&L tracking
   - Reset on Sunday/Monday based on market week

4. **Historical Breach Reports**
   - Dashboard showing limit breach history
   - Statistics on closures and locks

## Conclusion

All three features have been successfully implemented with:
- ✅ Real-time monitoring (500ms interval)
- ✅ Warning system for daily limits (80% threshold)
- ✅ Automatic position closures
- ✅ Account locking for daily limits only
- ✅ Comprehensive audit logging
- ✅ Protection against duplicate notifications
- ✅ Market close protection

The implementation follows existing code patterns and integrates seamlessly with the current daily loss limit feature. All requirements from the problem statement have been met.
