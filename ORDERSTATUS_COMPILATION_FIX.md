# OrderStatus.Working Compilation Error Fix

## Problem
The code was attempting to use `OrderStatus.Working`, but this enum value does not exist in the `OrderStatus` enum provided by the TradingPlatform.BusinessLayer API (QuantTower).

This caused compilation errors:
- 'OrderStatus' does not contain a definition for 'Working' (3 occurrences)

## Root Cause
A previous change incorrectly added `OrderStatus.Working` to three locations in the code:
1. Line 3038 - `UpdateLedIndicator()` method
2. Line 8372 - `CancelAllWorkingOrdersForAccount()` method
3. Line 8413 - `CancelAllWorkingOrders()` method

The assumption was that `OrderStatus.Working` existed in the QuantTower API, but this enum value is not available.

## Solution
Reverted the code to its original state by removing `|| order.Status == OrderStatus.Working` from all three locations.

The code now only checks for the two OrderStatus values that are confirmed to exist:
- `OrderStatus.Opened`
- `OrderStatus.PartiallyFilled`

```csharp
// Reverted code
(order.Status == OrderStatus.Opened || order.Status == OrderStatus.PartiallyFilled)
```

## Changes Made
1. **RiskManagerControl.cs line 3038** - Removed `OrderStatus.Working` from LED indicator order count
2. **RiskManagerControl.cs line 8372** - Removed `OrderStatus.Working` from account-specific order cancellation
3. **RiskManagerControl.cs line 8413** - Removed `OrderStatus.Working` from global order cancellation

## Impact
- ✅ Code now compiles successfully (no more OrderStatus.Working errors)
- ✅ Application can build without compilation errors
- ✅ Existing OrderStatus checks remain functional

## Status
The compilation errors have been resolved. The code is back to its original state and will compile successfully in the QuantTower environment.

## Note
The original issue "LED indicator does not work for open orders or open positions" may require a different investigation. The available `OrderStatus` enum values in QuantTower's TradingPlatform.BusinessLayer API appear to be:
- `Opened` - For open orders
- `PartiallyFilled` - For partially filled orders
- `Filled` - For completely filled orders
- `Canceled` - For canceled orders
- `Rejected` - For rejected orders

If the LED indicator is still not working correctly, the issue may lie elsewhere in the logic, not in the OrderStatus checks.
