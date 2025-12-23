# Daily Loss Limit Feature

## Overview
The Daily Loss Limit feature provides critical risk management functionality by monitoring Gross Profit and Loss (P&L) in real-time and automatically locking accounts when configurable loss limits are breached. This feature ensures traders can better control their trading exposure and prevent catastrophic losses.

## Key Features

### 1. Real-Time Gross P&L Monitoring
- Continuously monitors **Gross P&L** every 500ms (half second)
- Uses account's `Gross P&L` or `GrossPnL` field from AdditionalInfo
- Falls back to `TotalPnL` if Gross P&L is unavailable
- All monitoring activity is logged for audit purposes

### 2. Configurable Loss Limits
- Individual accounts can have different daily loss limits
- Limits are configured as **negative values** (e.g., -$1000)
- Settings are persisted per account in JSON files
- Located at: `%LocalAppData%\RiskManager\{AccountNumber}.json`

### 3. Warning Notifications (80% Threshold)
When an account reaches **80% of its daily loss limit**:
- ⚠️ Visual warning dialog is displayed
- 🔊 Audio alert is played
- Shows current P&L vs limit
- Displays percentage of limit reached
- Warning is sent **only once per day** to prevent spam
- Automatically resets at the start of a new trading day

**Example Warning:**
```
⚠️ Warning: Account ABC123 is approaching daily loss limit!

Current Gross P&L: $-820.00
Daily Loss Limit: $-1000.00
You are at 82% of your limit.

Account will be locked if limit is reached.
```

### 4. Automatic Account Lock
When the loss limit is breached:
- ✅ Account is immediately locked
- ✅ All open positions are closed automatically
- ✅ Trading is disabled until 5 PM ET
- ✅ Lock reason is recorded with timestamp
- ✅ User receives lock notification

**Example Lock Notification:**
```
🔒 ACCOUNT LOCKED!

Account: ABC123
Current Gross P&L: $-1050.00
Daily Loss Limit: $-1000.00

Your account has been locked until 5 PM ET due to exceeding the daily loss limit.
All positions have been closed.

Please contact an administrator if you need to unlock the account before the scheduled time.
```

### 5. Admin Override Mechanism
Administrators can manually unlock accounts at any time:
1. Navigate to the "Manual Lock" tab
2. Select the locked account
3. Click the "Unlock Trading" button
4. Account is immediately unlocked
5. Action is logged with timestamp and reason

### 6. Comprehensive Audit Logging
All actions are logged to the Debug console with timestamps (UTC):

**P&L Evaluation Logs:**
```
[P&L Evaluation] Account: ABC123, Gross P&L: $-750.00, Loss Limit: $-1000.00, Profit Target: None
```

**Warning Notification Logs:**
```
[WARNING NOTIFICATION] Account: ABC123, Current P&L: $-820.00, Limit: $-1000.00, Threshold: 80%, Timestamp: 2025-12-23 14:30:15 UTC
[AUDIT LOG] Warning notification sent to account ABC123 at 80% threshold
```

**Account Lock Logs:**
```
[ACCOUNT LOCK] Account: ABC123, Reason: Daily Loss Limit reached: Gross P&L $-1050.00 ≤ Limit $-1000.00, Timestamp: 2025-12-23 14:35:22 UTC
[AUDIT LOG] Account ABC123 locked due to daily loss limit breach at $-1050.00
```

**Admin Override Logs:**
```
[ADMIN OVERRIDE] Account: ABC123, Action: Manual Unlock, User: Admin, Timestamp: 2025-12-23 15:00:00 UTC, Reason: Manual override via Unlock Trading button
```

## Configuration

### Setting Up Daily Loss Limits

1. **Navigate to the Limits Tab:**
   - Open Risk Manager
   - Select an account from the dropdown
   - Click on "📊 Limits" in the left navigation

2. **Configure Daily Loss Limit:**
   - Enable "Daily Loss Limit"
   - Enter a **negative value** (e.g., -1000 for $1000 loss limit)
   - Click "Save Settings"

3. **Verify Configuration:**
   - Check the "Feature Toggles" tab to ensure the master switch is enabled
   - Settings are automatically saved to disk

### JSON Configuration Example
```json
{
  "accountNumber": "ABC123",
  "featureToggleEnabled": true,
  "dailyLossLimit": -1000.00,
  "dailyProfitTarget": 2000.00,
  "dailyLossWarning": {
    "warningNotificationSent": true,
    "warningDate": "2025-12-23T00:00:00Z",
    "warningPnLValue": -820.00
  },
  "tradingLock": null,
  "createdAt": "2025-12-23T10:00:00Z",
  "updatedAt": "2025-12-23T14:30:15Z"
}
```

## How It Works

### Monitoring Flow
```
1. Timer triggers every 500ms
   ↓
2. For each account:
   - Get Gross P&L from AdditionalInfo
   - Compare against configured limits
   ↓
3. If at 80% of limit:
   - Check if warning already sent today
   - If not, send warning notification
   - Mark warning as sent
   ↓
4. If limit breached:
   - Lock account until 5 PM ET
   - Close all open positions
   - Send lock notification
   - Log all actions
```

