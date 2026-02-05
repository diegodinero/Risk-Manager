# Automated Settings Lock - Quick Reference

## What is it?

A feature that automatically locks your Risk Manager settings at a specific time each day, so you don't have to remember to lock them manually.

## Where to find it

1. Open Risk Manager application
2. Click on the **🔒 Lock Settings** tab
3. Scroll down to the **"Automated Daily Lock"** section

## How to set it up

### Step 1: Select your account
Make sure an account is selected from the dropdown at the top.

### Step 2: Enable automated lock
☑ Check the box: **"Enable Automated Lock"**

### Step 3: Set the time
Enter the time when you want settings to lock:
- **Hour box:** 00-23 (e.g., 09 for 9 AM)
- **Minute box:** 00-59 (e.g., 30 for :30)
- Time is in **Eastern Time (ET)**

### Step 4: Save
Click: **"SAVE AUTO-LOCK SETTINGS"**

You'll see: "Automated lock enabled. Settings will lock daily at HH:MM ET."

## Common setups

### Market Open (Most Common)
```
Hour: 09
Minute: 30
Result: Locks at 9:30 AM ET when market opens
```

### Pre-Market
```
Hour: 08
Minute: 00
Result: Locks at 8:00 AM ET before market opens
```

### Afternoon
```
Hour: 14
Minute: 00
Result: Locks at 2:00 PM ET during trading
```

## What happens daily

1. **Before lock time** - You can modify all settings
2. **At lock time** - Settings automatically lock (no action needed)
3. **During lock** - You can view settings but not modify them
4. **At 5:00 PM ET** - Lock automatically unlocks
5. **After 5 PM** - You can modify settings for next day
6. **Next day** - Process repeats automatically

## Important notes

✅ **Each account is independent** - Set different times for different accounts

✅ **Works with manual lock** - You can still manually lock/unlock trading

✅ **Survives restart** - Settings saved even if you close the app

✅ **Can be disabled** - Just uncheck and save to turn it off

✅ **Time in Eastern Time** - Always uses US Eastern Time (ET)

✅ **Unlocks at 5 PM** - All locks (manual or auto) unlock at 5 PM ET

## To disable

1. Navigate to Lock Settings tab
2. Uncheck: **"Enable Automated Lock"**
3. Click: **"SAVE AUTO-LOCK SETTINGS"**

You'll see: "Automated lock disabled."

## Troubleshooting

### Settings didn't lock at the right time
- Check that "Enable Automated Lock" is checked
- Verify the time is correct (remember: ET, not your local time)
- Make sure you clicked "SAVE AUTO-LOCK SETTINGS"

### Can't save settings
- Hour must be 0-23
- Minute must be 0-59
- Make sure an account is selected

### Want different times for different accounts
That's supported! Just:
1. Select first account
2. Configure and save
3. Select second account
4. Configure different time and save

### Need to change settings during lock
Use the **"UNLOCK SETTINGS"** button in the manual lock section above. Your automated lock will still work tomorrow.

## Examples

### Example 1: Day Trader
"I trade 9:30 AM - 4:00 PM. I want settings locked during trading."

**Setup:**
- Hour: 09, Minute: 30
- Settings lock at market open
- Can't accidentally change limits during trading
- Can review/adjust after 5 PM for next day

### Example 2: Swing Trader
"I set my limits before market opens and don't want to change them."

**Setup:**
- Hour: 08, Minute: 00
- Settings lock before market opens
- Gives me time to review before trading starts
- Prevents emotional changes during market hours

### Example 3: Multiple Accounts
"I have a main account and a paper trading account with different rules."

**Setup:**
- Main account: Hour 09, Minute 30 (lock during trading)
- Paper account: Hour 16, Minute 00 (lock after close)
- Each account locks independently

## Tips

💡 **Set it up once** - Configure it today, works every trading day automatically

💡 **Test it first** - Set it for a few minutes in the future to see how it works

💡 **Combine with manual** - Use auto-lock for daily routine, manual lock for special situations

💡 **Document your time** - Write down what time you chose and why

💡 **Review regularly** - Check every few weeks if the time still makes sense

## Need help?

See full documentation:
- `AUTOMATED_SETTINGS_LOCK_FEATURE.md` - Complete feature guide
- `AUTOMATED_LOCK_UI_LAYOUT.md` - Visual layout guide
- `AUTOMATED_LOCK_TESTING_GUIDE.md` - Testing procedures

---

**Quick Summary:** Set a time, enable it, save it, forget it. Your settings will lock automatically every day at that time and unlock at 5 PM ET.
