# Trading Times Lock Feature - Architecture Diagram

## System Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                     Risk Manager Application                     │
├─────────────────────────────────────────────────────────────────┤
│                                                                   │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │         RiskManagerControl.cs (Frontend)                 │   │
│  │                                                           │   │
│  │  ┌─────────────────────────────────────────────────┐    │   │
│  │  │  MonitorPnLLimits() Timer (500ms interval)     │    │   │
│  │  │                                                  │    │   │
│  │  │  For each connected account:                   │    │   │
│  │  │  1. ─── Call EnforceTradingTimeLocks() ────┐   │    │   │
│  │  │  2. Check if locked (skip if locked)       │   │    │   │
│  │  │  3. Check other limits (P&L, positions)    │   │    │   │
│  │  └──────────────────────────────────────────────┘   │    │   │
│  │                              │                        │   │   │
│  │                              ▼                        │   │   │
│  │  ┌──────────────────────────────────────────────┐   │   │   │
│  │  │  EnforceTradingTimeLocks(accountId)          │   │   │   │
│  │  │                                               │   │   │   │
│  │  │  1. Call service.CheckAndEnforceTradeTime... │   │   │   │
│  │  │  2. Update badge if current account          │   │   │   │
│  │  └──────────────────────────────────────────────┘   │   │   │
│  │                              │                        │   │   │
│  └──────────────────────────────┼────────────────────────┘   │
│                                 │                              │
│                                 ▼                              │
│  ┌─────────────────────────────────────────────────────────┐ │
│  │    RiskManagerSettingsService.cs (Backend)              │ │
│  │                                                          │ │
│  │  ┌────────────────────────────────────────────────┐    │ │
│  │  │  CheckAndEnforceTradeTimeLocks(accountId)     │    │ │
│  │  │                                                 │    │ │
│  │  │  1. Call IsTradingAllowedNow() ──────────┐    │    │ │
│  │  │  2. Call IsTradingLocked() ──────────┐   │    │    │ │
│  │  │                                       │   │    │    │ │
│  │  │  Decision Matrix:                    │   │    │    │ │
│  │  │  ┌────────────────────────────────┐  │   │    │    │ │
│  │  │  │ Allowed? │ Locked? │ Action    │  │   │    │    │ │
│  │  │  ├──────────┼─────────┼───────────┤  │   │    │    │ │
│  │  │  │   No     │   No    │ LOCK      │  │   │    │    │ │
│  │  │  │   No     │   Yes   │ Keep Lock │  │   │    │    │ │
│  │  │  │   Yes    │   No    │ No Action │  │   │    │    │ │
│  │  │  │   Yes    │   Yes   │ UNLOCK*   │  │   │    │    │ │
│  │  │  └────────────────────────────────┘  │   │    │    │ │
│  │  │  * Only if lock reason = "Trading times"  │   │    │ │
│  │  └────────────────────────────────────────────┘   │    │ │
│  │                        │                │           │    │ │
│  │                        ▼                ▼           │    │ │
│  │  ┌──────────────────────────────────────────────┐ │    │ │
│  │  │  IsTradingAllowedNow(accountId)              │ │    │ │
│  │  │                                               │ │    │ │
│  │  │  1. Get account settings                     │ │    │ │
│  │  │  2. Check if locked → return false           │ │    │ │
│  │  │  3. Convert UTC → EST                        │ │    │ │
│  │  │  4. Get current day/time in EST              │ │    │ │
│  │  │  5. Check restrictions for current day       │ │    │ │
│  │  │  6. Return true if in allowed window         │ │    │ │
│  │  └──────────────────────────────────────────────┘ │    │ │
│  │                                                     │    │ │
│  │  ┌──────────────────────────────────────────────┐ │    │ │
│  │  │  GetTradingLockDuration(accountId)           │ │    │ │
│  │  │                                               │ │    │ │
│  │  │  1. Get account settings                     │ │    │ │
│  │  │  2. Convert UTC → EST                        │ │    │ │
│  │  │  3. Check if in allowed window → null        │ │    │ │
│  │  │  4. Find next allowed window (up to 7 days)  │ │    │ │
│  │  │  5. Calculate duration to 5 PM ET            │ │    │ │
│  │  │  6. Return min(next window, 5 PM ET)         │ │    │ │
│  │  └──────────────────────────────────────────────┘ │    │ │
│  │                                                     │    │ │
│  └─────────────────────────────────────────────────────────┘ │
│                                                                 │
│  ┌─────────────────────────────────────────────────────────┐  │
│  │               Settings Storage (JSON Files)              │  │
│  │                                                           │  │
│  │  AccountSettings:                                        │  │
│  │  {                                                       │  │
│  │    "tradingTimeRestrictions": [                         │  │
│  │      {                                                   │  │
│  │        "dayOfWeek": "Monday",                           │  │
│  │        "startTime": "08:00:00",  // EST                 │  │
│  │        "endTime": "17:00:00",    // EST                 │  │
│  │        "isAllowed": true,                               │  │
│  │        "name": "NY Session"                             │  │
│  │      }                                                   │  │
│  │    ],                                                    │  │
│  │    "tradingLock": {                                     │  │
│  │      "isLocked": true/false,                            │  │
│  │      "lockReason": "Outside allowed trading times",     │  │
│  │      "lockExpirationTime": "2026-01-03T13:00:00Z"      │  │
│  │    }                                                     │  │
│  │  }                                                       │  │
│  └─────────────────────────────────────────────────────────┘  │
└───────────────────────────────────────────────────────────────┘
```

## Time Flow Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│                        Time Enforcement Flow                     │
└─────────────────────────────────────────────────────────────────┘

System Time (UTC): 2026-01-02 18:00:00 UTC
                        ↓
                Convert to EST
                        ↓
EST Time: 2026-01-02 13:00:00 EST (1:00 PM)

┌─────────────────────────────────────────────────────────────────┐
│              Trading Time Configuration (EST)                    │
├─────────────────────────────────────────────────────────────────┤
│  Monday:    NY Session  8 AM - 5 PM  [✓ Enabled]               │
│  Tuesday:   NY Session  8 AM - 5 PM  [✓ Enabled]               │
│  Wednesday: NY Session  8 AM - 5 PM  [✓ Enabled]               │
│  Thursday:  NY Session  8 AM - 5 PM  [✓ Enabled]               │
│  Friday:    NY Session  8 AM - 5 PM  [✓ Enabled]               │
│  Saturday:  No restrictions configured                          │
│  Sunday:    No restrictions configured                          │
└─────────────────────────────────────────────────────────────────┘

Current: Thursday 1:00 PM EST
         └─── Is in allowed window (8 AM - 5 PM)? YES ✓
         └─── Action: Keep Unlocked (if not locked by other reason)

┌─────────────────────────────────────────────────────────────────┐
│                    Lock Duration Calculation                     │
└─────────────────────────────────────────────────────────────────┘

Example 1: Current Time = Thursday 6:00 PM EST
           └─── Outside allowed window (NY session ends at 5 PM)
           └─── Next allowed window: Friday 8:00 AM EST
           └─── Duration = 14 hours (6 PM Thu → 8 AM Fri)
           └─── 5 PM ET = Friday 5:00 PM EST
           └─── Duration to 5 PM = 23 hours
           └─── LOCK FOR: 14 hours (shorter duration)

Example 2: Current Time = Friday 6:00 PM EST
           └─── Outside allowed window (NY session ends at 5 PM)
           └─── Next allowed window: Monday 8:00 AM EST
           └─── Duration = 62 hours (6 PM Fri → 8 AM Mon)
           └─── 5 PM ET = Saturday 5:00 PM EST
           └─── Duration to 5 PM = 23 hours
           └─── LOCK FOR: 23 hours (5 PM ET is sooner)

Example 3: Current Time = Saturday 10:00 AM EST
           └─── No restrictions configured for Saturday
           └─── Trading allowed 24/7 on Saturday
           └─── NO LOCK
```

