# Trading Journal Data Storage Guide

## Where Does Trading Journal Data Get Written?

**Quick Answer:**
```
%AppData%\RiskManager\Journal\
```

**Full Windows Path:**
```
C:\Users\<YourUsername>\AppData\Roaming\RiskManager\Journal\
```

**Mac/Linux Path:**
```
~/.config/RiskManager/Journal/
```

## Files Created

The Trading Journal system creates and maintains 3 JSON files:

### 1. trading_journal.json
- **Contains:** All trade log data (entries, exits, P&L, etc.)
- **Format:** JSON dictionary organized by account number
- **Size:** Grows with each trade added
- **Updated:** Immediately after Add/Update/Delete trade operations

### 2. trading_models.json
- **Contains:** Trading models/strategies definitions
- **Format:** JSON dictionary organized by account number
- **Size:** Small, contains model metadata only
- **Updated:** Immediately after Add/Update/Delete model operations

### 3. journal_notes.json
- **Contains:** Journal notes and trade analysis
- **Format:** JSON dictionary organized by account number
- **Size:** Varies based on note content
- **Updated:** Immediately after Add/Update/Delete note operations

## JSON File Structure

### trading_journal.json Example

```json
{
  "ACCOUNT123": [
    {
      "Id": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
      "Date": "2024-01-15T10:30:00",
      "Symbol": "ES",
      "Direction": "Long",
      "Quantity": 1,
      "EntryPrice": 4850.00,
      "ExitPrice": 4860.00,
      "Commission": 4.50,
      "NetPL": 495.50,
      "GrossPL": 500.00,
      "Model": "Strategy A",
      "Session": "New York",
      "FollowedPlan": true,
      "Outcome": "Win",
      "Notes": "Perfect setup, followed rules",
      "RiskAmount": 100.00,
      "Tags": ["scalp", "momentum"]
    },
    {
      "Id": "b2c3d4e5-f6g7-8901-bcde-fg2345678901",
      "Date": "2024-01-15T14:30:00",
      "Symbol": "NQ",
      "Direction": "Short",
      "Quantity": 1,
      "EntryPrice": 17200.00,
      "ExitPrice": 17180.00,
      "Commission": 4.50,
      "NetPL": 395.50,
      "GrossPL": 400.00,
      "Model": "Strategy B",
      "Session": "London",
      "FollowedPlan": true,
      "Outcome": "Win",
      "Notes": "Quick scalp",
      "RiskAmount": 100.00,
      "Tags": ["scalp"]
    }
  ],
  "ACCOUNT456": [
    {
      "Id": "c3d4e5f6-g7h8-9012-cdef-gh3456789012",
      "Date": "2024-01-16T09:00:00",
      "Symbol": "ES",
      "Direction": "Long",
      "Quantity": 2,
      "EntryPrice": 4855.00,
      "ExitPrice": 4850.00,
      "Commission": 9.00,
      "NetPL": -509.00,
      "GrossPL": -500.00,
      "Model": "Strategy A",
      "Session": "Asia",
      "FollowedPlan": false,
      "Outcome": "Loss",
      "Notes": "Broke rules, entered too early",
      "RiskAmount": 100.00,
      "Tags": ["violation"]
    }
  ]
}
```

### trading_models.json Example

```json
{
  "ACCOUNT123": [
    {
      "Id": "model-001",
      "Name": "Strategy A",
      "Description": "Momentum trading strategy",
      "Rules": "1. Wait for trend\n2. Enter on pullback\n3. Stop at 2R",
      "CreatedDate": "2024-01-01T00:00:00"
    },
    {
      "Id": "model-002",
      "Name": "Strategy B",
      "Description": "Mean reversion strategy",
      "Rules": "1. Identify overbought/oversold\n2. Enter at extremes\n3. Target mean",
      "CreatedDate": "2024-01-01T00:00:00"
    }
  ]
}
```

## When Data is Written

### Immediate Write Operations

**All save operations are IMMEDIATE - no delays or batching:**

1. **Add Trade**
   - User clicks "Add Trade" in Trade Log
   - Fills in dialog and clicks Save
   - Trade immediately added to _accountTrades
   - SaveJournal() called
   - File written to disk instantly

2. **Update Trade**
   - User selects trade and clicks Edit
   - Modifies data and clicks Save
   - Trade immediately updated in _accountTrades
   - SaveJournal() called
   - File written to disk instantly

3. **Delete Trade**
   - User selects trade and clicks Delete
   - Confirms deletion
   - Trade immediately removed from _accountTrades
   - SaveJournal() called
   - File written to disk instantly

4. **Add/Update Model**
   - User adds or edits trading model
   - Clicks Save
   - Model immediately saved to _accountModels
   - SaveModels() called
   - File written to disk instantly

5. **Delete Model**
   - User deletes model
   - Confirms deletion
   - Model immediately removed from _accountModels
   - SaveModels() called
   - File written to disk instantly

## Code Flow

### Complete Data Write Flow

