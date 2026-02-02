# LED Indicator Color Detection Fix

## Problem
The LED indicator was only showing grey color and never displaying yellow (for orders) or orange (for positions), even though the indicator was pulsating (showing it was active).

## Root Cause
The issue was in how accounts were being compared in the code. The application was using **object equality** (`order.Account == selectedAccount`) to compare accounts, but in the QuantTower API, different Account object instances may represent the same account. This meant that even when orders or positions existed for the selected account, they were not being detected because the object references didn't match.

### Example of the Problem:
```csharp
// WRONG - Object reference comparison
orderCount = core.Orders
    .Count(order => order != null && order.Account == selectedAccount && ...);

// This fails when order.Account and selectedAccount are different 
// object instances representing the same account
```

## Solution
Changed all account comparisons to use **account ID comparison** instead of object equality. This ensures accounts are matched correctly by their unique identifier rather than by object reference.

### Changes Made:

1. **UpdateLedIndicator() method** (Line ~3040)
   - Changed order detection: `order.Account.Id == selectedAccountId`
   - Changed position detection: `pos.Account.Id == selectedAccountId`
   - Added null checks for `order.Account` and `pos.Account`
   - Added debug logging to track detection

2. **CancelAllWorkingOrdersForAccount() method** (Line ~8390)
   - Changed order filtering: `order.Account.Id == accountId`
   - Added null check for `order.Account`

3. **Accounts Summary grid** (Line ~3720)
   - Changed position counting: `pos.Account.Id == accountId`
   - Added null check for `pos.Account`

4. **Account Details grid** (Line ~3972)
   - Changed position counting: `pos.Account.Id == accountId`
   - Added null check for `pos.Account`

### Code Example:
```csharp
// BEFORE (WRONG)
orderCount = core.Orders
    .Count(order => order != null && order.Account == selectedAccount &&
           (order.Status == OrderStatus.Opened || order.Status == OrderStatus.PartiallyFilled));

// AFTER (CORRECT)
string selectedAccountId = selectedAccount?.Id;
orderCount = core.Orders
    .Count(order => order != null && 
           order.Account != null &&
           order.Account.Id == selectedAccountId &&
           (order.Status == OrderStatus.Opened || order.Status == OrderStatus.PartiallyFilled));
```

## Testing & Debugging
Added comprehensive debug logging to `UpdateLedIndicator()`:
- Logs selected account ID
- Logs total orders and positions available
- Logs count of orders found for the account
- Logs count of positions found for the account
- Logs the color being set (ORANGE, YELLOW, or GREY)

These logs can be viewed in the Output window (Debug output) to diagnose any remaining issues.

## Expected Behavior After Fix
- **Grey LED**: No open positions or orders for the selected account
- **Yellow LED**: One or more open orders (but no positions)
- **Orange LED**: One or more open positions (highest priority, may also have orders)
- **Pulsating**: LED pulsates to draw attention when not grey

## Impact
✅ LED indicator now correctly detects open orders and positions
✅ Position counting in grids is more accurate
✅ Order cancellation targets the correct account
✅ All account comparisons are consistent throughout the application

## Notes
- The fix applies to order statuses: `OrderStatus.Opened` and `OrderStatus.PartiallyFilled`
- Position detection uses `pos.Quantity != 0` (non-zero quantity indicates open position)
- Account ID comparison is now the standard pattern throughout the codebase