## Lock Decision Matrix

```
┌─────────────────────────────────────────────────────────────────┐
│                 Lock/Unlock Decision Logic                       │
└─────────────────────────────────────────────────────────────────┘

Input State:
  - Is Trading Allowed Now?  (from IsTradingAllowedNow)
  - Is Currently Locked?     (from IsTradingLocked)
  - Lock Reason              (from settings.TradingLock.LockReason)

┌──────────────┬───────────┬─────────────────────┬───────────────┐
│  Trading     │ Currently │   Lock Reason       │    Action     │
│  Allowed?    │  Locked?  │                     │               │
├──────────────┼───────────┼─────────────────────┼───────────────┤
│    NO        │    NO     │    N/A              │  LOCK         │
│              │           │                     │  - Get lock   │
│              │           │                     │    duration   │
│              │           │                     │  - Set reason │
│              │           │                     │  - Save lock  │
├──────────────┼───────────┼─────────────────────┼───────────────┤
│    NO        │   YES     │    Any              │  KEEP LOCKED  │
│              │           │                     │  - No change  │
├──────────────┼───────────┼─────────────────────┼───────────────┤
│    YES       │    NO     │    N/A              │  NO ACTION    │
│              │           │                     │  - Already    │
│              │           │                     │    unlocked   │
├──────────────┼───────────┼─────────────────────┼───────────────┤
│    YES       │   YES     │ "Daily Loss Limit"  │  KEEP LOCKED  │
│              │           │ "Daily Profit..."   │  - Day lock   │
│              │           │ "Manual lock"       │    takes      │
│              │           │                     │    precedence │
├──────────────┼───────────┼─────────────────────┼───────────────┤
│    YES       │   YES     │ "Outside allowed    │  UNLOCK       │
│              │           │  trading times"     │  - Check exp  │
│              │           │ "Trading times"     │  - Clear lock │
│              │           │                     │  - Set reason │
└──────────────┴───────────┴─────────────────────┴───────────────┘
```

## Sequence Diagram