```
User Interface (TradeLogControl.cs)
    ↓
User clicks "Add Trade"
    ↓
TradeEntryDialog.cs shows
    ↓
User enters trade data
    ↓
User clicks "Save" button
    ↓
TradingJournalService.Instance.AddTrade(trade, accountNumber)
    ↓
In TradingJournalService.cs:
    ↓
_accountTrades[accountNumber].Add(trade)
    ↓
SaveJournal() method called
    ↓
var json = JsonSerializer.Serialize(_accountTrades, _jsonOptions)
    ↓
File.WriteAllText(_journalFilePath, json)
    ↓
Data written to: %AppData%\RiskManager\Journal\trading_journal.json
    ↓
File saved to disk
    ↓
Dashboard and Trade Log updated with new data
```

### Code Implementation

**From TradingJournalService.cs:**

```csharp
// Constructor sets up paths
private TradingJournalService()
{
    _dataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RiskManager", "Journal"
    );
    
    Directory.CreateDirectory(_dataDirectory);  // Ensures directory exists
    
    _journalFilePath = Path.Combine(_dataDirectory, "trading_journal.json");
    _modelsFilePath = Path.Combine(_dataDirectory, "trading_models.json");
    _notesFilePath = Path.Combine(_dataDirectory, "journal_notes.json");
    
    LoadJournal();  // Loads existing data
    LoadModels();
    LoadNotes();
}

// Save method writes to disk
private void SaveJournal()
{
    try
    {
        var json = JsonSerializer.Serialize(_accountTrades, _jsonOptions);
        File.WriteAllText(_journalFilePath, json);
    }
    catch (Exception ex)
    {
        // Error handling
    }
}
```

## How to Access Files

### Windows

**Method 1: Run Dialog**
1. Press `Win + R`
2. Type: `%AppData%\RiskManager\Journal\`
3. Press Enter
4. Explorer opens to the folder

**Method 2: File Explorer Address Bar**
1. Open File Explorer
2. Click address bar
3. Type: `%AppData%\RiskManager\Journal\`
4. Press Enter

**Method 3: Manual Navigation**
1. Open File Explorer
2. Navigate to: `C:\Users\<YourUsername>\AppData\Roaming\RiskManager\Journal\`
3. (You may need to show hidden files: View → Show → Hidden items)

### Mac

**Terminal:**
```bash
cd ~/.config/RiskManager/Journal/
ls -la
```

**Finder:**
1. Press `Cmd + Shift + G`
2. Type: `~/.config/RiskManager/Journal/`
3. Press Enter

### Linux

**Terminal:**
```bash
cd ~/.config/RiskManager/Journal/
ls -la
```

**File Manager:**
Navigate to: `~/.config/RiskManager/Journal/`

## Automatic File Creation

### First Time Setup

**No manual setup required!**

When you add your first trade:
1. Application checks if directory exists
2. If not, creates: `%AppData%\RiskManager\Journal\`
3. Creates empty `trading_journal.json` file
4. Writes first trade data to file

**Directory structure is created automatically:**
```
%AppData%\RiskManager\
    └── Journal\
        ├── trading_journal.json  (created on first trade)
        ├── trading_models.json   (created on first model)
        └── journal_notes.json    (created on first note)
