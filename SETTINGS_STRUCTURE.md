# Per-Account Settings Structure

## Overview
The Risk Manager stores settings per account in JSON files located at:
- Windows: `%LocalAppData%\RiskManager\`
- Each account has its own file: `{AccountNumber}.json`

## JSON Structure Example

```json
{
  "accountNumber": "account1",
  "featureToggleEnabled": true,
  "dailyLossLimit": 5000,
  "dailyProfitTarget": 10000,
  "positionLossLimit": 1000,
  "positionProfitTarget": 2000,
  "weeklyLossLimit": 20000,
  "weeklyProfitTarget": 50000,
  "defaultContractLimit": 100,
  "createdAt": "2024-12-19T16:00:00Z",
  "updatedAt": "2024-12-19T16:30:00Z",
  "blockedSymbols": [
    "AAPL",
    "TSLA"
  ],
  "symbolContractLimits": {
    "MSFT": 50,
    "GOOGL": 75
  },
  "tradingTimeRestrictions": [
    {
      "dayOfWeek": 1,
      "startTime": "09:30:00",
      "endTime": "16:00:00",
      "isAllowed": true,
      "name": "Regular Trading Hours"
    }
  ],
  "tradingLock": {
    "isLocked": false,
    "lockTime": null,
    "lockDayOfWeek": null,
    "lockReason": null
  },
  "settingsLock": {
    "isLocked": false,
    "lockTime": null,
    "lockDayOfWeek": null,
    "lockReason": null
  }
}
```

## Field Descriptions

### Core Settings
- **accountNumber**: Unique identifier for the account
- **featureToggleEnabled**: Master toggle for all risk management features (default: true)
- **createdAt**: UTC timestamp when settings were first created
- **updatedAt**: UTC timestamp when settings were last modified

### Daily Limits
- **dailyLossLimit**: Maximum loss allowed per day in USD (nullable)
- **dailyProfitTarget**: Target profit per day in USD (nullable)

### Position Limits
- **positionLossLimit**: Maximum loss allowed per position in USD (nullable)
- **positionProfitTarget**: Target profit per position in USD (nullable)

### Weekly Limits
- **weeklyLossLimit**: Maximum loss allowed per week in USD (nullable)
- **weeklyProfitTarget**: Target profit per week in USD (nullable)

### Symbol Management
- **blockedSymbols**: Array of symbol names that are blocked from trading
- **symbolContractLimits**: Dictionary mapping symbol names to contract limits
- **defaultContractLimit**: Default contract limit for symbols not in symbolContractLimits (nullable)

### Trading Time Restrictions
Array of time windows defining when trading is allowed:
- **dayOfWeek**: Day of week (0=Sunday, 1=Monday, etc.)
- **startTime**: Start time in HH:mm:ss format
- **endTime**: End time in HH:mm:ss format
- **isAllowed**: Whether trading is allowed in this window
- **name**: Human-readable name for this restriction

### Locks
Both tradingLock and settingsLock have the same structure:
- **isLocked**: Whether the lock is active
- **lockTime**: UTC timestamp when lock was activated (null if unlocked)
- **lockDayOfWeek**: Day of week when locked (null if unlocked)
- **lockReason**: Reason for locking (nullable string)

## Validation Rules

1. **Non-negative Values**: All limit and target fields must be non-negative (>= 0)
2. **Positive Contract Limits**: Contract limits must be positive (> 0) if specified
3. **Thread Safety**: File operations are protected by locks for concurrent access
4. **Caching**: Settings are cached for 30 seconds to reduce file I/O

## API Usage Examples

### Loading Settings
```csharp
var service = RiskManagerSettingsService.Instance;
var settings = service.GetSettings("account1");
```

### Updating Daily Limits
```csharp
service.UpdateDailyLossLimit("account1", 5000m);
service.UpdateDailyProfitTarget("account1", 10000m);
```

### Managing Symbol Blacklist
```csharp
service.SetBlockedSymbols("account1", new[] { "AAPL", "TSLA" });
bool isBlocked = service.IsSymbolBlocked("account1", "AAPL"); // true
```

### Setting Locks
```csharp
service.SetTradingLock("account1", true, "Daily loss limit exceeded");
service.SetSettingsLock("account1", true, "Manual lock");
```

### Copying Settings Between Accounts
```csharp
// Copy all settings from account1 to account2 and account3
var results = service.CopySettingsToAccounts(
    "account1", 
    new[] { "account2", "account3" }
);

// Check results
foreach (var result in results)
{
    if (result.Value.Success)
        Console.WriteLine($"✓ {result.Key}: {result.Value.Message}");
    else
        Console.WriteLine($"✗ {result.Key}: {result.Value.Message}");
}
```

## UI Integration

The Risk Manager UI automatically:
1. Loads settings when an account is selected from the dropdown
2. Saves all settings when the "SAVE SETTINGS" button is clicked
3. Validates input values before saving
4. Updates status badges to reflect lock states
5. Shows error messages for validation failures
6. **NEW:** Provides "Copy Settings" tab for bulk settings replication across accounts

## Copy Settings Feature

**Location:** "📋 Copy Settings" tab in navigation menu

**Functionality:**
- Select a source account to copy settings from
- Select multiple target accounts via checkboxes
- Use "Select All" / "Deselect All" for quick selection
- Confirmation dialog before copying
- Detailed success/failure feedback per account

**What Gets Copied:**
- All risk limits (daily, weekly, position)
- Feature toggle states
- Symbol blocks and contract limits
- Trading time restrictions
- Lock states

**What Doesn't Get Copied:**
- Account number (each account keeps its identity)
- Timestamps (each account maintains its own history)

## Migration Notes

Existing accounts without settings files will automatically have settings created with default values when first accessed.