```
┌─────────────────────────────────────────────────────────────────┐
│              Account Lock/Unlock Sequence                        │
└─────────────────────────────────────────────────────────────────┘

Timer (500ms)           Frontend              Backend              Storage
     │                      │                     │                    │
     │─── Tick ────────────>│                     │                    │
     │                      │                     │                    │
     │                      │─── EnforceTrading...>                    │
     │                      │      TimeLocks      │                    │
     │                      │                     │                    │
     │                      │                     │─── Get Settings ──>│
     │                      │                     │<── Settings ───────│
     │                      │                     │                    │
     │                      │                     │─ IsTradingAllowed? │
     │                      │                     │  (Convert UTC→EST) │
     │                      │                     │  (Check windows)   │
     │                      │                     │<── Result: NO      │
     │                      │                     │                    │
     │                      │                     │─ GetLockDuration   │
     │                      │                     │  (Find next window)│
     │                      │                     │  (Compare w/ 5PM)  │
     │                      │                     │<── Duration: 2hrs  │
     │                      │                     │                    │
     │                      │                     │─── SetTradingLock ─>
     │                      │                     │    (reason + dur)  │
     │                      │                     │<── Saved ──────────│
     │                      │                     │                    │
     │                      │<── Lock Applied ────│                    │
     │                      │                     │                    │
     │                      │─ UpdateBadge()      │                    │
     │                      │  "Locked (2h 0m)"   │                    │
     │                      │                     │                    │
     │<── Complete ─────────│                     │                    │
     │                      │                     │                    │
     │                      │                     │                    │
     │                  [2 hours later]           │                    │
     │                      │                     │                    │
     │─── Tick ────────────>│                     │                    │
     │                      │─── EnforceTrading...>                    │
     │                      │                     │─── Get Settings ──>│
     │                      │                     │<── Settings ───────│
     │                      │                     │                    │
     │                      │                     │─ IsTradingAllowed? │
     │                      │                     │<── Result: YES     │
     │                      │                     │                    │
     │                      │                     │─ IsTradingLocked?  │
     │                      │                     │<── Result: YES     │
     │                      │                     │                    │
     │                      │                     │─ Check Lock Reason │
     │                      │                     │  "Outside allowed  │
     │                      │                     │   trading times"   │
     │                      │                     │                    │
     │                      │                     │─ Check Expiration  │
     │                      │                     │  Now > Expiration? │
     │                      │                     │<── YES             │
     │                      │                     │                    │
     │                      │                     │─── SetTradingLock ─>
     │                      │                     │    (unlock)        │
     │                      │                     │<── Saved ──────────│
     │                      │                     │                    │
     │                      │<── Unlocked ────────│                    │
     │                      │─ UpdateBadge()      │                    │
     │                      │  "Unlocked"         │                    │
     │<── Complete ─────────│                     │                    │
```

## Data Model

```
┌─────────────────────────────────────────────────────────────────┐
│                    AccountSettings JSON                          │
└─────────────────────────────────────────────────────────────────┘

{
  "accountNumber": "SIM123456",
  "featureToggleEnabled": true,
  
  "tradingTimeRestrictions": [
    {
      "dayOfWeek": "Monday",          // DayOfWeek enum
      "startTime": "08:00:00",        // TimeSpan (EST)
      "endTime": "17:00:00",          // TimeSpan (EST)
      "isAllowed": true,              // Checkbox state
      "name": "NY Session"            // Display name
    },
    {
      "dayOfWeek": "Monday",
      "startTime": "19:00:00",        // 7 PM EST
      "endTime": "04:00:00",          // 4 AM EST (next day)
      "isAllowed": true,
      "name": "Asia Session"
    }
  ],
  
  "tradingLock": {
    "isLocked": true,
    "lockTime": "2026-01-02T13:00:00Z",        // UTC
    "lockDayOfWeek": "Thursday",
    "lockReason": "Outside allowed trading times",
    "lockDuration": "02:00:00",                // 2 hours
    "lockExpirationTime": "2026-01-02T15:00:00Z"  // UTC
  },
  
  // Other settings...
  "dailyLossLimit": -500.00,
  "dailyProfitTarget": 1000.00,
  // ...
}
```

## Integration Points

```
┌─────────────────────────────────────────────────────────────────┐
│            Integration with Existing Features                    │
└─────────────────────────────────────────────────────────────────┘

Trading Times Lock Feature
         │
         │ Integrates with:
         │
         ├─→ P&L Monitoring Timer (500ms)
         │   └─ Runs alongside P&L limit checks
         │   └─ Same timer, different check
         │
         ├─→ Trading Lock System
         │   └─ Uses existing lock infrastructure
         │   └─ Respects existing locks (day/manual)
         │
         ├─→ Status Badge System
         │   └─ Updates "Trading Status" badge
         │   └─ Shows lock duration countdown
         │
         ├─→ Settings Service
         │   └─ Stores lock state in JSON
         │   └─ Loads trading time restrictions
         │
         └─→ Timezone System
             └─ Uses existing TimeZoneInfo
             └─ EST conversion for consistency
```

---

*Architecture diagrams created for Trading Times Lock Feature*
*Version 1.0 - 2026-01-02*
