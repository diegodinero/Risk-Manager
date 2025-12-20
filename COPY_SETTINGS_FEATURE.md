# Copy Settings Feature

## Overview
The Copy Settings feature enables users to efficiently replicate risk management settings from one trading account to multiple target accounts in a single operation. This streamlines the process of managing multiple accounts with similar risk parameters.

## Location
The feature is accessible via the **"📋 Copy Settings"** tab in the main navigation menu.

## User Interface

### Components

1. **Source Account Dropdown**
   - Displays all connected trading accounts
   - User selects the account to copy settings from
   - Only accounts with active connections are shown

2. **Target Accounts Selection Panel**
   - Automatically populated when a source account is selected
   - Shows checkboxes for all accounts except the source
   - Each checkbox displays the account name and identifier
   - If only one account exists, shows "No other accounts available"

3. **Quick Selection Buttons**
   - **Select All**: Checks all target account checkboxes
   - **Deselect All**: Unchecks all target account checkboxes
   - Useful when managing many accounts

4. **Copy Button**
   - Located at the bottom of the panel
   - Text: "COPY SETTINGS TO SELECTED ACCOUNTS"
   - Triggers the copy operation after validation

## Usage Workflow

1. **Navigate to Copy Settings Tab**
   - Click on "📋 Copy Settings" in the left navigation menu

2. **Select Source Account**
   - Choose the account to copy settings from using the dropdown
   - Target accounts list will automatically update

3. **Select Target Accounts**
   - Check the boxes for accounts you want to copy settings to
   - Use "Select All" for bulk selection if needed

4. **Initiate Copy**
   - Click "COPY SETTINGS TO SELECTED ACCOUNTS"
   - A confirmation dialog will appear

5. **Confirm Operation**
   - Review the source account and number of targets
   - Click "Yes" to proceed or "No" to cancel
   - Warning: This will overwrite existing settings on target accounts

6. **View Results**
   - A results dialog shows success/failure for each account
   - Successful operations are marked with ✓
   - Failed operations are marked with ✗ and include error messages
   - Checkboxes are automatically cleared on successful copy

## Settings Copied

The following settings are replicated from source to target accounts:

### Risk Limits
- Daily Loss Limit
- Daily Profit Target
- Position Loss Limit
- Position Profit Target
- Weekly Loss Limit
- Weekly Profit Target

### Contract Management
- Default Contract Limit
- Symbol-Specific Contract Limits

### Symbol Controls
- Blocked Symbols List

### Time Restrictions
- Trading Time Restrictions (day, start time, end time, allowed status)

### Lock States
- Trading Lock (status, time, day, reason)
- Settings Lock (status, time, day, reason)

### Feature Toggles
- Feature Toggle Enabled/Disabled State

## What's NOT Copied

The following are preserved for each account:

- **Account Number**: Each account maintains its unique identifier
- **Created At Timestamp**: Original creation time is preserved
- **Updated At Timestamp**: Set to current time after copy

## Validation & Error Handling

### Pre-Copy Validation
- **No Source Selected**: Shows warning "Please select a source account to copy settings from"
- **No Targets Selected**: Shows warning "Please select at least one target account to copy settings to"
- **Source Has No Settings**: Error message if source account has no saved settings

### During Copy
- Individual account failures are caught and reported
- One account failure doesn't prevent others from succeeding
- Each target account gets its own success/failure status

### Post-Copy Feedback
- Success count and failure count displayed
- For failures, specific account names and error messages shown
- Message box icon indicates overall status (success vs. warnings)

## Backend Implementation

### API Method
```csharp
public Dictionary<string, (bool Success, string Message)> CopySettingsToAccounts(
    string sourceAccountNumber, 
    IEnumerable<string> targetAccountNumbers)
```

### Process Flow
1. Validate source and target account parameters
2. Load source account settings from JSON file
3. For each target account:
   - Get or create settings object
   - Deep copy all settings from source
   - Preserve account identity and timestamps
   - Save to target account's JSON file
   - Record success/failure result
4. Return dictionary of results

### Thread Safety
- Uses existing file locking mechanism from settings service
- Safe for concurrent use with other settings operations
- Cache is updated automatically for modified accounts

## Example Scenarios

### Scenario 1: Setting Up Multiple Demo Accounts
**Goal**: Copy conservative risk settings to all demo accounts

1. Configure risk limits on Demo Account 1
2. Navigate to Copy Settings
3. Select "Demo Account 1" as source
4. Click "Select All" (or individually select demo accounts)
5. Click copy button and confirm
6. All demo accounts now have identical risk parameters

### Scenario 2: Migrating Production Settings
**Goal**: Apply tested settings from one live account to others

1. Verify settings on Live Account 1 (already tested)
2. Use Copy Settings feature
3. Select "Live Account 1" as source
4. Carefully select specific live accounts as targets
5. Review confirmation carefully
6. Confirm and verify success for each account

### Scenario 3: Copying to Specific Accounts
**Goal**: Apply high-frequency trading settings to HFT accounts only

1. Select source account with HFT settings
2. Manually check only HFT-designated accounts
3. Leave other account types unchecked
4. Execute copy operation
5. HFT accounts updated, others unchanged

## Best Practices

1. **Test First**: Test settings on one account before copying to many
2. **Review Before Copying**: Double-check source account settings
3. **Use Confirmation Dialog**: Read the confirmation message carefully
4. **Check Results**: Review the results dialog for any failures
5. **Backup Important Settings**: Keep notes of custom configurations
6. **Verify After Copy**: Spot-check a few target accounts to ensure settings applied correctly

## Troubleshooting

### Issue: "No settings found for source account"
**Solution**: Ensure the source account has saved settings. Navigate to other tabs and save settings first.

### Issue: Copy succeeds but settings don't appear
**Solution**: The target account may be cached. Try refreshing the account selector or reloading the settings.

### Issue: Some accounts fail while others succeed
**Solution**: Check the error messages in the results dialog. Common causes:
- File system permissions
- Disk space issues
- Invalid account identifiers

### Issue: Can't find source account in dropdown
**Solution**: Ensure the account has an active connection. Check the connection status in the main account selector.

## Technical Notes

### File Storage
- Settings stored in: `%LocalAppData%\RiskManager\{AccountNumber}.json`
- Each account has its own file
- Files are created if they don't exist

### Account Identifiers
- Accounts identified using Connection Name + Account Name/ID
- Ensures uniqueness across different brokers/connections
- Same logic used throughout the application

### Performance
- Copy operation is fast (milliseconds per account)
- No network calls required (local file operations)
- Suitable for copying to dozens of accounts simultaneously

## Related Documentation

- [IMPLEMENTATION_SUMMARY.md](IMPLEMENTATION_SUMMARY.md) - Overall implementation details
- [SETTINGS_STRUCTURE.md](SETTINGS_STRUCTURE.md) - JSON settings structure and API reference

## Version History

### Version 1.0 (Current)
- Initial implementation of Copy Settings feature
- Support for all existing risk management settings
- Multi-account selection with validation
- Detailed success/failure feedback
- Integration with existing settings service