### Warning State Management
- Warning state is stored per account
- Includes: notification sent flag, date, and P&L value
- Automatically resets at day change (UTC midnight)
- Prevents duplicate warnings within the same day

### Lock Duration
- Default: Locked until 5 PM Eastern Time (ET)
- Uses timezone-aware calculation
- Can be manually unlocked by admin at any time
- Lock expires automatically at 5 PM ET

## Best Practices

### For Traders
1. ✅ Set realistic loss limits based on account size
2. ✅ Monitor the 80% warning carefully
3. ✅ Consider reducing position sizes when warning appears
4. ✅ Contact admin immediately if you need an emergency unlock

### For Administrators
1. ✅ Review audit logs regularly for compliance
2. ✅ Set appropriate loss limits for different account types
3. ✅ Use admin override judiciously and document reasons
4. ✅ Monitor accounts approaching their limits
5. ✅ Review lock history to identify problematic patterns

### For Risk Managers
1. ✅ Analyze audit logs for risk trends
2. ✅ Adjust limits based on account performance
3. ✅ Set up alerts for multiple lock events
4. ✅ Review Gross P&L patterns across accounts
5. ✅ Use warning notifications as early indicators

## Technical Details

### Data Structures

**DailyLossWarningInfo:**
```csharp
public class DailyLossWarningInfo
{
    public bool WarningNotificationSent { get; set; }
    public DateTime? WarningDate { get; set; }
    public decimal? WarningPnLValue { get; set; }
}
```

**AccountSettings (Enhanced):**
```csharp
public class AccountSettings
{
    // Existing fields...
    public decimal? DailyLossLimit { get; set; }
    public decimal? DailyProfitTarget { get; set; }
    
    // New fields for warning tracking
    public DailyLossWarningInfo? DailyLossWarning { get; set; }
}
```

### Key Methods

**Gross P&L Retrieval:**
```csharp
private double GetAccountGrossPnL(Account account)
```
- Searches for "Gross P&L" or "GrossPnL" in AdditionalInfo
- Falls back to TotalPnL if not found
- Logs retrieval for audit

**Daily Loss Limit Checking:**
```csharp
private void CheckDailyPnLLimits(Account account, string accountId, AccountSettings settings, Core core)
```
- Evaluates Gross P&L against limits
- Handles 80% warning threshold
- Triggers lock and position closure on breach
- Comprehensive logging

**Warning State Management:**
```csharp
public void SetDailyLossWarningSent(string accountNumber, decimal pnlValue)
public bool HasDailyLossWarningSent(string accountNumber)
public void ResetDailyLossWarning(string accountNumber)
```

## Troubleshooting

### Warning Not Appearing
- Verify Daily Loss Limit is configured (negative value)
- Check Feature Toggle is enabled
- Ensure P&L is at least 80% of limit
- Check if warning was already sent today
- Review Debug logs for errors

### Account Not Locking
- Verify Gross P&L has actually exceeded the limit
- Check Core.LockAccount method is available
- Review Debug logs for lock attempts
- Ensure Feature Toggle is enabled

### Gross P&L Not Found
- Check if account's AdditionalInfo contains "Gross P&L" or "GrossPnL" field
- System will fallback to TotalPnL automatically
- Review Debug logs for fallback messages
- Contact broker if P&L fields are missing

### Admin Unlock Not Working
- Verify Core.UnLockAccount method is available
- Check account identifier is correct
- Review Debug logs for unlock attempts
- Try manual unlock from Manual Lock tab

## Compliance & Auditing

### Log Locations
- **Debug Console:** Real-time monitoring during development
- **Audit Trail:** All logs include UTC timestamps
- **Settings Files:** `%LocalAppData%\RiskManager\{AccountNumber}.json`

### Audit Log Categories
1. **[P&L Evaluation]** - Regular monitoring checks
2. **[WARNING NOTIFICATION]** - 80% threshold warnings
3. **[ACCOUNT LOCK]** - Loss limit breaches
4. **[ADMIN OVERRIDE]** - Manual lock/unlock actions
5. **[ADMIN ACTION]** - Manual lock actions
6. **[AUDIT LOG]** - High-level audit entries
7. **[ERROR]** - Error conditions

### Regulatory Compliance
- All limit evaluations are logged
- Timestamps are in UTC for consistency
- Account locks are auditable
- Admin overrides are tracked
- P&L values are recorded at each action

## Future Enhancements

### Potential Improvements
- [ ] Email/SMS notifications for warnings and locks
- [ ] Weekly and monthly loss limit tracking
- [ ] Configurable warning threshold (not just 80%)
- [ ] Multiple warning levels (50%, 80%, 95%)
- [ ] Historical lock event reporting
- [ ] Risk analytics dashboard
- [ ] Machine learning for risk prediction

## Support

For questions or issues:
1. Review Debug logs in your development environment
2. Check settings files in `%LocalAppData%\RiskManager\`
3. Verify account configuration in the UI
4. Review this documentation for configuration steps

## Version History

### Version 1.0 (2025-12-23)
- Initial implementation
- Gross P&L monitoring
- 80% warning threshold
- Automatic account lock
- Comprehensive audit logging
- Admin override capability
- Warning state management with daily reset