```

## Data Organization

### By Account Number

All data is organized by account number (trading account ID):

```json
{
  "ACCOUNT123": [ /* trades for account 123 */ ],
  "ACCOUNT456": [ /* trades for account 456 */ ],
  "DEMO001": [ /* trades for demo account */ ]
}
```

**Benefits:**
- Multiple accounts supported
- Data isolated per account
- Easy to filter by account
- Dashboard automatically filters by selected account

### Data Persistence

**Data persists across:**
- ✅ Application restarts
- ✅ Computer restarts
- ✅ Application updates
- ✅ Windows updates

**Data is stored on disk, not in memory!**

## Backup Procedures

### Why Backup?

Your trading journal contains valuable data:
- Complete trade history
- Performance statistics
- Learning insights
- Tax documentation

**Loss of this data cannot be recovered without backups!**

### Backup Procedure

**Step 1: Close Application**
- Close Risk Manager completely
- Ensures all data is saved

**Step 2: Locate Files**
- Navigate to: `%AppData%\RiskManager\Journal\`
- You should see 3 JSON files

**Step 3: Copy Files**
- Select all 3 JSON files
- Copy to backup location

**Step 4: Add Date to Backup**
- Rename backup folder with date
- Example: `Journal_Backup_2024-01-15`

**Step 5: Store Safely**
- Cloud storage (Google Drive, Dropbox, OneDrive)
- External hard drive
- Network attached storage (NAS)
- Multiple locations for redundancy

### Backup Schedule

**Recommended:**
- Daily: If actively trading
- Weekly: If occasional trading
- Monthly: Minimum recommendation
- Before updates: Always backup before updating

### Automated Backup

**Option 1: Windows Task Scheduler**
Create batch file to copy files on schedule

**Option 2: Cloud Sync**
Put Journal folder in cloud-synced directory

**Option 3: Backup Software**
Include folder in your regular backup routine

## Migration Guide

### Moving Data Between Computers

**Step 1: Export from Old Computer**
1. Close Risk Manager
2. Navigate to: `%AppData%\RiskManager\Journal\`
3. Copy all 3 JSON files
4. Save to USB drive or cloud storage

**Step 2: Import to New Computer**
1. Install Risk Manager on new computer
2. Run it once (creates directory structure)
3. Close Risk Manager
4. Navigate to: `%AppData%\RiskManager\Journal\`
5. Replace files with your backed up files
6. Restart Risk Manager

**Your data is now on the new computer!**

### Merging Data

**If you have data on both computers:**

Manual merge required:
1. Open both JSON files in text editor
2. Combine arrays for each account
3. Ensure no duplicate IDs
4. Save merged file

**Automatic merge not currently supported.**

## File Format Details

### JSON Format

**Human Readable:**
- Files are plain text
- Can be opened in any text editor
- Formatted with indentation
- Easy to inspect and verify

**Standard JSON:**
- Industry-standard format
- Compatible with many tools
- Easy to parse programmatically
- Can be imported into Excel, databases, etc.

### File Encoding

**UTF-8 encoding:**
- Supports international characters
- Standard for JSON files
- Compatible across platforms

### File Permissions

**Windows:**
- User read/write permissions
- Located in user AppData (not Program Files)
- No admin rights needed

**Mac/Linux:**
- User read/write permissions (644)
- Located in user home directory
- No sudo required

## Troubleshooting

### Files Not Created

**Issue:** Files don't exist after adding data

**Solutions:**
1. Check if you actually saved the trade (clicked Save button)
2. Verify directory permissions
3. Check disk space available
4. Run application as administrator (Windows)
5. Check antivirus isn't blocking file creation

### Cannot Find Files

**Issue:** Can't locate the Journal folder

**Solutions:**
1. Ensure hidden files are visible (Windows: View → Show → Hidden items)
2. Copy and paste path exactly: `%AppData%\RiskManager\Journal\`
3. Check user account (files are per-user)
4. Verify you're running the right version of application

### Files Empty or Corrupted

**Issue:** Files exist but show {} or error when opening

**Solutions:**
1. Restore from backup
2. Check if application is still running (might be in-memory only)
3. Verify file wasn't manually edited and corrupted
4. Check disk errors (run chkdsk on Windows)

### Data Not Showing in Application

**Issue:** Files have data but dashboard shows empty

**Solutions:**
1. Verify correct account is selected
2. Check account number matches in JSON file
3. Restart application
4. Verify file format is valid JSON
5. Check debug output for errors

### Permission Errors

**Issue:** "Access denied" when saving

**Solutions:**
1. Check file isn't marked read-only
2. Verify user has write permissions
3. Close any programs that might have file open
4. Run as administrator (Windows)
5. Check antivirus/security software

## Advanced Topics

### Manual Data Editing

**Possible but risky:**
- Files are JSON, can be edited
- **Always backup first!**
- Use proper JSON editor
- Validate JSON format after editing
- Test with small changes first

**Recommended tools:**
- Visual Studio Code (with JSON extension)
- Notepad++ (with JSON plugin)
- Online JSON validators

### Data Export

**Current format is already portable:**
- JSON is universal format
- Can import into Excel (JSON to Table)
- Can query with jq tool (command line)
- Can import into databases
- Can analyze with Python/JavaScript

### Data Recovery

**If files are deleted:**
1. Check Windows Recycle Bin
2. Use file recovery software (Recuva, PhotoRec)
3. Restore from backup (if available)
4. Check cloud sync history
5. Check shadow copies (Windows System Restore)

## Security Considerations

### Data Privacy

**Files contain sensitive information:**
- Trading strategies
- Account performance
- Financial data

**Recommendations:**
- Encrypt backups
- Use secure cloud storage
- Don't share files publicly
- Be careful with public computers

### File Encryption

**Optional encryption:**
- Windows: Use BitLocker for drive encryption
- Mac: Use FileVault
- Third-party: VeraCrypt, 7-Zip with password

**Note:** Application doesn't currently encrypt files itself.

## Summary

**Quick Reference:**

| Item | Value |
|------|-------|
| **Location** | `%AppData%\RiskManager\Journal\` |
| **Files** | 3 JSON files (trades, models, notes) |
| **Format** | JSON (human readable text) |
| **Organization** | By account number |
| **Write Timing** | Immediate after each operation |
| **Persistence** | Survives restarts |
| **Backup** | Manual, recommended regularly |
| **Portability** | Copy files to migrate |

**Remember:**
- ✅ Data written immediately
- ✅ Files created automatically
- ✅ Organized by account
- ✅ Backup regularly
- ✅ Keep multiple copies
- ✅ Files are portable

## Need Help?

If you have issues with data storage:
1. Check this guide first
2. Verify files exist and have data
3. Check troubleshooting section
4. Review debug output in application
5. Ensure backups are current

Your trading data is important - keep it safe!
