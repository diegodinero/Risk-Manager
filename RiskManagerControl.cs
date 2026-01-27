using Risk_Manager.Data;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using TradingPlatform.BusinessLayer;
using TradingPlatform.PresentationLayer.Renderers.Chart;
using DockStyle = System.Windows.Forms.DockStyle;


public class CustomValueLabel : Panel
{
    public Label TextLabel { get; }
    public PictureBox IconBox { get; }

    public CustomValueLabel(string text, Image icon, Font font = null, Color? foreColor = null, Color? backColor = null)
    {
        this.Height = 28;
        this.Width = 240;
        this.BackColor = backColor ?? Color.Transparent;

        // Icon (PictureBox)
        if (icon != null)
        {
            IconBox = new PictureBox
            {
                Image = icon,
                SizeMode = PictureBoxSizeMode.Zoom,
                Width = 20,
                Height = 20,
                Location = new Point(0, 4),
                Margin = new Padding(0, 0, 8, 0)
            };
            this.Controls.Add(IconBox);
        }

        // Text (Label)
        TextLabel = new Label
        {
            Text = text,
            AutoSize = true,
            Font = font ?? new Font("Segoe UI Emoji", 10, FontStyle.Regular),
            ForeColor = foreColor ?? Color.White,
            BackColor = Color.Transparent,
            Location = new Point(icon != null ? 28 : 0, 4),
            TextAlign = ContentAlignment.MiddleLeft,
            UseCompatibleTextRendering = false
        };
        this.Controls.Add(TextLabel);
    }

    // Optional: update text and icon after creation
    public void SetText(string text) => TextLabel.Text = text;
    public void SetIcon(Image icon)
    {
        if (IconBox != null)
            IconBox.Image = icon;
    }
}
class CustomCardHeaderControl : Panel
{
    private Label titleLabel;
    private PictureBox iconBox;
    
    public CustomCardHeaderControl(string title, Image icon)
    {
        this.Dock = DockStyle.Top;
        this.Height = 40;
        this.BackColor = Color.Transparent;

        // Title (Label)
        titleLabel = new Label
        {
            Text = title, // Title without emojis
            AutoSize = true,
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0) // Padding to separate text from the icon
        };
        this.Controls.Add(titleLabel);

        // Icon (PictureBox) - increased size from 24 to 32 for better visibility
        if (icon != null)
        {
            iconBox = new PictureBox
            {
                Image = icon,
                SizeMode = PictureBoxSizeMode.Zoom,
                Width = 32,
                Height = 32,
                Dock = DockStyle.Left,
                Margin = new Padding(8, 4, 0, 4) // Adjusted margin for proper alignment
            };
            this.Controls.Add(iconBox);
        }

        

        // Add spacing below the header
        this.Padding = new Padding(0, 0, 0, 10); // Add padding below the header
    }
    
    public Label TitleLabel => titleLabel;
    public PictureBox IconBox => iconBox;
}
class CustomHeaderControl : Panel
{
    private Label textLabel;
    private PictureBox iconBox;
    
    public CustomHeaderControl(string text, Image icon)
    {
        this.Dock = DockStyle.Top;
        this.Height = 40;
        this.BackColor = Color.Transparent;

        // Text (Label)
        textLabel = new Label
        {
            Text = text,
            AutoSize = true,
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            ForeColor = Color.White,
            Dock = DockStyle.Left,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(8, 0, 0, 0) // Add padding to separate text from the icon
        };
        this.Controls.Add(textLabel);

        // Icon (PictureBox) - increased size from 36 to 48 for better visibility
        if (icon != null)
        {
            iconBox = new PictureBox
            {
                Image = icon,
                SizeMode = PictureBoxSizeMode.Zoom,
                Width = 48,
                Height = 48,
                Dock = DockStyle.Left,
                Margin = new Padding(8, 0, 0, 0) // Add some margin around the icon
            };
            this.Controls.Add(iconBox);
        }
    }
    
    public Label TextLabel => textLabel;
    public PictureBox IconBox => iconBox;
}

namespace Risk_Manager
{

    public class RiskManagerControl : UserControl
    {
        private readonly Panel contentPanel;
        private readonly Panel leftPanel;
        private Panel topPanel;
        private readonly Dictionary<string, Control> pageContents = new();
        private DataGridView statsGrid;
        private System.Windows.Forms.Timer statsRefreshTimer;
        private Account selectedAccount;
        private int selectedAccountIndex = -1; // Track the index of selected account
        private string displayedAccountNumber; // Cache the account number shown in UI
        private Button lockSettingsButton; // Reference to lock settings button
        private Button unlockSettingsButton; // Reference to unlock settings button
        private DataGridView statsDetailGrid;
        private System.Windows.Forms.Timer statsDetailRefreshTimer;
        private DataGridView typeSummaryGrid;
        private System.Windows.Forms.Timer typeSummaryRefreshTimer;
        private System.Windows.Forms.Timer lockExpirationCheckTimer;
        private System.Windows.Forms.Timer pnlMonitorTimer; // Timer to monitor P&L limits
        private System.Windows.Forms.Timer badgeRefreshTimer; // Timer to refresh badge from JSON (like Accounts Summary)
        private ComboBox typeSummaryFilterComboBox;
        private string selectedNavItem = null;
        private readonly List<Button> navButtons = new();
        private Label settingsStatusBadge;
        private Label tradingStatusBadge;
        private DataGridView statusTableView; // Data table displaying settings and trading status
        private Label lblTradingStatusBadgeDebug; // Debug label to show badge transition information
        private ComboBox accountSelector;
        private Button lockTradingButton; // Lock Trading button reference
        private Button unlockTradingButton; // Unlock Trading button reference
        private ComboBox lockDurationComboBox; // Lock duration selector
        private CheckBox showProgressBarsCheckBox; // Show Progress Bars checkbox in General Settings
        private bool showProgressBars = false; // Whether to show progress bars in data grids
        private CheckBox showPercentageCheckBox; // Show Percentage checkbox in General Settings
        private bool showPercentage = false; // Whether to show percentage instead of dollar amount in progress bars
        private Label currentThemeLabel; // Label to display current theme name
        private Dictionary<string, TypeSummaryData> typeSummaryAggregatedData = new Dictionary<string, TypeSummaryData>(); // Aggregated data for type summary rows
        
        // State caching for badge updates - stored per account to prevent cross-account state confusion
        private readonly Dictionary<string, bool?> _accountTradingLockStateCache = new Dictionary<string, bool?>();
        private readonly Dictionary<string, bool?> _accountSettingsLockStateCache = new Dictionary<string, bool?>();
        
        // Cache for full settings lock status strings (includes duration) to detect changes between accounts
        private readonly Dictionary<string, string> _accountSettingsLockStatusCache = new Dictionary<string, string>();
        
        // Track which account is currently displayed on each badge to force updates when switching accounts
        private string _currentTradingBadgeAccountNumber = null;  // For Trading Status Badge
        private string _currentSettingsBadgeAccountNumber = null;  // For Settings Status Badge
        
        // Debug mode configuration
        private bool _badgeDebugMode = true; // Enable/disable visual debugging of badge transitions
        
        // File-based debug logging for Settings Badge
        private string _badgeDebugLogPath = null;
        private readonly object _badgeDebugLogLock = new object();

        // Settings input control references for persistence
        private TextBox dailyLossLimitInput;
        private CheckBox dailyLossLimitEnabled;
        private TextBox dailyProfitTargetInput;
        private CheckBox dailyProfitTargetEnabled;
        private TextBox blockedSymbolsInput;
        private CheckBox blockedSymbolsEnabled;
        private TextBox defaultContractLimitInput;
        private TextBox symbolContractLimitsInput;
        private CheckBox symbolContractLimitsEnabled;
        private CheckBox tradingLockCheckBox;
        private CheckBox settingsLockCheckBox;
        
        // Weekly limits
        private TextBox weeklyLossLimitInput;
        private CheckBox weeklyLossLimitEnabled;
        private TextBox weeklyProfitTargetInput;
        private CheckBox weeklyProfitTargetEnabled;
        
        // Position limits
        private TextBox positionLossLimitInput;
        private CheckBox positionLossLimitEnabled;
        private TextBox positionProfitTargetInput;
        private CheckBox positionProfitTargetEnabled;
        
        // Allowed trading times
        private List<CheckBox> tradingTimeCheckboxes = new();
        
        // Feature toggle master switch
        private CheckBox featureToggleEnabledCheckbox;
        
        // Individual feature toggle checkboxes
        private CheckBox positionsFeatureCheckbox;
        private CheckBox limitsFeatureCheckbox;
        private CheckBox symbolsFeatureCheckbox;
        private CheckBox tradingTimesFeatureCheckbox;
        
        // Risk enforcement mode radio buttons
        private RadioButton strictModeRadioButton;
        private RadioButton warningModeRadioButton;
        private RadioButton monitorModeRadioButton;
        
        // Copy Settings controls
        private ComboBox copySettingsSourceComboBox;
        private FlowLayoutPanel copySettingsTargetPanel;
        
        private SoundPlayer alertSoundPlayer;
        
        // Shutdown sound player (separate from alertSoundPlayer to avoid conflicts)
        private SoundPlayer shutdownSoundPlayer;
        
        // Tooltip for draggable title
        private ToolTip titleToolTip;
        
        // Navigation collapse state
        private bool isNavigationCollapsed = false;
        private Button navToggleButton;
        
        // Cache for WPF window dragging to avoid repeated reflection
        private object cachedWpfWindow;
        private PropertyInfo cachedLeftProperty;
        private PropertyInfo cachedTopProperty;

        private readonly Dictionary<string, Image> IconMap = new();
        private Image DollarImage;

        private Image themeButtonScaledImage;

        private Image cautionButtonScaledImage;
        
        private Image navToggleButtonScaledImage;

        // add near other private Image fields in RiskManagerControl class
        private Image cautionButtonBgImage;
        
        // Shutdown button related fields
        private Button shutdownButton;
        private Image shutdownButtonScaledImage;
        private System.Windows.Forms.Timer shutdownTimer;
        private Form shutdownCountdownForm;
        private int shutdownCountdownSeconds;

        /// <summary>
        /// Sets the WPF window reference for dragging functionality
        /// </summary>
        public void SetWpfWindow(object wpfWindow)
        {
            if (wpfWindow != null)
            {
                cachedWpfWindow = wpfWindow;
                var wpfWindowType = wpfWindow.GetType();
                cachedLeftProperty = wpfWindowType.GetProperty("Left");
                cachedTopProperty = wpfWindowType.GetProperty("Top");
            }
        }

        // Theme management
        private enum Theme
        {
            Blue,
            Black,
            White,
            YellowBlueBlack   // New Black variant with yellow negatives / blue positives
        }
        
        private Theme currentTheme = Theme.Blue;  // Default theme
        private bool isInitializing = true;  // Flag to prevent saving during initialization

        // Per-value accent colors (used by YellowBlueBlack)
        private Color PositiveValueColor;
        private Color NegativeValueColor;

        // Default values for settings
        private const decimal DEFAULT_WEEKLY_LOSS_LIMIT = 1000m;
        private const decimal DEFAULT_WEEKLY_PROFIT_TARGET = 2000m;
        private const int DEFAULT_CONTRACT_LIMIT = 10;

        // P&L monitoring constants
        private const int PNL_MONITOR_INTERVAL_MS = 500; // Check P&L every half second
        private const int FALLBACK_LOCK_HOURS = 8; // Fallback lock duration if timezone calculation fails
        private const decimal DAILY_LOSS_WARNING_THRESHOLD = 0.80m; // 80% of loss limit triggers warning

        // Account type constants
        private const string ACCOUNT_TYPE_PA = "PA";
        private const string ACCOUNT_TYPE_SIM_FUND = "Sim-Funded";
        private const string ACCOUNT_TYPE_EVAL = "Eval";
        private const string ACCOUNT_TYPE_CASH = "Cash";
        private const string ACCOUNT_TYPE_PRAC = "Prac";
        private const string ACCOUNT_TYPE_DEMO = "Demo";
        private const string ACCOUNT_TYPE_LIVE = "Live";
        private const string ACCOUNT_TYPE_UNKNOWN = "Unknown";
        
        // Filter mode constants for Type Summary
        private const string FILTER_MODE_TYPE = "Type";
        private const string FILTER_MODE_FIRM = "Firm";
        private static readonly string[] TypeSummaryFilterOptions = new[] { FILTER_MODE_TYPE, FILTER_MODE_FIRM };

        // P&L field identifiers
        private const string TOTAL_PNL_ID = "TotalPnL";
        
        // Lock status constants
        private const string LOCK_STATUS_UNLOCKED = "Unlocked";
        private const string LOCK_STATUS_COLUMN_NAME = "LockStatus";
        private const int LOG_PARTS_MAX = 6; // Max parts in badge logging helpers (LogBadgeUpdate, LogSettingsBadgeUpdate): Caller, Account, LockStatus/IsLocked, PreviousState, Message
        
        // Debug label constants
        private const int DEBUG_LABEL_WIDTH = 600; // Width of the trading status badge debug label (increased to show full debug text)
        private const int DEBUG_LABEL_HEIGHT = 16; // Height of the trading status badge debug label
        private const int DEBUG_LABEL_SPACING = 2; // Spacing between trading status badge and debug label
        private const int DEBUG_CONTAINER_SPACING = 4; // Extra spacing in container for proper layout
        private const string LOCK_EMOJI = "🔒";
        private const string UNLOCK_EMOJI = "🔓";
        
        // Risk Overview card title constants
        private const string CARD_TITLE_ACCOUNT_STATUS = "Account Status";
        
        // Status table row indices
        private const int STATUS_TABLE_SETTINGS_ROW = 0; // Settings Status row index
        private const int STATUS_TABLE_TRADING_ROW = 1;  // Trading Status row index

        // Regex patterns for account type detection (compiled for performance)
        // Using word boundaries to avoid false positives (e.g., "space" won't match "pa", "evaluate" won't match "eval")
        private static readonly Regex PAPattern = new Regex(@"\bpa\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex EvalPattern = new Regex(@"\beval\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SimFundPattern = new Regex(@"\bsim\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex CashPattern = new Regex(@"\bcash\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex PracPattern = new Regex(@"\bprac\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // Theme colors - instance fields that can be updated
        private Color DarkBackground;
        private Color DarkerBackground;
        private Color CardBackground;
        private Color AccentGreen;
        private Color AccentAmber;
        private Color TextWhite;
        private Color TextGray;
        private Color HoverColor;
        private Color SelectedColor;

        // Navigation items - includes Stats and Accounts Summary
        // Consolidated tabs: "Positions" (Position Win + Position Loss), "Limits" (Daily Loss + Daily Profit Target), "Symbols" (Block Symbols + Position Size)
        private static readonly string[] NavItems = new[]
        {
            "📊 Accounts Summary", "📈 Stats", "📋 Type", "🔍 Risk Overview", "⚙️ Feature Toggles", "📋 Copy Settings", "📈 Positions", "📊 Limits", "🛡️ Symbols", "🕐 Allowed Trading Times",
            "🔒 Lock Settings", "🔒 Manual Lock", "⚙️ General Settings"
        };

        private const int LeftPanelWidth = 200;
        private const int LeftPanelCollapsedWidth = 50; // Width when collapsed (show only icons)
        private const int LeftPanelExpandedWidth = 200; // Width when expanded (show icons + text)

        public RiskManagerControl()
        {
            // Load saved theme preference or use default (Blue)
            var savedTheme = LoadThemePreference();
            ApplyTheme(savedTheme);
            
            // Load progress bar preference
            showProgressBars = LoadProgressBarPreference();
            
            // Load show percentage preference
            showPercentage = LoadShowPercentagePreference();

            LoadIcons();

            Dock = DockStyle.Fill;
            BackColor = DarkBackground;
            DoubleBuffered = true;
            AutoScroll = true;
            MinimumSize = new Size(600, 400);

            // Top panel with title, account selector, and status badges
            var topPanel = CreateTopPanel();

            // Left sidebar panel
            leftPanel = CreateLeftSidebar();

            // Main content panel
            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                AutoScroll = true,
                BackColor = DarkBackground,
                Padding = new Padding(20)
            };

            // Create pages and contents for navigation items
            foreach (var name in NavItems)
            {
                Control placeholder;
                // Match navigation items with emoji prefixes
                if (name.EndsWith("Accounts Summary"))
                    placeholder = CreateAccountsSummaryPanel();
                else if (name.EndsWith("Stats"))
                    placeholder = CreateAccountStatsPanel();
                else if (name.EndsWith("Type"))
                    placeholder = CreateTypeSummaryPanel();
                else if (name.EndsWith("Risk Overview"))
                    placeholder = CreateRiskOverviewPanel();
                else if (name.EndsWith("Feature Toggles"))
                    placeholder = CreateFeatureTogglesPanel();
                else if (name.EndsWith("Copy Settings"))
                    placeholder = CreateCopySettingsPanel();
                else if (name.EndsWith("Positions"))
                    placeholder = CreatePositionsPanel();
                else if (name.EndsWith("Limits"))
                    placeholder = CreateLimitsPanel();
                else if (name.EndsWith("Symbols"))
                    placeholder = CreateSymbolsPanel();
                else if (name.EndsWith("Allowed Trading Times"))
                    placeholder = CreateAllowedTradingTimesDarkPanel();
                else if (name.EndsWith("Lock Settings"))
                    placeholder = CreateLockSettingsDarkPanel();
                else if (name.EndsWith("Manual Lock"))
                    placeholder = CreateManualLockDarkPanel();
                else if (name.EndsWith("General Settings"))
                    placeholder = CreateGeneralSettingsPanel();
                else
                    placeholder = CreatePlaceholderPanel(name);
                pageContents[name] = placeholder;
            }

            Controls.Add(contentPanel);
            Controls.Add(leftPanel);
            Controls.Add(topPanel);

            // Populate account dropdown
            RefreshAccountDropdown();

            // Refresh dropdown periodically as accounts connect/disconnect
            var dropdownRefreshTimer = new System.Windows.Forms.Timer { Interval = 2000 };
            dropdownRefreshTimer.Tick += (s, e) => 
            {
                RefreshAccountDropdown();
                RefreshCopySettingsAccounts();
            };
            dropdownRefreshTimer.Start();

            // Check for expired locks and enforce lock status every second
            lockExpirationCheckTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            lockExpirationCheckTimer.Tick += (s, e) => CheckExpiredLocks();
            lockExpirationCheckTimer.Start();

            // Monitor P&L limits and auto-close positions every half second
            pnlMonitorTimer = new System.Windows.Forms.Timer { Interval = PNL_MONITOR_INTERVAL_MS };
            pnlMonitorTimer.Tick += (s, e) => MonitorPnLLimits();
            pnlMonitorTimer.Start();

            // Refresh Trading Status badge every second (same as Accounts Summary)
            // This ensures badge always shows current JSON state in real-time
            badgeRefreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            badgeRefreshTimer.Tick += (s, e) => RefreshTradingStatusBadgeFromJSON();
            badgeRefreshTimer.Start();

            // Show Accounts Summary by default
            selectedNavItem = "📊 Accounts Summary";
            UpdateNavButtonStates();
            ShowPage("📊 Accounts Summary");
            
            // Initialization complete, enable theme saving
            isInitializing = false;
        }

        /// <summary>
        /// Applies the specified theme to all controls
        /// </summary>
        private void ApplyTheme(Theme theme)
        {
            currentTheme = theme;

            // Default mapping: use Blue theme values as "dark" baseline
            switch (theme)
            {
                case Theme.Blue:
                    DarkBackground = Color.FromArgb(45, 62, 80);
                    DarkerBackground = Color.FromArgb(35, 52, 70);
                    CardBackground = Color.FromArgb(55, 72, 90);
                    AccentGreen = Color.FromArgb(39, 174, 96);
                    AccentAmber = Color.FromArgb(243, 156, 18);
                    TextWhite = Color.White;
                    TextGray = Color.FromArgb(189, 195, 199);
                    HoverColor = Color.FromArgb(65, 82, 100);
                    SelectedColor = Color.FromArgb(75, 92, 110);
                    PositiveValueColor = TextWhite;
                    NegativeValueColor = Color.Red;
                    break;

                case Theme.Black:
                    DarkBackground = Color.FromArgb(20, 20, 20);
                    DarkerBackground = Color.FromArgb(10, 10, 10);
                    CardBackground = Color.FromArgb(30, 30, 30);
                    AccentGreen = Color.FromArgb(0, 200, 83);
                    AccentAmber = Color.FromArgb(255, 185, 0);
                    TextWhite = Color.White;
                    TextGray = Color.FromArgb(160, 160, 160);
                    HoverColor = Color.FromArgb(50, 50, 50);
                    SelectedColor = Color.FromArgb(60, 60, 60);
                    PositiveValueColor = TextWhite;
                    NegativeValueColor = Color.Red;
                    break;

                case Theme.White:
                    DarkBackground = Color.FromArgb(245, 245, 245);
                    DarkerBackground = Color.FromArgb(220, 220, 220);
                    CardBackground = Color.White;
                    AccentGreen = Color.FromArgb(39, 174, 96);
                    AccentAmber = Color.FromArgb(243, 156, 18);
                    TextWhite = Color.FromArgb(30, 30, 30);
                    TextGray = Color.FromArgb(90, 90, 90);
                    HoverColor = Color.FromArgb(230, 230, 230);
                    SelectedColor = Color.FromArgb(210, 210, 210);
                    PositiveValueColor = TextWhite;
                    NegativeValueColor = Color.Red;
                    break;

                case Theme.YellowBlueBlack:
                    DarkBackground = Color.FromArgb(20, 20, 20);
                    DarkerBackground = Color.FromArgb(10, 10, 10);
                    CardBackground = Color.FromArgb(30, 30, 30);
                    AccentGreen = Color.FromArgb(0, 200, 83);
                    AccentAmber = Color.FromArgb(255, 185, 0);
                    TextWhite = Color.White;
                    TextGray = Color.FromArgb(160, 160, 160);
                    HoverColor = Color.FromArgb(50, 50, 50);
                    SelectedColor = Color.FromArgb(60, 60, 60);
                    // Use requested colors
                    PositiveValueColor = ColorTranslator.FromHtml("#3179f5"); // blue
                    NegativeValueColor = ColorTranslator.FromHtml("#fbc02d"); // yellow
                    break;
            }

            // Save theme preference
            SaveThemePreference();
            
            // Update the current theme label in General Settings if it exists
            if (currentThemeLabel != null)
            {
                currentThemeLabel.Text = $"Current Theme: {GetThemeDisplayName(currentTheme)}";
            }

            // Apply theme to all controls
            UpdateAllControlColors();

            // Re-apply numeric and risk-overview specific coloring after the recursive updates
            // so YellowBlueBlack colors are not overwritten by generic label updates.
            try
            {
                // Colorize grids again (ensures per-cell styles are set last)
                ColorizeNumericCells(statsGrid, "OpenPnL", "ClosedPnL", "DailyPnL", "GrossPnL");
                ColorizeNumericCells(typeSummaryGrid, "OpenPnL", "ClosedPnL", "TotalPnL");
                
                // Re-apply Lock Status coloring after theme change
                ColorizeLockStatusCells(statsGrid);

                // Refresh Risk Overview values (applies label.Tag based coloring)
                if (!string.IsNullOrEmpty(selectedNavItem) && selectedNavItem.EndsWith("Risk Overview") && pageContents.TryGetValue(selectedNavItem, out var panel))
                {
                    RefreshRiskOverviewPanel(panel);
                }

                // Refresh the stats detail grid coloring (RefreshAccountStats applies the row-level coloring)
                RefreshAccountStats();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyTheme post-refresh: {ex.Message}");
            }
        }

        // Helper: returns true if string represents a negative numeric (parentheses or leading '-')
        // Regex pattern for matching numeric values with optional currency symbols and formatting
        private static readonly string NumericValuePattern = @"\(?-?\$?\d{1,3}(?:,\d{3})*(?:\.\d+)?\)?";

        private static bool IsNegativeNumericString(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            // Normalize
            s = s.Trim();

            try
            {
                // Match a numeric token that may include optional currency symbol and optional parentheses:
                // Examples matched: "(1,234.56)", "$-1,234.56", "-1234.56", "1234.56"
                var m = Regex.Match(s, NumericValuePattern);
                if (!m.Success) return false;

                var token = m.Value.Trim();

                // Parentheses indicate negative: "(123.45)"
                if (token.StartsWith("(") && token.EndsWith(")")) return true;

                // A '-' anywhere in the token indicates negativity ("-123.45" or "$-123.45")
                if (token.Contains("-")) return true;

                return false;
            }
            catch
            {
                // Fallback conservative answer
                return false;
            }
        }

        /// <summary>
        /// Determines if a numeric string represents exactly zero (0.00).
        /// Handles various formats including currency symbols, commas, and parentheses.
        /// </summary>
        /// <param name="s">The string to check</param>
        /// <returns>True if the string represents zero, false otherwise</returns>
        private static bool IsZeroValue(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            // Normalize
            s = s.Trim();
            
            try
            {
                // Match a numeric token that may include optional currency symbol and optional parentheses
                var m = Regex.Match(s, NumericValuePattern);
                if (!m.Success) return false;
                
                var token = m.Value.Trim();
                
                // Remove non-numeric characters for parsing
                token = Regex.Replace(token, @"[\$\(\),]", "");
                
                // Try to parse as decimal and check if it equals zero
                if (decimal.TryParse(token, out decimal value))
                {
                    return value == 0m;
                }
                
                return false;
            }
            catch
            {
                return false;
            }
        }

        // Color numeric cells for specified column keys when YellowBlueBlack theme active
        private void ColorizeNumericCells(DataGridView grid, params string[] columnNames)
        {
            if (grid == null || columnNames == null || columnNames.Length == 0) return;
            try
            {
                bool applySpecial = currentTheme == Theme.YellowBlueBlack;
                for (int r = 0; r < grid.Rows.Count; r++)
                {
                    var row = grid.Rows[r];
                    foreach (var colName in columnNames)
                    {
                        if (!grid.Columns.Contains(colName)) continue;
                        var cell = row.Cells[colName];
                        if (cell == null) continue;
                        var raw = (cell.Value ?? string.Empty).ToString();
                        if (applySpecial)
                        {
                            // Check if value is exactly 0.00 - if so, use white (TextWhite)
                            if (IsZeroValue(raw))
                                cell.Style.ForeColor = TextWhite;
                            else if (IsNegativeNumericString(raw))
                                cell.Style.ForeColor = NegativeValueColor;
                            else
                                cell.Style.ForeColor = PositiveValueColor;
                        }
                        else
                        {
                            // Reset to default readable color per theme
                            cell.Style.ForeColor = TextWhite;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ColorizeNumericCells error: {ex.Message}");
            }
        }

        // Helper method to check if a value is a lock status value
        private bool IsLockStatusValue(string valueText)
        {
            if (string.IsNullOrEmpty(valueText))
                return false;
            return valueText.Contains(UNLOCK_EMOJI) || valueText.Contains(LOCK_EMOJI);
        }

        // Helper method to extract lock status text from emoji-prefixed strings
        // e.g., "🔓 Unlocked" -> "Unlocked", "🔒 Locked (2h 30m)" -> "Locked (2h 30m)"
        private string ExtractLockStatusText(string valueText)
        {
            if (string.IsNullOrEmpty(valueText))
                return valueText;
                
            var lockStatusText = valueText;
            if (IsLockStatusValue(valueText))
            {
                var spaceIndex = valueText.IndexOf(' ');
                if (spaceIndex >= 0 && spaceIndex + 1 < valueText.Length)
                {
                    lockStatusText = valueText.Substring(spaceIndex + 1).Trim();
                }
            }
            return lockStatusText;
        }

        // Helper method to get the color for a lock status
        // Returns Green for "Unlocked", Red for "Locked" (any format), TextWhite for other
        private Color GetLockStatusColor(string lockStatus)
        {
            if (string.IsNullOrEmpty(lockStatus))
                return TextWhite;
                
            // "Locked" with any duration format (Locked, Locked (2h 30m), etc.)
            if (lockStatus.StartsWith("Locked", StringComparison.OrdinalIgnoreCase))
                return Color.Red;
            
            // "Unlocked"
            if (lockStatus.Equals(LOCK_STATUS_UNLOCKED, StringComparison.OrdinalIgnoreCase))
                return AccentGreen;
            
            // Default for any other status
            return TextWhite;
        }

        // Color Lock Status cells based on lock state (Green for Unlocked, Red for Locked)
        private void ColorizeLockStatusCells(DataGridView grid)
        {
            if (grid == null) return;
            try
            {
                // Check if LockStatus column exists
                if (!grid.Columns.Contains(LOCK_STATUS_COLUMN_NAME)) return;
                
                for (int r = 0; r < grid.Rows.Count; r++)
                {
                    var row = grid.Rows[r];
                    var cell = row.Cells[LOCK_STATUS_COLUMN_NAME];
                    if (cell == null) continue;
                    
                    var lockStatus = (cell.Value ?? string.Empty).ToString();
                    cell.Style.ForeColor = GetLockStatusColor(lockStatus);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ColorizeLockStatusCells error: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates colors for all controls in the application
        /// </summary>
        private void UpdateAllControlColors()
        {
            // Update main control
            this.BackColor = DarkBackground;
            
            // Update panels
            if (topPanel != null) topPanel.BackColor = DarkBackground;
            if (contentPanel != null) contentPanel.BackColor = DarkBackground;
            if (leftPanel != null) leftPanel.BackColor = DarkerBackground;
            
            // Update navigation buttons
            foreach (var btn in navButtons)
            {
                var itemName = btn.Tag as string;
                btn.BackColor = itemName == selectedNavItem ? SelectedColor : DarkerBackground;
                btn.ForeColor = TextWhite;
                btn.FlatAppearance.MouseOverBackColor = HoverColor;
                btn.FlatAppearance.MouseDownBackColor = SelectedColor;
                btn.Invalidate();
            }
            
            // Update account selector
            if (accountSelector != null)
            {
                accountSelector.BackColor = CardBackground;
                accountSelector.ForeColor = TextWhite;
            }
            
            // Update status badges
            if (settingsStatusBadge != null)
            {
                // Badge colors stay the same but need repaint for rounded corners
                settingsStatusBadge.Invalidate();
            }
            
            if (tradingStatusBadge != null)
            {
                tradingStatusBadge.Invalidate();
            }
            
            // Update status table view
            if (statusTableView != null)
            {
                statusTableView.BackgroundColor = CardBackground;
                statusTableView.GridColor = DarkerBackground;
                statusTableView.DefaultCellStyle.BackColor = CardBackground;
                statusTableView.DefaultCellStyle.ForeColor = TextWhite;
                statusTableView.DefaultCellStyle.SelectionBackColor = CardBackground;
                statusTableView.DefaultCellStyle.SelectionForeColor = TextWhite;
                
                // Re-apply lock status colors to cells using helper method
                if (statusTableView.Rows.Count > STATUS_TABLE_TRADING_ROW)
                {
                    // Settings Status row
                    var settingsStatusText = statusTableView.Rows[STATUS_TABLE_SETTINGS_ROW].Cells[2].Value?.ToString() ?? "Unlocked";
                    UpdateStatusTableCellColor(STATUS_TABLE_SETTINGS_ROW, settingsStatusText);
                    
                    // Trading Status row
                    var tradingStatusText = statusTableView.Rows[STATUS_TABLE_TRADING_ROW].Cells[2].Value?.ToString() ?? "Unlocked";
                    UpdateStatusTableCellColor(STATUS_TABLE_TRADING_ROW, tradingStatusText);
                }
            }
            
            // Update all page contents
            foreach (var kvp in pageContents)
            {
                UpdateControlRecursively(kvp.Value);
            }
            
            // Update topPanel and its children
            if (topPanel != null)
            {
                UpdateControlRecursively(topPanel);
            }

            // After recursive updates and invalidation, color numeric columns for special theme
            // Accounts Summary: OpenPnL, ClosedPnL, DailyPnL, GrossPnL (column keys match grid.Columns.Add names)
            ColorizeNumericCells(statsGrid, "OpenPnL", "ClosedPnL", "DailyPnL", "GrossPnL");


            // Type summary: OpenPnL, ClosedPnL, TotalPnL (column keys added earlier)
            ColorizeNumericCells(typeSummaryGrid, "OpenPnL", "ClosedPnL", "TotalPnL");

            // After numeric/grid coloring, re-apply value-label coloring so special-theme colors persist
            try
            {
                // apply to entire content area and topPanel so all risk overview/value labels are handled
                ApplyValueLabelColoring(this);
                if (contentPanel != null) ApplyValueLabelColoring(contentPanel);
                if (topPanel != null) ApplyValueLabelColoring(topPanel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ApplyValueLabelColoring error: {ex.Message}");
            }

            // Refresh current page
            this.Invalidate(true);
        }

        /// <summary>
        /// Recursively updates colors for a control and its children
        /// </summary>
        private void UpdateControlRecursively(Control control)
        {
            if (control == null) return;

            // Update background colors based on control type
            if (control is CustomHeaderControl headerControl)
            {
                // Update the label text color in CustomHeaderControl
                if (headerControl.TextLabel != null)
                {
                    headerControl.TextLabel.ForeColor = TextWhite;
                }
            }
            else if (control is CustomCardHeaderControl cardHeaderControl)
            {
                // Update the label text color in CustomCardHeaderControl
                if (cardHeaderControl.TitleLabel != null)
                {
                    cardHeaderControl.TitleLabel.ForeColor = TextWhite;
                }
            }
            else if (control is Panel)
            {
                if (control.BackColor == Color.FromArgb(45, 62, 80) ||
                    control.BackColor == Color.FromArgb(20, 20, 20) ||
                    control.BackColor == Color.FromArgb(245, 245, 245))
                {
                    control.BackColor = DarkBackground;
                }
                else if (control.BackColor == Color.FromArgb(35, 52, 70) ||
                         control.BackColor == Color.FromArgb(10, 10, 10) ||
                         control.BackColor == Color.FromArgb(220, 220, 220))
                {
                    control.BackColor = DarkerBackground;
                }
                else if (control.BackColor == Color.FromArgb(55, 72, 90) ||
                         control.BackColor == Color.FromArgb(30, 30, 30) ||
                         control.BackColor == Color.White)
                {
                    control.BackColor = CardBackground;
                }
            }
            else if (control is DataGridView grid)
            {
                grid.BackgroundColor = CardBackground;
                grid.GridColor = DarkerBackground;
                grid.DefaultCellStyle.BackColor = CardBackground;
                grid.DefaultCellStyle.ForeColor = TextWhite;
                grid.DefaultCellStyle.SelectionBackColor = SelectedColor;
                grid.DefaultCellStyle.SelectionForeColor = TextWhite;
                grid.ColumnHeadersDefaultCellStyle.BackColor = DarkerBackground;
                grid.ColumnHeadersDefaultCellStyle.ForeColor = TextWhite;
            }
            else if (control is Label label)
            {
                // If label.Tag is a getter delegate (used by Risk Overview value labels),
                // skip overriding ForeColor here so specialized coloring (YellowBlueBlack) persists.
                bool isValueGetterLabel = label.Tag is Func<string>;

                if (!isValueGetterLabel)
                {
                    // Update text color for labels (only if not a bound value label)
                    if (label.ForeColor == Color.White ||
                        label.ForeColor == Color.FromArgb(30, 30, 30))
                    {
                        label.ForeColor = TextWhite;
                    }
                    else if (label.ForeColor == Color.FromArgb(189, 195, 199) ||
                             label.ForeColor == Color.FromArgb(160, 160, 160) ||
                             label.ForeColor == Color.FromArgb(90, 90, 90))
                    {
                        label.ForeColor = TextGray;
                    }
                }
                // Update background (safe to do for all labels)
                if (label.BackColor == Color.FromArgb(45, 62, 80) ||
                    label.BackColor == Color.FromArgb(20, 20, 20) ||
                    label.BackColor == Color.FromArgb(245, 245, 245))
                {
                    label.BackColor = DarkBackground;
                }
                else if (label.BackColor == Color.FromArgb(55, 72, 90) ||
                         label.BackColor == Color.FromArgb(30, 30, 30) ||
                         label.BackColor == Color.White)
                {
                    label.BackColor = CardBackground;
                }

                if (!string.IsNullOrEmpty(label.Text) && label.Text.StartsWith("$") && DollarImage != null)
                {
                    // Remove leading dollar sign from text and set image
                    label.Text = label.Text.TrimStart('$', ' ');
                    label.Image = DollarImage;
                    label.ImageAlign = ContentAlignment.MiddleLeft;
                    label.Padding = new Padding(24, 0, 0, 0);
                    label.TextAlign = ContentAlignment.MiddleRight;
                }

                label.Invalidate();
            }
            else if (control is TextBox textBox)
            {
                if (textBox.BackColor == Color.FromArgb(35, 52, 70) ||
                    textBox.BackColor == Color.FromArgb(10, 10, 10) ||
                    textBox.BackColor == Color.FromArgb(220, 220, 220))
                {
                    textBox.BackColor = DarkerBackground;
                }
                else if (textBox.BackColor == Color.FromArgb(55, 72, 90) ||
                         textBox.BackColor == Color.FromArgb(30, 30, 30) ||
                         textBox.BackColor == Color.White)
                {
                    textBox.BackColor = CardBackground;
                }
                textBox.ForeColor = TextWhite;
            }
            else if (control is ComboBox comboBox)
            {
                comboBox.BackColor = CardBackground;
                comboBox.ForeColor = TextWhite;
            }
            else if (control is CheckBox checkBox)
            {
                checkBox.ForeColor = TextWhite;
                if (checkBox.BackColor == Color.FromArgb(55, 72, 90) ||
                    checkBox.BackColor == Color.FromArgb(30, 30, 30) ||
                    checkBox.BackColor == Color.White)
                {
                    checkBox.BackColor = CardBackground;
                }
                else if (checkBox.BackColor == Color.FromArgb(35, 52, 70) ||
                         checkBox.BackColor == Color.FromArgb(10, 10, 10) ||
                         checkBox.BackColor == Color.FromArgb(220, 220, 220))
                {
                    checkBox.BackColor = DarkerBackground;
                }
                checkBox.Invalidate();
            }
            else if (control is Button button)
            {
                // Only update non-accent buttons
                if (button.BackColor != AccentGreen &&
                    button.BackColor != AccentAmber &&
                    button.BackColor != Color.Red)
                {
                    button.ForeColor = TextWhite;
                }
            }

            // Recursively update children
            foreach (Control child in control.Controls)
            {
                UpdateControlRecursively(child);
            }
        }

        /// <summary>
        /// Gets the theme preferences file path
        /// </summary>
        private string GetThemePreferencesPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folderPath = Path.Combine(appDataPath, "RiskManager");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return Path.Combine(folderPath, "theme_preference.txt");
        }

        /// <summary>
        /// Saves the current theme preference to disk
        /// </summary>
        private void SaveThemePreference()
        {
            // Don't save during initialization to avoid unnecessary file I/O
            if (isInitializing)
                return;
                
            try
            {
                var themePath = GetThemePreferencesPath();
                File.WriteAllText(themePath, currentTheme.ToString());
                System.Diagnostics.Debug.WriteLine($"Theme preference saved: {currentTheme}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save theme preference: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the theme preference from disk
        /// </summary>
        private Theme LoadThemePreference()
        {
            try
            {
                var themePath = GetThemePreferencesPath();
                if (File.Exists(themePath))
                {
                    var themeString = File.ReadAllText(themePath).Trim();
                    if (Enum.TryParse<Theme>(themeString, out var theme))
                    {
                        System.Diagnostics.Debug.WriteLine($"Theme preference loaded: {theme}");
                        return theme;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load theme preference: {ex.Message}");
            }
            
            // Default to Blue theme
            return Theme.Blue;
        }

        /// <summary>
        /// Gets a user-friendly display name for the theme
        /// </summary>
        private string GetThemeDisplayName(Theme theme)
        {
            switch (theme)
            {
                case Theme.Blue:
                    return "Blue";
                case Theme.Black:
                    return "Black";
                case Theme.White:
                    return "White";
                case Theme.YellowBlueBlack:
                    return "Yellow/Blue/Black";
                default:
                    return "Unknown";
            }
        }

        /// <summary>
        /// Saves the navigation collapse state preference to a file.
        /// </summary>
        private void SaveNavigationCollapsePreference()
        {
            try
            {
                var navStatePath = GetNavigationStatePreferencesPath();
                File.WriteAllText(navStatePath, isNavigationCollapsed.ToString());
                System.Diagnostics.Debug.WriteLine($"Navigation collapse state saved: {isNavigationCollapsed}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save navigation collapse state: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the saved navigation collapse state preference from a file.
        /// </summary>
        private bool LoadNavigationCollapsePreference()
        {
            try
            {
                var navStatePath = GetNavigationStatePreferencesPath();
                if (File.Exists(navStatePath))
                {
                    var stateString = File.ReadAllText(navStatePath).Trim();
                    if (bool.TryParse(stateString, out var isCollapsed))
                    {
                        System.Diagnostics.Debug.WriteLine($"Navigation collapse state loaded: {isCollapsed}");
                        return isCollapsed;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load navigation collapse state: {ex.Message}");
            }
            
            // Default to expanded (not collapsed)
            return false;
        }

        /// <summary>
        /// Gets the path to the progress bar preference file
        /// </summary>
        private string GetProgressBarPreferencesPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folderPath = Path.Combine(appDataPath, "RiskManager");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return Path.Combine(folderPath, "progressbar_preference.txt");
        }

        /// <summary>
        /// Gets the path where show percentage preference is stored
        /// </summary>
        private string GetShowPercentagePreferencesPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folderPath = Path.Combine(appDataPath, "RiskManager");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return Path.Combine(folderPath, "showpercentage_preference.txt");
        }

        /// <summary>
        /// Saves the progress bar preference to disk
        /// </summary>
        private void SaveProgressBarPreference()
        {
            // Don't save during initialization to avoid unnecessary file I/O
            if (isInitializing)
                return;
                
            try
            {
                var progressBarPath = GetProgressBarPreferencesPath();
                File.WriteAllText(progressBarPath, showProgressBars.ToString());
                System.Diagnostics.Debug.WriteLine($"Progress bar preference saved: {showProgressBars}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save progress bar preference: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves the show percentage preference to disk
        /// </summary>
        private void SaveShowPercentagePreference()
        {
            // Don't save during initialization to avoid unnecessary file I/O
            if (isInitializing)
                return;
                
            try
            {
                var percentagePath = GetShowPercentagePreferencesPath();
                File.WriteAllText(percentagePath, showPercentage.ToString());
                System.Diagnostics.Debug.WriteLine($"Show percentage preference saved: {showPercentage}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to save show percentage preference: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the progress bar preference from disk
        /// </summary>
        private bool LoadProgressBarPreference()
        {
            try
            {
                var progressBarPath = GetProgressBarPreferencesPath();
                if (File.Exists(progressBarPath))
                {
                    var progressBarString = File.ReadAllText(progressBarPath).Trim();
                    if (bool.TryParse(progressBarString, out var showBars))
                    {
                        System.Diagnostics.Debug.WriteLine($"Progress bar preference loaded: {showBars}");
                        return showBars;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load progress bar preference: {ex.Message}");
            }
            
            // Default to false (show normal columns)
            return false;
        }

        /// <summary>
        /// Loads the show percentage preference from disk
        /// </summary>
        private bool LoadShowPercentagePreference()
        {
            try
            {
                var percentagePath = GetShowPercentagePreferencesPath();
                if (File.Exists(percentagePath))
                {
                    var percentageString = File.ReadAllText(percentagePath).Trim();
                    if (bool.TryParse(percentageString, out var showPct))
                    {
                        System.Diagnostics.Debug.WriteLine($"Show percentage preference loaded: {showPct}");
                        return showPct;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load show percentage preference: {ex.Message}");
            }
            
            // Default to false (show dollar amount)
            return false;
        }

        /// <summary>
        /// Gets the path to the navigation state preferences file.
        /// </summary>
        private string GetNavigationStatePreferencesPath()
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var folderPath = Path.Combine(appDataPath, "RiskManager");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return Path.Combine(folderPath, "navigation_state.txt");
        }

        private void RefreshAccountDropdown()
        {
            if (accountSelector == null) return;

            var core = Core.Instance;
            var connectedAccounts = core?.Accounts?
                .Where(a => a != null && a.Connection != null)
                .ToList() ?? new List<Account>();

            // Preserve current selection if it still exists
            var currentSelection = accountSelector.SelectedItem as Account;
            var currentIndex = accountSelector.SelectedIndex;

            // Check if the accounts list has actually changed before refreshing
            bool needsUpdate = false;
            if (accountSelector.Items.Count != connectedAccounts.Count)
            {
                needsUpdate = true;
            }
            else
            {
                // Check if the same accounts are present
                for (int i = 0; i < connectedAccounts.Count; i++)
                {
                    // Check bounds first to prevent IndexOutOfRangeException
                    if (i >= accountSelector.Items.Count)
                    {
                        needsUpdate = true;
                        break;
                    }
                    if (accountSelector.Items[i] != connectedAccounts[i])
                    {
                        needsUpdate = true;
                        break;
                    }
                }
            }

            // Only update if accounts have changed
            if (!needsUpdate)
                return;

            // Temporarily disable event handling during update
            accountSelector.SelectedIndexChanged -= AccountSelectorOnSelectedIndexChanged;

            accountSelector.Items.Clear();
            foreach (var account in connectedAccounts)
            {
                accountSelector.Items.Add(account);
            }

            // Restore selection or select first
            // Check if current selection exists in the new items list
            if (currentSelection != null && accountSelector.Items.Contains(currentSelection))
            {
                accountSelector.SelectedItem = currentSelection;
                selectedAccountIndex = accountSelector.SelectedIndex; // Update stored index
            }
            else if (accountSelector.Items.Count > 0)
            {
                accountSelector.SelectedIndex = 0;
                selectedAccountIndex = 0; // Update stored index
            }
            else
            {
                accountSelector.SelectedIndex = -1;
                selectedAccountIndex = -1; // Update stored index
            }

            // Re-enable event handling
            accountSelector.SelectedIndexChanged += AccountSelectorOnSelectedIndexChanged;
            
            // If we just selected the first account and there was no previous selection,
            // manually trigger LoadAccountSettings since the event might not fire
            if (currentSelection == null && accountSelector.Items.Count > 0 && accountSelector.SelectedIndex == 0)
            {
                if (accountSelector.SelectedItem is Account account)
                {
                    selectedAccount = account;
                    selectedAccountIndex = 0; // Ensure index is set                  
                    LoadAccountSettings();
                }
            }
        }

        private void AccountSelectorOnSelectedIndexChanged(object sender, EventArgs e)
        {
            LogToFileAndDebug($"[AccountSelectorOnSelectedIndexChanged] === ACCOUNT CHANGE EVENT START ===");
            
            if (accountSelector.SelectedItem is Account account)
            {
                var oldAccountId = selectedAccount?.Id ?? "NULL";
                var oldAccountName = selectedAccount?.Name ?? "NULL";
                var newAccountId = account.Id ?? "NULL";
                var newAccountName = account.Name ?? "NULL";
                
                LogToFileAndDebug($"[AccountSelectorOnSelectedIndexChanged] Switching from Account: Id='{oldAccountId}', Name='{oldAccountName}'");
                LogToFileAndDebug($"[AccountSelectorOnSelectedIndexChanged] Switching to Account: Id='{newAccountId}', Name='{newAccountName}', Index={accountSelector.SelectedIndex}");
                
                selectedAccount = account;
                selectedAccountIndex = accountSelector.SelectedIndex; // Store the index
                
                // NOTE: We no longer reset the badge state cache here because it's now per-account
                // Each account maintains its own cache entry, so switching accounts will naturally
                // load the cached state for that specific account
                
                // Debug logging to help identify account selection issues
                var accountId = account.Id ?? "NULL";
                var accountName = account.Name ?? "NULL";
                LogToFileAndDebug($"[AccountSelectorOnSelectedIndexChanged] Account selected at index {selectedAccountIndex}: Id='{accountId}', Name='{accountName}'");
                
                UpdateAllLockAccountDisplays();
                
                // Update settings lock status labels and badge for the new account
                LogToFileAndDebug($"[AccountSelectorOnSelectedIndexChanged] Calling UpdateSettingsStatusLabelsRecursive...");
                UpdateSettingsStatusLabelsRecursive(this);
                
                // Update manual lock status labels for the new account
                LogToFileAndDebug($"[AccountSelectorOnSelectedIndexChanged] Calling UpdateManualLockStatusLabelsRecursive...");
                UpdateManualLockStatusLabelsRecursive(this);
                
                // Refresh Stats tab if visible
                if (statsDetailGrid != null)
                    RefreshAccountStats();
                
                // Refresh Risk Overview tab if it's currently displayed
                if (selectedNavItem != null && selectedNavItem.EndsWith("Risk Overview"))
                {
                    if (pageContents.TryGetValue(selectedNavItem, out var riskOverviewPanel))
                    {
                        RefreshRiskOverviewPanel(riskOverviewPanel);
                    }
                }
                
                // Load settings for the selected account
                LogToFileAndDebug($"[AccountSelectorOnSelectedIndexChanged] Calling LoadAccountSettings...");
                LoadAccountSettings();
                
                LogToFileAndDebug($"[AccountSelectorOnSelectedIndexChanged] === ACCOUNT CHANGE EVENT END ===");
            }
            else
            {
                LogToFileAndDebug($"[AccountSelectorOnSelectedIndexChanged] === ACCOUNT CHANGE EVENT END (Not an Account object) ===");
            }
        }

        /// <summary>
        /// Custom draw handler for account selector to support privacy mode masking.
        /// </summary>
        private void AccountSelector_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            
            if (e.Index < 0 || e.Index >= accountSelector.Items.Count)
                return;
            
            var account = accountSelector.Items[e.Index] as Account;
            if (account == null)
                return;
            
            // Get the display text (with masking if privacy mode is enabled)
            string displayText = MaskAccountNumber(account.Number);
            
            // Use the same colors as the combo box
            using (var textBrush = new SolidBrush(e.ForeColor))
            {
                var font = accountSelector.Font;
                
                // Draw the text
                e.Graphics.DrawString(displayText, font, textBrush, e.Bounds);
                
                e.DrawFocusRectangle();
            }
        }

        /// <summary>
        /// Custom draw handler for copy settings source combo box to support privacy mode masking.
        /// </summary>
        private void CopySettingsSourceComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            e.DrawBackground();
            
            if (e.Index < 0 || e.Index >= copySettingsSourceComboBox.Items.Count)
                return;
            
            var account = copySettingsSourceComboBox.Items[e.Index] as Account;
            if (account == null)
                return;
            
            // Get the display text (with masking if privacy mode is enabled)
            string displayText = MaskAccountNumber(account.Number);
            
            // Use the same colors as the combo box
            using (var textBrush = new SolidBrush(e.ForeColor))
            {
                var font = copySettingsSourceComboBox.Font;
                
                // Draw the text
                e.Graphics.DrawString(displayText, font, textBrush, e.Bounds);
                
                e.DrawFocusRectangle();
            }
        }

        /// <summary>
        /// Refreshes the Copy Settings panel source dropdown with connected accounts.
        /// </summary>
        private void RefreshCopySettingsAccounts()
        {
            if (copySettingsSourceComboBox == null || copySettingsTargetPanel == null)
                return;

            var core = Core.Instance;
            var connectedAccounts = core?.Accounts?
                .Where(a => a != null && a.Connection != null)
                .ToList() ?? new List<Account>();

            // Preserve current selection
            var currentSelection = copySettingsSourceComboBox.SelectedItem as Account;

            // Check if accounts list has changed
            bool needsUpdate = false;
            if (copySettingsSourceComboBox.Items.Count != connectedAccounts.Count)
            {
                needsUpdate = true;
            }
            else
            {
                for (int i = 0; i < connectedAccounts.Count; i++)
                {
                    if (i >= copySettingsSourceComboBox.Items.Count)
                    {
                        needsUpdate = true;
                        break;
                    }
                    if (copySettingsSourceComboBox.Items[i] != connectedAccounts[i])
                    {
                        needsUpdate = true;
                        break;
                    }
                }
            }

            if (!needsUpdate)
                return;

            // Temporarily disable event handling during update
            copySettingsSourceComboBox.SelectedIndexChanged -= null; // Will be re-added automatically by designer

            copySettingsSourceComboBox.Items.Clear();
            foreach (var account in connectedAccounts)
            {
                copySettingsSourceComboBox.Items.Add(account);
            }

            // Restore selection if it still exists
            if (currentSelection != null && copySettingsSourceComboBox.Items.Contains(currentSelection))
            {
                copySettingsSourceComboBox.SelectedItem = currentSelection;
            }
            else if (copySettingsSourceComboBox.Items.Count > 0)
            {
                // Don't auto-select, let user choose
                copySettingsSourceComboBox.SelectedIndex = -1;
            }

            // Clear target panel since source selection may have changed
            copySettingsTargetPanel.Controls.Clear();
        }

        /// <summary>
        /// Loads settings for the currently selected account and updates all UI controls.
        /// </summary>
        private void LoadAccountSettings()
        {
            try
            {
                var accountNumber = GetSelectedAccountNumber();
                
                // Debug logging
                System.Diagnostics.Debug.WriteLine($"LoadAccountSettings called for account: '{accountNumber}'");
                
                if (string.IsNullOrEmpty(accountNumber))
                {
                    System.Diagnostics.Debug.WriteLine("LoadAccountSettings: No account selected, clearing inputs");
                    ClearSettingsInputs();
                    return;
                }

                var settingsService = RiskManagerSettingsService.Instance;
                if (!settingsService.IsInitialized)
                {
                    System.Diagnostics.Debug.WriteLine("LoadAccountSettings: Settings service not initialized");
                    return;
                }

                var settings = settingsService.GetSettings(accountNumber);
                
                // If no settings exist yet, use defaults
                if (settings == null)
                {
                    System.Diagnostics.Debug.WriteLine($"LoadAccountSettings: No settings found for account '{accountNumber}', using defaults");
                    // Clear all inputs to defaults
                    ClearSettingsInputs();
                    return;
                }
                
                System.Diagnostics.Debug.WriteLine($"LoadAccountSettings: Loading settings for account '{accountNumber}'");

                // Load Feature Toggle Enabled
                if (featureToggleEnabledCheckbox != null)
                {
                    featureToggleEnabledCheckbox.Checked = settings.FeatureToggleEnabled;
                }

                // Load Individual Feature Toggles
                if (positionsFeatureCheckbox != null)
                {
                    positionsFeatureCheckbox.Checked = settings.PositionsEnabled;
                }
                if (limitsFeatureCheckbox != null)
                {
                    limitsFeatureCheckbox.Checked = settings.LimitsEnabled;
                }
                if (symbolsFeatureCheckbox != null)
                {
                    symbolsFeatureCheckbox.Checked = settings.SymbolsEnabled;
                }
                if (tradingTimesFeatureCheckbox != null)
                {
                    tradingTimesFeatureCheckbox.Checked = settings.TradingTimesEnabled;
                }

                // Load Enforcement Mode
                if (strictModeRadioButton != null && warningModeRadioButton != null && monitorModeRadioButton != null)
                {
                    switch (settings.EnforcementMode)
                    {
                        case RiskEnforcementMode.Strict:
                            strictModeRadioButton.Checked = true;
                            break;
                        case RiskEnforcementMode.Warning:
                            warningModeRadioButton.Checked = true;
                            break;
                        case RiskEnforcementMode.Monitor:
                            monitorModeRadioButton.Checked = true;
                            break;
                    }
                }

                // Load Daily Limits
                if (dailyLossLimitEnabled != null && dailyLossLimitInput != null)
                {
                    dailyLossLimitEnabled.Checked = settings.DailyLossLimit.HasValue;
                    // Display as positive value for user-friendly input (stored as negative internally)
                    dailyLossLimitInput.Text = settings.DailyLossLimit.HasValue 
                        ? Math.Abs(settings.DailyLossLimit.Value).ToString() 
                        : "0";
                }

                if (dailyProfitTargetEnabled != null && dailyProfitTargetInput != null)
                {
                    dailyProfitTargetEnabled.Checked = settings.DailyProfitTarget.HasValue;
                    dailyProfitTargetInput.Text = settings.DailyProfitTarget?.ToString() ?? "0";
                }

                // Load Position Limits
                if (positionLossLimitEnabled != null && positionLossLimitInput != null)
                {
                    positionLossLimitEnabled.Checked = settings.PositionLossLimit.HasValue;
                    // Display as positive value for user-friendly input (stored as negative internally)
                    positionLossLimitInput.Text = settings.PositionLossLimit.HasValue 
                        ? Math.Abs(settings.PositionLossLimit.Value).ToString() 
                        : "0";
                }

                if (positionProfitTargetEnabled != null && positionProfitTargetInput != null)
                {
                    positionProfitTargetEnabled.Checked = settings.PositionProfitTarget.HasValue;
                    positionProfitTargetInput.Text = settings.PositionProfitTarget?.ToString() ?? "0";
                }

                // Load Weekly Limits
                if (weeklyLossLimitEnabled != null && weeklyLossLimitInput != null)
                {
                    weeklyLossLimitEnabled.Checked = settings.WeeklyLossLimit.HasValue;
                    // Display as positive value for user-friendly input (stored as negative internally)
                    weeklyLossLimitInput.Text = settings.WeeklyLossLimit.HasValue 
                        ? Math.Abs(settings.WeeklyLossLimit.Value).ToString() 
                        : DEFAULT_WEEKLY_LOSS_LIMIT.ToString();
                }

                if (weeklyProfitTargetEnabled != null && weeklyProfitTargetInput != null)
                {
                    weeklyProfitTargetEnabled.Checked = settings.WeeklyProfitTarget.HasValue;
                    weeklyProfitTargetInput.Text = settings.WeeklyProfitTarget?.ToString() ?? DEFAULT_WEEKLY_PROFIT_TARGET.ToString();
                }

                // Load Symbol Blacklist
                if (blockedSymbolsEnabled != null && blockedSymbolsInput != null)
                {
                    blockedSymbolsEnabled.Checked = settings.BlockedSymbols != null && settings.BlockedSymbols.Any();
                    blockedSymbolsInput.Text = settings.BlockedSymbols != null ? string.Join(", ", settings.BlockedSymbols) : "";
                }

                // Load Contract Limits
                if (symbolContractLimitsEnabled != null && defaultContractLimitInput != null)
                {
                    symbolContractLimitsEnabled.Checked = settings.DefaultContractLimit.HasValue || 
                                                          (settings.SymbolContractLimits != null && settings.SymbolContractLimits.Any());
                    defaultContractLimitInput.Text = settings.DefaultContractLimit?.ToString() ?? DEFAULT_CONTRACT_LIMIT.ToString();
                }

                if (symbolContractLimitsInput != null && settings.SymbolContractLimits != null)
                {
                    var limitEntries = settings.SymbolContractLimits
                        .Select(kvp => $"{kvp.Key}:{kvp.Value}");
                    symbolContractLimitsInput.Text = string.Join(", ", limitEntries);
                }

                // Load Locks
                if (tradingLockCheckBox != null)
                {
                    tradingLockCheckBox.Checked = settings.TradingLock?.IsLocked ?? false;
                }

                if (settingsLockCheckBox != null)
                {
                    settingsLockCheckBox.Checked = settings.SettingsLock?.IsLocked ?? false;
                }

                // Load Trading Time Restrictions
                LoadTradingTimeRestrictions(settings);

                // Update status displays
                LogToFileAndDebug($"[LoadAccountSettings] About to call UpdateTradingStatusBadge...");
                UpdateTradingStatusBadge();
                LogToFileAndDebug($"[LoadAccountSettings] About to call UpdateSettingsStatusBadge...");
                UpdateSettingsStatusBadge();
                LogToFileAndDebug($"[LoadAccountSettings] Badge updates completed.");
                if (settingsLockCheckBox?.Tag is Label statusLabel)
                {
                    UpdateSettingsLockStatus(statusLabel);
                }
                
                // Enable/disable controls based on settings lock status
                try
                {
                    UpdateSettingsControlsEnabledState();
                }
                catch (Exception updateEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error updating settings controls enabled state: {updateEx.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading account settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads trading time restrictions into the UI from settings.
        /// </summary>
        private void LoadTradingTimeRestrictions(AccountSettings settings)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"=== LoadTradingTimeRestrictions called ===");
                System.Diagnostics.Debug.WriteLine($"Settings: {(settings != null ? "Found" : "NULL")}");
                if (settings?.TradingTimeRestrictions != null)
                {
                    System.Diagnostics.Debug.WriteLine($"TradingTimeRestrictions count: {settings.TradingTimeRestrictions.Count}");
                    foreach (var r in settings.TradingTimeRestrictions)
                    {
                        var startFormatted = FormatTimeSpan(r.StartTime);
                        var endFormatted = FormatTimeSpan(r.EndTime);
                        System.Diagnostics.Debug.WriteLine($"  - {r.DayOfWeek}: {startFormatted} to {endFormatted} ({r.Name})");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("TradingTimeRestrictions is NULL or empty");
                }
                
                // Find the trading times rows container
                foreach (Control pageControl in pageContents.Values)
                {
                    var tradingTimeContentArea = FindControlByTag(pageControl, "TradingTimeContentArea");
                    if (tradingTimeContentArea != null)
                    {
                        System.Diagnostics.Debug.WriteLine("Found TradingTimeContentArea for loading");
                        var rowsContainer = FindControlByTag(tradingTimeContentArea, "TradingTimeRowsContainer") as FlowLayoutPanel;
                        if (rowsContainer != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Found rowsContainer, clearing {rowsContainer.Controls.Count} existing controls");
                            // Clear existing rows
                            rowsContainer.Controls.Clear();

                            // Add rows from settings
                            if (settings?.TradingTimeRestrictions != null && settings.TradingTimeRestrictions.Any())
                            {
                                System.Diagnostics.Debug.WriteLine($"Loading {settings.TradingTimeRestrictions.Count} restrictions from settings");
                                foreach (var restriction in settings.TradingTimeRestrictions)
                                {
                                    AddTradingTimeRow(rowsContainer, restriction);
                                    var startTimeFormatted = FormatTimeSpan(restriction.StartTime);
                                    var endTimeFormatted = FormatTimeSpan(restriction.EndTime);
                                    System.Diagnostics.Debug.WriteLine($"Added row for: {restriction.DayOfWeek} {startTimeFormatted} - {endTimeFormatted}");
                                }
                            }
                            else
                            {
                                // Add one default row if no restrictions exist
                                System.Diagnostics.Debug.WriteLine("No restrictions in settings, adding default row");
                                AddTradingTimeRow(rowsContainer);
                            }
                            System.Diagnostics.Debug.WriteLine($"Load complete. rowsContainer now has {rowsContainer.Controls.Count} controls");
                            
                            // Force UI refresh to ensure the controls are visible
                            rowsContainer.PerformLayout();
                            rowsContainer.Refresh();
                            if (rowsContainer.Parent != null)
                            {
                                rowsContainer.Parent.PerformLayout();
                                rowsContainer.Parent.Refresh();
                            }
                            System.Diagnostics.Debug.WriteLine("UI refresh completed");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("rowsContainer NOT found");
                        }
                        break;
                    }
                }
                System.Diagnostics.Debug.WriteLine("=== LoadTradingTimeRestrictions completed ===");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading trading time restrictions: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Enables or disables all settings input controls and navigation tabs based on whether settings are locked.
        /// When settings are locked, only the "Lock Trading" action remains available.
        /// </summary>
        private void UpdateSettingsControlsEnabledState()
        {
            try
            {
                var accountNumber = GetSelectedAccountNumber();
                if (string.IsNullOrEmpty(accountNumber))
                {
                    // No account selected - disable all controls and tabs
                    SetAllSettingsControlsEnabled(false);
                    SetNavigationTabsEnabled(false);
                    UpdateLockUnlockButtonStates(null);
                    return;
                }

                var settingsService = RiskManagerSettingsService.Instance;
                if (!settingsService.IsInitialized)
                {
                    // Service not initialized - enable controls by default
                    SetAllSettingsControlsEnabled(true);
                    SetNavigationTabsEnabled(true);
                    UpdateLockUnlockButtonStates(false); // Assume unlocked
                    return;
                }

                bool isLocked = settingsService.AreSettingsLocked(accountNumber);
                
                // Disable all settings controls and tabs if locked, enable if unlocked
                SetAllSettingsControlsEnabled(!isLocked);
                SetNavigationTabsEnabled(!isLocked);
                UpdateLockUnlockButtonStates(isLocked);
                
                System.Diagnostics.Debug.WriteLine($"UpdateSettingsControlsEnabledState: account='{accountNumber}', isLocked={isLocked}, controlsEnabled={!isLocked}, tabsEnabled={!isLocked}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating settings controls enabled state: {ex.Message}");
            }
        }

        // Overload that accepts explicit account number to avoid timing issues
        private void UpdateSettingsControlsEnabledState(string accountNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(accountNumber))
                {
                    // No account specified - disable all controls and tabs
                    SetAllSettingsControlsEnabled(false);
                    SetNavigationTabsEnabled(false);
                    UpdateLockUnlockButtonStates(null);
                    return;
                }

                var settingsService = RiskManagerSettingsService.Instance;
                if (!settingsService.IsInitialized)
                {
                    // Service not initialized - enable controls by default
                    SetAllSettingsControlsEnabled(true);
                    SetNavigationTabsEnabled(true);
                    UpdateLockUnlockButtonStates(false); // Assume unlocked
                    return;
                }

                bool isLocked = settingsService.AreSettingsLocked(accountNumber);
                
                // Disable all settings controls and tabs if locked, enable if unlocked
                SetAllSettingsControlsEnabled(!isLocked);
                SetNavigationTabsEnabled(!isLocked);
                UpdateLockUnlockButtonStates(isLocked);
                
                System.Diagnostics.Debug.WriteLine($"UpdateSettingsControlsEnabledState (explicit account): account='{accountNumber}', isLocked={isLocked}, controlsEnabled={!isLocked}, tabsEnabled={!isLocked}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating settings controls enabled state for account '{accountNumber}': {ex.Message}");
            }
        }

        // Update lock/unlock button states based on current lock status
        private void UpdateLockUnlockButtonStates(bool? isLocked)
        {
            try
            {
                if (lockSettingsButton != null && unlockSettingsButton != null)
                {
                    if (isLocked.HasValue)
                    {
                        // Disable lock button if already locked, disable unlock button if already unlocked
                        lockSettingsButton.Enabled = !isLocked.Value;
                        unlockSettingsButton.Enabled = isLocked.Value;
                        System.Diagnostics.Debug.WriteLine($"UpdateLockUnlockButtonStates: isLocked={isLocked.Value}, lockButtonEnabled={!isLocked.Value}, unlockButtonEnabled={isLocked.Value}");
                    }
                    else
                    {
                        // No lock status - enable both buttons
                        lockSettingsButton.Enabled = true;
                        unlockSettingsButton.Enabled = true;
                        System.Diagnostics.Debug.WriteLine("UpdateLockUnlockButtonStates: No lock status, both buttons enabled");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating lock/unlock button states: {ex.Message}");
            }
        }

        
        /// <summary>
        /// Enables or disables navigation tabs that should be locked when settings are locked.
        /// Tabs to disable: Feature Toggles, Positions, Limits, Symbols, Allowed Trading Times, Copy Settings
        /// </summary>
        private void SetNavigationTabsEnabled(bool enabled)
        {
            try
            {
                // Define tabs that should be disabled when settings are locked
                var tabsToDisable = new[]
                {
                    "⚙️ Feature Toggles",
                    "📈 Positions",
                    "📊 Limits",
                    "🛡️ Symbols",
                    "🕐 Allowed Trading Times",
                    "📋 Copy Settings"
                };

                foreach (var btn in navButtons)
                {
                    var itemName = btn.Tag as string;
                    if (!string.IsNullOrEmpty(itemName) && tabsToDisable.Contains(itemName))
                    {
                        btn.Enabled = enabled;
                        
                        // Grey out disabled buttons by adjusting ForeColor
                        if (!enabled)
                        {
                            btn.ForeColor = Color.FromArgb(100, 100, 100); // Dark grey
                            btn.Cursor = Cursors.No;
                        }
                        else
                        {
                            btn.ForeColor = TextWhite;
                            btn.Cursor = Cursors.Hand;
                        }
                        
                        btn.Invalidate(); // Force repaint
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"SetNavigationTabsEnabled: enabled={enabled}, affected {tabsToDisable.Length} tabs");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting navigation tabs enabled state: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Recursively enables or disables all settings input controls.
        /// Trading lock controls are intentionally excluded to remain functional during settings lock.
        /// </summary>
        private void SetAllSettingsControlsEnabled(bool enabled)
        {
            try
            {
                // List of controls to enable/disable
                // Note: tradingLockCheckBox is intentionally excluded so trading can still be locked/unlocked
                // even when settings are locked
                var controls = new Control[]
                {
                    dailyLossLimitEnabled,
                    dailyLossLimitInput,
                    dailyProfitTargetEnabled,
                    dailyProfitTargetInput,
                    positionLossLimitEnabled,
                    positionLossLimitInput,
                    positionProfitTargetEnabled,
                    positionProfitTargetInput,
                    weeklyLossLimitEnabled,
                    weeklyLossLimitInput,
                    weeklyProfitTargetEnabled,
                    weeklyProfitTargetInput,
                    blockedSymbolsEnabled,
                    blockedSymbolsInput,
                    symbolContractLimitsEnabled,
                    defaultContractLimitInput,
                    symbolContractLimitsInput,
                    // tradingLockCheckBox deliberately excluded
                    featureToggleEnabledCheckbox
                };

                foreach (var control in controls)
                {
                    if (control != null)
                    {
                        control.Enabled = enabled;
                    }
                }
                
                // Also disable trading time checkboxes if we have them
                if (tradingTimeCheckboxes != null)
                {
                    foreach (var checkbox in tradingTimeCheckboxes)
                    {
                        if (checkbox != null)
                        {
                            checkbox.Enabled = enabled;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting settings controls enabled state: {ex.Message}");
            }
        }

        /// <summary>
        /// Clears all settings inputs to default values.
        /// </summary>
        private void ClearSettingsInputs()
        {
            if (dailyLossLimitEnabled != null) dailyLossLimitEnabled.Checked = false;
            if (dailyLossLimitInput != null) dailyLossLimitInput.Text = "0";
            
            if (dailyProfitTargetEnabled != null) dailyProfitTargetEnabled.Checked = false;
            if (dailyProfitTargetInput != null) dailyProfitTargetInput.Text = "0";
            
            if (positionLossLimitEnabled != null) positionLossLimitEnabled.Checked = false;
            if (positionLossLimitInput != null) positionLossLimitInput.Text = "0";
            
            if (positionProfitTargetEnabled != null) positionProfitTargetEnabled.Checked = false;
            if (positionProfitTargetInput != null) positionProfitTargetInput.Text = "0";
            
            if (weeklyLossLimitEnabled != null) weeklyLossLimitEnabled.Checked = false;
            if (weeklyLossLimitInput != null) weeklyLossLimitInput.Text = DEFAULT_WEEKLY_LOSS_LIMIT.ToString();
            
            if (weeklyProfitTargetEnabled != null) weeklyProfitTargetEnabled.Checked = false;
            if (weeklyProfitTargetInput != null) weeklyProfitTargetInput.Text = DEFAULT_WEEKLY_PROFIT_TARGET.ToString();
            
            if (blockedSymbolsEnabled != null) blockedSymbolsEnabled.Checked = false;
            if (blockedSymbolsInput != null) blockedSymbolsInput.Text = "";
            
            if (symbolContractLimitsEnabled != null) symbolContractLimitsEnabled.Checked = false;
            if (defaultContractLimitInput != null) defaultContractLimitInput.Text = DEFAULT_CONTRACT_LIMIT.ToString();
            if (symbolContractLimitsInput != null) symbolContractLimitsInput.Text = "";
            
            if (tradingLockCheckBox != null) tradingLockCheckBox.Checked = false;
            if (settingsLockCheckBox != null) settingsLockCheckBox.Checked = false;
            
            if (featureToggleEnabledCheckbox != null) featureToggleEnabledCheckbox.Checked = true;
            if (positionsFeatureCheckbox != null) positionsFeatureCheckbox.Checked = true;
            if (limitsFeatureCheckbox != null) limitsFeatureCheckbox.Checked = true;
            if (symbolsFeatureCheckbox != null) symbolsFeatureCheckbox.Checked = true;
            if (tradingTimesFeatureCheckbox != null) tradingTimesFeatureCheckbox.Checked = true;
        }

        // Add this helper method in the RiskManagerControl class (anywhere above CreateTopPanel)
        // LoadIcons and helper
        // LoadIcons and helper
        // LoadIcons and helper
        private void LoadIcons()
        {
            try
            {
                IconMap.Clear();

                // Tab / header mappings (exact title keys, case-insensitive)
                IconMap["Accounts Summary"] = Properties.Resources.summary;
                IconMap["Stats"] = Properties.Resources.stats;
                IconMap["Type"] = Properties.Resources.type;
                IconMap["Risk Overview"] = Properties.Resources.riskoverview;
                IconMap["Positions"] = Properties.Resources.positions;
                IconMap["Feature Toggles"] = Properties.Resources.featuretoggles;
                IconMap["Copy Settings"] = Properties.Resources.copy;
                IconMap["Limits"] = Properties.Resources.limit;
                IconMap["Position Loss Limit"] = Properties.Resources.positionloss;
                IconMap["Position Profit Target"] = Properties.Resources.positionprofit;
                IconMap["Daily Loss Limit"] = Properties.Resources.dailyloss;
                IconMap["Daily Profit Target"] = Properties.Resources.dailyprofit;
                IconMap["Symbols"] = Properties.Resources.blocked;
                IconMap["Locked"] = Properties.Resources._lock; // fallback or generic lock
                IconMap["Unlocked"] = Properties.Resources.unlock;
                IconMap["Allowed Trading Times"] = Properties.Resources.clock;

                // Use specific PNGs for Lock tabs per request
                // "Lock Settings" header/tab uses locksettins.png resource (identifier: locksettins)
                // "Manual Lock" header/tab uses locktrading.png resource (identifier: locktrading)
                // Make sure these resources exist in Properties.Resources with these exact identifiers.
                try
                {
                    IconMap["Lock Settings"] = Properties.Resources.locksettings;
                }
                catch
                {
                    // fallback to generic lock if specific resource missing
                    IconMap["Lock Settings"] = Properties.Resources._lock;
                }

                try
                {
                    IconMap["Manual Lock"] = Properties.Resources.locktrading;
                }
                catch
                {
                    // fallback to generic lock if specific resource missing
                    IconMap["Manual Lock"] = Properties.Resources._lock;
                }

                // General Settings icon
                try
                {
                    IconMap["General Settings"] = Properties.Resources.generalsettings;
                }
                catch
                {
                    // fallback to feature toggles icon if missing
                    IconMap["General Settings"] = Properties.Resources.featuretoggles;
                }

                // Explicit card header mappings for Risk Overview
                IconMap[CARD_TITLE_ACCOUNT_STATUS] = Properties.Resources._lock;     // lock.png
                IconMap["Position Limits"] = Properties.Resources.positions;         // positions.png
                IconMap["Daily Limits"] = Properties.Resources.limit;                // limit.png
                IconMap["Symbol Restrictions"] = Properties.Resources.blocked;       // blocked.png
                IconMap["Allowed Trading Times"] = Properties.Resources.clock;       // clock.png

                // Additional lock-related title variants (keep fallback)
                IconMap["Settings Lock"] = Properties.Resources._lock;
                IconMap["Trading Lock"] = Properties.Resources._lock;

                // Emoji fallbacks (if any code passes raw emoji tokens)
                IconMap["📊"] = Properties.Resources.summary;
                IconMap["📈"] = Properties.Resources.stats;
                IconMap["📋"] = Properties.Resources.type;
                IconMap["🔍"] = Properties.Resources.riskoverview;
                IconMap["⚙️"] = Properties.Resources.featuretoggles;
                IconMap["🛡️"] = Properties.Resources.blocked;
                IconMap["🔒"] = Properties.Resources._lock;
                IconMap["🔓"] = Properties.Resources.unlock;
                IconMap["🕐"] = Properties.Resources.clock;
                IconMap["💵"] = Properties.Resources.dollar;

                // Theme switcher image (optional)
                if (Properties.Resources.ResourceManager.GetObject("themeswitcher2") is Image themeImg)
                    IconMap["ThemeSwitcher"] = themeImg;

                // Shutdown button image (leave.png)
                IconMap["Leave"] = Properties.Resources.leavefour;

                DollarImage = Properties.Resources.dollar;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoadIcons failed: {ex.Message}");
            }
        }

        private Image GetIconForTitle(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
                return null;

            var trimmed = raw.Trim();

            // If the string starts with an emoji token, try to remove it and use remainder as key:
            // find first space — assumes "EMOJI Title" format used across the app
            var after = trimmed;
            var idx = trimmed.IndexOf(' ');
            if (idx >= 0 && idx + 1 < trimmed.Length)
                after = trimmed.Substring(idx + 1).Trim();

            // Try exact title lookup (case-insensitive)
            foreach (var key in IconMap.Keys)
            {
                if (string.Equals(key, after, StringComparison.OrdinalIgnoreCase))
                    return IconMap[key];
            }

            // Try exact raw lookup (in case resource keyed to raw string)
            foreach (var key in IconMap.Keys)
            {
                if (string.Equals(key, trimmed, StringComparison.OrdinalIgnoreCase))
                    return IconMap[key];
            }

            // Fallback: first char (emoji) mapping
            var first = trimmed.Length > 0 ? trimmed[0].ToString() : null;
            if (!string.IsNullOrEmpty(first) && IconMap.TryGetValue(first, out var img))
                return img;

            return null;
        }

        // add helper method in RiskManagerControl class (anywhere above CreateTopPanel)
        private Image ScaleImageToFit(Image src, int maxWidth, int maxHeight)
        {
            if (src == null) return null;

            // Determine scale preserving aspect ratio
            double ratioX = (double)maxWidth / src.Width;
            double ratioY = (double)maxHeight / src.Height;
            double ratio = Math.Min(ratioX, ratioY);
            if (ratio <= 0) ratio = 1.0;

            int newWidth = Math.Max(1, (int)(src.Width * ratio));
            int newHeight = Math.Max(1, (int)(src.Height * ratio));

            var bmp = new Bitmap(newWidth, newHeight);
            using (var g = Graphics.FromImage(bmp))
            {
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = SmoothingMode.HighQuality;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.Clear(Color.Transparent);
                g.DrawImage(src, 0, 0, newWidth, newHeight);
            }
            return bmp;
        }

        private Panel CreateTopPanel()
        {
            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 90,  // Height to accommodate shutdown button on left and status elements
                BackColor = DarkBackground,
                Padding = new Padding(15, 10, 15, 10),
                Cursor = Cursors.SizeAll  // Show move cursor to indicate draggability
            };

            // Make the top panel draggable to move the parent window
            EnableDragging(topPanel);

            // Shutdown button (placed on the left before the title)
            shutdownButton = new Button
            {
                Text = "",
                Width = 44,  // Same size as before
                Height = 36,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Location = new Point(0, 5),  // Position at left edge
                Padding = new Padding(0),
                UseCompatibleTextRendering = true
            };
            shutdownButton.FlatAppearance.BorderSize = 0;
            shutdownButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(231, 76, 60); // Red hover color

            // Set shutdown icon image
            Image shutdownImg = null;
            if (IconMap.TryGetValue("Leave", out var leaveImg) && leaveImg != null)
                shutdownImg = leaveImg;
            else
            {
                try { shutdownImg = Properties.Resources.leave; } catch { shutdownImg = null; }
            }

            if (shutdownImg != null)
            {
                // Dispose previous scaled image to avoid leaks
                shutdownButtonScaledImage?.Dispose();

                // Fill height almost completely
                float scaleBoost = 1.15f;
                int targetHeight = (int)((shutdownButton.Height - 2) * scaleBoost);

                float scale = (float)targetHeight / shutdownImg.Height;

                int targetWidth = (int)(shutdownImg.Width * scale);

                shutdownButtonScaledImage = new Bitmap(targetWidth, targetHeight);
                using (var g = Graphics.FromImage(shutdownButtonScaledImage))
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.DrawImage(shutdownImg, 0, 0, targetWidth, targetHeight);
                }
                shutdownButton.Image = shutdownButtonScaledImage;
                shutdownButton.ImageAlign = ContentAlignment.MiddleCenter;
                shutdownButton.Text = "";
                shutdownButton.TextImageRelation = TextImageRelation.ImageBeforeText;
            }
            else
            {
                // fallback to text if resource is missing
                shutdownButton.Text = "🚪";
                shutdownButton.Font = new Font("Segoe UI Emoji", 18, FontStyle.Bold);
            }

            shutdownButton.Click += ShutdownButton_Click;
            topPanel.Controls.Add(shutdownButton);

            // Title label (positioned after shutdown button with spacing)
            var titleLabel = new Label
            {
                Text = "Risk Manager",
                AutoSize = true,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(40, 6),  // Moved right to accommodate button (15 + 50 + 15 spacing)
                Cursor = Cursors.SizeAll  // Show move cursor
            };
            // Add tooltip to indicate draggability
            titleToolTip = new ToolTip();
            titleToolTip.SetToolTip(titleLabel, "Click and drag to move window");
            EnableDragging(titleLabel);  // Make title draggable too
            topPanel.Controls.Add(titleLabel);

            // Account selector
            var accountLabel = new Label
            {
                Text = "Account:",
                AutoSize = true,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Location = new Point(15, 40)  // Align with title label horizontal position
            };
            topPanel.Controls.Add(accountLabel);

            accountSelector = new ComboBox
            {
                Location = new Point(80, 37),  // Adjusted to accommodate label shift
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                BackColor = CardBackground,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat,
                DrawMode = DrawMode.OwnerDrawFixed
            };
            accountSelector.DrawItem += AccountSelector_DrawItem;
            accountSelector.SelectedIndexChanged += AccountSelectorOnSelectedIndexChanged;
            topPanel.Controls.Add(accountSelector);

            // Emergency Flatten button next to Account Selector       
            // Emergency Flatten button (image-left + image-right, centered text)
            var emergencyFlattenButton = new Button
            {
                Text = "", // painted manually
                Location = new Point(340, 37),  // Adjusted to align with new account selector position
                Width = 250,
                Height = 26,
                Font = new Font("Arial", 10, FontStyle.Bold),
                BackColor = Color.Red,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            emergencyFlattenButton.FlatAppearance.BorderSize = 0;
            emergencyFlattenButton.Click += EmergencyFlattenButton_Click;

            // Load caution image (try IconMap key "caution" first, then fallback to resource)
            Image cautionImg = null;
            if (IconMap.TryGetValue("caution", out var ico) && ico != null)
                cautionImg = ico;
            else
            {
                try { cautionImg = Properties.Resources.caution; } catch { cautionImg = null; }
            }

            // Prepare a single scaled icon used on both sides (dispose previous if any)
            if (cautionImg != null)
            {
                try { cautionButtonScaledImage?.Dispose(); } catch { }
                int pad = 6;
                int iconSize = Math.Max(8, emergencyFlattenButton.Height - pad);
                cautionButtonScaledImage = ScaleImageToFit(cautionImg, iconSize, iconSize);
            }

            // Custom paint: draw left icon, right icon, centered text
            emergencyFlattenButton.Paint += (s, e) =>
            {
                var btn = (Button)s;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Fill background
                using (var bg = new SolidBrush(btn.BackColor))
                    e.Graphics.FillRectangle(bg, 0, 0, btn.Width, btn.Height);

                int leftPadding = 8;
                int rightPadding = 8;
                int xLeft = leftPadding;
                int xRight = btn.Width - rightPadding;

                int leftIconWidth = 0;
                int rightIconWidth = 0;

                if (cautionButtonScaledImage is Bitmap leftBmp)
                {
                    leftIconWidth = leftBmp.Width;
                    int iconY = (btn.Height - leftBmp.Height) / 2;
                    e.Graphics.DrawImage(leftBmp, new Rectangle(xLeft, iconY, leftBmp.Width, leftBmp.Height));
                    xLeft += leftIconWidth + 8;
                }

                if (cautionButtonScaledImage is Bitmap rightBmp)
                {
                    rightIconWidth = rightBmp.Width;
                    int iconY = (btn.Height - rightBmp.Height) / 2;
                    int drawX = btn.Width - rightPadding - rightIconWidth;
                    e.Graphics.DrawImage(rightBmp, new Rectangle(drawX, iconY, rightBmp.Width, rightBmp.Height));
                    xRight = drawX - 8;
                }

                // Compute available rect between left and right icons
                int availableX = xLeft;
                int availableWidth = Math.Max(0, xRight - availableX);

                // Center the text within available rect
                string text = "EMERGENCY FLATTEN";
                using (var brush = new SolidBrush(btn.ForeColor))
                {
                    var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Center };
                    var textRect = new RectangleF(availableX, 0, availableWidth, btn.Height);
                    e.Graphics.DrawString(text, btn.Font, brush, textRect, sf);
                }
            };

            topPanel.Controls.Add(emergencyFlattenButton);

            // Status badges container (right-aligned)
            var badgesPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = Color.Transparent,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Padding = new Padding(0)
            };

            // Create status table to display Settings Status and Trading Status
            statusTableView = new DataGridView
            {
                Width = 350,
                Height = 70,
                BackgroundColor = CardBackground,
                GridColor = DarkerBackground,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                ColumnHeadersVisible = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AllowUserToResizeColumns = false,
                ReadOnly = true,
                ScrollBars = ScrollBars.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    BackColor = CardBackground,
                    ForeColor = TextWhite,
                    SelectionBackColor = CardBackground,
                    SelectionForeColor = TextWhite,
                    Font = new Font("Segoe UI", 9, FontStyle.Regular),
                    Padding = new Padding(5, 2, 5, 2)
                }
            };
            
            // Add columns - Icon, Status Label, Value
            var iconColumn = new DataGridViewImageColumn
            {
                Name = "Icon",
                HeaderText = "Icon",
                Width = 48,
                ImageLayout = DataGridViewImageCellLayout.Zoom,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.None
            };
            statusTableView.Columns.Add(iconColumn);
            statusTableView.Columns.Add("Status", "Status");
            statusTableView.Columns.Add("Value", "Value");
            
            // Get icons for the status rows
            var settingsIcon = GetIconForTitle("Lock Settings") ?? GetIconForTitle("Locked");
            var tradingIcon = GetIconForTitle("Manual Lock") ?? GetIconForTitle("Locked");
            
            // Add rows for Settings Status and Trading Status with icons
            statusTableView.Rows.Add(settingsIcon, "Settings Status:", "Unlocked");
            statusTableView.Rows.Add(tradingIcon, "Trading Status:", "Unlocked");
            
            // Apply green color to initial "Unlocked" values using helper method
            UpdateStatusTableCellColor(STATUS_TABLE_SETTINGS_ROW, "Unlocked");
            UpdateStatusTableCellColor(STATUS_TABLE_TRADING_ROW, "Unlocked");
            
            // Keep reference to old badges for backward compatibility
            // Note: These badges are hidden but maintained to ensure existing code
            // that may reference these controls continues to work without errors
            settingsStatusBadge = CreateStatusBadge("Settings Unlocked", AccentGreen);
            settingsStatusBadge.Visible = false; // Hide but maintain for compatibility
            
            tradingStatusBadge = CreateStatusBadge("Trading Unlocked", AccentGreen);
            tradingStatusBadge.Visible = false; // Hide but maintain for compatibility
            
            badgesPanel.Controls.Add(statusTableView);

            // Create vertical container for theme and shutdown buttons
            var buttonsPanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                BackColor = Color.Transparent,
                Margin = new Padding(5, 0, 0, 0),
                Padding = new Padding(0)
            };

            // Theme Changer button (replaces the X button)
            var themeButton = new Button
            {
                Text = "", // we use the image instead of emoji text
                Width = 44,
                Height = 36,
                BackColor = Color.Transparent,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 0, 0),
                Padding = new Padding(0),
                UseCompatibleTextRendering = true
            };
            themeButton.FlatAppearance.BorderSize = 0;
            themeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(41, 128, 185);

            // Set image if available (prefer loaded IconMap entry "ThemeSwitcher", fallback to resource)
            // In CreateTopPanel(), replace the theme image assignment block with this scaled image logic
            Image themeImg = null;
            if (IconMap.TryGetValue("ThemeSwitcher", out var img) && img != null)
                themeImg = img;
            else
            {
                try { themeImg = Properties.Resources.themeswitcher2; } catch { themeImg = null; }
            }

            if (themeImg != null)
            {
                // Dispose previous scaled image to avoid leaks
                themeButtonScaledImage?.Dispose();

                // Scale to fit inside button with small padding
                int pad = 6; // padding inside button
                themeButtonScaledImage = ScaleImageToFit(themeImg, Math.Max(8, themeButton.Width - pad), Math.Max(8, themeButton.Height - pad));

                themeButton.Image = themeButtonScaledImage;
                themeButton.ImageAlign = ContentAlignment.MiddleCenter;
                themeButton.Text = "";
                themeButton.TextImageRelation = TextImageRelation.ImageBeforeText;
            }
            else
            {
                // fallback to emoji if resource is missing
                themeButton.Text = "🎨";
                themeButton.Font = new Font("Segoe UI Emoji", 16, FontStyle.Bold);
            }

            themeButton.Click += (s, e) =>
            {
                // Cycle through themes: Blue -> Black -> White -> Blue
                switch (currentTheme)
                {
                    case Theme.Blue:
                        ApplyTheme(Theme.Black);
                        break;
                    case Theme.Black:
                        ApplyTheme(Theme.White);
                        break;
                    case Theme.White:
                        ApplyTheme(Theme.YellowBlueBlack);
                        break;
                    case Theme.YellowBlueBlack:
                        ApplyTheme(Theme.Blue);
                        break;
                    default:
                        ApplyTheme(Theme.Blue);
                        break;
                }
            };
            buttonsPanel.Controls.Add(themeButton);

            // Add the buttons panel to the badges panel
            badgesPanel.Controls.Add(buttonsPanel);

            // Position the badges panel initially and on resize
            PositionBadgesPanel(topPanel, badgesPanel);
            topPanel.Controls.Add(badgesPanel);

            // Debug label positioned above and to the left of the badges
            // Commented out per user request
            /*
            lblTradingStatusBadgeDebug = new Label
            {
                Text = "Debug: Waiting for updates...",
                AutoSize = false,
                Width = DEBUG_LABEL_WIDTH,
                Height = DEBUG_LABEL_HEIGHT,
                ForeColor = Color.Yellow,
                BackColor = Color.Transparent,
                Font = new Font("Consolas", 7, FontStyle.Regular),
                TextAlign = ContentAlignment.TopLeft,
                Visible = _badgeDebugMode,
                Anchor = AnchorStyles.Top | AnchorStyles.Right
            };
            topPanel.Controls.Add(lblTradingStatusBadgeDebug);
            
            // Position debug label above badges (called initially and on resize)
            PositionDebugLabel(topPanel, badgesPanel, lblTradingStatusBadgeDebug);

            topPanel.Resize += (s, e) => 
            {
                PositionBadgesPanel(topPanel, badgesPanel);
                PositionDebugLabel(topPanel, badgesPanel, lblTradingStatusBadgeDebug);
            };
            */
            
            topPanel.Resize += (s, e) => 
            {
                PositionBadgesPanel(topPanel, badgesPanel);
                // PositionDebugLabel(topPanel, badgesPanel, lblTradingStatusBadgeDebug);
            };

            return topPanel;
        }

        private static void PositionBadgesPanel(Panel topPanel, FlowLayoutPanel badgesPanel)
        {
            // Position badges panel in top-right with vertical centering
            int verticalCenter = (topPanel.Height - badgesPanel.PreferredSize.Height) / 2;
            badgesPanel.Location = new Point(topPanel.Width - badgesPanel.PreferredSize.Width - 20, Math.Max(10, verticalCenter));
        }

        private static void PositionDebugLabel(Panel topPanel, FlowLayoutPanel badgesPanel, Label debugLabel)
        {
            if (debugLabel != null && debugLabel.Visible)
            {
                // Position debug label above the badges panel, aligned to the right
                // Place it just above the badges with a small gap
                int debugY = 2; // Near top of panel
                int debugX = topPanel.Width - debugLabel.Width - 20; // Aligned with badges panel
                debugLabel.Location = new Point(debugX, debugY);
            }
        }

        private Label CreateStatusBadge(string text, Color badgeColor)
        {
            var badge = new Label
            {
                Text = "  " + text + "  ",
                AutoSize = true,
                ForeColor = TextWhite,
                BackColor = badgeColor,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(8, 4, 8, 4),
                Margin = new Padding(5, 0, 5, 0),
                TextAlign = ContentAlignment.MiddleCenter
            };

            // Make rounded corners effect by using custom paint
            badge.Paint += (s, e) =>
            {
                var lbl = (Label)s;
                using var path = GetRoundedRectPath(new Rectangle(0, 0, lbl.Width, lbl.Height), 4);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                // Use the label's current BackColor instead of the captured badgeColor variable
                using var brush = new SolidBrush(lbl.BackColor);
                e.Graphics.FillPath(brush, path);
                using var textBrush = new SolidBrush(TextWhite);
                // Use the label's current Text instead of the captured text variable
                var textSize = e.Graphics.MeasureString(lbl.Text, lbl.Font);
                var textX = (lbl.Width - textSize.Width) / 2;
                var textY = (lbl.Height - textSize.Height) / 2;
                e.Graphics.DrawString(lbl.Text, lbl.Font, textBrush, textX, textY);
            };

            return badge;
        }
        
        /// <summary>
        /// Helper method to update status table cell color based on lock status.
        /// Applies green text for "Unlocked" and red text for "Locked".
        /// </summary>
        /// <param name="rowIndex">The row index in the status table</param>
        /// <param name="statusText">The status text (e.g., "Locked", "Unlocked", "Locked (2h 30m)")</param>
        private void UpdateStatusTableCellColor(int rowIndex, string statusText)
        {
            if (statusTableView != null && statusTableView.Rows.Count > rowIndex && rowIndex >= 0)
            {
                bool isLocked = statusText.StartsWith("Locked", StringComparison.OrdinalIgnoreCase);
                // Value is now in column 2 (0=Icon, 1=Label, 2=Value)
                var cell = statusTableView.Rows[rowIndex].Cells[2];
                cell.Style.ForeColor = isLocked ? Color.Red : AccentGreen;
                cell.Style.SelectionForeColor = isLocked ? Color.Red : AccentGreen;
                cell.Style.BackColor = CardBackground;
                cell.Style.SelectionBackColor = CardBackground;
            }
        }

        private GraphicsPath GetRoundedRectPath(Rectangle rect, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, radius * 2, radius * 2, 180, 90);
            path.AddArc(rect.Right - radius * 2, rect.Y, radius * 2, radius * 2, 270, 90);
            path.AddArc(rect.Right - radius * 2, rect.Bottom - radius * 2, radius * 2, radius * 2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - radius * 2, radius * 2, radius * 2, 90, 90);
            path.CloseFigure();
            return path;
        }

        private Panel CreateLeftSidebar()
        {
            // Load saved navigation collapse state
            isNavigationCollapsed = LoadNavigationCollapsePreference();
            
            var initialWidth = isNavigationCollapsed ? LeftPanelCollapsedWidth : LeftPanelExpandedWidth;
            
            var sidebarPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = initialWidth,
                MinimumSize = new Size(LeftPanelCollapsedWidth, 0),
                AutoSize = false,
                AutoScroll = true,
                BackColor = DarkerBackground,
                Padding = new Padding(0, 0, 0, 10)
            };

            // Create toggle button header
            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                BackColor = DarkerBackground,
                Padding = new Padding(5)
            };

            navToggleButton = new Button
            {
                Text = "", // use image instead of emoji
                Width = 30,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                BackColor = CardBackground,
                ForeColor = TextWhite,
                Cursor = Cursors.Hand,
                Location = new Point(5, 5)
            };
            navToggleButton.FlatAppearance.BorderSize = 0;
            
            // Set the left-and-right-arrows image
            try
            {
                var navArrowImg = Properties.Resources.left_and_right_arrows;
                if (navArrowImg != null)
                {
                    // Dispose previous scaled image to avoid leaks
                    navToggleButtonScaledImage?.Dispose();
                    
                    // Scale to fit inside button with small padding
                    int pad = 4; // padding inside button
                    navToggleButtonScaledImage = ScaleImageToFit(navArrowImg, Math.Max(8, navToggleButton.Width - pad), Math.Max(8, navToggleButton.Height - pad));
                    
                    navToggleButton.Image = navToggleButtonScaledImage;
                    navToggleButton.ImageAlign = ContentAlignment.MiddleCenter;
                }
                else
                {
                    // fallback to emoji if resource is missing
                    navToggleButton.Text = isNavigationCollapsed ? "➡️" : "⬅️";
                    navToggleButton.Font = new Font("Segoe UI Emoji", 10f, FontStyle.Regular);
                }
            }
            catch
            {
                // fallback to emoji if resource loading fails
                navToggleButton.Text = isNavigationCollapsed ? "➡️" : "⬅️";
                navToggleButton.Font = new Font("Segoe UI Emoji", 10f, FontStyle.Regular);
            }
            
            navToggleButton.Click += ToggleNavigation;
            headerPanel.Controls.Add(navToggleButton);

            var navContainer = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoScroll = true,
                BackColor = DarkerBackground,
                Padding = new Padding(0, 5, 0, 5)
            };

            foreach (var item in NavItems)
            {
                var navButton = CreateNavButton(item);
                navContainer.Controls.Add(navButton);
                navButtons.Add(navButton);
            }

            sidebarPanel.Controls.Add(navContainer);
            sidebarPanel.Controls.Add(headerPanel);
            return sidebarPanel;
        }

        private Button CreateNavButton(string text)
        {
            var button = new Button
            {
                Text = "  " + text,
                Width = LeftPanelWidth - 4,
                Height = 36,
                FlatStyle = FlatStyle.Flat,
                TextAlign = ContentAlignment.MiddleLeft,
                ForeColor = TextWhite,
                BackColor = text == selectedNavItem ? SelectedColor : DarkerBackground,
                Font = new Font("Segoe UI Emoji", 9.5f, FontStyle.Regular),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 1, 0, 1),
                Padding = new Padding(10, 0, 0, 0),
                Tag = text,
                UseCompatibleTextRendering = false // Use GDI+ for better emoji rendering
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = HoverColor;
            button.FlatAppearance.MouseDownBackColor = SelectedColor;

            // Custom paint for colored emoji rendering using GDI+
            button.Paint += (s, e) =>
            {
                var btn = (Button)s;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                e.Graphics.Clear(btn.BackColor);

                var tag = btn.Tag as string ?? btn.Text ?? string.Empty;
                var icon = GetIconForTitle(tag);

                float xOffset = btn.Padding.Left;
                if (icon != null)
                {
                    var imgHeight = Math.Min(btn.Height - 8, icon.Height);
                    var imgWidth = (int)(icon.Width * (imgHeight / (float)icon.Height));
                    var imgRect = new Rectangle(btn.Padding.Left, (btn.Height - imgHeight) / 2, imgWidth, imgHeight);
                    e.Graphics.DrawImage(icon, imgRect);
                    xOffset += imgWidth + 8;
                }

                // Display text without leading emoji token (only if not collapsed)
                var raw = (btn.Text ?? string.Empty).Trim();
                string displayText = raw;
                
                // Hide text when collapsed
                if (!isNavigationCollapsed)
                {
                    var idx = raw.IndexOf(' ');
                    if (idx >= 0 && IconMap.Any(k => k.Key.Equals(raw.Substring(0, idx), StringComparison.OrdinalIgnoreCase) || k.Key.Equals(raw.Substring(idx + 1), StringComparison.OrdinalIgnoreCase)))
                    {
                        displayText = raw.Substring(idx + 1);
                    }
                    else if (icon != null)
                    {
                        // remove leading token if present
                        if (idx >= 0)
                            displayText = raw.Substring(idx + 1);
                    }

                    using (var brush = new SolidBrush(btn.ForeColor))
                    {
                        var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near };
                        e.Graphics.DrawString(displayText, btn.Font, brush, new RectangleF(xOffset, 0, btn.Width - xOffset, btn.Height), sf);
                    }
                }
            };

            button.Click += (s, e) =>
            {
                var btn = (Button)s;
                // Check if button is disabled (settings locked)
                if (!btn.Enabled)
                {
                    MessageBox.Show(
                        "This tab is currently disabled because settings are locked.\n\nPlease unlock settings first from the \"Lock Settings\" tab.",
                        "Tab Locked",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    return;
                }
                
                selectedNavItem = text;
                UpdateNavButtonStates();
                ShowPage(text);
            };

            return button;
        }

        /// <summary>
        /// Toggles the navigation panel between collapsed and expanded states with animation.
        /// </summary>
        private void ToggleNavigation(object sender, EventArgs e)
        {
            isNavigationCollapsed = !isNavigationCollapsed;
            
            // Note: No need to update toggle button since the left-and-right-arrows image is bidirectional
            
            // Calculate target width
            int targetWidth = isNavigationCollapsed ? LeftPanelCollapsedWidth : LeftPanelExpandedWidth;
            
            // Animate the width change
            var timer = new System.Windows.Forms.Timer { Interval = 10 };
            int currentWidth = leftPanel.Width;
            int step = (targetWidth - currentWidth) / 10; // 10 steps for animation
            if (step == 0) step = targetWidth > currentWidth ? 1 : -1;
            
            timer.Tick += (s, args) =>
            {
                if (Math.Abs(leftPanel.Width - targetWidth) <= Math.Abs(step))
                {
                    leftPanel.Width = targetWidth;
                    timer.Stop();
                    timer.Dispose();
                    
                    // Update button widths after animation completes
                    foreach (var btn in navButtons)
                    {
                        btn.Width = targetWidth - 4;
                    }
                }
                else
                {
                    leftPanel.Width += step;
                }
                
                // Force all buttons to repaint during animation
                foreach (var btn in navButtons)
                {
                    btn.Invalidate();
                }
            };
            
            timer.Start();
            
            // Save preference
            SaveNavigationCollapsePreference();
        }

        private void UpdateNavButtonStates()
        {
            foreach (var btn in navButtons)
            {
                var itemName = btn.Tag as string;
                btn.BackColor = itemName == selectedNavItem ? SelectedColor : DarkerBackground;
                btn.Invalidate(); // Force repaint for custom rendering
            }
        }

        private Label CreateHeaderLabel(string text, Image icon)
        {
            var label = new Label
            {
                Text = text,
                AutoSize = false,
                Height = 32,
                Dock = DockStyle.Top,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = TextWhite,
                BackColor = Color.Transparent,
                ImageAlign = ContentAlignment.MiddleLeft,
                TextAlign = ContentAlignment.MiddleLeft
            };

            if (icon != null)
            {
                // Scale the icon to fit the label's height while maintaining aspect ratio
                int targetHeight = Math.Max(16, (int)(label.Font.Height * 1.4));
                var scaledIcon = ScaleImageToFit(icon, targetHeight, targetHeight);
                label.Image = scaledIcon;

                // Explicitly add space between the icon and the text
                int iconWidth = scaledIcon.Width;
                int gap = 12; // Space between the icon and the text
                label.Padding = new Padding(iconWidth + gap, 0, 0, 0);
            }
            else
            {
                // Default padding when no icon is present
                label.Padding = new Padding(8, 0, 0, 0);
            }

            return label;
        }

        /// <summary>
        /// Creates a label with colored emoji support using Segoe UI Emoji font
        /// </summary>
        private Label CreateEmojiLabel(string text, int fontSize, FontStyle fontStyle = FontStyle.Regular, Color? backgroundColor = null)
        {
            var label = new Label
            {
                Text = text,
                Font = new Font("Segoe UI Emoji", fontSize, fontStyle),
                ForeColor = TextWhite,
                BackColor = backgroundColor ?? DarkBackground,
                AutoSize = false,
                UseCompatibleTextRendering = false
            };

            var icon = GetIconForTitle(text);
            if (icon != null)
            {
                // remove leading emoji token if present
                var trimmed = text.Trim();
                var idx = trimmed.IndexOf(' ');
                if (idx >= 0 && idx + 1 < trimmed.Length)
                    label.Text = trimmed.Substring(idx + 1).Trim();
                else if (trimmed.Length > 1)
                    label.Text = trimmed;

                var key = (idx >= 0 && idx + 1 < trimmed.Length) ? trimmed.Substring(idx + 1).Trim() : trimmed;

                // Choose a larger multiplier for Manual Lock / Trading Lock headers
                double multiplier = 1.4;
                if ( string.Equals(key, "Trading Lock", StringComparison.OrdinalIgnoreCase) )
                {
                    multiplier = 5; // make the icon larger
                }

                // Determine sensible target size from font height (keeps consistent across headers)
                int targetHeight = Math.Max(16, (int)(label.Font.Height * 1.4));
                int targetWidth = targetHeight; // square icon area

                // Dispose previously created image if it's a Bitmap we created earlier
                if (label.Image != null && !(label.Image is Metafile) && label.Image != icon && label.Image is Bitmap bmpPrev)
                {
                    try { bmpPrev.Dispose(); } catch { /* ignore */ }
                }

                // Create and assign scaled bitmap so Label draws correctly
                var scaled = ScaleImageToFit(icon, targetWidth, targetHeight);
                label.Image = scaled;
                label.ImageAlign = ContentAlignment.MiddleLeft;
                // Increased left padding so text doesn't sit on top of the icon
                label.Padding = new Padding(targetWidth + 18, 0, 0, 0);
            }

            return label;
        }

        private Control CreateAccountsSummaryPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            var header = new CustomHeaderControl("Accounts Summary", GetIconForTitle("Accounts Summary"));
            header.Dock = DockStyle.Top;
            header.Margin = new Padding(10, 0, 0, 0); // External spacing

            // Create a container panel to hold the centered Lock Trading button
            // Width of 400px provides adequate space for centering while staying within
            // typical window widths. The button (250px) centers within this space,
            // creating approximately 75px margins on each side for visual balance.
            var buttonContainer = new Panel
            {
                Dock = DockStyle.Right,
                Width = 1000, // Fixed width appropriate for button (250px) + margins
                Height = 40,
                BackColor = Color.Transparent
            };

            // Lock All Accounts button with icons on both sides - centered in container
            // Dimensions match Emergency Flatten button (250x26)
            var lockAllButton = new Panel
            {
                Width = 250,  // Match Emergency Flatten button width
                Height = 26,  // Match Emergency Flatten button height
                BackColor = Color.FromArgb(192, 0, 0), // Dark red
                Cursor = Cursors.Hand,
                // Position will be set in buttonContainer.Resize event for centering
                Anchor = AnchorStyles.None  // Allow free positioning for centering
            };

            // Load the lock icon once for reuse
            try
            {
                var lockIconResource = Properties.Resources.lockallaccounts;
                if (lockIconResource != null)
                {
                    // Left icon - adjusted to match button height
                    var leftPicture = new PictureBox
                    {
                        Image = new Bitmap(lockIconResource, new Size(30, 30)),
                        SizeMode = PictureBoxSizeMode.CenterImage,
                        Width = 40,
                        Height = 26,  // Match button height
                        Dock = DockStyle.Left,
                        BackColor = Color.Transparent
                    };
                    lockAllButton.Controls.Add(leftPicture);

                    // Right icon - adjusted to match button height
                    var rightPicture = new PictureBox
                    {
                        Image = new Bitmap(lockIconResource, new Size(30, 30)),
                        SizeMode = PictureBoxSizeMode.CenterImage,
                        Width = 40,
                        Height = 26,  // Match button height
                        Dock = DockStyle.Right,
                        BackColor = Color.Transparent
                    };
                    lockAllButton.Controls.Add(rightPicture);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Could not load lockallaccounts icons: {ex.Message}");
            }

            // Center label with text
            var lockAllLabel = new Label
            {
                Text = "Lock Trading",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),  // Adjusted for smaller button height
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent
            };
            lockAllButton.Controls.Add(lockAllLabel);

            // Make the entire panel clickable
            lockAllButton.Click += BtnLockAllAccounts_Click;
            lockAllLabel.Click += BtnLockAllAccounts_Click;
            
            // Add click handlers to icons too (pass through to parent)
            foreach (Control ctrl in lockAllButton.Controls)
            {
                if (ctrl is PictureBox)
                {
                    ctrl.Click += BtnLockAllAccounts_Click;
                }
            }

            // Center the button in the container
            CenterControlInContainer(buttonContainer, lockAllButton);
            
            // Re-center on resize
            buttonContainer.Resize += (s, e) => CenterControlInContainer(buttonContainer, lockAllButton);

            // Add button to container
            buttonContainer.Controls.Add(lockAllButton);
            
            // Add container to header (must be added before text/icon due to dock order)
            header.Controls.Add(buttonContainer);

            statsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = CardBackground,
                GridColor = DarkerBackground,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false
            };

            // Style the grid for dark theme
            statsGrid.DefaultCellStyle.BackColor = CardBackground;
            statsGrid.DefaultCellStyle.ForeColor = TextWhite;
            statsGrid.DefaultCellStyle.SelectionBackColor = SelectedColor;
            statsGrid.DefaultCellStyle.SelectionForeColor = TextWhite;
            statsGrid.ColumnHeadersDefaultCellStyle.BackColor = DarkerBackground;
            statsGrid.ColumnHeadersDefaultCellStyle.ForeColor = TextWhite;
            statsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            statsGrid.Columns.Add("Provider", "Provider");
            statsGrid.Columns.Add("Connection", "Connection");
            statsGrid.Columns.Add("Account", "Account");
            statsGrid.Columns.Add("Type", "Type");
            statsGrid.Columns.Add("Equity", "Equity");
            statsGrid.Columns.Add("OpenPnL", "Open P&L");
            statsGrid.Columns.Add("ClosedPnL", "Closed P&L");
            statsGrid.Columns.Add("DailyPnL", "Daily P&L");
            statsGrid.Columns.Add("GrossPnL", "Gross P&L");
            statsGrid.Columns.Add("TrailingDrawdown", "Trailing Drawdown");
            statsGrid.Columns.Add("Positions", "Positions");
            statsGrid.Columns.Add("Status", "Status");
            statsGrid.Columns.Add("LockStatus", "Lock Status");
            statsGrid.Columns.Add("LossLimit", "Loss Limit");
            statsGrid.Columns.Add("ProfitTarget", "Profit Target");
            statsGrid.Columns.Add("Drawdown", "Drawdown");

            // Allow row selection to populate Stats tab and sync with account dropdown
            statsGrid.SelectionChanged += (s, e) =>
            {
                if (statsGrid.SelectedRows.Count > 0)
                {
                    var selectedRowIndex = statsGrid.SelectedRows[0].Index;
                    if (selectedRowIndex >= 0 && selectedRowIndex < statsGrid.Rows.Count)
                    {
                        var core = Core.Instance;
                        if (core?.Accounts is System.Collections.ICollection accts && selectedRowIndex < accts.Count)
                        {
                            selectedAccount = core.Accounts.ElementAtOrDefault(selectedRowIndex);
                        }
                    }
                }
            };

            // Add CellClick handler to sync grid selection with account dropdown
            statsGrid.CellClick += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.RowIndex < statsGrid.Rows.Count)
                {
                    var core = Core.Instance;
                    if (core?.Accounts != null && e.RowIndex < core.Accounts.Count())
                    {
                        var clickedAccount = core.Accounts.ElementAtOrDefault(e.RowIndex);
                        if (clickedAccount != null)
                        {
                            // Update the account selector dropdown to match the clicked row
                            if (accountSelector != null)
                            {
                                // Find the account in the dropdown
                                for (int i = 0; i < accountSelector.Items.Count; i++)
                                {
                                    if (accountSelector.Items[i] is Account dropdownAccount && 
                                        dropdownAccount.Id == clickedAccount.Id)
                                    {
                                        // Temporarily disable the event handler to avoid recursive calls
                                        accountSelector.SelectedIndexChanged -= AccountSelectorOnSelectedIndexChanged;
                                        accountSelector.SelectedIndex = i;
                                        accountSelector.SelectedIndexChanged += AccountSelectorOnSelectedIndexChanged;
                                        
                                        // Manually trigger the account selection logic
                                        selectedAccount = clickedAccount;
                                        selectedAccountIndex = i;
                                        displayedAccountNumber = clickedAccount.Id ?? clickedAccount.Name ?? "Unknown";
                                        
                                        // Load settings for this account
                                        LoadAccountSettings();
                                        
                                        // Update badges
                                        UpdateSettingsStatusBadge();
                                        UpdateTradingStatusBadge();
                                        
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            };

            // Add CellPainting event handler for progress bars
            statsGrid.CellPainting += StatsGrid_CellPainting;

            RefreshAccountsSummary();
            statsRefreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            statsRefreshTimer.Tick += (s, e) => RefreshAccountsSummary();
            statsRefreshTimer.Start();

            // Add controls in correct order: Fill control first, then Top controls
            // In WinForms, docking is processed in reverse Z-order
            mainPanel.Controls.Add(statsGrid);
            mainPanel.Controls.Add(header);
            return mainPanel;
        }

        private void RefreshAccountsSummary()
        {
            if (InvokeRequired) { BeginInvoke(new Action(RefreshAccountsSummary)); return; }
            if (statsGrid == null) return;

            try
            {
                statsGrid.SuspendLayout();
                statsGrid.Rows.Clear();

                var core = Core.Instance;
                if (core == null || core.Accounts == null || !core.Accounts.Any())
                {
                    // Demo data - last column is Drawdown (Equity - Trailing Drawdown)
                    statsGrid.Rows.Add("DemoProvider", "DemoConn", "ACC123", "Live", "1000.00", "12.34", "50.00", "5.67", "18.01", "0.00", "1", "Connected", "Unlocked", "500.00", "1000.00", "1000.00");
                    statsGrid.Rows.Add("DemoProvider", "DemoConn2", "ACC456", "Demo", "2500.50", "(8.20)", "25.00", "(2.00)", "(10.20)", "0.00", "2", "Connected", "Unlocked", "500.00", "1000.00", "2500.50");
                    return;
                }

                // Track account index for generating unique identifiers
                int accountIndex = 0;
                foreach (var account in core.Accounts)
                {
                    if (account == null) continue;

                    var provider = account.Connection?.VendorName ?? account.Connection?.Name ?? "Unknown";
                    var connectionName = account.Connection?.Name ?? "Unknown";
                    var accountId = account.Id ?? account.Name ?? "Unknown";
                    var equity = account.Balance;

                    // Get account type using centralized method
                    var accountType = DetermineAccountType(account);

                    // Count positions
                    int positionsCount = 0;
                    if (core.Positions != null)
                    {
                        foreach (var pos in core.Positions)
                        {
                            if (pos == null) continue;
                            if (pos.Account == account && pos.Quantity != 0)
                            {
                                positionsCount++;
                            }
                        }
                    }

                    // Extract all P&L values from AdditionalInfo
                    double openPnL = 0, dailyPnL = 0, grossPnL = 0, closedPnL = 0, trailingDrawdown = 0;
                    if (account.AdditionalInfo != null)
                    {
                        foreach (var info in account.AdditionalInfo)
                        {
                            if (info?.Id == null) continue;
                            var id = info.Id;
                            
#if DEBUG
                            // Debug: Log all AdditionalInfo IDs to help identify field names
                            System.Diagnostics.Debug.WriteLine($"AdditionalInfo ID: '{id}', Value: {info.Value}, Type: {info.Value?.GetType().Name}");
#endif
                            
                            // Open P&L - from OpenPnl field
                            if (string.Equals(id, "OpenPnl", StringComparison.OrdinalIgnoreCase))
                            {
                                if (info.Value is double ov) openPnL = ov;
                            }

                            // Daily P&L - from TotalPnL field
                            if (string.Equals(id, "TotalPnL", StringComparison.OrdinalIgnoreCase))
                            {
                                if (info.Value is double dv) dailyPnL = dv;
                            }

                            // Gross P&L - from NetPnL field
                            if (string.Equals(id, "NetPnL", StringComparison.OrdinalIgnoreCase))
                            {
                                if (info.Value is double gv) grossPnL = gv;
                            }

                            // Closed P&L - from ClosedPnl field
                            if (string.Equals(id, "ClosedPnl", StringComparison.OrdinalIgnoreCase))
                            {
                                if (info.Value is double cv) closedPnL = cv;
                            }

                            // Trailing Drawdown - from AutoLiquidateThresholdCurrentValue field
                            if (string.Equals(id, "AutoLiquidateThresholdCurrentValue", StringComparison.OrdinalIgnoreCase))
                            {
                                if (info.Value is double td)
                                {
                                    trailingDrawdown = td;
#if DEBUG
                                    System.Diagnostics.Debug.WriteLine($"Trailing Drawdown found: {trailingDrawdown} from field '{id}'");
#endif
                                }
                            }

                            // Account Type from AdditionalInfo
                            if (string.Equals(id, "Account Type", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(id, "AccountType", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(id, "Type", StringComparison.OrdinalIgnoreCase))
                            {
                                if (info.Value is string at) accountType = at;
                            }
                        }
                    }

                    var status = account.Connection == null ? "Disconnected" : account.Connection.State.ToString();

                    // Generate unique account identifier using same logic as GetSelectedAccountNumber()
                    // This ensures we look up settings using the same key that was used to save them
                    var uniqueAccountId = GetUniqueAccountIdentifier(account, accountIndex);

                    // Get lock status and limits from settings service
                    var settingsService = RiskManagerSettingsService.Instance;
                    var lockStatus = "Unlocked";
                    decimal? lossLimit = null;
                    decimal? profitTarget = null;
                    if (settingsService.IsInitialized)
                    {
                        // Use the new method that includes remaining time
                        lockStatus = settingsService.GetLockStatusString(uniqueAccountId);

                        // Get loss limit and profit target from settings using unique identifier
                        var settings = settingsService.GetSettings(uniqueAccountId);
                        if (settings != null)
                        {
                            lossLimit = settings.DailyLossLimit;
                            profitTarget = settings.DailyProfitTarget;
                        }
                    }

                    accountIndex++;

                    // Calculate Drawdown as Equity - Trailing Drawdown
                    var drawdown = equity - trailingDrawdown;

                    statsGrid.Rows.Add(
                        provider, 
                        connectionName, 
                        accountId, 
                        accountType,
                        FormatNumeric(equity), 
                        FormatNumeric(openPnL), 
                        FormatNumeric(closedPnL),
                        FormatNumeric(dailyPnL), 
                        FormatNumeric(grossPnL), 
                        FormatNumeric(trailingDrawdown),
                        positionsCount.ToString(), 
                        status,
                        lockStatus,
                        FormatLossLimit(lossLimit),
                        FormatNumeric(profitTarget),
                        FormatNumeric(drawdown)
                    );
                }
            }
            catch (Exception ex)
            {
                // Log error for debugging but don't crash the UI
                System.Diagnostics.Debug.WriteLine($"RefreshAccountsSummary error: {ex.Message}");
                
                // Show error in grid if it's empty
                if (statsGrid.Rows.Count == 0)
                {
                    try
                    {
                        // Create error row with proper column count
                        var errorValues = new object[statsGrid.Columns.Count];
                        errorValues[0] = "Error";
                        errorValues[1] = "API Connection Failed";
                        errorValues[2] = ex.Message;
                        for (int i = 3; i < errorValues.Length; i++)
                        {
                            errorValues[i] = "-";
                        }
                        statsGrid.Rows.Add(errorValues);
                    }
                    catch
                    {
                        // If even adding error row fails, just ignore
                    }
                }
            }
            finally
            {
                statsGrid.ResumeLayout();
                // Re-apply numeric coloring for the accounts summary *after* rows were rebuilt
                if (currentTheme == Theme.YellowBlueBlack)
                {
                    ColorizeNumericCells(statsGrid, "OpenPnL", "ClosedPnL", "DailyPnL", "GrossPnL");
                }
                // Apply Lock Status coloring (Green for Unlocked, Red for Locked)
                ColorizeLockStatusCells(statsGrid);
                // Refresh risk-overview/value labels around the grid
                ApplyValueLabelColoring(statsGrid.Parent ?? this);
            }        
        }

        private Control CreateAccountStatsPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title (use emoji label so header PNG is used)
            var header = new CustomHeaderControl("Stats", GetIconForTitle("Stats"));
            header.Dock = DockStyle.Top;
            header.Margin = new Padding(10, 0, 0, 0);

            // Stats display grid
            statsDetailGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = CardBackground,
                GridColor = DarkerBackground,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            // Style the grid for dark theme
            statsDetailGrid.DefaultCellStyle.BackColor = CardBackground;
            statsDetailGrid.DefaultCellStyle.ForeColor = TextWhite;
            statsDetailGrid.DefaultCellStyle.SelectionBackColor = SelectedColor;
            statsDetailGrid.DefaultCellStyle.SelectionForeColor = TextWhite;
            statsDetailGrid.ColumnHeadersDefaultCellStyle.BackColor = DarkerBackground;
            statsDetailGrid.ColumnHeadersDefaultCellStyle.ForeColor = TextWhite;
            statsDetailGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            statsDetailGrid.Columns.Add("Metric", "Metric");
            statsDetailGrid.Columns.Add("Value", "Value");

            RefreshAccountStats();
            statsDetailRefreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            statsDetailRefreshTimer.Tick += (s, e) => RefreshAccountStats();
            statsDetailRefreshTimer.Start();

            // Add controls in correct order: Fill control first, then Top controls
            // In WinForms, docking is processed in reverse Z-order
            mainPanel.Controls.Add(statsDetailGrid);
            mainPanel.Controls.Add(header);
            return mainPanel;
        }

        private void RefreshAccountStats()
        {
            if (InvokeRequired) { BeginInvoke(new Action(RefreshAccountStats)); return; }
            if (statsDetailGrid == null) return;

            try
            {
                statsDetailGrid.SuspendLayout();
                statsDetailGrid.Rows.Clear();

                // Fix: Use the selected account from dropdown to ensure consistency
                var accountToDisplay = accountSelector?.SelectedItem as Account;
                
                if (accountToDisplay == null)
                {
                    statsDetailGrid.Rows.Add("Status", "No account selected");
                    statsDetailGrid.ResumeLayout();
                    return;
                }

                var core = Core.Instance;

                // Get provider and connection info
                var provider = accountToDisplay.Connection?.VendorName ?? accountToDisplay.Connection?.Name ?? "Unknown";
                var connectionName = accountToDisplay.Connection?.Name ?? "Unknown";
                var accountId = accountToDisplay.Id ?? accountToDisplay.Name ?? "Unknown";
                var balance = accountToDisplay.Balance;

                // Count positions
                int positionsCount = 0;
                if (core?.Positions != null)
                {
                    foreach (var pos in core.Positions)
                    {
                        if (pos == null) continue;
                        if (pos.Account == accountToDisplay && pos.Quantity != 0)
                        {
                            positionsCount++;
                        }
                    }
                }

                // Get all P&L values from AdditionalInfo (same as Accounts Summary)
                double openPnL = 0, dailyPnL = 0, grossPnL = 0;
                if (accountToDisplay.AdditionalInfo != null)
                {
                    foreach (var info in accountToDisplay.AdditionalInfo)
                    {
                        if (info?.Id == null) continue;
                        var id = info.Id;
                        
                        // Open P&L - from OpenPnl field
                        if (string.Equals(id, "OpenPnl", StringComparison.OrdinalIgnoreCase))
                        {
                            if (info.Value is double ov) openPnL = ov;
                        }

                        // Daily P&L - from TotalPnL field
                        if (string.Equals(id, "TotalPnL", StringComparison.OrdinalIgnoreCase))
                        {
                            if (info.Value is double dv) dailyPnL = dv;
                        }

                        // Gross P&L - from NetPnL field
                        if (string.Equals(id, "NetPnL", StringComparison.OrdinalIgnoreCase))
                        {
                            if (info.Value is double gv) grossPnL = gv;
                        }
                    }
                }

                // Get connection status
                var connectionStatus = accountToDisplay.Connection == null ? "Disconnected" : accountToDisplay.Connection.State.ToString();

                // Get trading lock status from settings service
                var settingsService = RiskManagerSettingsService.Instance;
                var lockStatus = "Unlocked";
                if (settingsService.IsInitialized)
                {
                    // Generate the account identifier from accountToDisplay to ensure we check the correct account
                    // Find the account index
                    int accountIndex = 0;
                    if (core != null && core.Accounts != null)
                    {
                        foreach (var acc in core.Accounts)
                        {
                            if (acc == accountToDisplay)
                            {
                                break;
                            }
                            accountIndex++;
                        }
                    }
                    
                    var accountNumber = GetUniqueAccountIdentifier(accountToDisplay, accountIndex);
                    if (!string.IsNullOrEmpty(accountNumber))
                    {
                        // Use the new method that includes remaining time
                        lockStatus = settingsService.GetLockStatusString(accountNumber);
                    }
                }

                // Display all stats matching Accounts Summary data
                statsDetailGrid.Rows.Add("Provider", provider);
                statsDetailGrid.Rows.Add("Connection", connectionName);
                statsDetailGrid.Rows.Add("Account", accountId);
                statsDetailGrid.Rows.Add("Balance", FormatNumeric(balance));
                statsDetailGrid.Rows.Add("Open P&L", FormatNumeric(openPnL));
                statsDetailGrid.Rows.Add("Daily P&L", FormatNumeric(dailyPnL));
                statsDetailGrid.Rows.Add("Gross P&L", FormatNumeric(grossPnL));
                statsDetailGrid.Rows.Add("Positions", positionsCount.ToString());
                statsDetailGrid.Rows.Add("Connection Status", connectionStatus);
                statsDetailGrid.Rows.Add("Trading Lock Status", lockStatus);

                // Apply special coloring for metrics when theme requires it
                if (currentTheme == Theme.YellowBlueBlack)
                {
                    for (int i = 0; i < statsDetailGrid.Rows.Count; i++)
                    {
                        var metric = statsDetailGrid.Rows[i].Cells[0].Value?.ToString() ?? "";
                        var valueCell = statsDetailGrid.Rows[i].Cells[1];
                        var raw = (valueCell.Value ?? string.Empty).ToString();
                        if (metric == "Open P&L" || metric == "Daily P&L" || metric == "Gross P&L")
                        {
                            // Check if value is exactly 0.00 - if so, use white (TextWhite)
                            if (IsZeroValue(raw))
                                valueCell.Style.ForeColor = TextWhite;
                            else if (IsNegativeNumericString(raw))
                                valueCell.Style.ForeColor = NegativeValueColor;
                            else
                                valueCell.Style.ForeColor = PositiveValueColor;
                        }
                        else
                        {
                            valueCell.Style.ForeColor = TextWhite;
                        }
                    }
                }
                
                // Apply Lock Status coloring for all themes (Green for Unlocked, Red for Locked)
                for (int i = 0; i < statsDetailGrid.Rows.Count; i++)
                {
                    var metric = statsDetailGrid.Rows[i].Cells[0].Value?.ToString() ?? "";
                    if (string.Equals(metric, "Trading Lock Status", StringComparison.OrdinalIgnoreCase))
                    {
                        var valueCell = statsDetailGrid.Rows[i].Cells[1];
                        var lockStatusValue = (valueCell.Value ?? string.Empty).ToString();
                        valueCell.Style.ForeColor = GetLockStatusColor(lockStatusValue);
                        break; // Found and colored, exit loop
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error for debugging but don't crash the UI
                System.Diagnostics.Debug.WriteLine($"RefreshAccountStats error: {ex.Message}");
                
                // Show error message in grid
                if (statsDetailGrid.Rows.Count == 0)
                {
                    try
                    {
                        statsDetailGrid.Rows.Add("Error", "API Connection Failed");
                        statsDetailGrid.Rows.Add("Details", ex.Message);
                    }
                    catch
                    {
                        // If even adding error row fails, just ignore
                    }
                }
            }
            finally
            {
                statsDetailGrid.ResumeLayout();
                // re-apply theme-specific coloring after the stats detail rows were rebuilt
                // Per-metric coloring already applied above (Open P&L, Daily P&L, Gross P&L).
                // Do NOT color the entire statsDetailGrid here.
                ApplyValueLabelColoring(statsDetailGrid.Parent ?? this);
            }
        }

        /// <summary>
        /// Custom cell painting for progress bars in the stats grid
        /// </summary>
        private void StatsGrid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            // Only paint progress bars if the feature is enabled
            if (!showProgressBars)
                return;

            // Skip header row
            if (e.RowIndex < 0)
                return;

            var grid = sender as DataGridView;
            if (grid == null)
                return;

            // Determine which columns should have progress bars
            string columnName = grid.Columns[e.ColumnIndex].Name;
            
            // Debug: Log when method is called for P&L columns
            if (columnName == "GrossPnL" || columnName == "OpenPnL")
            {
                System.Diagnostics.Debug.WriteLine($"CellPainting called: Row={e.RowIndex}, Column={columnName}, ShowProgressBars={showProgressBars}");
            }
            
            // Progress bars for: GrossPnL and OpenPnL
            bool isGrossPnL = columnName == "GrossPnL";
            bool isOpenPnL = columnName == "OpenPnL";
            
            if (!isGrossPnL && !isOpenPnL)
                return;

            // Get the cell value
            var cellValue = e.Value;
            if (cellValue == null)
                return;

            // Parse the P&L value (handle formatted strings like "$1,234.56")
            string valueStr = cellValue.ToString().Replace("$", "").Replace(",", "").Trim();
            if (!double.TryParse(valueStr, out double pnlValue))
                return;

            // Get account-specific limits from the row data
            double dailyLossLimit = 0;
            double dailyProfitTarget = 0;
            double positionLossLimit = 0;
            double positionProfitTarget = 0;

            try
            {
                // Get the account from Core.Accounts using the row index
                // This matches the same order used when populating the grid
                var core = Core.Instance;
                if (core?.Accounts != null && e.RowIndex < core.Accounts.Count())
                {
                    var account = core.Accounts.ElementAtOrDefault(e.RowIndex);
                    if (account != null)
                    {
                        // Generate unique account identifier using same logic as RefreshAccountsSummary
                        var uniqueAccountId = GetUniqueAccountIdentifier(account, e.RowIndex);
                        
                        // Load settings for this account
                        var settingsService = RiskManagerSettingsService.Instance;
                        if (settingsService.IsInitialized)
                        {
                            var settings = settingsService.GetSettings(uniqueAccountId);
                            if (settings != null)
                            {
                                dailyLossLimit = (double)(settings.DailyLossLimit ?? 0);
                                dailyProfitTarget = (double)(settings.DailyProfitTarget ?? 0);
                                positionLossLimit = (double)(settings.PositionLossLimit ?? 0);
                                positionProfitTarget = (double)(settings.PositionProfitTarget ?? 0);
                                
                                System.Diagnostics.Debug.WriteLine($"Loaded settings for account {uniqueAccountId}: DailyLossLimit={dailyLossLimit}, DailyProfitTarget={dailyProfitTarget}, PositionLossLimit={positionLossLimit}, PositionProfitTarget={positionProfitTarget}");
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine($"No settings found for account {uniqueAccountId}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log the error and continue with default values
                System.Diagnostics.Debug.WriteLine($"Error loading settings for progress bar: {ex.Message}");
            }

            // Calculate progress percentage and color
            double percentage = 0;
            Color barColor = Color.FromArgb(100, 180, 100); // Modern green

            if (isGrossPnL)
            {
                // Gross P&L: use Daily Loss Limit and Daily Profit Target
                // Note: Loss limits are stored as negative values in the settings
                
                // Debug: Log the values being used for Gross P&L calculation
                System.Diagnostics.Debug.WriteLine($"Gross P&L Calculation: pnlValue={pnlValue:F2}, dailyLossLimit={dailyLossLimit:F2}, dailyProfitTarget={dailyProfitTarget:F2}");
                
                if (pnlValue == 0)
                {
                    // Zero value = 0% bar
                    percentage = 0;
                    barColor = Color.FromArgb(108, 117, 125);        // Bootstrap secondary gray
                    System.Diagnostics.Debug.WriteLine($"Gross P&L: Zero value - showing 0% bar");
                }
                else if (pnlValue < 0 && dailyLossLimit < 0)
                {
                    // Negative P&L approaching loss limit (both values are negative)
                    // Calculate what percentage of the limit we've used
                    percentage = Math.Abs(pnlValue) / Math.Abs(dailyLossLimit) * 100;
                    
                    // Modern color scheme based on proximity to limit
                    if (percentage >= 90)
                        barColor = Color.FromArgb(220, 53, 69);      // Bootstrap danger red
                    else if (percentage >= 70)
                        barColor = Color.FromArgb(255, 133, 27);     // Modern orange
                    else if (percentage >= 50)
                        barColor = Color.FromArgb(255, 193, 7);      // Bootstrap warning yellow
                    else
                        barColor = Color.FromArgb(40, 167, 69);      // Bootstrap success green
                    
                    System.Diagnostics.Debug.WriteLine($"Gross P&L: Loss scenario - percentage={percentage:F1}%");
                }
                else if (pnlValue > 0 && dailyProfitTarget > 0)
                {
                    // Positive P&L approaching profit target
                    percentage = pnlValue / dailyProfitTarget * 100;
                    
                    // Modern color scheme for profits
                    if (percentage >= 90)
                        barColor = Color.FromArgb(0, 192, 118);      // Bright success green
                    else if (percentage >= 70)
                        barColor = Color.FromArgb(40, 167, 69);      // Medium green
                    else
                        barColor = Color.FromArgb(100, 180, 100);    // Light green
                    
                    System.Diagnostics.Debug.WriteLine($"Gross P&L: Profit scenario - percentage={percentage:F1}%");
                }
                else
                {
                    // No limits configured but value is non-zero, show small bar with neutral color
                    percentage = 10;  // Show at least 10% so it's visible
                    barColor = Color.FromArgb(108, 117, 125);        // Bootstrap secondary gray
                    System.Diagnostics.Debug.WriteLine($"Gross P&L: No limits configured - showing 10% gray bar (pnlValue={pnlValue:F2}, lossLimit={dailyLossLimit:F2}, profitTarget={dailyProfitTarget:F2})");
                }
            }
            else if (isOpenPnL)
            {
                // Open P&L: use Position Loss Limit and Position Profit Target
                // Note: Loss limits are stored as negative values in the settings
                
                // Debug: Log the values being used for Open P&L calculation
                System.Diagnostics.Debug.WriteLine($"Open P&L Calculation: pnlValue={pnlValue:F2}, positionLossLimit={positionLossLimit:F2}, positionProfitTarget={positionProfitTarget:F2}");
                
                if (pnlValue == 0)
                {
                    // Zero value = 0% bar
                    percentage = 0;
                    barColor = Color.FromArgb(108, 117, 125);        // Bootstrap secondary gray
                    System.Diagnostics.Debug.WriteLine($"Open P&L: Zero value - showing 0% bar");
                }
                else if (pnlValue < 0 && positionLossLimit < 0)
                {
                    // Negative P&L approaching position loss limit (both values are negative)
                    // Calculate what percentage of the limit we've used
                    percentage = Math.Abs(pnlValue) / Math.Abs(positionLossLimit) * 100;
                    
                    // Modern color scheme based on proximity to limit
                    if (percentage >= 90)
                        barColor = Color.FromArgb(220, 53, 69);      // Bootstrap danger red
                    else if (percentage >= 70)
                        barColor = Color.FromArgb(255, 133, 27);     // Modern orange
                    else if (percentage >= 50)
                        barColor = Color.FromArgb(255, 193, 7);      // Bootstrap warning yellow
                    else
                        barColor = Color.FromArgb(40, 167, 69);      // Bootstrap success green
                    
                    System.Diagnostics.Debug.WriteLine($"Open P&L: Loss scenario - percentage={percentage:F1}%");
                }
                else if (pnlValue > 0 && positionProfitTarget > 0)
                {
                    // Positive P&L approaching position profit target
                    percentage = pnlValue / positionProfitTarget * 100;
                    
                    // Modern color scheme for profits
                    if (percentage >= 90)
                        barColor = Color.FromArgb(0, 192, 118);      // Bright success green
                    else if (percentage >= 70)
                        barColor = Color.FromArgb(40, 167, 69);      // Medium green
                    else
                        barColor = Color.FromArgb(100, 180, 100);    // Light green
                    
                    System.Diagnostics.Debug.WriteLine($"Open P&L: Profit scenario - percentage={percentage:F1}%");
                }
                else
                {
                    // No limits configured but value is non-zero, show small bar with neutral color
                    percentage = 10;  // Show at least 10% so it's visible
                    barColor = Color.FromArgb(108, 117, 125);        // Bootstrap secondary gray
                    System.Diagnostics.Debug.WriteLine($"Open P&L: No limits configured - showing 10% gray bar (pnlValue={pnlValue:F2}, lossLimit={positionLossLimit:F2}, profitTarget={positionProfitTarget:F2})");
                }
            }

            // Clamp percentage to 0-100
            percentage = Math.Max(0, Math.Min(100, percentage));

            // Debug logging to help troubleshoot
            System.Diagnostics.Debug.WriteLine($"Progress Bar: Column={columnName}, Value={pnlValue:F2}, DailyLossLimit={dailyLossLimit:F2}, DailyProfitTarget={dailyProfitTarget:F2}, PositionLossLimit={positionLossLimit:F2}, PositionProfitTarget={positionProfitTarget:F2}, Percentage={percentage:F1}%, Color={barColor.Name}");

            // Determine display text based on showPercentage setting
            string displayText = showPercentage ? $"{percentage:F1}%" : cellValue.ToString();

            // Draw the progress bar
            DrawProgressBarInCell(e, percentage, barColor, displayText);
            e.Handled = true;
        }

        /// <summary>
        /// Helper method to draw a modern progress bar in a DataGridView cell
        /// </summary>
        private void DrawProgressBarInCell(DataGridViewCellPaintingEventArgs e, double percentage, Color barColor, string text)
        {
            // Paint the cell background
            e.Paint(e.CellBounds, DataGridViewPaintParts.All & ~DataGridViewPaintParts.ContentForeground);

            // Set high quality rendering
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            // Add padding for better appearance
            int padding = 4;
            int barHeight = e.CellBounds.Height - (padding * 2);
            
            // Background bar (track) - slightly darker background with rounded corners
            var trackRect = new Rectangle(
                e.CellBounds.X + padding, 
                e.CellBounds.Y + padding, 
                e.CellBounds.Width - (padding * 2), 
                barHeight
            );
            
            // Draw track background with subtle border
            using (var trackPath = GetRoundedRectanglePath(trackRect, 3))
            {
                // Fill track with subtle background
                using (var trackBrush = new SolidBrush(Color.FromArgb(30, Color.Gray)))
                {
                    e.Graphics.FillPath(trackBrush, trackPath);
                }
                
                // Draw track border
                using (var trackPen = new Pen(Color.FromArgb(50, Color.Gray), 1))
                {
                    e.Graphics.DrawPath(trackPen, trackPath);
                }
            }

            // Calculate progress bar dimensions
            if (percentage > 0)
            {
                var progressWidth = (int)((trackRect.Width) * percentage / 100);
                progressWidth = Math.Max(1, progressWidth); // Ensure at least 1 pixel if percentage > 0
                
                var progressRect = new Rectangle(
                    trackRect.X, 
                    trackRect.Y, 
                    progressWidth, 
                    trackRect.Height
                );

                // Draw progress bar with gradient and rounded corners
                using (var progressPath = GetRoundedRectanglePath(progressRect, 3))
                {
                    // Create gradient brush for more modern look
                    var lighterColor = LightenColor(barColor, 0.2f);
                    using (var gradientBrush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        progressRect, 
                        lighterColor, 
                        barColor, 
                        System.Drawing.Drawing2D.LinearGradientMode.Vertical))
                    {
                        e.Graphics.FillPath(gradientBrush, progressPath);
                    }
                    
                    // Add subtle inner glow for depth
                    using (var glowPen = new Pen(Color.FromArgb(100, Color.White), 1))
                    {
                        var glowRect = new Rectangle(
                            progressRect.X + 1, 
                            progressRect.Y + 1, 
                            progressRect.Width - 2, 
                            progressRect.Height / 2 - 1
                        );
                        if (glowRect.Width > 0 && glowRect.Height > 0)
                        {
                            using (var glowPath = GetRoundedRectanglePath(glowRect, 2))
                            {
                                e.Graphics.DrawPath(glowPen, glowPath);
                            }
                        }
                    }
                    
                    // Draw progress border
                    using (var progressPen = new Pen(DarkenColor(barColor, 0.2f), 1))
                    {
                        e.Graphics.DrawPath(progressPen, progressPath);
                    }
                }
            }

            // Draw the value text on top with shadow for better readability
            var textFormat = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };
            
            // Text shadow for better contrast
            using (var shadowBrush = new SolidBrush(Color.FromArgb(100, Color.Black)))
            {
                var shadowBounds = new RectangleF(
                    e.CellBounds.X + 1, 
                    e.CellBounds.Y + 1, 
                    e.CellBounds.Width, 
                    e.CellBounds.Height
                );
                e.Graphics.DrawString(text, e.CellStyle.Font, shadowBrush, shadowBounds, textFormat);
            }
            
            // Main text
            using (var textBrush = new SolidBrush(e.CellStyle.ForeColor))
            {
                e.Graphics.DrawString(text, e.CellStyle.Font, textBrush, e.CellBounds, textFormat);
            }
        }

        /// <summary>
        /// Creates a rounded rectangle path for smooth corners
        /// </summary>
        private System.Drawing.Drawing2D.GraphicsPath GetRoundedRectanglePath(Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            
            if (rect.Width < radius * 2 || rect.Height < radius * 2)
            {
                // Rectangle too small for rounded corners, use regular rectangle
                path.AddRectangle(rect);
                return path;
            }
            
            int diameter = radius * 2;
            
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            
            return path;
        }

        /// <summary>
        /// Lightens a color by a specified factor
        /// </summary>
        private Color LightenColor(Color color, float factor)
        {
            return Color.FromArgb(
                color.A,
                Math.Min(255, (int)(color.R + (255 - color.R) * factor)),
                Math.Min(255, (int)(color.G + (255 - color.G) * factor)),
                Math.Min(255, (int)(color.B + (255 - color.B) * factor))
            );
        }

        /// <summary>
        /// Darkens a color by a specified factor
        /// </summary>
        private Color DarkenColor(Color color, float factor)
        {
            return Color.FromArgb(
                color.A,
                Math.Max(0, (int)(color.R * (1 - factor))),
                Math.Max(0, (int)(color.G * (1 - factor))),
                Math.Max(0, (int)(color.B * (1 - factor)))
            );
        }

        /// <summary>
        /// Custom cell painting for progress bars in the type summary grid
        /// </summary>
        private void TypeSummaryGrid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            // Only paint progress bars if the feature is enabled
            if (!showProgressBars)
                return;

            // Skip header row
            if (e.RowIndex < 0)
                return;

            var grid = sender as DataGridView;
            if (grid == null)
                return;

            // Determine which columns should have progress bars
            string columnName = grid.Columns[e.ColumnIndex].Name;
            
            // Progress bars for: TotalPnL and OpenPnL
            bool isTotalPnL = columnName == "TotalPnL";
            bool isOpenPnL = columnName == "OpenPnL";
            
            if (!isTotalPnL && !isOpenPnL)
                return;

            // Get the cell value
            var cellValue = e.Value;
            if (cellValue == null)
                return;

            // Parse the P&L value (handle formatted strings like "$1,234.56")
            string valueStr = cellValue.ToString().Replace("$", "").Replace(",", "").Trim();
            if (!double.TryParse(valueStr, out double pnlValue))
                return;

            // Get the type/group name from the first column
            string typeName = "";
            if (e.RowIndex < grid.Rows.Count && grid.Rows[e.RowIndex].Cells.Count > 0)
            {
                var typeCell = grid.Rows[e.RowIndex].Cells[0].Value;
                if (typeCell != null)
                    typeName = typeCell.ToString();
            }

            // Get aggregated limits for this type/group
            double dailyLossLimit = 0;
            double dailyProfitTarget = 0;
            double positionLossLimit = 0;
            double positionProfitTarget = 0;

            if (!string.IsNullOrEmpty(typeName) && typeSummaryAggregatedData.ContainsKey(typeName))
            {
                var data = typeSummaryAggregatedData[typeName];
                dailyLossLimit = data.DailyLossLimit;
                dailyProfitTarget = data.DailyProfitTarget;
                positionLossLimit = data.PositionLossLimit;
                positionProfitTarget = data.PositionProfitTarget;
            }
            
            // Calculate progress percentage and color
            double percentage = 0;
            Color barColor = Color.FromArgb(100, 180, 100); // Modern green

            if (pnlValue == 0)
            {
                // Zero value = 0% bar
                percentage = 0;
                barColor = Color.FromArgb(108, 117, 125);        // Bootstrap secondary gray
            }
            else if (isTotalPnL)
            {
                // Total P&L: use Daily Loss Limit and Daily Profit Target (aggregated)
                if (pnlValue < 0 && dailyLossLimit < 0)
                {
                    // Negative P&L approaching loss limit
                    percentage = Math.Min(100, Math.Abs(pnlValue) / Math.Abs(dailyLossLimit) * 100);
                    
                    // Modern color scheme based on magnitude
                    if (percentage >= 90)
                        barColor = Color.FromArgb(220, 53, 69);      // Bootstrap danger red
                    else if (percentage >= 70)
                        barColor = Color.FromArgb(255, 133, 27);     // Modern orange
                    else if (percentage >= 50)
                        barColor = Color.FromArgb(255, 193, 7);      // Bootstrap warning yellow
                    else
                        barColor = Color.FromArgb(40, 167, 69);      // Bootstrap success green
                }
                else if (pnlValue > 0 && dailyProfitTarget > 0)
                {
                    // Positive P&L approaching profit target
                    percentage = Math.Min(100, pnlValue / dailyProfitTarget * 100);
                    
                    // Modern color scheme for profits
                    if (percentage >= 90)
                        barColor = Color.FromArgb(0, 192, 118);      // Bright success green
                    else if (percentage >= 70)
                        barColor = Color.FromArgb(40, 167, 69);      // Medium green
                    else
                        barColor = Color.FromArgb(100, 180, 100);    // Light green
                }
                else
                {
                    // No limits configured, use default threshold for visualization
                    double defaultLimit = 1000;
                    percentage = Math.Min(100, Math.Abs(pnlValue) / defaultLimit * 100);
                    
                    if (pnlValue < 0)
                    {
                        // Negative - red tones
                        if (percentage >= 70)
                            barColor = Color.FromArgb(220, 53, 69);
                        else if (percentage >= 50)
                            barColor = Color.FromArgb(255, 133, 27);
                        else
                            barColor = Color.FromArgb(255, 193, 7);
                    }
                    else
                    {
                        // Positive - green tones
                        if (percentage >= 70)
                            barColor = Color.FromArgb(0, 192, 118);
                        else
                            barColor = Color.FromArgb(40, 167, 69);
                    }
                }
            }
            else if (isOpenPnL)
            {
                // Open P&L: use Position Loss Limit and Position Profit Target (aggregated)
                if (pnlValue < 0 && positionLossLimit < 0)
                {
                    // Negative P&L approaching position loss limit
                    percentage = Math.Min(100, Math.Abs(pnlValue) / Math.Abs(positionLossLimit) * 100);
                    
                    // Modern color scheme based on magnitude
                    if (percentage >= 90)
                        barColor = Color.FromArgb(220, 53, 69);      // Bootstrap danger red
                    else if (percentage >= 70)
                        barColor = Color.FromArgb(255, 133, 27);     // Modern orange
                    else if (percentage >= 50)
                        barColor = Color.FromArgb(255, 193, 7);      // Bootstrap warning yellow
                    else
                        barColor = Color.FromArgb(40, 167, 69);      // Bootstrap success green
                }
                else if (pnlValue > 0 && positionProfitTarget > 0)
                {
                    // Positive P&L approaching position profit target
                    percentage = Math.Min(100, pnlValue / positionProfitTarget * 100);
                    
                    // Modern color scheme for profits
                    if (percentage >= 90)
                        barColor = Color.FromArgb(0, 192, 118);      // Bright success green
                    else if (percentage >= 70)
                        barColor = Color.FromArgb(40, 167, 69);      // Medium green
                    else
                        barColor = Color.FromArgb(100, 180, 100);    // Light green
                }
                else
                {
                    // No limits configured, use default threshold for visualization
                    double defaultLimit = 1000;
                    percentage = Math.Min(100, Math.Abs(pnlValue) / defaultLimit * 100);
                    
                    if (pnlValue < 0)
                    {
                        // Negative - red tones
                        if (percentage >= 70)
                            barColor = Color.FromArgb(220, 53, 69);
                        else if (percentage >= 50)
                            barColor = Color.FromArgb(255, 133, 27);
                        else
                            barColor = Color.FromArgb(255, 193, 7);
                    }
                    else
                    {
                        // Positive - green tones
                        if (percentage >= 70)
                            barColor = Color.FromArgb(0, 192, 118);
                        else
                            barColor = Color.FromArgb(40, 167, 69);
                    }
                }
            }

            // Determine display text based on showPercentage setting
            string displayText = showPercentage ? $"{percentage:F1}%" : cellValue.ToString();

            // Draw the progress bar using helper method
            DrawProgressBarInCell(e, percentage, barColor, displayText);
            e.Handled = true;
        }

        private Control CreateTypeSummaryPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };
            // Title and filter row
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = DarkBackground,
                Padding = new Padding(10, 5, 10, 5)
            };
            // Title with colored emoji rendering (ensure key matches IconMap -> "Type")
            var header = new CustomHeaderControl("Type", GetIconForTitle("Type"));
            header.Dock = DockStyle.Left;
            header.Margin = new Padding(10, 0, 0, 0);

            // Filter label
            var filterLabel = new Label
            {
                Text = "Filter by:",
                Dock = DockStyle.Left,
                Width = 70,
                Font = new Font("Segoe UI", 9),
                ForeColor = TextWhite,
                TextAlign = ContentAlignment.MiddleLeft,
                BackColor = DarkBackground
            };

            // Filter dropdown
            typeSummaryFilterComboBox = new ComboBox
            {
                Dock = DockStyle.Left,
                Width = 120,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                BackColor = CardBackground,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat
            };
            typeSummaryFilterComboBox.Items.AddRange(TypeSummaryFilterOptions);
            typeSummaryFilterComboBox.SelectedIndex = 0; // Default to "Type"
            typeSummaryFilterComboBox.SelectedIndexChanged += (s, e) => RefreshTypeSummary();

            topPanel.Controls.Add(typeSummaryFilterComboBox);
            topPanel.Controls.Add(filterLabel);
            topPanel.Controls.Add(header);

            typeSummaryGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = CardBackground,
                GridColor = DarkerBackground,
                BorderStyle = BorderStyle.None,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false
            };

            // Style the grid for dark theme
            typeSummaryGrid.DefaultCellStyle.BackColor = CardBackground;
            typeSummaryGrid.DefaultCellStyle.ForeColor = TextWhite;
            typeSummaryGrid.DefaultCellStyle.SelectionBackColor = SelectedColor;
            typeSummaryGrid.DefaultCellStyle.SelectionForeColor = TextWhite;
            typeSummaryGrid.ColumnHeadersDefaultCellStyle.BackColor = DarkerBackground;
            typeSummaryGrid.ColumnHeadersDefaultCellStyle.ForeColor = TextWhite;
            typeSummaryGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            // Add columns as requested
            typeSummaryGrid.Columns.Add("Type", "Type");
            typeSummaryGrid.Columns.Add("Count", "Count");
            typeSummaryGrid.Columns.Add("Equity", "Equity");
            typeSummaryGrid.Columns.Add("OpenPnL", "Open P&L");
            typeSummaryGrid.Columns.Add("ClosedPnL", "Closed P&L");
            typeSummaryGrid.Columns.Add("TotalPnL", "Total P&L");
            typeSummaryGrid.Columns.Add("Drawdown", "Drawdown");

            // Add CellPainting event handler for progress bars
            typeSummaryGrid.CellPainting += TypeSummaryGrid_CellPainting;

            RefreshTypeSummary();
            typeSummaryRefreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            typeSummaryRefreshTimer.Tick += (s, e) => RefreshTypeSummary();
            typeSummaryRefreshTimer.Start();

            // Add controls in correct order: Fill control first, then Top controls
            mainPanel.Controls.Add(typeSummaryGrid);
            mainPanel.Controls.Add(topPanel);
            return mainPanel;
        }

        /// <summary>
        /// Refreshes all data grids to apply or remove progress bars based on the showProgressBars setting
        /// </summary>
        private void RefreshAllDataGrids()
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(RefreshAllDataGrids));
                return;
            }

            try
            {
                // Refresh the grids by calling their existing refresh methods
                if (statsGrid != null)
                {
                    RefreshAccountsSummary();
                    statsGrid.Invalidate(); // Force redraw
                }
                
                // statsDetailGrid displays individual metrics (not P&L columns), so no progress bars are shown
                // We still refresh it to keep data in sync
                if (statsDetailGrid != null)
                {
                    RefreshAccountStats();
                    statsDetailGrid.Invalidate(); // Force redraw
                }
                
                if (typeSummaryGrid != null)
                {
                    RefreshTypeSummary();
                    typeSummaryGrid.Invalidate(); // Force redraw
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error refreshing data grids: {ex.Message}");
            }
        }

        private void RefreshTypeSummary()
        {
            if (InvokeRequired) { BeginInvoke(new Action(RefreshTypeSummary)); return; }
            if (typeSummaryGrid == null) return;

            try
            {
                typeSummaryGrid.SuspendLayout();
                typeSummaryGrid.Rows.Clear();

                var core = Core.Instance;
                if (core == null || core.Accounts == null || !core.Accounts.Any())
                {
                    // Demo data
                    typeSummaryGrid.Rows.Add("Live", "1", "1000.00", "12.34", "50.00", "62.34", "0.00");
                    typeSummaryGrid.Rows.Add("Demo", "1", "2500.50", "(8.20)", "25.00", "16.80", "0.00");
                    typeSummaryGrid.Rows.Add("Total", "2", "3500.50", "4.14", "75.00", "79.14", "0.00");
                    return;
                }

                // Determine if we're filtering by Type or Firm
                var filterByFirm = typeSummaryFilterComboBox?.SelectedItem?.ToString() == FILTER_MODE_FIRM;
                
                // Update column header based on filter mode
                if (typeSummaryGrid.Columns.Count > 0)
                {
                    typeSummaryGrid.Columns[0].HeaderText = filterByFirm ? FILTER_MODE_FIRM : FILTER_MODE_TYPE;
                }

                // Get settings service for loading account limits
                var settingsService = RiskManagerSettingsService.Instance;

                // Dictionary to store aggregated data by type or firm
                var aggregatedData = new Dictionary<string, TypeSummaryData>();

                // Track account index for GetUniqueAccountIdentifier
                int accountIndex = 0;
                foreach (var account in core.Accounts)
                {
                    if (account == null)
                    {
                        accountIndex++;
                        continue;
                    }

                    // Get the grouping key based on filter mode
                    string groupKey = filterByFirm ? GetAccountIbId(account) : DetermineAccountType(account);

                    // Initialize data if not exists
                    if (!aggregatedData.ContainsKey(groupKey))
                    {
                        aggregatedData[groupKey] = new TypeSummaryData();
                    }

                    var data = aggregatedData[groupKey];
                    data.Count++;
                    data.Equity += account.Balance;

                    // Aggregate limits from settings for this account
                    try
                    {
                        if (settingsService != null && settingsService.IsInitialized)
                        {
                            // Get unique account identifier
                            var accountNumber = GetUniqueAccountIdentifier(account, accountIndex);
                            if (!string.IsNullOrEmpty(accountNumber))
                            {
                                var settings = settingsService.GetSettings(accountNumber);
                                if (settings != null)
                                {
                                    // Aggregate limits (sum them up for the group)
                                    data.DailyLossLimit += (double)(settings.DailyLossLimit ?? 0);
                                    data.DailyProfitTarget += (double)(settings.DailyProfitTarget ?? 0);
                                    data.PositionLossLimit += (double)(settings.PositionLossLimit ?? 0);
                                    data.PositionProfitTarget += (double)(settings.PositionProfitTarget ?? 0);
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error loading limits for account in type summary: {ex.Message}");
                    }

                    accountIndex++;

                    // Get Open P&L, Closed P&L and Drawdown from AdditionalInfo
                    if (account.AdditionalInfo != null)
                    {
                        foreach (var info in account.AdditionalInfo)
                        {
                            if (info?.Id == null) continue;
                            var id = info.Id;

                            // Open P&L - from OpenPnl field
                            if (string.Equals(id, "OpenPnl", StringComparison.OrdinalIgnoreCase))
                            {
                                if (info.Value is double ov) data.OpenPnL += ov;
                            }

                            // Closed P&L - from ClosedPnl field
                            if (string.Equals(id, "ClosedPnl", StringComparison.OrdinalIgnoreCase))
                            {
                                if (info.Value is double cv) data.ClosedPnL += cv;
                            }

                            // Trailing Drawdown - from AutoLiquidateThresholdCurrentValue field
                            if (string.Equals(id, "AutoLiquidateThresholdCurrentValue", StringComparison.OrdinalIgnoreCase))
                            {
                                if (info.Value is double td) data.TrailingDrawdown += td;
                            }
                        }
                    }
                }

                // Calculate totals
                var totalData = new TypeSummaryData();
                foreach (var kvp in aggregatedData)
                {
                    totalData.Count += kvp.Value.Count;
                    totalData.Equity += kvp.Value.Equity;
                    totalData.OpenPnL += kvp.Value.OpenPnL;
                    totalData.ClosedPnL += kvp.Value.ClosedPnL;
                    totalData.TrailingDrawdown += kvp.Value.TrailingDrawdown;
                    // Aggregate limits for total row
                    totalData.DailyLossLimit += kvp.Value.DailyLossLimit;
                    totalData.DailyProfitTarget += kvp.Value.DailyProfitTarget;
                    totalData.PositionLossLimit += kvp.Value.PositionLossLimit;
                    totalData.PositionProfitTarget += kvp.Value.PositionProfitTarget;
                }

                // Store aggregated data for CellPainting to access
                typeSummaryAggregatedData = new Dictionary<string, TypeSummaryData>(aggregatedData);
                typeSummaryAggregatedData["Total"] = totalData;

                // Add rows for each group
                foreach (var kvp in aggregatedData.OrderBy(x => x.Key))
                {
                    var groupName = kvp.Key;
                    var data = kvp.Value;
                    var totalPnL = data.OpenPnL + data.ClosedPnL;
                    // Calculate Drawdown as Equity - Trailing Drawdown
                    var drawdown = data.Equity - data.TrailingDrawdown;

                    typeSummaryGrid.Rows.Add(
                        groupName,
                        data.Count.ToString(),
                        FormatNumeric(data.Equity),
                        FormatNumeric(data.OpenPnL),
                        FormatNumeric(data.ClosedPnL),
                        FormatNumeric(totalPnL),
                        FormatNumeric(drawdown)
                    );
                }

                // Add total row
                var totalTotalPnL = totalData.OpenPnL + totalData.ClosedPnL;
                // Calculate total Drawdown as Equity - Trailing Drawdown
                var totalDrawdown = totalData.Equity - totalData.TrailingDrawdown;
                typeSummaryGrid.Rows.Add(
                    "Total",
                    totalData.Count.ToString(),
                    FormatNumeric(totalData.Equity),
                    FormatNumeric(totalData.OpenPnL),
                    FormatNumeric(totalData.ClosedPnL),
                    FormatNumeric(totalTotalPnL),
                    FormatNumeric(totalDrawdown)
                );
            }
            catch (Exception ex)
            {
                // Log error for debugging but don't crash the UI
                System.Diagnostics.Debug.WriteLine($"RefreshTypeSummary error: {ex.Message}");
                
                // Show error in grid if it's empty
                if (typeSummaryGrid.Rows.Count == 0)
                {
                    try
                    {
                        // Create error row with proper column count
                        var errorValues = new object[typeSummaryGrid.Columns.Count];
                        errorValues[0] = "Error";
                        for (int i = 1; i < errorValues.Length - 1; i++)
                        {
                            errorValues[i] = "-";
                        }
                        errorValues[errorValues.Length - 1] = $"API Connection Failed: {ex.Message}";
                        typeSummaryGrid.Rows.Add(errorValues);
                    }
                    catch
                    {
                        // If even adding error row fails, just ignore
                    }
                }
            }
            finally
            {
                // After typeSummaryGrid.ResumeLayout();
                typeSummaryGrid.ResumeLayout();
                // Re-apply theme-specific coloring for accounts/type grids and labels.
                // DO NOT color statsDetailGrid here — RefreshAccountStats must own statsDetailGrid coloring.
                ReapplyThemeColoringAfterRefresh();

                // Ensure surrounding UI labels are refreshed; statsDetailGrid coloring is handled inside RefreshAccountStats()
                ApplyValueLabelColoring(typeSummaryGrid.Parent ?? this);
            }          
        }

        // Helper class to aggregate type summary data
        private class TypeSummaryData
        {
            public int Count { get; set; }
            public double Equity { get; set; }
            public double OpenPnL { get; set; }
            public double ClosedPnL { get; set; }
            public double TrailingDrawdown { get; set; }
            // Aggregated limits for progress bar calculations
            public double DailyLossLimit { get; set; }
            public double DailyProfitTarget { get; set; }
            public double PositionLossLimit { get; set; }
            public double PositionProfitTarget { get; set; }
        }

        // Replace body of ReapplyThemeColoringAfterRefresh to omit statsDetailGrid full-column coloring
        private void ReapplyThemeColoringAfterRefresh()
        {
            try
            {
                if (currentTheme == Theme.YellowBlueBlack)
                {
                    // Accounts summary columns
                    ColorizeNumericCells(statsGrid, "OpenPnL", "ClosedPnL", "DailyPnL", "GrossPnL");

                    // Type summary
                    ColorizeNumericCells(typeSummaryGrid, "OpenPnL", "ClosedPnL", "TotalPnL");
                }

                // Re-apply label coloring for Risk Overview value labels and the rest of UI
                ApplyValueLabelColoring(this);
                if (contentPanel != null) ApplyValueLabelColoring(contentPanel);
                if (topPanel != null) ApplyValueLabelColoring(topPanel);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ReapplyThemeColoringAfterRefresh error: {ex.Message}");
            }
        }

        private Panel CreateStatisticsOverviewPanel()
        {
            var mainPanel = new Panel
            {
                BackColor = DarkBackground,
                Dock = DockStyle.Fill,
                Padding = new Padding(10)
            };

            // Title
            var titleLabel = new Label
            {
                Text = "Statistics Overview",
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = TextWhite,
                BackColor = DarkBackground,
                Padding = new Padding(5, 0, 0, 0)
            };
            mainPanel.Controls.Add(titleLabel);

            // Stats container with two-column layout
            var statsContainer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 7,
                BackColor = CardBackground,
                Padding = new Padding(15),
                CellBorderStyle = TableLayoutPanelCellBorderStyle.None
            };

            statsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            statsContainer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));

            for (int i = 0; i < 7; i++)
            {
                statsContainer.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            }

            // Stats data - matching the QuantGuard design
            var stats = new[]
            {
                ("Daily Net", "$0.00"),
                ("Daily Profit", "$0.00"),
                ("Daily Loss", "$0.00"),
                ("Weekly Net", "$0.00"),
                ("Weekly Profit", "$0.00"),
                ("Weekly Loss", "$0.00"),
                ("Drawdown", "$0.00")
            };

            for (int i = 0; i < stats.Length; i++)
            {
                var (label, value) = stats[i];

                var labelCtrl = new Label
                {
                    Text = label,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = new Font("Segoe UI", 10, FontStyle.Regular),
                    ForeColor = TextWhite,
                    BackColor = CardBackground,
                    Padding = new Padding(10, 0, 0, 0)
                };

                var valueCtrl = new Label
                {
                    Text = value,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleRight,
                    Font = new Font("Segoe UI", 10, FontStyle.Regular),
                    ForeColor = TextWhite,
                    BackColor = CardBackground,
                    Padding = new Padding(0, 0, 10, 0),
                    Tag = label // Store the metric name for updates
                };

                statsContainer.Controls.Add(labelCtrl, 0, i);
                statsContainer.Controls.Add(valueCtrl, 1, i);
            }

            mainPanel.Controls.Add(statsContainer);

            // Setup refresh timer
            statsDetailRefreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            statsDetailRefreshTimer.Tick += (s, e) => RefreshStatisticsOverview(statsContainer);
            statsDetailRefreshTimer.Start();

            return mainPanel;
        }

        private void RefreshStatisticsOverview(TableLayoutPanel statsContainer)
        {
            if (InvokeRequired) { BeginInvoke(new Action(() => RefreshStatisticsOverview(statsContainer))); return; }
            if (statsContainer == null) return;

            try
            {
                var core = Core.Instance;
                if (core?.Accounts == null || !core.Accounts.Any()) return;

                // Use first account if none selected
                var account = selectedAccount ?? core.Accounts.FirstOrDefault();
                if (account == null) return;

                double dailyProfit = 0, dailyLoss = 0, weeklyProfit = 0, weeklyLoss = 0, drawdown = 0;

                if (account.AdditionalInfo != null)
                {
                    foreach (var info in account.AdditionalInfo)
                    {
                        if (info?.Id == null) continue;
                        var id = info.Id;

                        if (string.Equals(id, "Daily Profit", StringComparison.OrdinalIgnoreCase))
                        {
                            if (info.Value is double dp) dailyProfit = dp;
                        }
                        if (string.Equals(id, "Daily Loss", StringComparison.OrdinalIgnoreCase))
                        {
                            if (info.Value is double dl) dailyLoss = dl;
                        }
                        if (string.Equals(id, "Weekly Profit", StringComparison.OrdinalIgnoreCase))
                        {
                            if (info.Value is double wp) weeklyProfit = wp;
                        }
                        if (string.Equals(id, "Weekly Loss", StringComparison.OrdinalIgnoreCase))
                        {
                            if (info.Value is double wl) weeklyLoss = wl;
                        }
                        if (string.Equals(id, "Drawdown", StringComparison.OrdinalIgnoreCase))
                        {
                            if (info.Value is double dd) drawdown = dd;
                        }
                    }
                }

                double dailyNet = dailyProfit + dailyLoss;
                double weeklyNet = weeklyProfit + weeklyLoss;

                var values = new Dictionary<string, string>
                {
                    { "Daily Net", $"${FormatNumeric(dailyNet)}" },
                    { "Daily Profit", $"${FormatNumeric(dailyProfit)}" },
                    { "Daily Loss", $"${FormatNumeric(dailyLoss)}" },
                    { "Weekly Net", $"${FormatNumeric(weeklyNet)}" },
                    { "Weekly Profit", $"${FormatNumeric(weeklyProfit)}" },
                    { "Weekly Loss", $"${FormatNumeric(weeklyLoss)}" },
                    { "Drawdown", $"${FormatNumeric(drawdown)}" }
                };

                foreach (Control ctrl in statsContainer.Controls)
                {
                    if (ctrl is Label lbl && lbl.Tag is string metricName && values.ContainsKey(metricName))
                    {
                        lbl.Text = values[metricName];
                    }
                }
            }
            catch
            {
                // ignore refresh errors
            }
        }

        private Control CreateDarkThemedPanel(string title, string subtitle = null)
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title
            var titleLabel = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Padding = new Padding(10, 0, 0, 0),
                BackColor = DarkBackground,
                ForeColor = TextWhite
            };
            mainPanel.Controls.Add(titleLabel);

            if (!string.IsNullOrEmpty(subtitle))
            {
                var subtitleLabel = new Label
                {
                    Text = subtitle,
                    Dock = DockStyle.Top,
                    Height = 30,
                    TextAlign = ContentAlignment.TopLeft,
                    Font = new Font("Segoe UI", 9, FontStyle.Regular),
                    Padding = new Padding(10, 0, 10, 0),
                    BackColor = DarkBackground,
                    ForeColor = TextGray,
                    AutoSize = false
                };
                mainPanel.Controls.Add(subtitleLabel);
            }

            return mainPanel;
        }

        private Control CreatePlaceholderPanel(string title)
        {
            // Handle Block Symbols specially
            if (string.Equals(title, "Block Symbols", StringComparison.OrdinalIgnoreCase))
            {
                return CreateBlockSymbolsDarkPanel();
            }

            // Handle Allowed Trading Times specially
            if (string.Equals(title, "Allowed Trading Times", StringComparison.OrdinalIgnoreCase))
            {
                return CreateAllowedTradingTimesDarkPanel();
            }

            // Handle Lock Settings specially
            if (string.Equals(title, "Lock Settings", StringComparison.OrdinalIgnoreCase))
            {
                return CreateLockSettingsDarkPanel();
            }

            // Handle Manual Lock specially
            if (string.Equals(title, "Manual Lock", StringComparison.OrdinalIgnoreCase))
            {
                return CreateManualLockDarkPanel();
            }

            // Handle Feature Toggles specially
            if (string.Equals(title, "Feature Toggles", StringComparison.OrdinalIgnoreCase))
            {
                return CreateFeatureTogglesPanel();
            }

            // Handle Weekly Loss specially
            if (string.Equals(title, "Weekly Loss", StringComparison.OrdinalIgnoreCase))
            {
                return CreateWeeklyLossDarkPanel();
            }

            // Handle Weekly Profit Target specially
            if (string.Equals(title, "Weekly Profit Target", StringComparison.OrdinalIgnoreCase))
            {
                return CreateWeeklyProfitTargetDarkPanel();
            }

            // Default dark placeholder for other tabs
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title
            var titleLabel = new Label
            {
                Text = title,
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Padding = new Padding(10, 0, 0, 0),
                BackColor = DarkBackground,
                ForeColor = TextWhite
            };

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Configure your settings below.",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(10, 0, 10, 0),
                BackColor = DarkBackground,
                ForeColor = TextGray,
                AutoSize = false
            };

            var contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBackground,
                Padding = new Padding(15)
            };

            var placeholderLabel = new Label
            {
                Text = $"Settings for {title} will be displayed here.",
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = TextGray,
                BackColor = CardBackground
            };
            contentArea.Controls.Add(placeholderLabel);

            // Add controls in correct order: Fill first, then Top
            // In WinForms, docking is processed in reverse Z-order
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(titleLabel);
            return mainPanel;
        }

        private Control CreateBlockSymbolsDarkPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title
            var titleLabel = new Label
            {
                Text = "Block Symbols",
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Padding = new Padding(10, 0, 0, 0),
                BackColor = DarkBackground,
                ForeColor = TextWhite
            };

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Select the symbols you want to block trading on:",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(10, 0, 10, 0),
                BackColor = DarkBackground,
                ForeColor = TextGray,
                AutoSize = false
            };

            var contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBackground,
                Padding = new Padding(10)
            };

            // Symbol list with dark theme styling
            var symbolsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                ReadOnly = false,
                BackgroundColor = CardBackground,
                GridColor = DarkerBackground,
                BorderStyle = BorderStyle.None,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                EnableHeadersVisualStyles = false
            };

            // Style the grid for dark theme
            symbolsGrid.DefaultCellStyle.BackColor = CardBackground;
            symbolsGrid.DefaultCellStyle.ForeColor = TextWhite;
            symbolsGrid.DefaultCellStyle.SelectionBackColor = SelectedColor;
            symbolsGrid.DefaultCellStyle.SelectionForeColor = TextWhite;
            symbolsGrid.ColumnHeadersDefaultCellStyle.BackColor = DarkerBackground;
            symbolsGrid.ColumnHeadersDefaultCellStyle.ForeColor = TextWhite;
            symbolsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);

            var symbolColumn = new DataGridViewTextBoxColumn
            {
                Name = "Symbol",
                HeaderText = "Symbol",
                ReadOnly = true,
                FillWeight = 70
            };
            symbolsGrid.Columns.Add(symbolColumn);

            var blockColumn = new DataGridViewCheckBoxColumn
            {
                Name = "Block",
                HeaderText = "Block Trading",
                ReadOnly = false,
                FillWeight = 30
            };
            symbolsGrid.Columns.Add(blockColumn);

            // TODO: Replace sample symbols with actual data from Core.Instance.Symbols or similar data source
            var sampleSymbols = new[] { "AAPL", "MSFT", "GOOGL", "AMZN", "TSLA", "META", "NVDA", "NFLX" };
            foreach (var symbol in sampleSymbols)
            {
                symbolsGrid.Rows.Add(symbol, false);
            }

            contentArea.Controls.Add(symbolsGrid);

            // Save button with dark theme
            var saveButton = CreateDarkSaveButton();

            // Add controls in correct order: Bottom first, Fill second, Top last
            // In WinForms, docking is processed in reverse Z-order
            mainPanel.Controls.Add(saveButton);
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(titleLabel);

            return mainPanel;
        }

        private Control CreateAllowedTradingTimesDarkPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title with emoji rendering (use the mapped key "Allowed Trading Times" so clock.png is used)
            var tradingTimesHeader = new CustomHeaderControl("Allowed Trading Times", GetIconForTitle("Allowed Trading Times"));
            tradingTimesHeader.Dock = DockStyle.Top;
            tradingTimesHeader.Margin = new Padding(10, 0, 0, 0);

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Configure custom trading windows with day and time restrictions:",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(10, 0, 10, 0),
                BackColor = DarkBackground,
                ForeColor = TextGray,
                AutoSize = false
            };

            // Main content area using FlowLayoutPanel for proper vertical layout
            var contentArea = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBackground,
                Padding = new Padding(15),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Tag = "TradingTimeContentArea"
            };

            // Container for trading time rows
            var rowsContainer = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = CardBackground,
                Padding = new Padding(0),
                Margin = new Padding(0, 0, 0, 10),
                Tag = "TradingTimeRowsContainer"
            };

            // Don't add a default row here - LoadTradingTimeRestrictions will handle it
            // when the page is shown in ShowPage()

            contentArea.Controls.Add(rowsContainer);

            // Button panel
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(0),
                Margin = new Padding(0, 10, 0, 0),
                BackColor = CardBackground
            };

            // Add Trading Window button
            var addButton = new Button
            {
                Text = "Add Trading Window",
                Width = 180,
                Height = 35,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = AccentGreen,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            addButton.FlatAppearance.BorderSize = 0;
            addButton.Click += (s, e) => AddTradingTimeRow(rowsContainer);
            buttonPanel.Controls.Add(addButton);

            // Clear All button
            var clearButton = new Button
            {
                Text = "Clear All",
                Width = 120,
                Height = 35,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = AccentAmber,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            clearButton.FlatAppearance.BorderSize = 0;
            clearButton.Click += (s, e) =>
            {
                var result = MessageBox.Show("Are you sure you want to remove all trading time restrictions?", 
                    "Confirm Clear", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (result == DialogResult.Yes)
                {
                    rowsContainer.Controls.Clear();
                }
            };
            buttonPanel.Controls.Add(clearButton);

            contentArea.Controls.Add(buttonPanel);

            var saveButton = CreateDarkSaveButton();

            // Add controls in correct order: Bottom first, Fill second, Top last
            // In WinForms, docking is processed in reverse Z-order
            mainPanel.Controls.Add(saveButton);
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(tradingTimesHeader);

            return mainPanel;
        }

        // Helper method to add a trading time row
        private void AddTradingTimeRow(FlowLayoutPanel container, TradingTimeRestriction restriction = null)
        {
            var rowPanel = new Panel
            {
                Width = 530, // Fixed width to accommodate all controls
                Height = 40,
                BackColor = DarkerBackground,
                Margin = new Padding(0, 5, 0, 5),
                Padding = new Padding(5)
            };

            // Day of Week dropdown
            var dayComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 100,
                Height = 30,
                Left = 5,
                Top = 5,
                BackColor = CardBackground,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 9),
                FlatStyle = FlatStyle.Flat
            };
            dayComboBox.Items.AddRange(new object[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" });
            dayComboBox.SelectedIndex = restriction != null ? (int)restriction.DayOfWeek : 1; // Default to Monday
            rowPanel.Controls.Add(dayComboBox);

            // Start Hour
            var startHourComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 50,
                Height = 30,
                Left = 115,
                Top = 5,
                BackColor = CardBackground,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 9),
                FlatStyle = FlatStyle.Flat
            };
            for (int i = 1; i <= 12; i++)
                startHourComboBox.Items.Add(i.ToString("D2"));
            rowPanel.Controls.Add(startHourComboBox);

            // Start Minute
            var startMinuteComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 50,
                Height = 30,
                Left = 170,
                Top = 5,
                BackColor = CardBackground,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 9),
                FlatStyle = FlatStyle.Flat
            };
            startMinuteComboBox.Items.AddRange(new object[] { "00", "15", "30", "45" });
            startMinuteComboBox.SelectedIndex = 0;
            rowPanel.Controls.Add(startMinuteComboBox);

            // Start AM/PM
            var startAmPmComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 50,
                Height = 30,
                Left = 225,
                Top = 5,
                BackColor = CardBackground,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 9),
                FlatStyle = FlatStyle.Flat
            };
            startAmPmComboBox.Items.AddRange(new object[] { "AM", "PM" });
            startAmPmComboBox.SelectedIndex = 0;
            rowPanel.Controls.Add(startAmPmComboBox);

            // "to" label
            var toLabel = new Label
            {
                Text = "to",
                Left = 285,
                Top = 10,
                Width = 20,
                Height = 20,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 9),
                TextAlign = ContentAlignment.MiddleCenter
            };
            rowPanel.Controls.Add(toLabel);

            // End Hour
            var endHourComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 50,
                Height = 30,
                Left = 310,
                Top = 5,
                BackColor = CardBackground,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 9),
                FlatStyle = FlatStyle.Flat
            };
            for (int i = 1; i <= 12; i++)
                endHourComboBox.Items.Add(i.ToString("D2"));
            rowPanel.Controls.Add(endHourComboBox);

            // End Minute
            var endMinuteComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 50,
                Height = 30,
                Left = 365,
                Top = 5,
                BackColor = CardBackground,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 9),
                FlatStyle = FlatStyle.Flat
            };
            endMinuteComboBox.Items.AddRange(new object[] { "00", "15", "30", "45" });
            endMinuteComboBox.SelectedIndex = 0;
            rowPanel.Controls.Add(endMinuteComboBox);

            // End AM/PM
            var endAmPmComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Width = 50,
                Height = 30,
                Left = 420,
                Top = 5,
                BackColor = CardBackground,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 9),
                FlatStyle = FlatStyle.Flat
            };
            endAmPmComboBox.Items.AddRange(new object[] { "AM", "PM" });
            endAmPmComboBox.SelectedIndex = 1;
            rowPanel.Controls.Add(endAmPmComboBox);

            // If restriction data provided, populate the controls
            if (restriction != null)
            {
                // Parse start time
                int startHour = restriction.StartTime.Hours;
                int startMinute = restriction.StartTime.Minutes;
                bool startIsPM = startHour >= 12;
                if (startHour > 12) startHour -= 12;
                if (startHour == 0) startHour = 12;
                
                startHourComboBox.SelectedItem = startHour.ToString("D2");
                startMinuteComboBox.SelectedItem = (startMinute / 15 * 15).ToString("D2");
                startAmPmComboBox.SelectedItem = startIsPM ? "PM" : "AM";

                // Parse end time
                int endHour = restriction.EndTime.Hours;
                int endMinute = restriction.EndTime.Minutes;
                bool endIsPM = endHour >= 12;
                if (endHour > 12) endHour -= 12;
                if (endHour == 0) endHour = 12;
                
                endHourComboBox.SelectedItem = endHour.ToString("D2");
                endMinuteComboBox.SelectedItem = (endMinute / 15 * 15).ToString("D2");
                endAmPmComboBox.SelectedItem = endIsPM ? "PM" : "AM";
            }
            else
            {
                // Default values: 9:00 AM to 5:00 PM
                startHourComboBox.SelectedItem = "09";
                startMinuteComboBox.SelectedItem = "00";
                startAmPmComboBox.SelectedItem = "AM";
                endHourComboBox.SelectedItem = "05";
                endMinuteComboBox.SelectedItem = "00";
                endAmPmComboBox.SelectedItem = "PM";
            }

            // Delete button
            var deleteButton = new Button
            {
                Text = "×",
                Width = 30,
                Height = 30,
                Left = 480,
                Top = 5,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                BackColor = Color.FromArgb(200, 50, 50),
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            deleteButton.FlatAppearance.BorderSize = 0;
            deleteButton.Click += (s, e) =>
            {
                container.Controls.Remove(rowPanel);
                rowPanel.Dispose();
            };
            rowPanel.Controls.Add(deleteButton);

            // Store references in rowPanel.Tag for easy retrieval during save
            rowPanel.Tag = new {
                DayComboBox = dayComboBox,
                StartHourComboBox = startHourComboBox,
                StartMinuteComboBox = startMinuteComboBox,
                StartAmPmComboBox = startAmPmComboBox,
                EndHourComboBox = endHourComboBox,
                EndMinuteComboBox = endMinuteComboBox,
                EndAmPmComboBox = endAmPmComboBox
            };

            container.Controls.Add(rowPanel);
        }

        private Control CreateLockSettingsDarkPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title with emoji rendering
            var lockSettingsHeader = new CustomHeaderControl("Lock Settings", GetIconForTitle("Lock Settings"));
            lockSettingsHeader.Dock = DockStyle.Top;
            lockSettingsHeader.Margin = new Padding(10, 0, 0, 0);
            contentPanel.Controls.Add(lockSettingsHeader);

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Prevent changes to settings until 5:00 PM ET.",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(10, 0, 10, 0),
                BackColor = DarkBackground,
                ForeColor = TextGray,
                AutoSize = false
            };

            // Account Number Display - shows which account settings will be locked
            var settingsAccountDisplay = new Label
            {
                Text = "Account: Not Selected",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding = new Padding(10, 5, 10, 0),
                BackColor = CardBackground,
                ForeColor = TextWhite,
                AutoSize = false,
                BorderStyle = BorderStyle.FixedSingle,
                Tag = "LockAccountDisplay" // Tag for identification - will be updated by UpdateLockAccountDisplay
            };

            var contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBackground,
                Padding = new Padding(15),
                AutoScroll = true
            };

            // Status label to show lock state with color and remaining time
            var lblSettingsStatus = new Label
            {
                Text = "Settings Unlocked",
                Left = 0,
                Top = 0,
                Width = 400,
                Height = 30,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = AccentGreen,
                BackColor = CardBackground,
                TextAlign = ContentAlignment.MiddleLeft,
                Tag = "SettingsStatus" // Tag for identification
            };
            contentArea.Controls.Add(lblSettingsStatus);

            // Lock Settings Button
            lockSettingsButton = new Button
            {
                Text = "LOCK SETTINGS FOR REST OF DAY (Until 5 PM ET)",
                Width = 400,
                Height = 40,
                Left = 0,
                Top = 50,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = AccentAmber,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            lockSettingsButton.FlatAppearance.BorderSize = 0;
            lockSettingsButton.Click += (s, e) => {
                try
                {
                    // Use the cached account number to ensure we lock exactly what's displayed
                    var accountNumber = displayedAccountNumber;
                    if (string.IsNullOrEmpty(accountNumber))
                    {
                        MessageBox.Show("Please select an account first.", "No Account Selected", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var settingsService = RiskManagerSettingsService.Instance;
                    if (!settingsService.IsInitialized)
                    {
                        MessageBox.Show("Settings service is not initialized.", "Service Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Calculate duration until 5 PM ET
                    var duration = RiskManagerSettingsService.CalculateDurationUntil5PMET();
                    
                    // Show confirmation dialog
                    var confirmResult = MessageBox.Show(
                        $"Are you sure you want to lock settings for account '{accountNumber}' until 5:00 PM ET?\n\n" +
                        $"Duration: {duration.Hours}h {duration.Minutes}m\n\n" +
                        "While locked, you will not be able to modify:\n" +
                        "• Daily/Weekly Limits\n" +
                        "• Position Limits\n" +
                        "• Blocked Symbols\n" +
                        "• Contract Limits\n" +
                        "• Trading Time Restrictions\n" +
                        "• Feature Toggles\n\n" +
                        "You can still lock/unlock trading during this time.",
                        "Confirm Lock Settings",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);
                    
                    if (confirmResult != DialogResult.Yes)
                    {
                        return; // User cancelled
                    }
                    
                    // Lock settings with calculated duration
                    settingsService.SetSettingsLock(accountNumber, true, "Locked until 5 PM ET", duration);
                    
                    // Update status display for the account we just locked
                    UpdateSettingsLockStatusForAccount(lblSettingsStatus, accountNumber);
                    
                    // Update controls and navigation tabs to disable them immediately (using explicit account)
                    UpdateSettingsControlsEnabledState(accountNumber);
                    
                    MessageBox.Show($"Settings locked until 5:00 PM ET.\nDuration: {duration.Hours}h {duration.Minutes}m", 
                        "Settings Locked", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error locking settings: {ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    System.Diagnostics.Debug.WriteLine($"Error in Lock Settings button: {ex}");
                }
            };
            contentArea.Controls.Add(lockSettingsButton);

            // Unlock Settings Button
            unlockSettingsButton = new Button
            {
                Text = "UNLOCK SETTINGS",
                Width = 200,
                Height = 40,
                Left = 0,
                Top = 110,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = AccentGreen,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            unlockSettingsButton.FlatAppearance.BorderSize = 0;
            unlockSettingsButton.Click += (s, e) => {
                try
                {
                    // Use the cached account number to ensure we unlock exactly what's displayed
                    var accountNumber = displayedAccountNumber;
                    if (string.IsNullOrEmpty(accountNumber))
                    {
                        MessageBox.Show("Please select an account first.", "No Account Selected", 
                            MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }

                    var settingsService = RiskManagerSettingsService.Instance;
                    if (!settingsService.IsInitialized)
                    {
                        MessageBox.Show("Settings service is not initialized.", "Service Error", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    // Unlock settings
                    settingsService.SetSettingsLock(accountNumber, false, "Manually unlocked");
                    
                    // Update status display for the account we just unlocked
                    UpdateSettingsLockStatusForAccount(lblSettingsStatus, accountNumber);
                    
                    // Update controls and navigation tabs to enable them immediately (using explicit account)
                    UpdateSettingsControlsEnabledState(accountNumber);
                    
                    MessageBox.Show("Settings unlocked successfully.", "Settings Unlocked", 
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error unlocking settings: {ex.Message}", "Error", 
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    System.Diagnostics.Debug.WriteLine($"Error in Unlock Settings button: {ex}");
                }
            };
            contentArea.Controls.Add(unlockSettingsButton);

            // Initialize status from settings service
            UpdateSettingsLockStatus(lblSettingsStatus);

            // Add controls in correct order: Fill second, Top last
            // In WinForms, docking is processed in reverse Z-order
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(settingsAccountDisplay);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(lockSettingsHeader);
            
            // Initialize the account display
            UpdateLockAccountDisplay(settingsAccountDisplay);

            return mainPanel;
        }

        private Control CreateManualLockDarkPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title
            var manualLockHeader = new CustomHeaderControl("Manual Lock", GetIconForTitle("Manual Lock"));
            manualLockHeader.Dock = DockStyle.Top;
            manualLockHeader.Margin = new Padding(10, 0, 0, 0);
            contentPanel.Controls.Add(manualLockHeader);

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Manually lock or unlock trading with optional duration.",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(10, 0, 10, 0),
                BackColor = DarkBackground,
                ForeColor = TextGray,
                AutoSize = false
            };

            // Account Number Display - shows which account will be locked/unlocked
            var lockAccountDisplay = new Label
            {
                Text = "Account: Not Selected",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding = new Padding(10, 5, 10, 0),
                BackColor = CardBackground,
                ForeColor = TextWhite,
                AutoSize = false,
                BorderStyle = BorderStyle.FixedSingle,
                Tag = "LockAccountDisplay" // Tag for identification
            };

            var contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBackground,
                Padding = new Padding(15),
                AutoScroll = true
            };

            // Status label to show lock state with color and remaining time
            var lblManualLockStatus = new Label
            {
                Text = "Unlocked",
                Left = 0,
                Top = 0,
                Width = 400,
                Height = 30,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = AccentGreen,
                BackColor = CardBackground,
                TextAlign = ContentAlignment.MiddleLeft,
                Tag = "ManualLockStatus" // Tag for identification
            };
            contentArea.Controls.Add(lblManualLockStatus);

            // Lock Duration Section
            var durationLabel = new Label
            {
                Text = "Lock Duration:",
                Left = 0,
                Top = 50,
                Width = 150,
                Height = 25,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                TextAlign = ContentAlignment.MiddleLeft
            };
            contentArea.Controls.Add(durationLabel);

            lockDurationComboBox = new ComboBox
            {
                Left = 160,
                Top = 50,
                Width = 200,
                Height = 25,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                BackColor = DarkerBackground,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat
            };

            // Add duration options (removed Indefinite per user request)
            lockDurationComboBox.Items.Add("5 Minutes");
            lockDurationComboBox.Items.Add("15 Minutes");
            lockDurationComboBox.Items.Add("1 Hour");
            lockDurationComboBox.Items.Add("2 Hours");
            lockDurationComboBox.Items.Add("4 Hours");
            lockDurationComboBox.Items.Add("All Day (Until 5PM ET)");
            lockDurationComboBox.Items.Add("All Week (Until 5PM ET Friday)");
            lockDurationComboBox.SelectedIndex = 0; // Default to 5 Minutes

            contentArea.Controls.Add(lockDurationComboBox);

            // Buttons section
            var lockButton = new Button
            {
                Text = "LOCK TRADING",
                Width = 200,
                Height = 40,
                Left = 0,
                Top = 100,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = AccentAmber,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            lockButton.FlatAppearance.BorderSize = 0;
            lockButton.Click += BtnLock_Click;
            lockTradingButton = lockButton; // Store reference
            contentArea.Controls.Add(lockButton);

            var unlockButton = new Button
            {
                Text = "UNLOCK TRADING",
                Width = 200,
                Height = 40,
                Left = 220,
                Top = 100,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = AccentGreen,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            unlockButton.FlatAppearance.BorderSize = 0;
            unlockButton.Click += BtnUnlock_Click;
            unlockTradingButton = unlockButton; // Store reference
            contentArea.Controls.Add(unlockButton);

            // Update account display on panel creation
            UpdateLockAccountDisplay(lockAccountDisplay);
            
            // Update button states based on current lock status
            UpdateLockButtonStates();
            
            // Initialize status from settings service
            UpdateManualLockStatus(lblManualLockStatus);

            // Add controls in correct order: Fill first, then Top
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(lockAccountDisplay);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(manualLockHeader);

            return mainPanel;
        }

        private void UpdateAllLockAccountDisplays()
        {
            try
            {
                // Find all LockAccountDisplay labels in the control tree and update them
                UpdateLockAccountDisplaysRecursive(this);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating lock account displays: {ex.Message}");
            }
        }

        private void UpdateLockAccountDisplaysRecursive(Control parent)
        {
            if (parent == null) return;
            
            foreach (Control control in parent.Controls)
            {
                if (control is Label label && label.Tag?.ToString() == "LockAccountDisplay")
                {
                    UpdateLockAccountDisplay(label);
                }
                
                // Recursively check child controls
                if (control.Controls.Count > 0)
                {
                    UpdateLockAccountDisplaysRecursive(control);
                }
            }
        }

        private void UpdateLockAccountDisplay(Label lockAccountDisplay)
        {
            try
            {
                if (lockAccountDisplay == null)
                    return;
                
                var accountNumber = GetSelectedAccountNumber();
                
                // Cache the account number so lock/unlock operations use exactly what's displayed
                displayedAccountNumber = accountNumber;
                
                if (string.IsNullOrEmpty(accountNumber))
                {
                    lockAccountDisplay.Text = "Account: Not Selected";
                    lockAccountDisplay.ForeColor = Color.Orange;
                }
                else
                {
                    lockAccountDisplay.Text = $"Account: {accountNumber}";
                    lockAccountDisplay.ForeColor = TextWhite;
                }
                
                System.Diagnostics.Debug.WriteLine($"UpdateLockAccountDisplay: Displaying and caching account='{accountNumber}'");
                lockAccountDisplay.Invalidate();
                
                // Update button states after account display changes
                UpdateLockButtonStates();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating lock account display: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the enabled/disabled state of lock and unlock buttons based on current lock status.
        /// Grey out Lock button if already locked, grey out Unlock button if already unlocked.
        /// </summary>
        private void UpdateLockButtonStates()
        {
            try
            {
                if (lockTradingButton == null || unlockTradingButton == null)
                    return;

                var accountNumber = GetSelectedAccountNumber();
                if (string.IsNullOrEmpty(accountNumber))
                {
                    // No account selected - disable both buttons
                    lockTradingButton.Enabled = false;
                    unlockTradingButton.Enabled = false;
                    return;
                }

                var settingsService = RiskManagerSettingsService.Instance;
                if (!settingsService.IsInitialized)
                {
                    // Service not initialized - enable both buttons as fallback
                    lockTradingButton.Enabled = true;
                    unlockTradingButton.Enabled = true;
                    return;
                }

                bool isLocked = settingsService.IsTradingLocked(accountNumber);

                // If locked: disable Lock button, enable Unlock button
                // If unlocked: enable Lock button, disable Unlock button
                lockTradingButton.Enabled = !isLocked;
                unlockTradingButton.Enabled = isLocked;
                
                System.Diagnostics.Debug.WriteLine($"UpdateLockButtonStates: account='{accountNumber}', isLocked={isLocked}, Lock.Enabled={lockTradingButton.Enabled}, Unlock.Enabled={unlockTradingButton.Enabled}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating lock button states: {ex.Message}");
            }
        }

        /// <summary>
        /// Finds an account by its unique identifier.
        /// </summary>
        /// <param name="accountNumber">The unique account identifier to search for</param>
        /// <returns>The matching account, or null if not found</returns>
        private Account FindAccountByIdentifier(string accountNumber)
        {
            var core = Core.Instance;
            if (core == null || core.Accounts == null)
                return null;

            int accountIndex = 0;
            foreach (var account in core.Accounts)
            {
                if (account == null)
                {
                    accountIndex++;
                    continue;
                }
                
                var uniqueAccountId = GetUniqueAccountIdentifier(account, accountIndex);
                if (uniqueAccountId == accountNumber)
                {
                    return account;
                }
                accountIndex++;
            }

            return null;
        }

        private void BtnLock_Click(object sender, EventArgs e)
        {
            try
            {
                // Use the cached account number to find the correct account
                var accountNumber = displayedAccountNumber;
                if (string.IsNullOrEmpty(accountNumber))
                {
                    MessageBox.Show("Please select an account first.", "No Account Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Get selected duration
                TimeSpan? duration = GetSelectedLockDuration();
                string durationText = lockDurationComboBox?.SelectedItem?.ToString() ?? "Unknown";

                // Show confirmation dialog
                var confirmResult = MessageBox.Show(
                    $"Are you sure you want to lock account '{accountNumber}' for {durationText}?\n\n" +
                    "This will close all open positions for this account and disable all Buy/Sell buttons until the lock expires.",
                    "Confirm Lock Trading",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (confirmResult != DialogResult.Yes)
                {
                    return; // User cancelled
                }

                // Find the account by the cached identifier
                var core = Core.Instance;
                if (core == null)
                {
                    MessageBox.Show("Core instance not available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var targetAccount = FindAccountByIdentifier(accountNumber);
                if (targetAccount == null)
                {
                    MessageBox.Show($"Could not find account: {accountNumber}", "Account Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Close all positions for this account before locking
                CloseAllPositionsForAccount(targetAccount, core);

                // Check if the method exists before calling (defensive programming)
                var lockMethod = core.GetType().GetMethod("LockAccount");
                if (lockMethod != null)
                {
                    lockMethod.Invoke(core, new object[] { targetAccount });
                    
                    // Update the settings service to track the lock status with duration
                    var settingsService = RiskManagerSettingsService.Instance;
                    if (settingsService.IsInitialized)
                    {
                        string reason = $"Manual lock via Lock Trading button for {durationText}";
                        System.Diagnostics.Debug.WriteLine($"LockTradingButton: Locking account='{accountNumber}' for {duration?.TotalMinutes ?? 0} minutes");
                        
                        // Enhanced audit logging for admin lock action
                        System.Diagnostics.Debug.WriteLine($"[ADMIN ACTION] Account: {accountNumber}, " +
                            $"Action: Manual Lock, " +
                            $"Duration: {durationText}, " +
                            $"User: Admin, " +
                            $"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC, " +
                            $"Reason: {reason}");
                        
                        settingsService.SetTradingLock(accountNumber, true, reason, duration);
                    }
                    
                    // Update the trading status badge by reading from settings service
                    // This ensures the badge reflects the actual persisted state
                    UpdateTradingStatusBadge();
                    
                    // Update button states - Lock button should now be disabled
                    // Do this BEFORE refresh to avoid race conditions
                    UpdateLockButtonStates();
                    
                    RefreshAccountsSummary();
                    RefreshAccountStats();
                    
                    // Update Manual Lock status labels immediately
                    UpdateManualLockStatusLabelsRecursive(this);
                    
                    MessageBox.Show(
                        $"Account '{accountNumber}' has been locked for {durationText}.\n\nBuy/Sell buttons are now disabled.",
                        "Trading Locked",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("LockAccount method not available in Core API.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error locking account: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Error locking the account: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnUnlock_Click(object sender, EventArgs e)
        {
            try
            {
                // Use the cached account number to find the correct account
                var accountNumber = displayedAccountNumber;
                if (string.IsNullOrEmpty(accountNumber))
                {
                    MessageBox.Show("Please select an account first.", "No Account Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Find the account by the cached identifier
                var core = Core.Instance;
                if (core == null)
                {
                    MessageBox.Show("Core instance not available.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                var targetAccount = FindAccountByIdentifier(accountNumber);
                if (targetAccount == null)
                {
                    MessageBox.Show($"Could not find account: {accountNumber}", "Account Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Check if the method exists before calling (defensive programming)
                var unlockMethod = core.GetType().GetMethod("UnLockAccount");
                if (unlockMethod != null)
                {
                    unlockMethod.Invoke(core, new object[] { targetAccount });
                    
                    // Update the settings service to track the unlock status
                    var settingsService = RiskManagerSettingsService.Instance;
                    if (settingsService.IsInitialized)
                    {
                        settingsService.SetTradingLock(accountNumber, false, "Manual unlock via Unlock Trading button");
                        
                        // Enhanced audit logging for admin override
                        System.Diagnostics.Debug.WriteLine($"[ADMIN OVERRIDE] Account: {accountNumber}, " +
                            $"Action: Manual Unlock, " +
                            $"User: Admin, " +
                            $"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC, " +
                            $"Reason: Manual override via Unlock Trading button");
                    }
                    
                    // Update the trading status badge by reading from settings service
                    // This ensures the badge reflects the actual persisted state
                    UpdateTradingStatusBadge();
                    
                    // Update button states - Unlock button should now be disabled
                    // Do this BEFORE refresh to avoid race conditions
                    UpdateLockButtonStates();
                    
                    RefreshAccountsSummary();
                    RefreshAccountStats();
                    
                    // Update Manual Lock status labels immediately
                    UpdateManualLockStatusLabelsRecursive(this);
                    
                    MessageBox.Show($"Account '{accountNumber}' has been unlocked successfully. Buy/Sell buttons are now enabled.", "Trading Unlocked", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("UnLockAccount method not available in Core API.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error unlocking account: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Error unlocking the account: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the Lock All Accounts button click event.
        /// Locks all connected accounts (both trading and settings) until 5PM EST with a single confirmation.
        /// </summary>
        private void BtnLockAllAccounts_Click(object sender, EventArgs e)
        {
            try
            {
                var core = Core.Instance;
                if (core == null || core.Accounts == null || !core.Accounts.Any())
                {
                    MessageBox.Show("No accounts available to lock.", "No Accounts", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                // Show confirmation dialog
                var confirmResult = MessageBox.Show(
                    "Are you sure you want to lock ALL accounts until 5PM ET?\n\n" +
                    "This will:\n" +
                    "• Flatten all open trades\n" +
                    "• Disable all Buy/Sell buttons\n" +
                    "• Lock all settings (limits, symbols, toggles, etc.)\n\n" +
                    "Both locks will remain until 5PM ET today.",
                    "Confirm Lock All Accounts",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirmResult != DialogResult.Yes)
                {
                    return; // User cancelled
                }

                // Flatten all trades before locking accounts
                FlattenAllTrades();

                var settingsService = RiskManagerSettingsService.Instance;
                if (!settingsService.IsInitialized)
                {
                    MessageBox.Show("Settings service not initialized.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Get lock duration for "All Day (Until 5PM ET)"
                TimeSpan? duration = GetLockDurationForAllDay();
                string durationText = "All Day (Until 5PM ET)";

                // Check if the LockAccount method exists
                var lockMethod = core.GetType().GetMethod("LockAccount");
                if (lockMethod == null)
                {
                    MessageBox.Show("LockAccount method not available in Core API.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                int lockedCount = 0;
                int accountIndex = 0;

                // Lock all accounts (both trading and settings)
                foreach (var account in core.Accounts)
                {
                    if (account == null)
                    {
                        accountIndex++;
                        continue;
                    }

                    // Generate account identifier
                    string accountNumber = GetUniqueAccountIdentifier(account, accountIndex);

                    try
                    {
                        // Lock the account trading via Core API
                        lockMethod.Invoke(core, new object[] { account });

                        // Update the settings service to track the trading lock status
                        string tradingReason = $"Lock All Accounts button - {durationText}";
                        settingsService.SetTradingLock(accountNumber, true, tradingReason, duration);

                        // Also lock the settings for this account
                        string settingsReason = $"Lock All Accounts button - {durationText}";
                        settingsService.SetSettingsLock(accountNumber, true, settingsReason, duration);

                        System.Diagnostics.Debug.WriteLine($"[LOCK ALL] Locked account (trading & settings): {accountNumber} for {durationText}");
                        lockedCount++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to lock account {accountNumber}: {ex.Message}");
                    }

                    accountIndex++;
                }

                // Play success sound
                PlayLockSound();

                // Update UI
                UpdateTradingStatusBadge();
                UpdateSettingsStatusBadge();
                UpdateLockButtonStates();
                RefreshAccountsSummary();
                RefreshAccountStats();
                UpdateManualLockStatusLabelsRecursive(this);
                UpdateSettingsControlsEnabledState();

                // Show success message
                MessageBox.Show(
                    $"Successfully locked {lockedCount} account(s) until 5PM ET.\n\n" +
                    "All Buy/Sell buttons and settings are now locked.",
                    "Accounts Locked",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error locking all accounts: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Error locking accounts: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Handles the Shutdown button click event.
        /// Locks all accounts and closes the application after a countdown with cancel option.
        /// </summary>
        private void ShutdownButton_Click(object sender, EventArgs e)
        {
            try
            {
                // Show confirmation dialog
                var confirmResult = MessageBox.Show(
                    "Are you sure you want to lock all accounts, settings, and FORCEFULLY close the application?",
                    "Confirm Shutdown",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (confirmResult != DialogResult.Yes)
                {
                    return; // User cancelled
                }

                // Execute lock all accounts logic - call with null sender for separation of concerns
                BtnLockAllAccounts_Click(null, EventArgs.Empty);

                // Play the leave-get-out sound
                PlayShutdownSound();

                // Show countdown dialog with cancel option
                ShowShutdownCountdown();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error in ShutdownButton_Click: {ex.Message}\n{ex.StackTrace}");
                MessageBox.Show($"Error during shutdown: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>
        /// Plays the leave-get-out.wav sound effect for shutdown.
        /// </summary>
        private void PlayShutdownSound()
        {
            try
            {
                var audioStream = Properties.Resources.leave_get_out;
                if (audioStream != null)
                {
                    // Dispose existing shutdown sound player if any
                    shutdownSoundPlayer?.Dispose();

                    // Create and store the player as a field to prevent premature garbage collection
                    shutdownSoundPlayer = new SoundPlayer(audioStream);
                    // Play asynchronously - sound plays in background without blocking
                    shutdownSoundPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing shutdown sound: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows a countdown dialog before closing the application with option to cancel.
        /// </summary>
        private void ShowShutdownCountdown()
        {
            shutdownCountdownSeconds = 5;

            // Create countdown form
            shutdownCountdownForm = new Form
            {
                Text = "Shutting Down...",
                Width = 400,
                Height = 180,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                StartPosition = FormStartPosition.CenterScreen,
                MaximizeBox = false,
                MinimizeBox = false,
                BackColor = CardBackground,
                ForeColor = TextWhite,
                TopMost = true  // Keep on top
            };

            var countdownLabel = new Label
            {
                Text = $"Application will close in {shutdownCountdownSeconds} seconds...",
                AutoSize = false,
                Width = 360,
                Height = 60,
                Location = new Point(20, 20),
                Font = new Font("Segoe UI", 11, FontStyle.Regular),
                ForeColor = TextWhite,
                TextAlign = ContentAlignment.MiddleCenter
            };
            shutdownCountdownForm.Controls.Add(countdownLabel);

            var cancelButton = new Button
            {
                Text = "Cancel Shutdown",
                Width = 150,
                Height = 35,
                Location = new Point(125, 90),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Font = new Font("Segoe UI", 10, FontStyle.Bold)
            };
            cancelButton.FlatAppearance.BorderSize = 0;
            cancelButton.Click += (s, e) =>
            {
                try
                {
                    shutdownTimer?.Stop();
                    shutdownTimer?.Dispose();
                    shutdownTimer = null;
                    
                    shutdownCountdownForm?.Close();
                    shutdownCountdownForm?.Dispose();
                    shutdownCountdownForm = null;
                    
                    MessageBox.Show("Shutdown cancelled.", "Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error cancelling shutdown: {ex.Message}");
                }
            };
            shutdownCountdownForm.Controls.Add(cancelButton);

            // Create timer for countdown
            shutdownTimer = new System.Windows.Forms.Timer
            {
                Interval = 1000 // 1 second
            };

            shutdownTimer.Tick += (s, e) =>
            {
                try
                {
                    shutdownCountdownSeconds--;
                    if (countdownLabel != null && !countdownLabel.IsDisposed)
                    {
                        countdownLabel.Text = $"Application will close in {shutdownCountdownSeconds} seconds...";
                    }

                    if (shutdownCountdownSeconds <= 0)
                    {
                        shutdownTimer?.Stop();
                        shutdownTimer?.Dispose();
                        shutdownTimer = null;
                        
                        shutdownCountdownForm?.Close();
                        shutdownCountdownForm?.Dispose();
                        shutdownCountdownForm = null;

                        // Close the application gracefully
                        var parentForm = this.FindForm();
                        if (parentForm != null)
                        {
                            // Use BeginInvoke to allow the form to close gracefully on the UI thread
                            parentForm.BeginInvoke(new Action(() => 
                            {
                                try
                                {
                                    parentForm.Close();
                                }
                                catch (Exception closeEx)
                                {
                                    System.Diagnostics.Debug.WriteLine($"Error closing parent form: {closeEx.Message}");
                                    // Last resort - forcefully exit the application
                                    Environment.Exit(0);

                                }
                            }));
                        }
                        else
                        {
                            // If we can't find the form, properly exit the application
                            Environment.Exit(0);

                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error in shutdown timer: {ex.Message}");
                    shutdownTimer?.Stop();
                    shutdownTimer?.Dispose();
                    shutdownCountdownForm?.Close();
                    shutdownCountdownForm?.Dispose();
                }
            };

            // Handle form closing to cleanup timer
            shutdownCountdownForm.FormClosing += (s, e) =>
            {
                shutdownTimer?.Stop();
                shutdownTimer?.Dispose();
                shutdownTimer = null;
            };

            shutdownTimer.Start();
            shutdownCountdownForm.Show();
        }

        /// <summary>
        /// Calculates the lock duration for "All Day (Until 5PM ET)".
        /// </summary>
        private TimeSpan? GetLockDurationForAllDay()
        {
            try
            {
                // Get current time in Eastern Time
                // Note: "Eastern Standard Time" is used for consistency with existing code throughout the application.
                // This timezone ID works on Windows and handles both EST and EDT automatically.
                TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                DateTime nowEt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone);

                // Calculate time until 5 PM ET today (or tomorrow if past 5 PM ET)
                var targetTime = new DateTime(nowEt.Year, nowEt.Month, nowEt.Day, 17, 0, 0); // 5 PM ET today
                if (nowEt >= targetTime)
                {
                    // If past 5 PM ET, lock until 5 PM ET tomorrow
                    targetTime = targetTime.AddDays(1);
                }
                return targetTime - nowEt;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating lock duration: {ex.Message}");
                return TimeSpan.FromHours(8); // Fallback to 8 hours
            }
        }

        /// <summary>
        /// Plays the metal clink sound effect for lock confirmation.
        /// </summary>
        private void PlayLockSound()
        {
            try
            {
                var audioStream = Properties.Resources.metal_clink;
                if (audioStream != null)
                {
                    // Dispose existing player if any
                    alertSoundPlayer?.Dispose();

                    // Create and store the player as a field to prevent premature garbage collection
                    alertSoundPlayer = new SoundPlayer(audioStream);
                    // Play asynchronously - sound plays in background without blocking
                    alertSoundPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error playing lock sound: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to set Core.TradingStatus property using reflection.
        /// </summary>
        /// <param name="core">The Core instance</param>
        /// <param name="statusValue">The status value to set (e.g., "Locked" or "Allowed")</param>
        private void SetCoreTradingStatus(object core, string statusValue)
        {
            try
            {
                var tradingStatusProperty = core.GetType().GetProperty("TradingStatus");
                if (tradingStatusProperty != null && tradingStatusProperty.CanWrite)
                {
                    // Get TradingStatus enum type
                    var tradingStatusType = tradingStatusProperty.PropertyType;
                    
                    // Try to parse the enum value (case-insensitive)
                    try
                    {
                        var enumValue = Enum.Parse(tradingStatusType, statusValue, ignoreCase: true);
                        tradingStatusProperty.SetValue(core, enumValue);
#if DEBUG
                        System.Diagnostics.Debug.WriteLine($"Set Core.TradingStatus to {statusValue}");
#endif
                    }
                    catch (ArgumentException)
                    {
                        System.Diagnostics.Debug.WriteLine($"'{statusValue}' value not found in TradingStatus enum");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error setting TradingStatus: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the selected lock duration from the combo box.
        /// All calculations are based on Eastern Time (ET).
        /// For short duration locks (5 min, 15 min, 1 hour, 4 hours), the lock will automatically 
        /// unlock at 5 PM ET if the calculated expiration would be after 5 PM ET.
        /// </summary>
        private TimeSpan? GetSelectedLockDuration()
        {
            if (lockDurationComboBox == null || lockDurationComboBox.SelectedItem == null)
                return null;

            string selection = lockDurationComboBox.SelectedItem.ToString();
            
            // Get Eastern Time Zone
            TimeZoneInfo easternZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
            DateTime nowEt = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, easternZone);
            DateTime fivePmEtToday = new DateTime(nowEt.Year, nowEt.Month, nowEt.Day, 17, 0, 0); // 5 PM ET today
            
            switch (selection)
            {
                case "5 Minutes":
                case "15 Minutes":
                case "1 Hour":
                case "4 Hours":
                    // Calculate the normal duration
                    TimeSpan normalDuration;
                    switch (selection)
                    {
                        case "5 Minutes":
                            normalDuration = TimeSpan.FromMinutes(5);
                            break;
                        case "15 Minutes":
                            normalDuration = TimeSpan.FromMinutes(15);
                            break;
                        case "1 Hour":
                            normalDuration = TimeSpan.FromHours(1);
                            break;
                        case "4 Hours":
                            normalDuration = TimeSpan.FromHours(4);
                            break;
                        default:
                            normalDuration = TimeSpan.Zero;
                            break;
                    }
                    
                    // Calculate when the lock would normally expire
                    DateTime normalExpiration = nowEt.Add(normalDuration);
                    
                    // If normal expiration is after 5 PM ET today, cap it at 5 PM ET today
                    if (normalExpiration > fivePmEtToday && nowEt < fivePmEtToday)
                    {
                        // Lock until 5 PM ET today instead
                        return fivePmEtToday - nowEt;
                    }
                    else
                    {
                        // Use normal duration
                        return normalDuration;
                    }
                    
                case "2 Hours":
                    return TimeSpan.FromHours(2);
                    
                case "All Day (Until 5PM ET)":
                    // Calculate time until 5 PM ET today (or tomorrow if past 5 PM ET)
                    var targetTime = new DateTime(nowEt.Year, nowEt.Month, nowEt.Day, 17, 0, 0); // 5 PM ET today
                    if (nowEt >= targetTime)
                    {
                        // If past 5 PM ET, lock until 5 PM ET tomorrow
                        targetTime = targetTime.AddDays(1);
                    }
                    return targetTime - nowEt;
                    
                case "All Week (Until 5PM ET Friday)":
                    // Lock until 5 PM ET Friday
                    int daysUntilFriday = ((int)DayOfWeek.Friday - (int)nowEt.DayOfWeek + 7) % 7;
                    DateTime fridayAt5PM;
                    
                    if (daysUntilFriday == 0)
                    {
                        // Today is Friday
                        fridayAt5PM = new DateTime(nowEt.Year, nowEt.Month, nowEt.Day, 17, 0, 0);
                        if (nowEt >= fridayAt5PM)
                        {
                            // Already past 5 PM Friday, lock until next Friday at 5 PM ET
                            fridayAt5PM = fridayAt5PM.AddDays(7);
                        }
                    }
                    else
                    {
                        // Calculate this coming Friday at 5 PM ET
                        fridayAt5PM = nowEt.AddDays(daysUntilFriday).Date.AddHours(17);
                    }
                    
                    return fridayAt5PM - nowEt;
                default:
                    return null; // Fallback, should not happen
            }
        }

        /// <summary>
        /// Checks all accounts for expired locks and triggers auto-unlock if needed.
        /// Also enforces lock status to prevent manual override.
        /// Called every second by the lockExpirationCheckTimer.
        /// </summary>
        private void CheckExpiredLocks()
        {
            try
            {
                var core = Core.Instance;
                if (core == null || core.Accounts == null)
                    return;

                var settingsService = RiskManagerSettingsService.Instance;
                if (!settingsService.IsInitialized)
                    return;

                int accountIndex = 0;
                bool anyUnlocked = false;
                bool anyLocked = false;
                bool anySettingsUnlocked = false; // Track if any settings locks expired
                bool selectedAccountChanged = false; // Track if the selected account's state changed

                // Get the currently selected account to check if its state changed
                var selectedAccountNumber = GetSelectedAccountNumber();

                foreach (var account in core.Accounts)
                {
                    if (account == null)
                    {
                        accountIndex++;
                        continue;
                    }

                    var uniqueAccountId = GetUniqueAccountIdentifier(account, accountIndex);
                    if (!string.IsNullOrEmpty(uniqueAccountId))
                    {
                        // Get current settings before checking lock status
                        var settings = settingsService.GetSettings(uniqueAccountId);
                        var wasLocked = settings?.TradingLock?.IsLocked == true;
                        var wasSettingsLocked = settingsService.AreSettingsLocked(uniqueAccountId);
                        
                        // IsTradingLocked checks expiration and auto-unlocks if expired
                        var isLocked = settingsService.IsTradingLocked(uniqueAccountId);
                        
                        // AreSettingsLocked checks expiration and auto-unlocks if expired
                        var isSettingsLocked = settingsService.AreSettingsLocked(uniqueAccountId);
                        
                        // If was locked but now unlocked, the lock expired and was auto-unlocked
                        if (wasLocked && !isLocked)
                        {
                            anyUnlocked = true;
                            
                            // Check if this is the selected account
                            if (!string.IsNullOrEmpty(selectedAccountNumber) && selectedAccountNumber == uniqueAccountId)
                            {
                                selectedAccountChanged = true;
                            }
                            
                            // Unlock the account in Core API
                            try
                            {
                                var unlockMethod = core.GetType().GetMethod("UnLockAccount");
                                if (unlockMethod != null)
                                {
                                    unlockMethod.Invoke(core, new object[] { account });
                                    System.Diagnostics.Debug.WriteLine($"Auto-unlocked account: {uniqueAccountId}");
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error auto-unlocking account {uniqueAccountId}: {ex.Message}");
                            }
                        }
                        // If still locked, enforce the lock to prevent manual override
                        else if (isLocked)
                        {
                            anyLocked = true;
                            
                            // Check if this is the selected account and it's being locked
                            // (Note: we only set selectedAccountChanged for unlocking, not for enforcing existing locks)
                            
                            // Ensure the account remains locked in Core API
                            try
                            {
                                var lockMethod = core.GetType().GetMethod("LockAccount");
                                if (lockMethod != null)
                                {
                                    lockMethod.Invoke(core, new object[] { account });
                                }
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"Error enforcing lock on account {uniqueAccountId}: {ex.Message}");
                            }
                        }
                        
                        // Check if settings lock expired
                        if (wasSettingsLocked && !isSettingsLocked)
                        {
                            anySettingsUnlocked = true;
                            
                            // Check if this is the selected account
                            if (!string.IsNullOrEmpty(selectedAccountNumber) && selectedAccountNumber == uniqueAccountId)
                            {
                                selectedAccountChanged = true;
                                System.Diagnostics.Debug.WriteLine($"Settings lock expired for selected account: {uniqueAccountId}");
                            }
                        }
                    }

                    accountIndex++;
                }

                // Update button states only if the selected account's state changed
                if (selectedAccountChanged)
                {
                    UpdateLockButtonStates();
                }
                
                // Refresh account summary and stats displays if any account changed
                if (anyUnlocked)
                {
                    RefreshAccountsSummary();
                    RefreshAccountStats();
                    
                    // Update Manual Lock status labels when trading lock expires
                    UpdateManualLockStatusLabelsRecursive(this);
                }
                
                // Update settings lock status display if any settings locks expired
                if (anySettingsUnlocked)
                {
                    // Find and update all SettingsStatus labels
                    UpdateSettingsStatusLabelsRecursive(this);
                    
                    // Update navigation tabs enabled state for the selected account
                    if (!string.IsNullOrEmpty(selectedAccountNumber))
                    {
                        UpdateSettingsControlsEnabledState();
                    }
                    
                    // Explicitly update the Settings Lock Badge if the selected account's settings were unlocked
                    if (selectedAccountChanged)
                    {
                        // Reuse settingsService from outer scope (line 3535)
                        if (settingsService.IsInitialized)
                        {
                            UpdateSettingsStatusBadge();
                            System.Diagnostics.Debug.WriteLine($"CheckExpiredLocks: Explicitly updated Settings Lock Badge for account '{selectedAccountNumber}'");
                        }
                    }
                }
                
                // Update badge only if the selected account changed
                if (selectedAccountChanged && !string.IsNullOrEmpty(selectedAccountNumber))
                {
                    System.Diagnostics.Debug.WriteLine($"CheckExpiredLocks: Selected account '{selectedAccountNumber}' state changed");
                    // NOTE: We do NOT update the badge here. The badge should only update from:
                    // 1. Manual lock/unlock button clicks
                    // 2. Account selection changes (LoadAccountSettings)
                    // The badge reads directly from JSON, so it will always show the current state
                    // without needing timer-triggered updates.
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking expired locks: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Recursively finds and updates all SettingsStatus labels in the control tree.
        /// </summary>
        private void UpdateSettingsStatusLabelsRecursive(Control parent)
        {
            if (parent == null) return;
            
            foreach (Control control in parent.Controls)
            {
                if (control is Label label && label.Tag?.ToString() == "SettingsStatus")
                {
                    UpdateSettingsLockStatus(label);
                }
                
                // Recursively check child controls
                if (control.Controls.Count > 0)
                {
                    UpdateSettingsStatusLabelsRecursive(control);
                }
            }
        }

        /// <summary>
        /// Recursively finds and updates all ManualLockStatus labels in the control tree.
        /// </summary>
        private void UpdateManualLockStatusLabelsRecursive(Control parent)
        {
            if (parent == null) return;
            
            foreach (Control control in parent.Controls)
            {
                if (control is Label label && label.Tag?.ToString() == "ManualLockStatus")
                {
                    UpdateManualLockStatus(label);
                }
                
                // Recursively check child controls
                if (control.Controls.Count > 0)
                {
                    UpdateManualLockStatusLabelsRecursive(control);
                }
            }
        }

        /// <summary>
        /// Monitors P&L limits for all accounts and enforces automatic lockouts and position closures.
        /// Checks daily P&L against daily limits and position P&L against position limits.
        /// Stops monitoring from 4:59 PM to 6:00 PM ET to prevent re-locking accounts at market close.
        /// </summary>
        private void MonitorPnLLimits()
        {
            try
            {
                var core = Core.Instance;
                if (core == null || core.Accounts == null)
                    return;

                var settingsService = RiskManagerSettingsService.Instance;
                if (!settingsService.IsInitialized)
                    return;

                // Check if we're between 4:59 PM and 6:00 PM ET - if so, stop P&L monitoring
                // This prevents accounts from being re-locked immediately after unlocking at market close
                if (IsNearMarketClose())
                {
                    System.Diagnostics.Debug.WriteLine("[P&L Monitor] Near/after market close (4:59 PM - 6:00 PM ET), skipping P&L checks to prevent re-locking");
                    return;
                }

                // Use indexed enumeration for cleaner code
                var accountsWithIndex = core.Accounts.Select((account, index) => new { account, index });

                foreach (var item in accountsWithIndex)
                {
                    if (item.account == null)
                        continue;

                    var uniqueAccountId = GetUniqueAccountIdentifier(item.account, item.index);
                    if (string.IsNullOrEmpty(uniqueAccountId))
                        continue;

                    // Get settings for this account
                    var settings = settingsService.GetSettings(uniqueAccountId);
                    if (settings == null || !settings.FeatureToggleEnabled)
                        continue;

                    // Enforce trading time locks (lock/unlock based on allowed trading times)
                    // Only enforce if Trading Times feature is enabled
                    if (settings.TradingTimesEnabled)
                    {
                        EnforceTradingTimeLocks(uniqueAccountId, settingsService);
                    }

                    // Skip if account is already locked
                    if (settingsService.IsTradingLocked(uniqueAccountId))
                        continue;

                    // Check Allowed Trading Times - close positions if outside trading hours
                    CheckTradingTimeRestrictions(item.account, uniqueAccountId, settings, settingsService, core);

                    // Check and flatten positions for blocked symbols, contract limits, and P&L limits
                    // This unified method handles all position-level flattening scenarios
                    CheckAndFlattenBlockedOrLimitExceededPositions(item.account, uniqueAccountId, settings, settingsService, core);

                    // Check Daily P&L limits (account-level checks that may lock the account)
                    CheckDailyPnLLimits(item.account, uniqueAccountId, settings, core);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error monitoring P&L limits: {ex.Message}");
            }
        }

        /// <summary>
        /// Enforces trading time locks by checking allowed trading times and locking/unlocking accounts accordingly.
        /// Locks account when outside allowed trading windows and unlocks when entering allowed windows (if no day/hour locks exist).
        /// </summary>
        private void EnforceTradingTimeLocks(string accountId, RiskManagerSettingsService settingsService)
        {
            try
            {
                if (string.IsNullOrEmpty(accountId) || settingsService == null)
                    return;

                // Use the service method to check and enforce trading time locks
                settingsService.CheckAndEnforceTradeTimeLocks(accountId);

                // NOTE: We do NOT update the badge here. The badge should only update from:
                // 1. Manual lock/unlock button clicks
                // 2. Account selection changes (LoadAccountSettings)
                // The badge reads directly from JSON via GetLockStatusString, so it will always
                // show the current persisted state without needing timer-triggered updates.
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error enforcing trading time locks for account {accountId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if daily P&L has exceeded limits and locks account if necessary.
        /// Monitors Gross P&L per requirements and sends warning notifications at 80% threshold.
        /// </summary>
        private void CheckDailyPnLLimits(Account account, string accountId, AccountSettings settings, Core core)
        {
            try
            {
                var settingsService = RiskManagerSettingsService.Instance;
                
                // Check if Limits feature is enabled - skip enforcement if disabled
                if (!settings.LimitsEnabled)
                {
                    return;
                }
                
                // IMPORTANT: Check if account is already locked first to prevent any further processing
                // This prevents infinite notifications by ensuring we don't re-process already locked accounts
                if (settingsService.IsTradingLocked(accountId))
                {
                    return;
                }
                
                // Get Net P&L from account for daily loss/profit monitoring
                double netPnL = GetAccountNetPnL(account);
                
                // Log P&L evaluation for audit purposes
                System.Diagnostics.Debug.WriteLine($"[P&L Evaluation] Account: {accountId}, Net P&L: ${netPnL:F2}, " +
                    $"Loss Limit: {(settings.DailyLossLimit.HasValue ? $"${settings.DailyLossLimit.Value:F2}" : "None")}, " +
                    $"Profit Target: {(settings.DailyProfitTarget.HasValue ? $"${settings.DailyProfitTarget.Value:F2}" : "None")}");

                // Check Daily Loss Limit (negative value)
                if (settings.DailyLossLimit.HasValue && settings.DailyLossLimit.Value < 0)
                {
                    decimal lossLimit = settings.DailyLossLimit.Value;
                    decimal currentPnL = (decimal)netPnL;
                    
                    // Calculate warning threshold using configurable constant
                    decimal warningThreshold = lossLimit * DAILY_LOSS_WARNING_THRESHOLD;
                    
                    // Check if loss limit is breached
                    if (currentPnL <= lossLimit)
                    {
                        // Loss limit exceeded
                        string reason = $"Daily Loss Limit reached: Net P&L ${netPnL:F2} ≤ Limit ${lossLimit:F2}";
                        
                        // Enhanced logging
                        System.Diagnostics.Debug.WriteLine($"[LIMIT BREACH] Account: {accountId}, Reason: {reason}, " +
                            $"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC, Mode: {settings.EnforcementMode}");
                        
                        // Take action based on enforcement mode
                        switch (settings.EnforcementMode)
                        {
                            case RiskEnforcementMode.Strict:
                                // Strict mode: Lock account and close positions
                                LockAccountUntil5PMET(accountId, reason, core, account);
                                CloseAllPositionsForAccount(account, core);
                                System.Diagnostics.Debug.WriteLine($"[AUDIT LOG] Account {accountId} locked due to daily loss limit breach at ${netPnL:F2}");
                                break;
                                
                            case RiskEnforcementMode.Warning:
                                // Warning mode: Just log the breach
                                System.Diagnostics.Debug.WriteLine($"[WARNING ONLY] Account {accountId} breached daily loss limit at ${netPnL:F2} - no enforcement action taken");
                                break;
                                
                            case RiskEnforcementMode.Monitor:
                                // Monitor mode: Silent tracking only (already logged above)
                                System.Diagnostics.Debug.WriteLine($"[MONITOR MODE] Account {accountId} breached daily loss limit at ${netPnL:F2} - silent tracking only");
                                break;
                        }
                        
                        // Reset warning state since we've breached the limit
                        settingsService.ResetDailyLossWarning(accountId);
                        
                        return; // Exit after handling breach
                    }
                    // Check if warning threshold is reached
                    else if (currentPnL <= warningThreshold)
                    {
                        // Check if we've already sent a warning today
                        if (!settingsService.HasDailyLossWarningSent(accountId))
                        {
                            // Calculate percentage safely
                            // Note: Although lossLimit is validated as negative, we keep this guard for defensive programming
                            // in case this method is called from other contexts in the future
                            decimal percentOfLimit = 0;
                            if (lossLimit != 0)
                            {
                                percentOfLimit = Math.Abs((currentPnL / lossLimit) * 100);
                            }
                            
                            // Log the warning
                            System.Diagnostics.Debug.WriteLine($"[WARNING NOTIFICATION] Account: {accountId}, " +
                                $"Current P&L: ${netPnL:F2}, Limit: ${lossLimit:F2}, " +
                                $"Threshold: {DAILY_LOSS_WARNING_THRESHOLD * 100:F0}%, Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                            
                            // Mark warning as sent
                            settingsService.SetDailyLossWarningSent(accountId, currentPnL);
                            
                            System.Diagnostics.Debug.WriteLine($"[AUDIT LOG] Warning notification sent to account {accountId} at 80% threshold");
                        }
                    }
                }

                // Check Daily Profit Target (positive value)
                if (settings.DailyProfitTarget.HasValue && settings.DailyProfitTarget.Value > 0)
                {
                    decimal profitTarget = settings.DailyProfitTarget.Value;
                    decimal currentPnL = (decimal)netPnL;
                    
                    // Calculate warning threshold (80% of target)
                    decimal warningThreshold = profitTarget * 0.80m;
                    
                    // Check if profit target is reached
                    if (currentPnL >= profitTarget)
                    {
                        // Profit target reached - lock account until 5 PM ET
                        string reason = $"Daily Profit Target reached: Net P&L ${netPnL:F2} ≥ Target ${profitTarget:F2}";
                        
                        // Enhanced logging
                        System.Diagnostics.Debug.WriteLine($"[ACCOUNT LOCK] Account: {accountId}, Reason: {reason}, " +
                            $"Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                        
                        // Close positions first, then lock account to ensure all positions are properly closed
                        // If position closing fails, still lock account for risk management
                        try
                        {
                            CloseAllPositionsForAccount(account, core);
                        }
                        catch (Exception closeEx)
                        {
                            System.Diagnostics.Debug.WriteLine($"[ERROR] Failed to close all positions for account {accountId}: {closeEx.Message}");
                        }
                        
                        // Always lock account even if position closing failed (critical for risk management)
                        LockAccountUntil5PMET(accountId, reason, core, account);
                        
                        // Reset warning state since we've reached the target
                        settingsService.ResetDailyProfitWarning(accountId);
                        
                        System.Diagnostics.Debug.WriteLine($"[AUDIT LOG] Account {accountId} locked due to daily profit target at ${currentPnL:F2}");
                    }
                    // Check if warning threshold is reached (80% of target)
                    else if (currentPnL >= warningThreshold)
                    {
                        // Check if we've already sent a warning today
                        if (!settingsService.HasDailyProfitWarningSent(accountId))
                        {
                            // Calculate percentage of target reached
                            decimal percentOfTarget = (currentPnL / profitTarget) * 100;
                            
                            // Log the warning
                            System.Diagnostics.Debug.WriteLine($"[WARNING NOTIFICATION] Account: {accountId}, " +
                                $"Current P&L: ${currentPnL:F2}, Target: ${profitTarget:F2}, " +
                                $"Threshold: 80%, Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                            
                            // Mark warning as sent
                            settingsService.SetDailyProfitWarningSent(accountId, currentPnL);
                            
                            System.Diagnostics.Debug.WriteLine($"[AUDIT LOG] Warning notification sent to account {accountId} at 80% profit target threshold");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error checking daily P&L limits for account {accountId}: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Checks if weekly P&L has exceeded limits and locks account if necessary.
        /// </summary>
        private void CheckWeeklyPnLLimits(Account account, string accountId, AccountSettings settings, Core core)
        {
            try
            {
                // Check if Limits feature is enabled - skip enforcement if disabled
                if (!settings.LimitsEnabled)
                {
                    return;
                }
                
                // Get daily P&L (we'll use this as proxy for weekly P&L tracking)
                // In a production system, you would track cumulative weekly P&L separately
                double dailyPnL = GetAccountDailyPnL(account);

                // Check Weekly Loss Limit (negative value)
                if (settings.WeeklyLossLimit.HasValue && settings.WeeklyLossLimit.Value < 0)
                {
                    decimal lossLimit = settings.WeeklyLossLimit.Value;
                    // For weekly limits, we compare against the same daily P&L
                    // In production, you'd track cumulative weekly P&L
                    if ((decimal)dailyPnL <= lossLimit)
                    {
                        // Weekly loss limit exceeded - lock account until 5 PM ET Friday
                        string reason = $"Weekly Loss Limit reached: P&L ${dailyPnL:F2} ≤ Limit ${lossLimit:F2}";
                        LockAccountUntil5PMETFriday(accountId, reason, core, account);
                        CloseAllPositionsForAccount(account, core);
                        System.Diagnostics.Debug.WriteLine($"Account {accountId} locked due to weekly loss limit");
                        return; // Exit after locking
                    }
                }

                // Check Weekly Profit Target (positive value)
                if (settings.WeeklyProfitTarget.HasValue && settings.WeeklyProfitTarget.Value > 0)
                {
                    decimal profitTarget = settings.WeeklyProfitTarget.Value;
                    if ((decimal)dailyPnL >= profitTarget)
                    {
                        // Weekly profit target reached - lock account until 5 PM ET Friday
                        string reason = $"Weekly Profit Target reached: P&L ${dailyPnL:F2} ≥ Target ${profitTarget:F2}";
                        LockAccountUntil5PMETFriday(accountId, reason, core, account);
                        CloseAllPositionsForAccount(account, core);
                        System.Diagnostics.Debug.WriteLine($"Account {accountId} locked due to weekly profit target");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking weekly P&L limits for account {accountId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if any position has exceeded P&L limits and closes position if necessary.
        /// Does NOT lock the account - only closes the individual position that breached limits.
        /// </summary>
        private void CheckPositionPnLLimits(Account account, string accountId, AccountSettings settings, Core core)
        {
            try
            {
                // Check if Positions feature is enabled - skip enforcement if disabled
                if (!settings.PositionsEnabled)
                {
                    return;
                }
                
                if (core.Positions == null)
                    return;

                // Get all positions for this account
                var accountPositions = core.Positions
                    .Where(p => p != null && p.Account == account)
                    .ToList();

                foreach (var position in accountPositions)
                {
                    double openPnL = GetPositionOpenPnL(position);

                    // Check Position Loss Limit (negative value)
                    if (settings.PositionLossLimit.HasValue && settings.PositionLossLimit.Value < 0)
                    {
                        decimal lossLimit = settings.PositionLossLimit.Value;
                        if ((decimal)openPnL <= lossLimit)
                        {
                            // Position loss limit exceeded - close position (but do NOT lock account)
                            string reason = $"Position Loss Limit: {position.Symbol} P&L ${openPnL:F2} ≤ Limit ${lossLimit:F2}";
                            
                            // Enhanced logging with timestamp
                            System.Diagnostics.Debug.WriteLine($"[POSITION CLOSURE] Account: {accountId}, Symbol: {position.Symbol}, " +
                                $"Reason: {reason}, Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                            
                            ClosePosition(position, core);
                            
                            System.Diagnostics.Debug.WriteLine($"[AUDIT LOG] Position closed for account {accountId}, Symbol: {position.Symbol}, " +
                                $"P&L: ${openPnL:F2}, Limit: ${lossLimit:F2}, Account NOT locked");
                        }
                    }

                    // Check Position Profit Target (positive value)
                    if (settings.PositionProfitTarget.HasValue && settings.PositionProfitTarget.Value > 0)
                    {
                        decimal profitTarget = settings.PositionProfitTarget.Value;
                        if ((decimal)openPnL >= profitTarget)
                        {
                            // Position profit target reached - close position (but do NOT lock account)
                            string reason = $"Position Profit Target: {position.Symbol} P&L ${openPnL:F2} ≥ Target ${profitTarget:F2}";
                            
                            // Enhanced logging with timestamp
                            System.Diagnostics.Debug.WriteLine($"[POSITION CLOSURE] Account: {accountId}, Symbol: {position.Symbol}, " +
                                $"Reason: {reason}, Timestamp: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
                            
                            ClosePosition(position, core);
                            
                            System.Diagnostics.Debug.WriteLine($"[AUDIT LOG] Position closed for account {accountId}, Symbol: {position.Symbol}, " +
                                $"P&L: ${openPnL:F2}, Target: ${profitTarget:F2}, Account NOT locked");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error checking position P&L limits for account {accountId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Unified method to check and flatten positions for blocked symbols, contract limit violations, and P&L limits.
        /// Closes positions immediately when any of the following conditions are met:
        /// 1. Symbol is blocked
        /// 2. Position volume exceeds contract limit for the symbol
        /// 3. Position loss reaches or exceeds the loss limit
        /// 4. Position profit reaches or exceeds the profit target
        /// Does NOT lock the account - only closes individual positions that breach limits.
        /// </summary>
        private void CheckAndFlattenBlockedOrLimitExceededPositions(Account account, string accountId, AccountSettings settings,
            RiskManagerSettingsService settingsService, Core core)
        {
            try
            {
                // Check if Symbols or Positions features are enabled
                // Skip enforcement entirely only if BOTH are disabled, since this method handles both feature types
                // If at least one is enabled, we proceed and check individual features below
                if (!settings.SymbolsEnabled && !settings.PositionsEnabled)
                {
                    return;
                }
                
                if (core.Positions == null)
                    return;

                // Get all positions for this account
                var accountPositions = core.Positions
                    .Where(p => p != null && p.Account == account && p.Quantity != 0)
                    .ToList();

                if (!accountPositions.Any())
                    return;

                // Generate timestamp once for consistent logging across this execution
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
                
                // Track positions that have been closed to avoid processing them again
                var closedPositions = new HashSet<Position>();

                // First, check and close positions for contract limit violations (by symbol)
                // Only enforce symbol restrictions if Symbols feature is enabled
                if (settings.SymbolsEnabled && 
                    (settings.DefaultContractLimit.HasValue || 
                    (settings.SymbolContractLimits != null && settings.SymbolContractLimits.Any())))
                {
                    // Group positions by symbol, filtering out invalid symbols early
                    var positionsBySymbol = accountPositions
                        .Where(p => !string.IsNullOrEmpty(p.Symbol?.Name))
                        .GroupBy(p => p.Symbol.Name);

                    foreach (var symbolGroup in positionsBySymbol)
                    {
                        var symbol = symbolGroup.Key;
                        var positions = symbolGroup.ToList();
                        var positionCount = positions.Count;

                        // Get the contract limit for this symbol
                        var contractLimit = settingsService.GetContractLimit(accountId, symbol);
                        
                        if (contractLimit.HasValue && positionCount > contractLimit.Value)
                        {
                            // Exceeded contract limit - close all positions for this symbol
                            string reason = $"Contract Limit Exceeded: {positionCount} positions > {contractLimit.Value} limit";
                            
                            foreach (var position in positions)
                            {
                                System.Diagnostics.Debug.WriteLine($"[FLATTEN POSITION] Account: {accountId}, Symbol: {symbol}, Reason: {reason}, Timestamp: {timestamp} UTC");
                                ClosePosition(position, core);
                                closedPositions.Add(position);
                            }
                        }
                    }
                }

                // Now check remaining positions for other violations
                foreach (var position in accountPositions)
                {
                    // Skip if already closed
                    if (closedPositions.Contains(position))
                        continue;
                        
                    var symbol = position.Symbol?.Name ?? string.Empty;
                    if (string.IsNullOrEmpty(symbol))
                        continue;

                    // 1. Check if symbol is blocked (only if Symbols feature is enabled)
                    if (settings.SymbolsEnabled && settingsService.IsSymbolBlocked(accountId, symbol))
                    {
                        string reason = $"Symbol Blocked: {symbol}";
                        System.Diagnostics.Debug.WriteLine($"[FLATTEN POSITION] Account: {accountId}, Symbol: {symbol}, Reason: {reason}, Timestamp: {timestamp} UTC");
                        
                        ClosePosition(position, core);
                        continue; // Move to next position
                    }

                    // 2. Check position P&L limits (only if Positions feature is enabled)
                    if (settings.PositionsEnabled)
                    {
                        double openPnL = GetPositionOpenPnL(position);

                        // Check Position Loss Limit (negative value)
                        if (settings.PositionLossLimit.HasValue && settings.PositionLossLimit.Value < 0)
                        {
                            decimal lossLimit = settings.PositionLossLimit.Value;
                            if ((decimal)openPnL <= lossLimit)
                            {
                                string reason = $"Position Loss Limit: P&L ${openPnL:F2} ≤ Limit ${lossLimit:F2}";
                                System.Diagnostics.Debug.WriteLine($"[FLATTEN POSITION] Account: {accountId}, Symbol: {symbol}, Reason: {reason}, Timestamp: {timestamp} UTC");
                                
                                ClosePosition(position, core);
                                continue; // Move to next position
                            }
                        }

                        // Check Position Profit Target (positive value)
                        if (settings.PositionProfitTarget.HasValue && settings.PositionProfitTarget.Value > 0)
                        {
                            decimal profitTarget = settings.PositionProfitTarget.Value;
                            if ((decimal)openPnL >= profitTarget)
                            {
                                string reason = $"Position Profit Target: P&L ${openPnL:F2} ≥ Target ${profitTarget:F2}";
                                System.Diagnostics.Debug.WriteLine($"[FLATTEN POSITION] Account: {accountId}, Symbol: {symbol}, Reason: {reason}, Timestamp: {timestamp} UTC");
                                
                                ClosePosition(position, core);
                                continue; // Move to next position
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[ERROR] Error checking and flattening positions for account {accountId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if current time is within allowed trading hours and closes positions if not.
        /// </summary>
        private void CheckTradingTimeRestrictions(Account account, string accountId, AccountSettings settings, 
            RiskManagerSettingsService settingsService, Core core)
        {
            try
            {
                // Check if Trading Times feature is enabled - skip enforcement if disabled
                if (!settings.TradingTimesEnabled)
                {
                    return;
                }
                
                // Check if trading is allowed right now
                if (settingsService.IsTradingAllowedNow(accountId))
                    return;

                // Trading is not allowed - close all positions for this account
                if (core.Positions == null)
                    return;

                var accountPositions = core.Positions
                    .Where(p => p != null && p.Account == account && p.Quantity != 0)
                    .ToList();

                if (accountPositions.Any())
                {
                    System.Diagnostics.Debug.WriteLine($"Closing {accountPositions.Count} positions for account {accountId} - outside allowed trading times");
                    
                    foreach (var position in accountPositions)
                    {
                        ClosePosition(position, core);
                        System.Diagnostics.Debug.WriteLine($"Position closed (outside trading hours): {position.Symbol}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking trading time restrictions for account {accountId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if any positions are for blacklisted symbols and closes them immediately.
        /// </summary>
        private void CheckSymbolBlacklist(Account account, string accountId, AccountSettings settings, 
            RiskManagerSettingsService settingsService, Core core)
        {
            try
            {
                // Check if blacklist is configured
                if (settings.BlockedSymbols == null || !settings.BlockedSymbols.Any())
                    return;

                if (core.Positions == null)
                    return;

                // Get all positions for this account
                var accountPositions = core.Positions
                    .Where(p => p != null && p.Account == account && p.Quantity != 0)
                    .ToList();

                foreach (var position in accountPositions)
                {
                    var symbol = position.Symbol?.Name ?? string.Empty;
                    
                    // Check if symbol is blacklisted
                    if (settingsService.IsSymbolBlocked(accountId, symbol))
                    {
                        ClosePosition(position, core);
                        System.Diagnostics.Debug.WriteLine($"Position closed (blacklisted symbol): {symbol} for account {accountId}");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking symbol blacklist for account {accountId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if the number of positions per symbol exceeds contract limits and closes excess positions.
        /// </summary>
        private void CheckSymbolContractLimits(Account account, string accountId, AccountSettings settings, 
            RiskManagerSettingsService settingsService, Core core)
        {
            try
            {
                // Check if contract limits are configured
                if (!settings.DefaultContractLimit.HasValue && 
                    (settings.SymbolContractLimits == null || !settings.SymbolContractLimits.Any()))
                    return;

                if (core.Positions == null)
                    return;

                // Get all positions for this account grouped by symbol
                var accountPositions = core.Positions
                    .Where(p => p != null && p.Account == account && p.Quantity != 0)
                    .ToList();

                // Group positions by symbol
                var positionsBySymbol = accountPositions
                    .GroupBy(p => p.Symbol?.Name ?? string.Empty)
                    .Where(g => !string.IsNullOrEmpty(g.Key));

                foreach (var symbolGroup in positionsBySymbol)
                {
                    var symbol = symbolGroup.Key;
                    var positions = symbolGroup.ToList();
                    var positionCount = positions.Count;

                    // Get the contract limit for this symbol
                    var contractLimit = settingsService.GetContractLimit(accountId, symbol);
                    
                    if (contractLimit.HasValue && positionCount > contractLimit.Value)
                    {
                        // Exceeded contract limit - close all positions for this symbol
                        System.Diagnostics.Debug.WriteLine($"Contract limit exceeded for symbol {symbol}: {positionCount} positions > {contractLimit.Value} limit");
                        
                        foreach (var position in positions)
                        {
                            ClosePosition(position, core);
                            System.Diagnostics.Debug.WriteLine($"Position closed (contract limit exceeded): {symbol} for account {accountId}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking symbol contract limits for account {accountId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the daily P&L for an account from AdditionalInfo.
        /// </summary>
        private double GetAccountDailyPnL(Account account)
        {
            try
            {
                if (account?.AdditionalInfo == null)
                    return 0;

                foreach (var info in account.AdditionalInfo)
                {
                    if (info == null)
                        continue;

                    string id = info.Id ?? string.Empty;

                    // Daily P&L is in TotalPnL field
                    if (string.Equals(id, TOTAL_PNL_ID, StringComparison.OrdinalIgnoreCase))
                    {
                        if (info.Value is double dailyPnL)
                            return dailyPnL;
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting daily P&L: {ex.Message}");
            }

            return 0;
        }

        /// <summary>
        /// Gets the Net P&L for an account from AdditionalInfo.
        /// This is used for monitoring daily loss limits and profit targets.
        /// </summary>
        private double GetAccountNetPnL(Account account)
        {
            try
            {
                if (account?.AdditionalInfo == null)
                    return 0;

                foreach (var info in account.AdditionalInfo)
                {
                    if (info == null)
                        continue;

                    string id = info.Id ?? string.Empty;

                    // Net P&L field
                    if (string.Equals(id, "NetPnL", StringComparison.OrdinalIgnoreCase))
                    {
                        if (info.Value is double netPnL)
                        {
                            System.Diagnostics.Debug.WriteLine($"[P&L Monitor] Net P&L retrieved: ${netPnL:F2} for account {account.Id ?? account.Name}");
                            return netPnL;
                        }
                    }
                }
                
                // Fallback to TotalPnL if Net P&L is not available
                System.Diagnostics.Debug.WriteLine($"[P&L Monitor] ⚠️ WARNING: Net P&L not found for account {account.Id ?? account.Name}, falling back to TotalPnL");
                return GetAccountDailyPnL(account);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting Net P&L: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the Gross P&L for an account from AdditionalInfo.
        /// This is the primary method for monitoring daily loss limits per requirements.
        /// </summary>
        private double GetAccountGrossPnL(Account account)
        {
            try
            {
                if (account?.AdditionalInfo == null)
                    return 0;

                foreach (var info in account.AdditionalInfo)
                {
                    if (info == null)
                        continue;

                    string id = info.Id ?? string.Empty;

                    // Gross P&L can be in "Gross P&L" or "GrossPnL" field
                    if (string.Equals(id, "Gross P&L", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(id, "GrossPnL", StringComparison.OrdinalIgnoreCase))
                    {
                        if (info.Value is double grossPnL)
                        {
                            System.Diagnostics.Debug.WriteLine($"[P&L Monitor] Gross P&L retrieved: ${grossPnL:F2} for account {account.Id ?? account.Name}");
                            return grossPnL;
                        }
                    }
                }
                
                // Fallback to TotalPnL if Gross P&L is not available
                // WARNING: This fallback may not provide the same risk management protection as Gross P&L
                var fallbackPnL = GetAccountDailyPnL(account);
                System.Diagnostics.Debug.WriteLine(
                    $"[P&L Monitor] ⚠️ CRITICAL: Gross P&L not found for account {account.Id ?? account.Name}\n" +
                    $"  Falling back to TotalPnL - consider verifying account configuration or broker P&L fields\n" +
                    $"  Using TotalPnL fallback: ${fallbackPnL:F2} for loss limit monitoring");
                return fallbackPnL;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting Gross P&L: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Gets the open P&L for a position.
        /// </summary>
        private double GetPositionOpenPnL(Position position)
        {
            try
            {
                if (position == null)
                    return 0;

                // Position.GrossPnL is a PnLItem object, access its Value property
                if (position.GrossPnL != null)
                    return position.GrossPnL.Value;
                
                return 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting position open P&L: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Locks an account until 5 PM ET.
        /// </summary>
        private void LockAccountUntil5PMET(string accountId, string reason, Core core, Account account)
        {
            try
            {
                // Calculate time until 5 PM ET
                TimeSpan lockDuration = CalculateTimeUntil5PMET();
                
                var settingsService = RiskManagerSettingsService.Instance;
                settingsService.SetTradingLock(accountId, true, reason, lockDuration);

                // Lock the account in Core API
                try
                {
                    var lockMethod = core.GetType().GetMethod("LockAccount");
                    if (lockMethod != null)
                    {
                        lockMethod.Invoke(core, new object[] { account });
                        System.Diagnostics.Debug.WriteLine($"Locked account {accountId} in Core API");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error locking account {accountId} in Core API: {ex.Message}");
                }

                // Update UI if this is the selected account
                var selectedAccountNumber = GetSelectedAccountNumber();
                if (!string.IsNullOrEmpty(selectedAccountNumber) && selectedAccountNumber == accountId)
                {
                    // NOTE: We do NOT update the badge here. The badge should only update from:
                    // 1. Manual lock/unlock button clicks
                    // 2. Account selection changes (LoadAccountSettings)
                    // The badge reads directly from JSON, so it will show the locked state
                    // when the user next interacts with it.
                    UpdateLockButtonStates();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error locking account {accountId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculates the time duration until 5 PM ET today (or tomorrow if past 5 PM).
        /// </summary>
        private TimeSpan CalculateTimeUntil5PMET()
        {
            try
            {
                // Get current time in ET (Eastern Time)
                // Try cross-platform ID first, then fall back to Windows ID
                TimeZoneInfo etZone;
                try
                {
                    etZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
                }
                catch (TimeZoneNotFoundException)
                {
                    etZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                }
                
                DateTime nowET = TimeZoneInfo.ConvertTime(DateTime.Now, etZone);

                // Target time is 5 PM ET today
                DateTime target5PM = nowET.Date.AddHours(17); // 5 PM = 17:00

                // If we're past 5 PM, target tomorrow's 5 PM
                if (nowET >= target5PM)
                {
                    target5PM = target5PM.AddDays(1);
                }

                TimeSpan duration = target5PM - nowET;
                return duration;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating time until 5 PM ET: {ex.Message}");
                // Fallback: lock for rest of trading day
                return TimeSpan.FromHours(FALLBACK_LOCK_HOURS);
            }
        }
        
        /// <summary>
        /// Checks if current time is near market close (4:59 PM to 6:00 PM ET).
        /// Used to prevent P&L monitoring from re-locking accounts at market close.
        /// P&L monitoring resumes at 6:00 PM ET to allow accounts to unlock cleanly at 5:00 PM.
        /// </summary>
        private bool IsNearMarketClose()
        {
            try
            {
                // Get current time in ET (Eastern Time)
                TimeZoneInfo etZone;
                try
                {
                    etZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
                }
                catch (TimeZoneNotFoundException)
                {
                    etZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                }
                
                DateTime nowET = TimeZoneInfo.ConvertTime(DateTime.Now, etZone);
                
                // Market close is 5 PM ET (17:00), P&L monitoring resumes at 6 PM ET (18:00)
                // Check if we're at or after 4:59 PM ET (16:59) and before 6:00 PM ET (18:00)
                TimeSpan currentTime = nowET.TimeOfDay;
                TimeSpan monitoringResumeTime = new TimeSpan(18, 0, 0); // 6:00 PM
                TimeSpan oneMinuteBeforeClose = new TimeSpan(16, 59, 0); // 4:59 PM
                
                // Return true if between 4:59 PM (inclusive) and 6:00 PM (exclusive)
                bool isNearClose = currentTime >= oneMinuteBeforeClose && currentTime < monitoringResumeTime;
                
                return isNearClose;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking if near market close: {ex.Message}");
                // On error, allow monitoring to continue
                return false;
            }
        }

        /// <summary>
        /// Locks an account until 5 PM ET Friday.
        /// </summary>
        private void LockAccountUntil5PMETFriday(string accountId, string reason, Core core, Account account)
        {
            try
            {
                // Calculate time until 5 PM ET Friday
                TimeSpan lockDuration = CalculateTimeUntil5PMETFriday();
                
                var settingsService = RiskManagerSettingsService.Instance;
                settingsService.SetTradingLock(accountId, true, reason, lockDuration);

                // Lock the account in Core API
                try
                {
                    var lockMethod = core.GetType().GetMethod("LockAccount");
                    if (lockMethod != null)
                    {
                        lockMethod.Invoke(core, new object[] { account });
                        System.Diagnostics.Debug.WriteLine($"Locked account {accountId} in Core API until Friday 5 PM ET");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error locking account {accountId} in Core API: {ex.Message}");
                }

                // Update UI if this is the selected account
                var selectedAccountNumber = GetSelectedAccountNumber();
                if (!string.IsNullOrEmpty(selectedAccountNumber) && selectedAccountNumber == accountId)
                {
                    // NOTE: We do NOT update the badge here. The badge should only update from:
                    // 1. Manual lock/unlock button clicks
                    // 2. Account selection changes (LoadAccountSettings)
                    // The badge reads directly from JSON, so it will show the locked state
                    // when the user next interacts with it.
                    UpdateLockButtonStates();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error locking account {accountId} until Friday: {ex.Message}");
            }
        }

        /// <summary>
        /// Calculates the time duration until 5 PM ET Friday.
        /// </summary>
        private TimeSpan CalculateTimeUntil5PMETFriday()
        {
            try
            {
                // Get current time in ET (Eastern Time)
                // Try cross-platform ID first, then fall back to Windows ID
                TimeZoneInfo etZone;
                try
                {
                    etZone = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
                }
                catch (TimeZoneNotFoundException)
                {
                    etZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
                }
                
                DateTime nowET = TimeZoneInfo.ConvertTime(DateTime.Now, etZone);

                // Target time is 5 PM ET on Friday
                DateTime targetFriday5PM = nowET.Date.AddHours(17); // 5 PM = 17:00
                
                // Get current day of week
                DayOfWeek currentDay = nowET.DayOfWeek;
                
                // Calculate days until Friday
                int daysUntilFriday;
                if (currentDay == DayOfWeek.Friday)
                {
                    // If it's Friday but past 5 PM, go to next Friday
                    if (nowET >= targetFriday5PM)
                    {
                        daysUntilFriday = 7;
                    }
                    else
                    {
                        daysUntilFriday = 0;
                    }
                }
                else if (currentDay == DayOfWeek.Saturday)
                {
                    daysUntilFriday = 6; // Saturday to Friday
                }
                else if (currentDay == DayOfWeek.Sunday)
                {
                    daysUntilFriday = 5; // Sunday to Friday
                }
                else
                {
                    // Monday-Thursday: calculate days until Friday
                    daysUntilFriday = (int)DayOfWeek.Friday - (int)currentDay;
                }
                
                targetFriday5PM = targetFriday5PM.AddDays(daysUntilFriday);
                
                TimeSpan duration = targetFriday5PM - nowET;
                return duration;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error calculating time until Friday 5 PM ET: {ex.Message}");
                // Fallback: lock for rest of week (assume 5 days)
                return TimeSpan.FromDays(5);
            }
        }

        /// <summary>
        /// Closes all positions for a specific account.
        /// </summary>
        private void CloseAllPositionsForAccount(Account account, Core core)
        {
            try
            {
                if (core.Positions == null)
                    return;

                // Cancel all working orders for this account first
                CancelAllWorkingOrdersForAccount(account, core);

                var accountPositions = core.Positions
                    .Where(p => p != null && p.Account == account)
                    .ToList();

                foreach (var position in accountPositions)
                {
                    // Close position without canceling orders again (already done above)
                    try
                    {
                        core.ClosePosition(position);
                        System.Diagnostics.Debug.WriteLine($"Closed position: {position.Symbol}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error closing position {position?.Symbol}: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"Closed {accountPositions.Count} positions for account");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing all positions for account: {ex.Message}");
            }
        }

        /// <summary>
        /// Cancels all working orders for a specific account.
        /// </summary>
        private void CancelAllWorkingOrdersForAccount(Account account, Core core)
        {
            try
            {
                if (account == null || core?.Orders == null)
                    return;

                var workingOrders = core.Orders
                    .Where(order => order != null && order.Account == account && 
                           (order.Status == OrderStatus.Opened || order.Status == OrderStatus.PartiallyFilled))
                    .ToList();

                foreach (var order in workingOrders)
                {
                    try
                    {
                        core.CancelOrder(order);
                        System.Diagnostics.Debug.WriteLine($"Canceled working order: {order.Symbol} for account {account.Id}");
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error canceling order {order.Symbol}: {ex.Message}");
                    }
                }

                if (workingOrders.Count > 0)
                {
                    System.Diagnostics.Debug.WriteLine($"Canceled {workingOrders.Count} working orders for account {account.Id}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error canceling working orders for account: {ex.Message}");
            }
        }

        /// <summary>
        /// Closes a single position.
        /// </summary>
        private void ClosePosition(Position position, Core core)
        {
            try
            {
                if (position == null)
                    return;

                // Cancel all working orders for this account before closing the position
                CancelAllWorkingOrdersForAccount(position.Account, core);

                core.ClosePosition(position);
                System.Diagnostics.Debug.WriteLine($"Closed position: {position.Symbol}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing position {position?.Symbol}: {ex.Message}");
            }
        }

        private void UpdateAccountStatus(Label lblTradingStatus)
        {
            try
            {
                var accountNumber = GetSelectedAccountNumber();
                if (string.IsNullOrEmpty(accountNumber))
                {
                    lblTradingStatus.Text = "No Account Selected";
                    lblTradingStatus.ForeColor = TextGray;
                    return;
                }

                var settingsService = RiskManagerSettingsService.Instance;
                if (!settingsService.IsInitialized)
                {
                    lblTradingStatus.Text = "Status Unknown";
                    lblTradingStatus.ForeColor = TextGray;
                    return;
                }

                bool isLocked = settingsService.IsTradingLocked(accountNumber);

                if (isLocked)
                {
                    lblTradingStatus.Text = "Trading Locked";
                    lblTradingStatus.ForeColor = Color.Red;
                }
                else
                {
                    lblTradingStatus.Text = "Trading Unlocked";
                    lblTradingStatus.ForeColor = AccentGreen;
                }
            }
            catch (Exception ex)
            {
                // Log error but don't interrupt UI flow with MessageBox
                System.Diagnostics.Debug.WriteLine($"Error updating account status: {ex.Message}");
                lblTradingStatus.Text = "Status Error";
                lblTradingStatus.ForeColor = TextGray;
            }
        }

        /// <summary>
        /// Gets the account number for the currently selected account.
        /// Creates a unique identifier using multiple properties to handle cases where Id/Name are the same.
        /// </summary>
        private string GetSelectedAccountNumber()
        {
            if (selectedAccount == null)
            {
                System.Diagnostics.Debug.WriteLine($"GetSelectedAccountNumber: selectedAccount is NULL");
                return null;
            }
            
            // CRITICAL: Use GetUniqueAccountIdentifier to ensure consistency
            // This ensures the same identifier is used for locks, settings, and badge updates
            var uniqueId = GetUniqueAccountIdentifier(selectedAccount, selectedAccountIndex);
            
            System.Diagnostics.Debug.WriteLine($"GetSelectedAccountNumber: Using GetUniqueAccountIdentifier result='{uniqueId}' with index={selectedAccountIndex}");
            
            return uniqueId;
        }

        /// <summary>
        /// Gets a unique identifier for any account object using the same logic as GetSelectedAccountNumber.
        /// This ensures consistency when saving and loading settings for the same account.
        /// </summary>
        /// <param name="account">The account to generate an identifier for</param>
        /// <param name="accountIndex">The index of the account in the accounts list</param>
        /// <returns>A unique identifier string for the account</returns>
        private string GetUniqueAccountIdentifier(object account, int accountIndex)
        {
            if (account == null)
                return $"Account_{accountIndex}";
            
            // Get account properties using reflection-like access
            var accountType = account.GetType();
            var idProperty = accountType.GetProperty("Id");
            var nameProperty = accountType.GetProperty("Name");
            var connectionProperty = accountType.GetProperty("Connection");
            
            var accountId = idProperty?.GetValue(account) as string;
            var accountName = nameProperty?.GetValue(account) as string;
            string connectionName = null;
            
            if (connectionProperty != null)
            {
                var connection = connectionProperty.GetValue(account);
                if (connection != null)
                {
                    var connNameProperty = connection.GetType().GetProperty("Name");
                    connectionName = connNameProperty?.GetValue(connection) as string;
                }
            }
            
            // Use same logic as GetSelectedAccountNumber()
            // Strategy 1: Connection.Name + Name (most unique)
            if (!string.IsNullOrEmpty(connectionName))
            {
                if (!string.IsNullOrEmpty(accountName))
                    return $"{connectionName}_{accountName}";
                if (!string.IsNullOrEmpty(accountId))
                    return $"{connectionName}_{accountId}";
                return connectionName;
            }
            
            // Strategy 2: Index-based (reliable fallback)
            if (accountIndex >= 0)
            {
                if (!string.IsNullOrEmpty(accountName))
                    return $"Account_{accountIndex}_{accountName}";
                if (!string.IsNullOrEmpty(accountId))
                    return $"Account_{accountIndex}_{accountId}";
                return $"Account_{accountIndex}";
            }
            
            // Strategy 3: Fallback to Id or Name alone
            if (!string.IsNullOrEmpty(accountId))
                return accountId;
            if (!string.IsNullOrEmpty(accountName))
                return accountName;
            
            return "UNKNOWN";
        }

        /// <summary>
        /// Gets a simple identifier for an account (without needing the index).
        /// Used for display and operations where we don't have the index.
        /// Delegates to GetUniqueAccountIdentifier with index=-1 for consistency.
        /// </summary>
        /// <param name="account">The account to get an identifier for</param>
        /// <returns>A unique identifier string for the account</returns>
        private string GetAccountIdentifier(Account account)
        {
            // Use GetUniqueAccountIdentifier with -1 index to get fallback behavior
            // This ensures consistent logic between both methods
            return GetUniqueAccountIdentifier(account, -1);
        }

        /// <summary>
        /// Determines the account type based on connection name, account ID, and AdditionalInfo.
        /// Uses word boundary pattern matching to avoid false positives.
        /// </summary>
        /// <param name="account">The account to check</param>
        /// <returns>Account type: PA, Eval, Cash, Prac, Demo, Live, or Unknown</returns>
        private string DetermineAccountType(Account account)
        {
            if (account == null || account.Connection == null)
                return ACCOUNT_TYPE_UNKNOWN;

            var connName = account.Connection.Name?.ToLower() ?? "";
            var accountId = account.Id ?? account.Name ?? "";

            // Check account ID prefixes first (most specific matching)
            // These mappings are based on known broker/provider account ID conventions:
            //   - 7PV: Sim-Funded (Sim Funded Account)
            //   - FFNX: PA (Personal Account)
            //   - FFN: Eval (Evaluation/Funded Account)
            //   - BX: PA (Personal Account)
            //   - APEX: Eval (Evaluation/Funded Account)

            // 5PV prefix -> Sim Funded Account
            if (accountId.StartsWith("7PV", StringComparison.OrdinalIgnoreCase))
                return ACCOUNT_TYPE_SIM_FUND;
            // FFNX prefix -> PA account
            if (accountId.StartsWith("ffnx", StringComparison.OrdinalIgnoreCase))
                return ACCOUNT_TYPE_PA;
            
            // FFN prefix -> Eval account (must come after FFNX check)
            if (accountId.StartsWith("ffn", StringComparison.OrdinalIgnoreCase))
                return ACCOUNT_TYPE_EVAL;
            
            // BX prefix -> PA account
            if (accountId.StartsWith("bx", StringComparison.OrdinalIgnoreCase))
                return ACCOUNT_TYPE_PA;
            
            // APEX prefix -> Eval account
            if (accountId.StartsWith("apex", StringComparison.OrdinalIgnoreCase))
                return ACCOUNT_TYPE_EVAL;
            
            // Convert to lowercase for remaining pattern checks
            accountId = accountId.ToLower();
            
            // Check for specific account type patterns with word boundaries to avoid false positives
            // PA (Personal Account)
            if (PAPattern.IsMatch(connName) || PAPattern.IsMatch(accountId) ||
                connName.Contains("personal"))
                return ACCOUNT_TYPE_PA;
            
            // Eval (Evaluation)
            if (EvalPattern.IsMatch(connName) || EvalPattern.IsMatch(accountId) ||
                connName.Contains("evaluation"))
                return ACCOUNT_TYPE_EVAL;

            // Eval (Evaluation)
            if (SimFundPattern.IsMatch(connName) || EvalPattern.IsMatch(accountId) ||
                connName.Contains("simFunded"))
                return ACCOUNT_TYPE_SIM_FUND;

            // Cash
            if (CashPattern.IsMatch(connName) || CashPattern.IsMatch(accountId))
                return ACCOUNT_TYPE_CASH;
            
            // Prac (Practice)
            if (PracPattern.IsMatch(connName) || PracPattern.IsMatch(accountId) ||
                connName.Contains("practice"))
                return ACCOUNT_TYPE_PRAC;
            
            // Demo/Simulation patterns
            if (connName.Contains("demo") || connName.Contains("simulation") || 
                connName.Contains("paper") || accountId.Contains("demo"))
                return ACCOUNT_TYPE_DEMO;
            
            // Live/Real patterns
            if (connName.Contains("live") || connName.Contains("real"))
                return ACCOUNT_TYPE_LIVE;
            
            // Check AdditionalInfo for explicit type override
            if (account.AdditionalInfo != null)
            {
                foreach (var info in account.AdditionalInfo)
                {
                    if (info?.Id == null) continue;
                    var id = info.Id;
                    if (string.Equals(id, "Account Type", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(id, "AccountType", StringComparison.OrdinalIgnoreCase) ||
                        string.Equals(id, "Type", StringComparison.OrdinalIgnoreCase))
                    {
                        if (info.Value is string at && !string.IsNullOrWhiteSpace(at))
                            return at;
                    }
                }
            }
            
            return ACCOUNT_TYPE_UNKNOWN;
        }

        /// <summary>
        /// Extracts the IbId (firm identifier) from an account's AdditionalInfo.
        /// </summary>
        /// <param name="account">The account to extract IbId from</param>
        /// <returns>IbId value or "Unknown" if not found</returns>
        private string GetAccountIbId(Account account)
        {
            if (account?.AdditionalInfo == null)
                return ACCOUNT_TYPE_UNKNOWN;

            foreach (var info in account.AdditionalInfo)
            {
                if (info?.Id == null) continue;
                if (string.Equals(info.Id, "IbId", StringComparison.OrdinalIgnoreCase))
                {
                    if (info.Value != null)
                        return info.Value.ToString();
                }
            }
            
            return ACCOUNT_TYPE_UNKNOWN;
        }

        /// <summary>
        /// Formats a numeric value for display, using parentheses for negative values.
        /// </summary>
        private string FormatNumeric(double value, int decimals = 2)
        {
            if (value < 0)
            {
                return $"({Math.Abs(value).ToString($"N{decimals}")})";
            }
            return value.ToString($"N{decimals}");
        }

        /// <summary>
        /// Formats a nullable decimal value for display, using parentheses for negative values.
        /// </summary>
        private string FormatNumeric(decimal? value, int decimals = 2)
        {
            if (!value.HasValue)
                return "-";
            
            if (value.Value < 0)
            {
                return $"({Math.Abs(value.Value).ToString($"N{decimals}")})";
            }
            return value.Value.ToString($"N{decimals}");
        }

        /// <summary>
        /// Formats a loss limit value for display, always using parentheses.
        /// Loss limits are displayed in parentheses to indicate they are constraints/limits.
        /// Uses absolute value to avoid double negatives.
        /// </summary>
        private string FormatLossLimit(decimal? value, int decimals = 2)
        {
            if (!value.HasValue)
                return "-";
            
            // Use absolute value to avoid confusing double negatives like (-(-500.00))
            return $"({Math.Abs(value.Value).ToString($"N{decimals}")})";
        }

        /// <summary>
        /// Masks an account number for privacy mode.
        /// Shows first few characters and masks the rest with asterisks.
        /// Only applies masking if privacy mode is enabled for the account.
        /// </summary>
        private string MaskAccountNumber(string accountNumber)
        {
            if (string.IsNullOrEmpty(accountNumber))
                return accountNumber;
            
            // Check if privacy mode is enabled for this account
            var settings = RiskManagerSettingsService.Instance.GetSettings(accountNumber);
            if (settings == null || !settings.PrivacyModeEnabled)
                return accountNumber; // Return as-is if privacy mode is off
            
            // Mask the account number: show first 4 characters, mask the rest
            // Example: "12345678" becomes "1234****"
            int visibleChars = Math.Min(4, accountNumber.Length);
            int maskedChars = Math.Max(0, accountNumber.Length - visibleChars);
            
            return accountNumber.Substring(0, visibleChars) + new string('*', maskedChars);
        }

        private Label GetTradingStatusLabel(Button button)
        {
            var contentPanel = button?.Parent;
            if (contentPanel == null) return null;
            
            foreach (Control control in contentPanel.Controls)
            {
                if (control.Tag?.ToString() == "TradingStatus" && control is Label label)
                {
                    return label;
                }
            }
            return null;
        }

        private bool ValidateSettingsService(out RiskManagerSettingsService settingsService)
        {
            settingsService = RiskManagerSettingsService.Instance;
            if (!settingsService.IsInitialized)
            {
                MessageBox.Show($"Settings service is not initialized: {settingsService.InitializationError}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
            return true;
        }

        /// <summary>
        /// Updates the trading status badge based on the current lock state of the selected account.
        /// Uses state caching to prevent redundant UI updates when the lock state hasn't changed.
        /// </summary>
        /// <param name="callerName">The name of the calling method (automatically populated by CallerMemberName attribute)</param>
        /// <remarks>
        /// This method:
        /// - Validates the lock status string from the settings service
        /// - Caches the previous state to avoid unnecessary UI updates
        /// - Logs all state transitions and validation issues for debugging
        /// - Defaults to "Unlocked" state if lock status cannot be determined
        /// 
        /// Expected lockStatusString values:
        /// - "Unlocked" - Trading is allowed
        /// - "Locked" - Trading is locked indefinitely
        /// - "Locked (Xd Xh Xm)" - Trading is locked with time remaining (e.g., "Locked (2h 30m)")
        /// - null/empty - Treated as "Unlocked" for safety
        /// </remarks>
        private void UpdateTradingStatusBadge([System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
        {
            try
            {
                // Get caller information including line number for better debugging
                var stackTrace = new System.Diagnostics.StackTrace(1, true);
                var callerFrame = stackTrace.GetFrame(0);
                var lineNumber = callerFrame?.GetFileLineNumber() ?? 0;
                var callerInfo = lineNumber > 0 ? $"{callerName}:{lineNumber}" : callerName;
                
                var accountNumber = GetSelectedAccountNumber();
                if (string.IsNullOrEmpty(accountNumber))
                {
                    LogBadgeUpdate(callerInfo, accountNumber, null, null, null, "No account selected, skipping update");
                    return;
                }

                var settingsService = RiskManagerSettingsService.Instance;
                if (!settingsService.IsInitialized)
                {
                    LogBadgeUpdate(callerInfo, accountNumber, null, null, null, "Settings service not initialized, skipping update");
                    return;
                }

                // Get lock status string with validation
                string lockStatusString = settingsService.GetLockStatusString(accountNumber);
                
                // LOG: Show what we got from the settings service
                System.Diagnostics.Debug.WriteLine($"[UpdateTradingStatusBadge] Retrieved lockStatusString='{lockStatusString}' for account='{accountNumber}'");
                
                // Validate lockStatusString for null/empty/unexpected values
                if (string.IsNullOrWhiteSpace(lockStatusString))
                {
                    // NOTE: We default to "Unlocked" (fail-open) rather than "Locked" (fail-closed) because:
                    // 1. This is a UI indicator issue, not a critical security control
                    // 2. The actual lock enforcement happens in the Core API/settings service
                    // 3. Failing open prevents UX confusion when status is temporarily unavailable
                    // 4. Manual lock/unlock operations always update settings service first
                    LogBadgeUpdate(callerInfo, accountNumber, null, null, null, "LockStatusString is null or empty, treating as unlocked");
                    lockStatusString = LOCK_STATUS_UNLOCKED; // Default to unlocked for safety
                }
                
                // Sanitize and determine lock status
                // The lockStatusString can be: "Unlocked", "Locked", "Locked (2h 30m)", etc.
                bool isLocked = !lockStatusString.Equals(LOCK_STATUS_UNLOCKED, StringComparison.OrdinalIgnoreCase);
                
                // Get the cached state for THIS account
                bool? previousState = _accountTradingLockStateCache.TryGetValue(accountNumber, out var cachedState) ? cachedState : null;
                
                // Check if we're switching to a different account
                bool switchingAccounts = _currentTradingBadgeAccountNumber != accountNumber;
                
                // Log the determination with all relevant context
                LogBadgeUpdate(callerInfo, accountNumber, lockStatusString, isLocked, previousState, switchingAccounts ? "Account switch detected" : null);

                // ALWAYS update UI to reflect current JSON state
                // This ensures the badge always shows the latest data from the settings service
                // including updated lock duration (e.g., "Locked (2h 30m)" -> "Locked (2h 29m)")
                // Cache optimization was preventing these updates
                
                // Cache the new state for THIS account BEFORE updating UI
                _accountTradingLockStateCache[accountNumber] = isLocked;
                
                LogBadgeUpdate(callerInfo, accountNumber, lockStatusString, isLocked, null, switchingAccounts ? "Account switch - updating UI" : "Updating UI from JSON");
                UpdateTradingStatusBadgeUI(lockStatusString, accountNumber, previousState, callerInfo);
                
                // Update which account is currently displayed AFTER successful UI update
                // This ensures we only mark the account as "switched" once the UI has been updated
                _currentTradingBadgeAccountNumber = accountNumber;
            }
            catch (Exception ex)
            {
                // Log error using the same structured format
                LogBadgeUpdate(callerName ?? "Unknown", null, null, null, null, $"ERROR: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[UpdateTradingStatusBadge] Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Refreshes the Trading Status badge by reading directly from JSON.
        /// This method is called by a timer every second (same pattern as Accounts Summary).
        /// It ensures the badge always displays the current persisted state from the settings service.
        /// </summary>
        private void RefreshTradingStatusBadgeFromJSON()
        {
            // Use InvokeRequired pattern for thread safety (timer runs on different thread)
            if (InvokeRequired) 
            { 
                BeginInvoke(new Action(RefreshTradingStatusBadgeFromJSON)); 
                return; 
            }

            try
            {
                // Get the currently selected account directly from the ComboBox to avoid stale cache
                // This ensures we always use the most recent selection
                Account currentAccount = null;
                int currentIndex = -1;
                
                if (accountSelector != null && accountSelector.SelectedItem is Account acc)
                {
                    currentAccount = acc;
                    currentIndex = accountSelector.SelectedIndex;
                }
                
                if (currentAccount == null)
                {
                    // No account selected, nothing to update
                    return;
                }
                
                // Generate account number using the current account from ComboBox
                var accountNumber = GetUniqueAccountIdentifier(currentAccount, currentIndex);
                if (string.IsNullOrEmpty(accountNumber))
                {
                    // Could not generate account identifier
                    return;
                }

                var settingsService = RiskManagerSettingsService.Instance;
                if (!settingsService.IsInitialized)
                {
                    // Settings service not ready
                    return;
                }

                // Read the current lock status directly from JSON
                string lockStatusString = settingsService.GetLockStatusString(accountNumber);
                
                // Handle null/empty status
                if (string.IsNullOrWhiteSpace(lockStatusString))
                {
                    lockStatusString = LOCK_STATUS_UNLOCKED;
                }

                // Determine lock state
                bool isLocked = !lockStatusString.Equals(LOCK_STATUS_UNLOCKED, StringComparison.OrdinalIgnoreCase);
                
                // Update the UI directly (always refresh from JSON, no state comparison to skip updates)
                UpdateTradingStatusBadgeUI(lockStatusString, accountNumber, null, "RefreshTradingStatusBadgeFromJSON");
                
                // Update the cache to keep it in sync (used by other methods)
                _accountTradingLockStateCache[accountNumber] = isLocked;
                _currentTradingBadgeAccountNumber = accountNumber;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[RefreshTradingStatusBadgeFromJSON] Error: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Helper method for consistent, structured logging of badge update operations.
        /// Formats log messages with contextual information for debugging and tracing.
        /// </summary>
        /// <param name="caller">The name of the calling method</param>
        /// <param name="accountNumber">The account number being processed (or null if not available)</param>
        /// <param name="lockStatusString">The raw lock status string from settings service (or null if not available)</param>
        /// <param name="isLocked">The determined lock state (true=locked, false=unlocked, null=not yet determined)</param>
        /// <param name="previousState">The cached previous lock state (true=locked, false=unlocked, null=not cached/first call)</param>
        /// <param name="message">Additional context or reason for the log entry (or null if general state logging)</param>
        /// <remarks>
        /// This method creates structured log messages with only the relevant fields for each scenario:
        /// - Always includes caller name for traceability
        /// - Conditionally includes account, lock status, state, and previous state if available
        /// - Appends custom message if provided
        /// 
        /// Example outputs:
        /// - "[UpdateTradingStatusBadge] Caller=LoadAccountSettings, Account='123456', LockStatus='Unlocked', IsLocked=False, PreviousState=True"
        /// - "[UpdateTradingStatusBadge] Caller=CheckExpiredLocks - No account selected, skipping update"
        /// </remarks>
        private void LogBadgeUpdate(string caller, string accountNumber, string lockStatusString, bool? isLocked, bool? previousState, string message)
        {
            // Pre-allocate array for better performance
            var parts = new string[LOG_PARTS_MAX];
            int index = 0;
            
            parts[index++] = $"[UpdateTradingStatusBadge] Caller={caller}";
            
            if (!string.IsNullOrEmpty(accountNumber))
                parts[index++] = $"Account='{accountNumber}'";
            
            if (lockStatusString != null)
                parts[index++] = $"LockStatus='{lockStatusString}'";
            
            if (isLocked.HasValue)
                parts[index++] = $"IsLocked={isLocked.Value}";
            
            if (previousState.HasValue)
                parts[index++] = $"PreviousState={previousState.Value}";
            
            if (!string.IsNullOrEmpty(message))
                parts[index++] = $"- {message}";
            
            // Join only the used parts - string.Join is optimized for arrays
            System.Diagnostics.Debug.WriteLine(string.Join(", ", parts, 0, index));
        }

        /// <summary>
        /// Helper method for consistent, structured logging of settings badge update operations.
        /// Formats log messages with contextual information for debugging and tracing.
        /// </summary>
        /// <param name="caller">The name of the calling method</param>
        /// <param name="accountNumber">The account number being processed (or null if not available)</param>
        /// <param name="isLocked">The determined lock state (true=locked, false=unlocked, null=not yet determined)</param>
        /// <param name="previousState">The cached previous lock state (true=locked, false=unlocked, null=not cached/first call)</param>
        /// <param name="message">Additional context or reason for the log entry (or null if general state logging)</param>
        /// <remarks>
        /// Example outputs:
        /// - "[UpdateSettingsStatusBadge] Caller=LoadAccountSettings, Account='123456', IsLocked=False, PreviousState=True"
        /// - "[UpdateSettingsStatusBadge] Caller=UpdateSettingsLockStatus - No account selected, skipping update"
        /// </remarks>
        /// <summary>
        /// Writes a debug message to both Debug output and a log file for Settings Badge diagnostics.
        /// File logging is enabled automatically when debugging issues.
        /// </summary>
        private void LogToFileAndDebug(string message)
        {
            // Always write to Debug output
            System.Diagnostics.Debug.WriteLine(message);
            
            // Initialize log file path on first use
            if (_badgeDebugLogPath == null)
            {
                try
                {
                    string tempPath = System.IO.Path.GetTempPath();
                    string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                    _badgeDebugLogPath = System.IO.Path.Combine(tempPath, $"RiskManager_SettingsBadge_Debug_{timestamp}.log");
                    
                    // Write header to new log file
                    string header = $"=== Settings Badge Debug Log ===\nStarted: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\nLog File: {_badgeDebugLogPath}\n{'='.ToString().PadRight(50, '=')}\n";
                    System.IO.File.WriteAllText(_badgeDebugLogPath, header);
                    
                    System.Diagnostics.Debug.WriteLine($"[SettingsBadge] Debug log file created: {_badgeDebugLogPath}");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[SettingsBadge] Failed to create debug log file: {ex.Message}");
                    _badgeDebugLogPath = ""; // Set to empty to prevent retry
                }
            }
            
            // Write to file if path is valid
            if (!string.IsNullOrEmpty(_badgeDebugLogPath))
            {
                try
                {
                    lock (_badgeDebugLogLock)
                    {
                        string timestampedMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {message}\n";
                        System.IO.File.AppendAllText(_badgeDebugLogPath, timestampedMessage);
                    }
                }
                catch (Exception ex)
                {
                    // Don't let file logging errors break the application
                    System.Diagnostics.Debug.WriteLine($"[SettingsBadge] Error writing to debug log: {ex.Message}");
                }
            }
        }

        private void LogSettingsBadgeUpdate(string caller, string accountNumber, bool? isLocked, bool? previousState, string message)
        {
            // Pre-allocate array for better performance
            var parts = new string[LOG_PARTS_MAX];
            int index = 0;
            
            parts[index++] = $"[UpdateSettingsStatusBadge] Caller={caller}";
            
            if (!string.IsNullOrEmpty(accountNumber))
                parts[index++] = $"Account='{accountNumber}'";
            
            if (isLocked.HasValue)
                parts[index++] = $"IsLocked={isLocked.Value}";
            
            if (previousState.HasValue)
                parts[index++] = $"PreviousState={previousState.Value}";
            
            if (!string.IsNullOrEmpty(message))
                parts[index++] = $"- {message}";
            
            // Join only the used parts - string.Join is optimized for arrays
            System.Diagnostics.Debug.WriteLine(string.Join(", ", parts, 0, index));
        }

        private void UpdateTradingStatusBadgeUI(string lockStatusString, string accountNumber, bool? previousStateParam = null, string callerInfoParam = null)
        {
            try
            {
                // Get caller information for debugging if not provided
                string callerName;
                if (!string.IsNullOrEmpty(callerInfoParam))
                {
                    callerName = callerInfoParam;
                }
                else
                {
                    var stackTrace = new System.Diagnostics.StackTrace(1, true);
                    var callerFrame = stackTrace.GetFrame(0);
                    var callerMethod = callerFrame?.GetMethod();
                    var lineNumber = callerFrame?.GetFileLineNumber() ?? 0;
                    callerName = callerMethod != null ? $"{callerMethod.DeclaringType?.Name}.{callerMethod.Name}" : "Unknown";
                    if (lineNumber > 0)
                    {
                        callerName = $"{callerName}:{lineNumber}";
                    }
                }
                
                // Use the passed account number instead of calling GetSelectedAccountNumber() again
                // This ensures we're using the SAME account number that was used to fetch the lock status
                bool? currentCachedState = !string.IsNullOrEmpty(accountNumber) && _accountTradingLockStateCache.TryGetValue(accountNumber, out var cached) ? cached : null;
                
                // Determine lock state from the status string
                bool isLocked = !lockStatusString.Equals(LOCK_STATUS_UNLOCKED, StringComparison.OrdinalIgnoreCase);
                
                string newState = isLocked ? "Locked (Red)" : "Unlocked (Green)";
                System.Diagnostics.Debug.WriteLine($"[UpdateTradingStatusBadgeUI] Called from {callerName}, Setting badge to {newState} (lockStatusString='{lockStatusString}') for account '{accountNumber}', Previous cache={(currentCachedState.HasValue ? currentCachedState.Value.ToString() : "null")}");
                
                // Update the status table
                if (statusTableView != null && statusTableView.Rows.Count > STATUS_TABLE_TRADING_ROW)
                {
                    // Trading Status is in row STATUS_TABLE_TRADING_ROW
                    // Use the lockStatusString directly from the settings service
                    // This ensures the badge shows exactly what's in the Accounts Summary tab
                    statusTableView.Rows[STATUS_TABLE_TRADING_ROW].Cells[2].Value = lockStatusString;
                    UpdateStatusTableCellColor(STATUS_TABLE_TRADING_ROW, lockStatusString);
                }
                
                // Also update the old badge for backward compatibility
                if (tradingStatusBadge != null)
                {
                    if (isLocked)
                    {
                        tradingStatusBadge.Text = "  Trading Locked  ";
                        tradingStatusBadge.BackColor = Color.Red;
                    }
                    else
                    {
                        tradingStatusBadge.Text = "  Trading Unlocked  ";
                        tradingStatusBadge.BackColor = AccentGreen;
                    }
                    tradingStatusBadge.Refresh(); // Force immediate repaint
                }
                
                // Update debug label with transition information
                // Use the previousStateParam if provided, otherwise use the cached value
                bool? previousStateForDebug = previousStateParam ?? currentCachedState;
                UpdateDebugLabel(callerName, previousStateForDebug, isLocked, accountNumber);
                
                // IMPORTANT: Update per-account cache to keep it in sync with the badge state
                // This ensures that direct calls to this method don't desync the cache
                if (!string.IsNullOrEmpty(accountNumber))
                {
                    _accountTradingLockStateCache[accountNumber] = isLocked;
                }
                
                System.Diagnostics.Debug.WriteLine($"[UpdateTradingStatusBadgeUI] Badge updated to {newState}, Cache for account '{accountNumber}' updated to {isLocked}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating trading status badge UI: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the debug label with badge transition information for visual debugging.
        /// </summary>
        /// <param name="callerName">The name of the calling function</param>
        /// <param name="previousState">The previous lock state (true=locked, false=unlocked, null=first call)</param>
        /// <param name="currentState">The current lock state (true=locked, false=unlocked)</param>
        /// <param name="accountNumber">The account number being displayed</param>
        private void UpdateDebugLabel(string callerName, bool? previousState, bool currentState, string accountNumber)
        {
            try
            {
                if (lblTradingStatusBadgeDebug != null && _badgeDebugMode)
                {
                    // Format: "Caller: FunctionName | Prev: False→True | Current: True | Account: 123456 | Time: HH:MM:SS.mmm"
                    var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                    var prevStateStr = previousState.HasValue ? previousState.Value.ToString() : "null";
                    var stateChangeStr = previousState.HasValue ? $"{prevStateStr}→{currentState}" : $"null→{currentState}";
                    var stateChanged = !previousState.HasValue || previousState.Value != currentState;
                    
                    var debugText = $"Caller: {callerName} | Prev: {stateChangeStr} | Current: {currentState} | Account: {accountNumber ?? "null"} | Changed: {stateChanged} | Time: {timestamp}";
                    
                    lblTradingStatusBadgeDebug.Text = debugText;
                    
                    // Change color based on whether state changed
                    lblTradingStatusBadgeDebug.ForeColor = stateChanged ? Color.Yellow : Color.LightGray;
                    
                    lblTradingStatusBadgeDebug.Refresh();
                    
                    System.Diagnostics.Debug.WriteLine($"[UpdateDebugLabel] {debugText}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating debug label: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the settings status badge based on the current lock state of the selected account.
        /// Displays lock duration when settings are locked (e.g., "Settings Locked (2h 30m)").
        /// Uses state caching to prevent redundant UI updates when the lock state hasn't changed.
        /// </summary>
        /// <param name="callerName">The name of the calling method (automatically populated by CallerMemberName attribute)</param>
        /// <remarks>
        /// This method:
        /// - Queries the settings service (JSON) directly for authoritative lock status with duration
        /// - Validates account selection and service initialization
        /// - Caches the previous state to avoid unnecessary UI updates
        /// - Logs all state transitions and validation issues for debugging
        /// - Only updates the UI when the state actually changes
        /// - Displays formatted duration (e.g., "Locked (2h 30m)", "Locked (1d 3h 15m)", "Unlocked")
        /// </remarks>
        private void UpdateSettingsStatusBadge([System.Runtime.CompilerServices.CallerMemberName] string callerName = "")
        {
            string accountNumber = null; // Declare outside try block for error logging
            try
            {
                // DEBUG: Entry point logging (FILE + Debug)
                LogToFileAndDebug($"[UpdateSettingsStatusBadge] === ENTRY === Caller={callerName}");
                
                accountNumber = GetSelectedAccountNumber();
                if (string.IsNullOrEmpty(accountNumber))
                {
                    LogSettingsBadgeUpdate(callerName, accountNumber, null, null, "No account selected, skipping update");
                    LogToFileAndDebug($"[UpdateSettingsStatusBadge] === EXIT (No Account) === Caller={callerName}");
                    return;
                }

                // DEBUG: Account number retrieved (FILE + Debug)
                LogToFileAndDebug($"[UpdateSettingsStatusBadge] Account Number: '{accountNumber}'");
                LogToFileAndDebug($"[UpdateSettingsStatusBadge] Current Badge Account: '{_currentSettingsBadgeAccountNumber ?? "NULL"}'");
                
                // Check if account has changed (account switch detection)
                bool accountChanged = _currentSettingsBadgeAccountNumber != accountNumber;
                LogToFileAndDebug($"[UpdateSettingsStatusBadge] Account Changed: {accountChanged}");

                var settingsService = RiskManagerSettingsService.Instance;
                if (!settingsService.IsInitialized)
                {
                    LogSettingsBadgeUpdate(callerName, accountNumber, null, null, "Settings service not initialized, skipping update");
                    LogToFileAndDebug($"[UpdateSettingsStatusBadge] === EXIT (Service Not Init) === Caller={callerName}");
                    return;
                }

                // Get current lock status with duration from service (authoritative JSON source)
                string lockStatusString = settingsService.GetSettingsLockStatusString(accountNumber);
                // Use explicit check to determine lock state (more robust than != "Unlocked")
                bool isLocked = lockStatusString.StartsWith("Locked", StringComparison.OrdinalIgnoreCase);
                
                // DEBUG: Status retrieved from service (FILE + Debug)
                LogToFileAndDebug($"[UpdateSettingsStatusBadge] Service returned: '{lockStatusString}', IsLocked={isLocked}");
                
                // Get the cached status string for THIS account (includes duration)
                string previousStatusString = _accountSettingsLockStatusCache.TryGetValue(accountNumber, out var cachedStatus) ? cachedStatus : null;
                
                // DEBUG: Cache state dump (FILE + Debug)
                LogToFileAndDebug($"[UpdateSettingsStatusBadge] === CACHE STATE DUMP ===");
                LogToFileAndDebug($"[UpdateSettingsStatusBadge] Cache Size: {_accountSettingsLockStatusCache.Count} entries");
                foreach (var kvp in _accountSettingsLockStatusCache)
                {
                    LogToFileAndDebug($"[UpdateSettingsStatusBadge] Cache Entry: Account='{kvp.Key}' -> Status='{kvp.Value}'");
                }
                LogToFileAndDebug($"[UpdateSettingsStatusBadge] === END CACHE DUMP ===");
                
                // DEBUG: Cache comparison (FILE + Debug)
                LogToFileAndDebug($"[UpdateSettingsStatusBadge] Cached Status: '{previousStatusString ?? "NULL"}'");
                LogToFileAndDebug($"[UpdateSettingsStatusBadge] Status Comparison: Previous='{previousStatusString ?? "NULL"}' vs Current='{lockStatusString}' Match={previousStatusString == lockStatusString}");
                
                // Log the determination with all relevant context
                LogSettingsBadgeUpdate(callerName, accountNumber, isLocked, isLocked ? (bool?)true : false, null);

                // Only update UI if the status string has actually changed OR if the account has changed
                // Account change detection ensures badge updates even if new account has same status as previous account
                if (!accountChanged && previousStatusString != null && previousStatusString == lockStatusString)
                {
                    LogSettingsBadgeUpdate(callerName, accountNumber, isLocked, null, "Status unchanged and same account, skipping UI update to prevent redundant refresh");
                    LogToFileAndDebug($"[UpdateSettingsStatusBadge] === EXIT (Status Unchanged, Same Account) === Caller={callerName}");
                    return;
                }
                
                // If we reach here, either status changed OR account changed - update UI
                if (accountChanged)
                {
                    LogToFileAndDebug($"[UpdateSettingsStatusBadge] FORCE UPDATE: Account changed from '{_currentSettingsBadgeAccountNumber ?? "NULL"}' to '{accountNumber}'");
                }
                
                // Cache the new status string for THIS account (includes duration for accurate comparison)
                _accountSettingsLockStatusCache[accountNumber] = lockStatusString;
                
                // DEBUG: Cache updated (FILE + Debug)
                LogToFileAndDebug($"[UpdateSettingsStatusBadge] Cache Updated: Account='{accountNumber}' -> Status='{lockStatusString}'");
                
                LogSettingsBadgeUpdate(callerName, accountNumber, isLocked, null, accountChanged ? "Account changed, updating UI" : "Status changed, updating UI");
                
                // Update the status table
                if (statusTableView != null && statusTableView.Rows.Count > STATUS_TABLE_SETTINGS_ROW)
                {
                    // Settings Status is in row STATUS_TABLE_SETTINGS_ROW
                    // Use the full status string with duration (e.g., "Locked (2h 30m)" or "Unlocked")
                    statusTableView.Rows[STATUS_TABLE_SETTINGS_ROW].Cells[2].Value = lockStatusString;
                    UpdateStatusTableCellColor(STATUS_TABLE_SETTINGS_ROW, lockStatusString);
                    LogToFileAndDebug($"[UpdateSettingsStatusBadge] Status Table Updated: '{lockStatusString}'");
                }
                else
                {
                    LogToFileAndDebug($"[UpdateSettingsStatusBadge] Status Table NOT updated (statusTableView={statusTableView != null}, RowCount={statusTableView?.Rows.Count ?? 0})");
                }
                
                // Also update the badge UI with duration information
                if (settingsStatusBadge != null)
                {
                    string oldBadgeText = settingsStatusBadge.Text;
                    Color oldBadgeColor = settingsStatusBadge.BackColor;
                    
                    if (isLocked)
                    {
                        // Display lock status with duration (e.g., "Settings Locked (2h 30m)")
                        settingsStatusBadge.Text = $"  Settings {lockStatusString}  ";
                        settingsStatusBadge.BackColor = Color.Red;
                    }
                    else
                    {
                        settingsStatusBadge.Text = "  Settings Unlocked  ";
                        settingsStatusBadge.BackColor = AccentGreen;
                    }
                    settingsStatusBadge.Invalidate();
                    
                    // DEBUG: Badge UI update confirmation (FILE + Debug)
                    LogToFileAndDebug($"[UpdateSettingsStatusBadge] Badge UI Updated:");
                    LogToFileAndDebug($"  Old: Text='{oldBadgeText}', Color={oldBadgeColor.Name}");
                    LogToFileAndDebug($"  New: Text='{settingsStatusBadge.Text}', Color={settingsStatusBadge.BackColor.Name}");
                }
                else
                {
                    LogToFileAndDebug($"[UpdateSettingsStatusBadge] Badge UI NOT updated (settingsStatusBadge is NULL)");
                }
                
                // Update the currently displayed account AFTER UI is successfully updated
                // This ensures we only mark the account as "displayed" once the badge actually shows it
                _currentSettingsBadgeAccountNumber = accountNumber;
                LogToFileAndDebug($"[UpdateSettingsStatusBadge] Current Badge Account Updated: '{_currentSettingsBadgeAccountNumber}' (after UI update)");
                
                // DEBUG: Exit point logging (FILE + Debug)
                LogToFileAndDebug($"[UpdateSettingsStatusBadge] === EXIT (Success) === Caller={callerName}, Final Status='{lockStatusString}'");
            }
            catch (Exception ex)
            {
                // Log error using the structured format with available context
                LogSettingsBadgeUpdate(callerName ?? "Unknown", accountNumber, null, null, $"ERROR: {ex.Message}");
                LogToFileAndDebug($"[UpdateSettingsStatusBadge] === EXIT (Exception) === Caller={callerName}");
                LogToFileAndDebug($"[UpdateSettingsStatusBadge] Stack trace: {ex.StackTrace}");
            }
        }

        private void UpdateSettingsLockStatus(Label lblSettingsStatus)
        {
            try
            {
                var accountNumber = GetSelectedAccountNumber();
                if (string.IsNullOrEmpty(accountNumber))
                {
                    lblSettingsStatus.Text = "No Account Selected";
                    lblSettingsStatus.ForeColor = TextGray;
                    return;
                }

                UpdateSettingsLockStatusForAccount(lblSettingsStatus, accountNumber);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating settings lock status: {ex.Message}");
                lblSettingsStatus.Text = "Status Error";
                lblSettingsStatus.ForeColor = TextGray;
            }
        }
        
        /// <summary>
        /// Updates the settings lock status label and badge for a specific account number.
        /// </summary>
        private void UpdateSettingsLockStatusForAccount(Label lblSettingsStatus, string accountNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(accountNumber))
                {
                    lblSettingsStatus.Text = "No Account Selected";
                    lblSettingsStatus.ForeColor = TextGray;
                    return;
                }

                var settingsService = RiskManagerSettingsService.Instance;
                if (!settingsService.IsInitialized)
                {
                    lblSettingsStatus.Text = "Status Unknown";
                    lblSettingsStatus.ForeColor = TextGray;
                    return;
                }

                bool isLocked = settingsService.AreSettingsLocked(accountNumber);

                if (isLocked)
                {
                    // Get remaining time and display it
                    var statusString = settingsService.GetSettingsLockStatusString(accountNumber);
                    lblSettingsStatus.Text = $"Settings {statusString}";
                    lblSettingsStatus.ForeColor = Color.Red;
                }
                else
                {
                    lblSettingsStatus.Text = "Settings Unlocked";
                    lblSettingsStatus.ForeColor = AccentGreen;
                }

                // Update top badge as well
                UpdateSettingsStatusBadge();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating settings lock status for account: {ex.Message}");
                lblSettingsStatus.Text = "Status Error";
                lblSettingsStatus.ForeColor = TextGray;
            }
        }

        /// <summary>
        /// Updates the manual lock status label with current lock state and remaining time.
        /// </summary>
        private void UpdateManualLockStatus(Label lblManualLockStatus)
        {
            try
            {
                var accountNumber = GetSelectedAccountNumber();
                if (string.IsNullOrEmpty(accountNumber))
                {
                    lblManualLockStatus.Text = "No Account Selected";
                    lblManualLockStatus.ForeColor = TextGray;
                    return;
                }

                UpdateManualLockStatusForAccount(lblManualLockStatus, accountNumber);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating manual lock status: {ex.Message}");
                lblManualLockStatus.Text = "Status Error";
                lblManualLockStatus.ForeColor = TextGray;
            }
        }
        
        /// <summary>
        /// Updates the manual lock status label for a specific account number.
        /// </summary>
        private void UpdateManualLockStatusForAccount(Label lblManualLockStatus, string accountNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(accountNumber))
                {
                    lblManualLockStatus.Text = "No Account Selected";
                    lblManualLockStatus.ForeColor = TextGray;
                    return;
                }

                var settingsService = RiskManagerSettingsService.Instance;
                if (!settingsService.IsInitialized)
                {
                    lblManualLockStatus.Text = "Status Unknown";
                    lblManualLockStatus.ForeColor = TextGray;
                    return;
                }

                bool isLocked = settingsService.IsTradingLocked(accountNumber);

                if (isLocked)
                {
                    // Get remaining time and display it using the same format as Lock Settings
                    var statusString = settingsService.GetLockStatusString(accountNumber);
                    lblManualLockStatus.Text = statusString;
                    lblManualLockStatus.ForeColor = Color.Red;
                }
                else
                {
                    lblManualLockStatus.Text = "Unlocked";
                    lblManualLockStatus.ForeColor = AccentGreen;
                }

                // NOTE: We do NOT update the badge here. The badge should only update from:
                // 1. Manual lock/unlock button clicks
                // 2. Account selection changes (LoadAccountSettings)
                // This method is called by timer-triggered updates, and we don't want the timer
                // to cause badge updates. The badge reads directly from JSON, so it will always
                // show the current state without needing timer-triggered updates.
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating manual lock status for account: {ex.Message}");
                lblManualLockStatus.Text = "Status Error";
                lblManualLockStatus.ForeColor = TextGray;
            }
        }

        private void EmergencyFlattenButton_Click(object sender, EventArgs e)
        {
            FlattenAllTrades();
            PlayAlertSound();
            MessageBox.Show("Emergency Flatten Triggered!\n\nAll trades have been closed.", "Emergency Flatten", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        /// <summary>
        /// Plays the alert sound from embedded resources when the Emergency Flatten button is pushed.
        /// </summary>
        private void PlayAlertSound()
        {
            try
            {
                // Get the sound stream from embedded resources
                var audioStream = Properties.Resources.dark_impact;
                if (audioStream != null)
                {
                    // Dispose existing player if any
                    alertSoundPlayer?.Dispose();
                    
                    // Create and store the player as a field to prevent premature garbage collection
                    alertSoundPlayer = new SoundPlayer(audioStream);
                    // Play asynchronously - sound plays in background without blocking
                    alertSoundPlayer.Play();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't interrupt the emergency flatten operation
                System.Diagnostics.Debug.WriteLine($"Error playing alert sound: {ex.Message}");
            }
        }

        /// <summary>
        /// Flattens all open trades across all accounts.
        /// This is a placeholder method that should be implemented to close all open positions.
        /// </summary>
        /// <remarks>
        /// When implemented, this method should:
        /// - Iterate through all open positions across all accounts
        /// - Close each position at market price
        /// - Handle any errors gracefully and report them to the user
        /// - Log all actions for audit purposes
        /// </remarks>
        public void FlattenAllTrades()
        {
            try
            {
                var core = Core.Instance;
                if (core != null)
                {
                    core.AdvancedTradingOperations.Flatten();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"FlattenAllTrades error: {ex.Message}");
                MessageBox.Show($"Error flattening trades: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Control CreateAutoLockPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title with emoji rendering
            var titleLabel = CreateEmojiLabel("🔒 Auto Lock Schedule", 14, FontStyle.Bold);
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 40;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleLabel.Padding = new Padding(10, 0, 0, 0);
            titleLabel.BackColor = DarkBackground;

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Configure automatic locking based on schedule:",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(10, 0, 10, 0),
                BackColor = DarkBackground,
                ForeColor = TextGray,
                AutoSize = false
            };

            var contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBackground,
                Padding = new Padding(15)
            };

            var enableCheckbox = new CheckBox
            {
                Text = "Enable Auto Lock Schedule",
                Left = 0,
                Top = 0,
                Width = 250,
                Height = 30,
                Checked = false,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = TextWhite,
                BackColor = CardBackground
            };
            contentArea.Controls.Add(enableCheckbox);

            var saveButton = CreateDarkSaveButton();

            // Add controls in correct order: Bottom first, Fill second, Top last
            mainPanel.Controls.Add(saveButton);
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(titleLabel);

            return mainPanel;
        }

        private Button CreateDarkSaveButton()
        {
            var saveButton = new Button
            {
                Text = "SAVE SETTINGS",
                Dock = DockStyle.Bottom,
                Height = 45,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = AccentGreen,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            saveButton.FlatAppearance.BorderSize = 0;
            saveButton.Click += (s, e) =>
            {
                try
                {
                    // Get the currently selected account number
                    var accountNumber = GetSelectedAccountNumber();
                    
                    // Debug logging
                    System.Diagnostics.Debug.WriteLine($"Save button clicked for account: '{accountNumber}'");
                    
                    if (string.IsNullOrEmpty(accountNumber))
                    {
                        MessageBox.Show(
                            "Please select an account first.",
                            "No Account Selected", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Warning);
                        return;
                    }
                    
                    // Access the settings service and create/update settings for this account
                    var service = RiskManagerSettingsService.Instance;
                    
                    if (!service.IsInitialized)
                    {
                        MessageBox.Show(
                            $"Settings initialization failed: {service.InitializationError}\n\nSettings will not be persisted.",
                            "Error", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Warning);
                        return;
                    }
                    
                    // Check if settings are locked
                    if (service.AreSettingsLocked(accountNumber))
                    {
                        var lockStatus = service.GetSettingsLockStatusString(accountNumber);
                        MessageBox.Show(
                            $"Settings are currently locked for this account.\n\nStatus: {lockStatus}\n\nYou cannot make changes to settings until the lock expires or is manually unlocked.",
                            "Settings Locked", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Warning);
                        return;
                    }
                    
                    // Ensure account exists
                    var settings = service.GetOrCreateSettings(accountNumber);
                    if (settings == null)
                    {
                        MessageBox.Show(
                            "Failed to create settings. Please try again.",
                            "Error", 
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Error);
                        return;
                    }

                    // Save Daily Loss Limit
                    if (dailyLossLimitEnabled?.Checked == true && dailyLossLimitInput != null)
                    {
                        if (decimal.TryParse(dailyLossLimitInput.Text, out var lossLimit))
                        {
                            if (lossLimit < 0)
                            {
                                MessageBox.Show("Daily loss limit cannot be negative.", "Validation Error", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            // Convert positive UI value to negative for internal storage (loss limits are negative)
                            service.UpdateDailyLossLimit(accountNumber, -lossLimit);
                        }
                        else
                        {
                            MessageBox.Show("Daily loss limit must be a valid number.", "Validation Error", 
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    else
                    {
                        service.UpdateDailyLossLimit(accountNumber, null);
                    }

                    // Save Daily Profit Target
                    if (dailyProfitTargetEnabled?.Checked == true && dailyProfitTargetInput != null)
                    {
                        if (decimal.TryParse(dailyProfitTargetInput.Text, out var profitTarget))
                        {
                            if (profitTarget < 0)
                            {
                                MessageBox.Show("Daily profit target cannot be negative.", "Validation Error", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            service.UpdateDailyProfitTarget(accountNumber, profitTarget);
                        }
                        else
                        {
                            MessageBox.Show("Daily profit target must be a valid number.", "Validation Error", 
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    else
                    {
                        service.UpdateDailyProfitTarget(accountNumber, null);
                    }

                    // Save Position Loss Limit
                    if (positionLossLimitEnabled?.Checked == true && positionLossLimitInput != null)
                    {
                        if (decimal.TryParse(positionLossLimitInput.Text, out var posLossLimit))
                        {
                            if (posLossLimit < 0)
                            {
                                MessageBox.Show("Position loss limit cannot be negative.", "Validation Error", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            // Convert positive UI value to negative for internal storage (loss limits are negative)
                            service.UpdatePositionLossLimit(accountNumber, -posLossLimit);
                        }
                        else
                        {
                            MessageBox.Show("Position loss limit must be a valid number.", "Validation Error", 
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    else
                    {
                        service.UpdatePositionLossLimit(accountNumber, null);
                    }

                    // Save Position Profit Target
                    if (positionProfitTargetEnabled?.Checked == true && positionProfitTargetInput != null)
                    {
                        if (decimal.TryParse(positionProfitTargetInput.Text, out var posProfitTarget))
                        {
                            if (posProfitTarget < 0)
                            {
                                MessageBox.Show("Position profit target cannot be negative.", "Validation Error", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            service.UpdatePositionProfitTarget(accountNumber, posProfitTarget);
                        }
                        else
                        {
                            MessageBox.Show("Position profit target must be a valid number.", "Validation Error", 
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    else
                    {
                        service.UpdatePositionProfitTarget(accountNumber, null);
                    }

                    // Save Weekly Loss Limit
                    if (weeklyLossLimitEnabled?.Checked == true && weeklyLossLimitInput != null)
                    {
                        if (decimal.TryParse(weeklyLossLimitInput.Text, out var weeklyLossLimit))
                        {
                            if (weeklyLossLimit < 0)
                            {
                                MessageBox.Show("Weekly loss limit cannot be negative.", "Validation Error", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            // Convert positive UI value to negative for internal storage (loss limits are negative)
                            service.UpdateWeeklyLossLimit(accountNumber, -weeklyLossLimit);
                        }
                        else
                        {
                            MessageBox.Show("Weekly loss limit must be a valid number.", "Validation Error", 
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    else
                    {
                        service.UpdateWeeklyLossLimit(accountNumber, null);
                    }

                    // Save Weekly Profit Target
                    if (weeklyProfitTargetEnabled?.Checked == true && weeklyProfitTargetInput != null)
                    {
                        if (decimal.TryParse(weeklyProfitTargetInput.Text, out var weeklyProfitTarget))
                        {
                            if (weeklyProfitTarget < 0)
                            {
                                MessageBox.Show("Weekly profit target cannot be negative.", "Validation Error", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            service.UpdateWeeklyProfitTarget(accountNumber, weeklyProfitTarget);
                        }
                        else
                        {
                            MessageBox.Show("Weekly profit target must be a valid number.", "Validation Error", 
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    else
                    {
                        service.UpdateWeeklyProfitTarget(accountNumber, null);
                    }

                    // Save Feature Toggle Enabled
                    if (featureToggleEnabledCheckbox != null)
                    {
                        service.UpdateFeatureToggleEnabled(accountNumber, featureToggleEnabledCheckbox.Checked);
                    }

                    // Save Individual Feature Toggles
                    if (positionsFeatureCheckbox != null && limitsFeatureCheckbox != null && 
                        symbolsFeatureCheckbox != null && tradingTimesFeatureCheckbox != null)
                    {
                        service.UpdateIndividualFeatureToggles(
                            accountNumber,
                            positionsFeatureCheckbox.Checked,
                            limitsFeatureCheckbox.Checked,
                            symbolsFeatureCheckbox.Checked,
                            tradingTimesFeatureCheckbox.Checked
                        );
                    }

                    // Save Enforcement Mode
                    if (strictModeRadioButton != null && warningModeRadioButton != null && monitorModeRadioButton != null)
                    {
                        RiskEnforcementMode mode = RiskEnforcementMode.Strict;
                        if (warningModeRadioButton.Checked)
                            mode = RiskEnforcementMode.Warning;
                        else if (monitorModeRadioButton.Checked)
                            mode = RiskEnforcementMode.Monitor;
                        
                        service.UpdateEnforcementMode(accountNumber, mode);
                    }

                    // Save Blocked Symbols
                    if (blockedSymbolsEnabled?.Checked == true && blockedSymbolsInput != null && !string.IsNullOrWhiteSpace(blockedSymbolsInput.Text))
                    {
                        var symbols = blockedSymbolsInput.Text
                            .Split(new[] { ',', ' ', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim())
                            .Where(s => !string.IsNullOrEmpty(s));
                        service.SetBlockedSymbols(accountNumber, symbols);
                    }
                    else
                    {
                        service.SetBlockedSymbols(accountNumber, Array.Empty<string>());
                    }

                    // Save Default Contract Limit
                    if (symbolContractLimitsEnabled?.Checked == true && defaultContractLimitInput != null)
                    {
                        if (int.TryParse(defaultContractLimitInput.Text, out var defaultLimit))
                        {
                            if (defaultLimit <= 0)
                            {
                                MessageBox.Show("Default contract limit must be a positive number.", "Validation Error", 
                                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            service.UpdateDefaultContractLimit(accountNumber, defaultLimit);
                        }
                        else
                        {
                            MessageBox.Show("Default contract limit must be a valid integer.", "Validation Error", 
                                MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            return;
                        }
                    }
                    else
                    {
                        service.UpdateDefaultContractLimit(accountNumber, null);
                    }

                    // Save Symbol-Specific Contract Limits
                    if (symbolContractLimitsEnabled?.Checked == true && symbolContractLimitsInput != null && !string.IsNullOrWhiteSpace(symbolContractLimitsInput.Text))
                    {
                        var limits = new Dictionary<string, int>();
                        var entries = symbolContractLimitsInput.Text.Split(new[] { ',', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var entry in entries)
                        {
                            var parts = entry.Split(':');
                            if (parts.Length != 2)
                            {
                                MessageBox.Show($"Invalid format for symbol contract limit: '{entry.Trim()}'. Expected format: 'SYMBOL:LIMIT'", 
                                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            
                            var symbol = parts[0].Trim();
                            if (string.IsNullOrEmpty(symbol))
                            {
                                MessageBox.Show("Symbol name cannot be empty in contract limits.", 
                                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            
                            if (!int.TryParse(parts[1].Trim(), out var limit))
                            {
                                MessageBox.Show($"Invalid contract limit value for symbol '{symbol}': '{parts[1].Trim()}'. Must be a valid integer.", 
                                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            
                            if (limit <= 0)
                            {
                                MessageBox.Show($"Contract limit for symbol '{symbol}' must be a positive number.", 
                                    "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                                return;
                            }
                            
                            limits[symbol] = limit;
                        }
                        service.SetSymbolContractLimits(accountNumber, limits);
                    }
                    else
                    {
                        service.SetSymbolContractLimits(accountNumber, new Dictionary<string, int>());
                    }

                    // Save Trading Lock
                    if (tradingLockCheckBox != null)
                    {
                        service.SetTradingLock(accountNumber, tradingLockCheckBox.Checked);
                    }

                    // Save Settings Lock
                    if (settingsLockCheckBox != null)
                    {
                        service.SetSettingsLock(accountNumber, settingsLockCheckBox.Checked);
                        
                        // Update the status label if it exists
                        if (settingsLockCheckBox.Tag is Label statusLabel)
                        {
                            UpdateSettingsLockStatus(statusLabel);
                        }
                    }

                    // Save Trading Time Restrictions
                    var tradingTimeRestrictions = new List<TradingTimeRestriction>();
                    
                    System.Diagnostics.Debug.WriteLine("=== Starting to save Trading Time Restrictions ===");
                    System.Diagnostics.Debug.WriteLine($"pageContents has {pageContents.Count} entries");
                    foreach (var kvp in pageContents)
                    {
                        System.Diagnostics.Debug.WriteLine($"  Page: '{kvp.Key}' - Control type: {kvp.Value?.GetType().Name}");
                    }
                    
                    // Find the trading times rows container
                    bool found = false;
                    foreach (Control pageControl in pageContents.Values)
                    {
                        System.Diagnostics.Debug.WriteLine($"Searching in page control: {pageControl?.GetType().Name}");
                        var tradingTimeContentArea = FindControlByTag(pageControl, "TradingTimeContentArea");
                        if (tradingTimeContentArea != null)
                        {
                            found = true;
                            System.Diagnostics.Debug.WriteLine("Found TradingTimeContentArea");
                            var rowsContainer = FindControlByTag(tradingTimeContentArea, "TradingTimeRowsContainer") as FlowLayoutPanel;
                            if (rowsContainer != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"Found rowsContainer with {rowsContainer.Controls.Count} controls");
                                foreach (Control rowControl in rowsContainer.Controls)
                                {
                                    if (rowControl is Panel rowPanel && rowPanel.Tag != null)
                                    {
                                        var controls = rowPanel.Tag as dynamic;
                                        if (controls != null)
                                        {
                                            try
                                            {
                                                // Parse day of week
                                                var dayStr = controls.DayComboBox.SelectedItem?.ToString();
                                                if (string.IsNullOrEmpty(dayStr)) continue;
                                                
                                                DayOfWeek dayOfWeek;
                                                if (!Enum.TryParse<DayOfWeek>(dayStr, out dayOfWeek))
                                                    continue;

                                                // Parse start time
                                                int startHour;
                                                if (!int.TryParse(controls.StartHourComboBox.SelectedItem?.ToString(), out startHour))
                                                    continue;
                                                int startMinute;
                                                if (!int.TryParse(controls.StartMinuteComboBox.SelectedItem?.ToString(), out startMinute))
                                                    continue;
                                                var startAmPm = controls.StartAmPmComboBox.SelectedItem?.ToString();
                                                if (string.IsNullOrEmpty(startAmPm)) continue;
                                                
                                                // Convert 12-hour to 24-hour
                                                if (startAmPm == "PM" && startHour != 12)
                                                    startHour += 12;
                                                else if (startAmPm == "AM" && startHour == 12)
                                                    startHour = 0;

                                                // Parse end time
                                                int endHour;
                                                if (!int.TryParse(controls.EndHourComboBox.SelectedItem?.ToString(), out endHour))
                                                    continue;
                                                int endMinute;
                                                if (!int.TryParse(controls.EndMinuteComboBox.SelectedItem?.ToString(), out endMinute))
                                                    continue;
                                                var endAmPm = controls.EndAmPmComboBox.SelectedItem?.ToString();
                                                if (string.IsNullOrEmpty(endAmPm)) continue;
                                                
                                                // Convert 12-hour to 24-hour
                                                if (endAmPm == "PM" && endHour != 12)
                                                    endHour += 12;
                                                else if (endAmPm == "AM" && endHour == 12)
                                                    endHour = 0;

                                                var startTime = new TimeSpan(startHour, startMinute, 0);
                                                var endTime = new TimeSpan(endHour, endMinute, 0);

                                                // Validate start time < end time
                                                if (startTime >= endTime)
                                                {
                                                    var startFormatted = FormatTimeSpan(startTime);
                                                    var endFormatted = FormatTimeSpan(endTime);
                                                    MessageBox.Show(
                                                        $"Start time must be before end time for {dayStr}.\nStart: {startFormatted}\nEnd: {endFormatted}",
                                                        "Validation Error",
                                                        MessageBoxButtons.OK,
                                                        MessageBoxIcon.Warning);
                                                    return;
                                                }

                                                // Create restriction with proper name formatting
                                                var startTimeFormatted = FormatTimeSpan(startTime);
                                                var endTimeFormatted = FormatTimeSpan(endTime);
                                                
                                                var restriction = new TradingTimeRestriction
                                                {
                                                    DayOfWeek = dayOfWeek,
                                                    StartTime = startTime,
                                                    EndTime = endTime,
                                                    IsAllowed = true,
                                                    Name = $"{dayStr} {startTimeFormatted} - {endTimeFormatted}"
                                                };

                                                tradingTimeRestrictions.Add(restriction);
                                                System.Diagnostics.Debug.WriteLine($"Added restriction: {restriction.Name}");
                                            }
                                            catch (Exception parseEx)
                                            {
                                                System.Diagnostics.Debug.WriteLine($"Error parsing trading time row: {parseEx.Message}");
                                                System.Diagnostics.Debug.WriteLine($"Stack trace: {parseEx.StackTrace}");
                                            }
                                        }
                                    }
                                }
                            }
                            break;
                        }
                    }
                    
                    if (!found)
                    {
                        System.Diagnostics.Debug.WriteLine("WARNING: TradingTimeContentArea NOT FOUND in any page!");
                    }
                    
                    // Save the trading time restrictions
                    System.Diagnostics.Debug.WriteLine($"Saving {tradingTimeRestrictions.Count} trading time restrictions for account: {accountNumber}");
                    service.SetTradingTimeRestrictions(accountNumber, tradingTimeRestrictions);
                    System.Diagnostics.Debug.WriteLine($"SetTradingTimeRestrictions completed. Settings file: {service.SettingsFolder}\\{accountNumber}.json");
                    
                    MessageBox.Show(
                        $"Settings saved successfully for account: {accountNumber}\n\nSettings folder: {service.SettingsFolder}",
                        "Success", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error saving settings: {ex.Message}",
                        "Error", 
                        MessageBoxButtons.OK, 
                        MessageBoxIcon.Error);
                }
            };
            return saveButton;
        }

        private Control CreateFeatureTogglesPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title (use exact key "Feature Toggles" so IconMap resolves to featuretoggles.png)
            var featureTogglesHeader = new CustomHeaderControl("Feature Toggles", GetIconForTitle("Feature Toggles"));
            featureTogglesHeader.Dock = DockStyle.Top;
            featureTogglesHeader.Margin = new Padding(10, 0, 0, 0);
            contentPanel.Controls.Add(featureTogglesHeader);

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Enable or disable features for your trading experience:",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(10, 0, 10, 0),
                BackColor = DarkBackground,
                ForeColor = TextGray,
                AutoSize = false
            };

            // Content area with features
            var contentArea = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBackground,
                Padding = new Padding(15),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            var features = new[]
            {
                "Enable All Features",
                "Positions",
                "Limits",
                "Symbols",
                "Allowed Trading Times"
            };

            for (int i = 0; i < features.Length; i++)
            {
                var feature = features[i];
                var checkbox = new CheckBox
                {
                    Text = feature,
                    AutoSize = true,
                    Checked = true,
                    Font = new Font("Segoe UI", 10, FontStyle.Regular),
                    ForeColor = TextWhite,
                    BackColor = CardBackground,
                    Margin = new Padding(0, 8, 0, 8)
                };
                contentArea.Controls.Add(checkbox);
                
                // Store reference to checkboxes
                if (i == 0 && feature == "Enable All Features")
                {
                    featureToggleEnabledCheckbox = checkbox;
                    // Add event handler for master toggle
                    checkbox.CheckedChanged += (s, e) =>
                    {
                        // When master toggle changes, sync individual toggles
                        var masterChecked = featureToggleEnabledCheckbox.Checked;
                        if (positionsFeatureCheckbox != null) positionsFeatureCheckbox.Checked = masterChecked;
                        if (limitsFeatureCheckbox != null) limitsFeatureCheckbox.Checked = masterChecked;
                        if (symbolsFeatureCheckbox != null) symbolsFeatureCheckbox.Checked = masterChecked;
                        if (tradingTimesFeatureCheckbox != null) tradingTimesFeatureCheckbox.Checked = masterChecked;
                    };
                }
                else if (feature == "Positions")
                {
                    positionsFeatureCheckbox = checkbox;
                }
                else if (feature == "Limits")
                {
                    limitsFeatureCheckbox = checkbox;
                }
                else if (feature == "Symbols")
                {
                    symbolsFeatureCheckbox = checkbox;
                }
                else if (feature == "Allowed Trading Times")
                {
                    tradingTimesFeatureCheckbox = checkbox;
                }
            }

            // Add a separator
            var separatorLabel = new Label
            {
                Text = "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━",
                AutoSize = true,
                Font = new Font("Segoe UI", 8, FontStyle.Regular),
                ForeColor = Color.FromArgb(80, 80, 80),
                BackColor = CardBackground,
                Margin = new Padding(0, 15, 0, 10)
            };
            contentArea.Controls.Add(separatorLabel);

            // Add enforcement mode section header
            var enforcementModeHeader = new Label
            {
                Text = "Risk Enforcement Mode:",
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                Margin = new Padding(0, 5, 0, 8)
            };
            contentArea.Controls.Add(enforcementModeHeader);

            // Add enforcement mode description
            var enforcementModeDesc = new Label
            {
                Text = "Select how risk limits are enforced (only one can be active):",
                AutoSize = true,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = TextGray,
                BackColor = CardBackground,
                Margin = new Padding(0, 0, 0, 8)
            };
            contentArea.Controls.Add(enforcementModeDesc);

            // Create radio buttons for enforcement modes
            strictModeRadioButton = new RadioButton
            {
                Text = "Strict Mode - Immediately enforce all limits (close positions and lock account)",
                AutoSize = true,
                Checked = true, // Default
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                Margin = new Padding(0, 4, 0, 4)
            };
            contentArea.Controls.Add(strictModeRadioButton);

            warningModeRadioButton = new RadioButton
            {
                Text = "Warning Mode - Show warnings only, no automatic enforcement",
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                Margin = new Padding(0, 4, 0, 4)
            };
            contentArea.Controls.Add(warningModeRadioButton);

            monitorModeRadioButton = new RadioButton
            {
                Text = "Monitor Mode - Track limits silently without warnings or enforcement",
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                Margin = new Padding(0, 4, 0, 12)
            };
            contentArea.Controls.Add(monitorModeRadioButton);

            var saveButton = CreateDarkSaveButton();

            // Add controls in correct order: Bottom first, Fill second, Top last
            // In WinForms, docking is processed in reverse Z-order
            mainPanel.Controls.Add(saveButton);
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(featureTogglesHeader);

            return mainPanel;
        }

        /// <summary>
        /// Creates the "General Settings" panel with Theme Switcher and Progress Bar checkbox.
        /// </summary>
        private Control CreateGeneralSettingsPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title (use exact key "General Settings" so IconMap resolves to generalsettings.png)
            var generalSettingsHeader = new CustomHeaderControl("General Settings", GetIconForTitle("General Settings"));
            generalSettingsHeader.Dock = DockStyle.Top;
            generalSettingsHeader.Margin = new Padding(10, 0, 0, 0);

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Customize your application appearance and behavior:",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(10, 0, 10, 0),
                BackColor = DarkBackground,
                ForeColor = TextGray,
                AutoSize = false
            };

            // Content area
            var contentArea = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBackground,
                Padding = new Padding(15),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            // Theme Switcher Section
            var themeSectionLabel = new Label
            {
                Text = "Theme Settings",
                AutoSize = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                Margin = new Padding(0, 0, 0, 10)
            };
            contentArea.Controls.Add(themeSectionLabel);

            // Theme switcher button with current theme label
            var themePanel = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                BackColor = CardBackground,
                Margin = new Padding(0, 0, 0, 20)
            };

            var themeSwitcherButton = new Button
            {
                Width = 50,
                Height = 40,
                ForeColor = Color.White,
                BackColor = Color.FromArgb(52, 152, 219),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0),
                Padding = new Padding(0),
                UseCompatibleTextRendering = true
            };
            themeSwitcherButton.FlatAppearance.BorderSize = 0;
            themeSwitcherButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(41, 128, 185);

            // Set theme switcher image
            Image themeImg = null;
            if (IconMap.TryGetValue("ThemeSwitcher", out var img) && img != null)
                themeImg = img;
            else
            {
                try { themeImg = Properties.Resources.themeswitcher2; } catch { themeImg = null; }
            }

            if (themeImg != null)
            {
                var scaledThemeImg = ScaleImageToFit(themeImg, themeSwitcherButton.Width - 6, themeSwitcherButton.Height - 6);
                themeSwitcherButton.Image = scaledThemeImg;
                themeSwitcherButton.ImageAlign = ContentAlignment.MiddleCenter;
                themeSwitcherButton.Text = "";
            }
            else
            {
                themeSwitcherButton.Text = "🎨";
                themeSwitcherButton.Font = new Font("Segoe UI Emoji", 16, FontStyle.Bold);
            }

            // Current theme label
            currentThemeLabel = new Label
            {
                Text = $"Current Theme: {GetThemeDisplayName(currentTheme)}",
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(0, 10, 0, 0)
            };

            themeSwitcherButton.Click += (s, e) =>
            {
                // Cycle through themes: Blue -> Black -> White -> YellowBlueBlack -> Blue
                switch (currentTheme)
                {
                    case Theme.Blue:
                        ApplyTheme(Theme.Black);
                        break;
                    case Theme.Black:
                        ApplyTheme(Theme.White);
                        break;
                    case Theme.White:
                        ApplyTheme(Theme.YellowBlueBlack);
                        break;
                    case Theme.YellowBlueBlack:
                        ApplyTheme(Theme.Blue);
                        break;
                    default:
                        ApplyTheme(Theme.Blue);
                        break;
                }
                // Update the theme label
                if (currentThemeLabel != null)
                {
                    currentThemeLabel.Text = $"Current Theme: {GetThemeDisplayName(currentTheme)}";
                }
            };

            themePanel.Controls.Add(themeSwitcherButton);
            themePanel.Controls.Add(currentThemeLabel);
            contentArea.Controls.Add(themePanel);

            // Divider
            var divider1 = new Panel
            {
                Height = 2,
                Width = 600,
                BackColor = DarkerBackground,
                Margin = new Padding(0, 10, 0, 20)
            };
            contentArea.Controls.Add(divider1);

            // Progress Bar Section
            var progressBarSectionLabel = new Label
            {
                Text = "Data Grid Display",
                AutoSize = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                Margin = new Padding(0, 0, 0, 10)
            };
            contentArea.Controls.Add(progressBarSectionLabel);

            // Progress bar checkbox
            showProgressBarsCheckBox = new CheckBox
            {
                Text = "Show Progress Bars",
                AutoSize = true,
                Checked = showProgressBars,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                Margin = new Padding(0, 0, 0, 8)
            };
            
            showProgressBarsCheckBox.CheckedChanged += (s, e) =>
            {
                showProgressBars = showProgressBarsCheckBox.Checked;
                SaveProgressBarPreference();
                
                // Refresh all data grids to apply/remove progress bars
                RefreshAllDataGrids();
            };

            contentArea.Controls.Add(showProgressBarsCheckBox);

            // Show percentage checkbox
            showPercentageCheckBox = new CheckBox
            {
                Text = "Show Percentage Instead of Dollar Amount",
                AutoSize = true,
                Checked = showPercentage,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                Margin = new Padding(0, 0, 0, 8)
            };
            
            showPercentageCheckBox.CheckedChanged += (s, e) =>
            {
                showPercentage = showPercentageCheckBox.Checked;
                SaveShowPercentagePreference();
                
                // Refresh all data grids to update display format
                RefreshAllDataGrids();
            };

            contentArea.Controls.Add(showPercentageCheckBox);

            // Info label for progress bars
            var progressBarInfoLabel = new Label
            {
                Text = "When enabled, data grid columns will be replaced with progress bars showing:\n" +
                       "• Gross P&L progress toward Daily Loss Limit or Daily Profit Target\n" +
                       "• Open P&L progress based on Position Profit and Position Loss Limit\n" +
                       "• Color-coded bars (green/yellow/red) based on proximity to limits",
                AutoSize = true,
                MaximumSize = new Size(600, 0),
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = TextGray,
                BackColor = CardBackground,
                Margin = new Padding(20, 0, 0, 10)
            };
            contentArea.Controls.Add(progressBarInfoLabel);

            // Divider
            var divider2 = new Panel
            {
                Height = 2,
                Width = 600,
                BackColor = DarkerBackground,
                Margin = new Padding(0, 10, 0, 20)
            };
            contentArea.Controls.Add(divider2);

            // Privacy Mode Section
            var privacyModeSectionLabel = new Label
            {
                Text = "Privacy Settings",
                AutoSize = true,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                Margin = new Padding(0, 0, 0, 10)
            };
            contentArea.Controls.Add(privacyModeSectionLabel);

            // Privacy mode checkbox
            var privacyModeCheckBox = new CheckBox
            {
                Text = "Privacy Mode (mask account numbers)",
                AutoSize = true,
                Checked = false,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                Margin = new Padding(0, 0, 0, 8)
            };
            
            // Load current privacy mode setting
            if (accountSelector != null && accountSelector.SelectedItem is Account selectedAcc)
            {
                var accountNumber = GetAccountIdentifier(selectedAcc);
                var settings = RiskManagerSettingsService.Instance.GetSettings(accountNumber);
                if (settings != null)
                {
                    privacyModeCheckBox.Checked = settings.PrivacyModeEnabled;
                }
            }
            
            privacyModeCheckBox.CheckedChanged += (s, e) =>
            {
                if (accountSelector != null && accountSelector.SelectedItem is Account selectedAcc)
                {
                    var accountNumber = GetAccountIdentifier(selectedAcc);
                    RiskManagerSettingsService.Instance.UpdatePrivacyMode(accountNumber, privacyModeCheckBox.Checked);
                    
                    // Refresh account selector to show/hide masked account numbers
                    RefreshAccountDropdown();
                }
            };

            contentArea.Controls.Add(privacyModeCheckBox);

            // Info label for privacy mode
            var privacyModeInfoLabel = new Label
            {
                Text = "When enabled, account numbers will be partially masked with asterisks (*).\n" +
                       "Only the first 4 characters will be visible, the rest will be masked.\n" +
                       "This is useful when streaming or taking screenshots to protect your account privacy.\n" +
                       "Note: This only affects the display - it does not change any backend data.",
                AutoSize = true,
                MaximumSize = new Size(600, 0),
                Font = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = TextGray,
                BackColor = CardBackground,
                Margin = new Padding(20, 0, 0, 10)
            };
            contentArea.Controls.Add(privacyModeInfoLabel);

            // Add controls in correct order for docking
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(generalSettingsHeader);

            return mainPanel;
        }

        /// <summary>
        /// Creates the "Copy Settings" panel for copying settings from one account to multiple accounts.
        /// </summary>
        private Control CreateCopySettingsPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title
            var copySettingsHeader = new CustomHeaderControl("Copy Settings", GetIconForTitle("Copy Settings"));
            copySettingsHeader.Dock = DockStyle.Top;
            copySettingsHeader.Margin = new Padding(10, 0, 0, 0);
            contentPanel.Controls.Add(copySettingsHeader);

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Copy risk management settings from one account to multiple target accounts:",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(10, 0, 10, 0),
                BackColor = DarkBackground,
                ForeColor = TextGray,
                AutoSize = false
            };

            // Content area
            var contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBackground,
                Padding = new Padding(20),
                AutoScroll = true
            };

            var contentFlow = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Dock = DockStyle.Top,
                BackColor = CardBackground
            };

            // Source Account Section
            var sourceLabel = new Label
            {
                Text = "Source Account (Copy From):",
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = TextWhite,
                Margin = new Padding(0, 0, 0, 10)
            };
            contentFlow.Controls.Add(sourceLabel);

            copySettingsSourceComboBox = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 10),
                Width = 400,
                BackColor = DarkBackground,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat,
                Margin = new Padding(0, 0, 0, 20),
                DrawMode = DrawMode.OwnerDrawFixed
            };
            copySettingsSourceComboBox.DrawItem += CopySettingsSourceComboBox_DrawItem;
            contentFlow.Controls.Add(copySettingsSourceComboBox);

            // Target Accounts Section
            var targetLabel = new Label
            {
                Text = "Target Accounts (Copy To):",
                AutoSize = true,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = TextWhite,
                Margin = new Padding(0, 10, 0, 10)
            };
            contentFlow.Controls.Add(targetLabel);

            copySettingsTargetPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                Width = 400,
                BackColor = DarkerBackground,
                Padding = new Padding(15),
                Margin = new Padding(0, 0, 0, 20)
            };
            contentFlow.Controls.Add(copySettingsTargetPanel);

            // Initial population will happen in RefreshCopySettingsAccounts
            RefreshCopySettingsAccounts();

            // Update target checkboxes when source is selected
            copySettingsSourceComboBox.SelectedIndexChanged += (s, e) =>
            {
                copySettingsTargetPanel.Controls.Clear();
                
                if (copySettingsSourceComboBox.SelectedItem is Account sourceAccount)
                {
                    // Get current connected accounts
                    var core = Core.Instance;
                    var connectedAccounts = core?.Accounts?
                        .Where(a => a != null && a.Connection != null)
                        .ToList() ?? new List<Account>();
                    
                    // Add checkboxes for all accounts except the source
                    foreach (var account in connectedAccounts)
                    {
                        if (account == sourceAccount) continue;

                        // Get account identifier and apply masking if needed
                        string accountIdentifier = GetAccountIdentifier(account);
                        string displayIdentifier = MaskAccountNumber(accountIdentifier);
                        
                        var checkbox = new CheckBox
                        {
                            Text = $"{account.Name} ({displayIdentifier})",
                            Tag = account,
                            AutoSize = true,
                            Font = new Font("Segoe UI", 9.5f),
                            ForeColor = TextWhite,
                            BackColor = DarkerBackground,
                            Margin = new Padding(0, 5, 0, 5)
                        };
                        copySettingsTargetPanel.Controls.Add(checkbox);
                    }

                    if (copySettingsTargetPanel.Controls.Count == 0)
                    {
                        var noAccountsLabel = new Label
                        {
                            Text = "No other accounts available",
                            AutoSize = true,
                            Font = new Font("Segoe UI", 9.5f, FontStyle.Italic),
                            ForeColor = TextGray,
                            BackColor = DarkerBackground
                        };
                        copySettingsTargetPanel.Controls.Add(noAccountsLabel);
                    }
                }
            };

            // Select All / Deselect All buttons
            var buttonPanel = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 20)
            };

            var selectAllButton = new Button
            {
                Text = "Select All",
                Width = 120,
                Height = 35,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 10, 0)
            };
            selectAllButton.FlatAppearance.BorderSize = 0;
            selectAllButton.Click += (s, e) =>
            {
                foreach (Control control in copySettingsTargetPanel.Controls)
                {
                    if (control is CheckBox cb)
                        cb.Checked = true;
                }
            };
            buttonPanel.Controls.Add(selectAllButton);

            var deselectAllButton = new Button
            {
                Text = "Deselect All",
                Width = 120,
                Height = 35,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                BackColor = Color.FromArgb(52, 73, 94),
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            deselectAllButton.FlatAppearance.BorderSize = 0;
            deselectAllButton.Click += (s, e) =>
            {
                foreach (Control control in copySettingsTargetPanel.Controls)
                {
                    if (control is CheckBox cb)
                        cb.Checked = false;
                }
            };
            buttonPanel.Controls.Add(deselectAllButton);

            contentFlow.Controls.Add(buttonPanel);

            contentArea.Controls.Add(contentFlow);

            // Copy Button
            var copyButton = new Button
            {
                Text = "COPY SETTINGS TO SELECTED ACCOUNTS",
                Dock = DockStyle.Bottom,
                Height = 50,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = AccentGreen,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            copyButton.FlatAppearance.BorderSize = 0;
            copyButton.Click += (s, e) =>
            {
                try
                {
                    // Validate source account selection
                    if (copySettingsSourceComboBox.SelectedItem == null)
                    {
                        MessageBox.Show(
                            "Please select a source account to copy settings from.",
                            "Validation Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    // Get selected target accounts
                    var targetAccounts = new List<string>();
                    foreach (Control control in copySettingsTargetPanel.Controls)
                    {
                        if (control is CheckBox cb && cb.Checked && cb.Tag is Account account)
                        {
                            targetAccounts.Add(GetAccountIdentifier(account));
                        }
                    }

                    if (targetAccounts.Count == 0)
                    {
                        MessageBox.Show(
                            "Please select at least one target account to copy settings to.",
                            "Validation Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                        return;
                    }

                    // Confirm action
                    if (!(copySettingsSourceComboBox.SelectedItem is Account sourceAccount))
                    {
                        MessageBox.Show(
                            "Invalid source account selected.",
                            "Error",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        return;
                    }
                    
                    var sourceAccountId = GetAccountIdentifier(sourceAccount);
                    var confirmMessage = $"Are you sure you want to copy settings from:\n\n" +
                                       $"Source: {sourceAccount.Name} ({sourceAccountId})\n\n" +
                                       $"To {targetAccounts.Count} target account(s)?\n\n" +
                                       $"This will overwrite existing settings on the target accounts.";
                    
                    var result = MessageBox.Show(
                        confirmMessage,
                        "Confirm Copy Settings",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Question);

                    if (result != DialogResult.Yes)
                        return;

                    // Perform the copy operation
                    var service = RiskManagerSettingsService.Instance;
                    var copyResults = service.CopySettingsToAccounts(sourceAccountId, targetAccounts);

                    // Build result message
                    var successCount = copyResults.Count(r => r.Value.Success);
                    var failureCount = copyResults.Count(r => !r.Value.Success);
                    
                    var resultMessage = $"Copy operation completed:\n\n" +
                                      $"✓ Successful: {successCount}\n" +
                                      $"✗ Failed: {failureCount}";

                    if (failureCount > 0)
                    {
                        resultMessage += "\n\nFailed accounts:\n";
                        foreach (var kvp in copyResults.Where(r => !r.Value.Success))
                        {
                            resultMessage += $"- {kvp.Key}: {kvp.Value.Message}\n";
                        }
                    }

                    MessageBox.Show(
                        resultMessage,
                        failureCount > 0 ? "Copy Completed with Errors" : "Success",
                        MessageBoxButtons.OK,
                        failureCount > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);

                    // Clear selections on success
                    if (successCount > 0)
                    {
                        foreach (Control control in copySettingsTargetPanel.Controls)
                        {
                            if (control is CheckBox cb)
                                cb.Checked = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Error copying settings: {ex.Message}",
                        "Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            };

            // Add controls in correct order
            mainPanel.Controls.Add(copyButton);
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(copySettingsHeader);

            return mainPanel;
        }

        /// <summary>
        /// Creates the "Risk Overview" panel displaying comprehensive risk settings for the selected account.
        /// </summary>
        private Control CreateRiskOverviewPanel()
        {
            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Comprehensive risk settings overview for the selected account:",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(10, 0, 10, 0),
                BackColor = DarkBackground,
                ForeColor = TextGray,
                AutoSize = false
            };

            // Content area with scroll
            var contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkBackground,
                Padding = new Padding(15),
                AutoScroll = true
            };

            // Create a flow layout for risk settings cards in 3 rows x 2 columns
            var flowLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                FlowDirection = FlowDirection.LeftToRight, // Arrange cards horizontally first
                WrapContents = true, // Enable wrapping to create a grid layout
                AutoSize = true,
                BackColor = DarkBackground,
                Padding = new Padding(0, 0, 15, 0) // Right padding for scrollbar
            };

            // Add cards to the flow layout
            flowLayout.Controls.Add(CreateRiskOverviewCard(
                CARD_TITLE_ACCOUNT_STATUS,
                new[] { "Lock Status:", "Settings Lock:" },
                new[] { GetAccountLockStatus, GetSettingsLockStatus }
            ));

            flowLayout.Controls.Add(CreateRiskOverviewCard(
                "Position Limits",
                new[] { "Loss Limit:", "Profit Target:" },
                new[] { GetPositionLossLimit, GetPositionProfitTarget },
                () => IsFeatureEnabled(s => s.PositionsEnabled)
            ));

            flowLayout.Controls.Add(CreateRiskOverviewCard(
                "Daily Limits",
                new[] { "Daily Loss Limit:", "Daily Profit Target:" },
                new[] { GetDailyLossLimit, GetDailyProfitTarget },
                () => IsFeatureEnabled(s => s.LimitsEnabled)
            ));

            flowLayout.Controls.Add(CreateRiskOverviewCard(
                "Symbol Restrictions",
                new[] { "Blocked Symbols:", "Default Contract Limit:", "Symbol-Specific Limits:" },
                new[] { GetBlockedSymbols, GetDefaultContractLimit, GetSymbolContractLimits },
                () => IsFeatureEnabled(s => s.SymbolsEnabled)
            ));

            var tradingTimesCard = CreateTradingTimesOverviewCard();
            flowLayout.Controls.Add(tradingTimesCard);

            // Add the flow layout to the content area
            contentArea.Controls.Add(flowLayout);

            // Create a container panel to hold the subtitle and content area
            var containerPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = DarkBackground
            };

            containerPanel.Controls.Add(contentArea);
            containerPanel.Controls.Add(subtitleLabel);

            return containerPanel;
        }

        /// <summary>
        /// Creates a card panel for displaying risk overview information.
        /// </summary>
        private Panel CreateRiskOverviewCard(string title, string[] labels, Func<string>[] valueGetters, Func<bool> isFeatureEnabled = null)
        {
            var cardPanel = new Panel
            {
                Width = 480, // Adjusted width for 2-column layout
                AutoSize = true,
                BackColor = CardBackground,
                Padding = new Padding(20),
                Margin = new Padding(0, 0, 15, 15), // Add right and bottom margin for spacing
                Tag = isFeatureEnabled // Store the feature checker for later refresh
            };

            var cardLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = CardBackground
            };

            var header = new CustomCardHeaderControl(title, GetIconForTitle(title))
            {
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 15)
            };
            cardLayout.Controls.Add(header);

            // Add each label-value pair
            for (int i = 0; i < labels.Length && i < valueGetters.Length; i++)
            {
                var rowPanel = new Panel
                {
                    Width = 440, // Adjusted for new card width
                    Height = 30,
                    BackColor = CardBackground,
                    Margin = new Padding(0, 5, 0, 5)
                };

                var labelControl = new Label
                {
                    Text = labels[i],
                    Left = 0,
                    Top = 5,
                    Width = 180, // Adjusted for new card width
                    Font = new Font("Segoe UI", 10, FontStyle.Bold),
                    ForeColor = TextWhite,
                    BackColor = CardBackground
                };
                rowPanel.Controls.Add(labelControl);

                var valueText = valueGetters[i]();
                var valueControl = new Label
                {
                    Text = valueText,
                    Left = 190, // Adjusted for new card width
                    Top = 5,
                    Width = 240, // Adjusted for new card width
                    Font = new Font("Segoe UI Emoji", 10, FontStyle.Regular), // Use Segoe UI Emoji for emoji support
                    ForeColor = TextGray,
                    BackColor = CardBackground,
                    Tag = valueGetters[i], // Store the getter function for later refresh
                    UseCompatibleTextRendering = false // Use GDI for proper emoji rendering
                };
                
                // Apply lock status color-coding for "Account Status" card
                if (title == CARD_TITLE_ACCOUNT_STATUS)
                {
                    var lockStatusText = ExtractLockStatusText(valueText);
                    valueControl.ForeColor = GetLockStatusColor(lockStatusText);
                    
                    // Add extra spacing between emoji and text for better readability
                    if (IsLockStatusValue(valueText))
                    {
                        var idx = valueText.IndexOf(' ');
                        if (idx >= 0 && idx + 1 < valueText.Length)
                        {
                            // Replace single space with double space after emoji
                            valueControl.Text = valueText.Substring(0, idx) + "  " + valueText.Substring(idx + 1);
                        }
                    }
                }
                
                rowPanel.Controls.Add(valueControl);

                cardLayout.Controls.Add(rowPanel);
            }

            cardPanel.Controls.Add(cardLayout);
            
            // Add disabled overlay if feature is disabled
            UpdateCardOverlay(cardPanel);
            
            return cardPanel;
        }

        /// <summary>
        /// Creates a specialized Trading Times card showing days and sessions (Asia, London, New York).
        /// </summary>
        private Panel CreateTradingTimesOverviewCard()
        {
            var cardPanel = new Panel
            {
                Width = 480,
                AutoSize = true,
                BackColor = CardBackground,
                Padding = new Padding(20),
                Margin = new Padding(0, 0, 15, 15),
                Tag = "TradingTimesCard" // Tag to identify this card for refresh
            };

            var cardLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                AutoSize = true,
                BackColor = CardBackground
            };

            // Card title
            var header = new CustomCardHeaderControl("Allowed Trading Times", GetIconForTitle("Allowed Trading Times"))
            {
                Dock = DockStyle.Top,
                Margin = new Padding(0, 0, 0, 10)
            };
            cardLayout.Controls.Add(header);

            // Get trading time restrictions
            var accountNumber = GetSelectedAccountNumber();
            var settingsService = RiskManagerSettingsService.Instance;

            if (string.IsNullOrEmpty(accountNumber) || !settingsService.IsInitialized)
            {
                var noDataLabel = new Label
                {
                    Text = "⚠️ No account selected",
                    Width = 440,
                    Height = 25,
                    Font = new Font("Segoe UI Emoji", 10, FontStyle.Regular),
                    ForeColor = TextGray,
                    BackColor = CardBackground,
                    UseCompatibleTextRendering = false
                };
                cardLayout.Controls.Add(noDataLabel);
            }
            else
            {
                var settings = settingsService.GetSettings(accountNumber);

                if (settings?.TradingTimeRestrictions == null || !settings.TradingTimeRestrictions.Any())
                {
                    // No restrictions = 24/7 trading
                    var noRestrictionsLabel = new Label
                    {
                        Text = "✅ No restrictions (24/7 trading allowed)",
                        Width = 440,
                        Height = 25,
                        Font = new Font("Segoe UI", 10, FontStyle.Regular),
                        ForeColor = AccentGreen,
                        BackColor = CardBackground
                    };
                    cardLayout.Controls.Add(noRestrictionsLabel);
                }
                else
                {
                    // Group restrictions by day of week
                    var groupedByDay = settings.TradingTimeRestrictions
                        .GroupBy(r => r.DayOfWeek)
                        .OrderBy(g => (int)g.Key);

                    foreach (var dayGroup in groupedByDay)
                    {
                        var dayName = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.GetDayName(dayGroup.Key);
                        
                        // Day header
                        var dayLabel = new Label
                        {
                            Text = dayName,
                            Width = 440,
                            Height = 25,
                            Font = new Font("Segoe UI", 10, FontStyle.Bold),
                            ForeColor = TextWhite,
                            BackColor = CardBackground,
                            Margin = new Padding(0, 5, 0, 2)
                        };
                        cardLayout.Controls.Add(dayLabel);

                        // Time windows for this day
                        foreach (var restriction in dayGroup.OrderBy(r => r.StartTime))
                        {
                            var timeText = $"  • {FormatTimeSpan(restriction.StartTime)} to {FormatTimeSpan(restriction.EndTime)}";
                            var timeLabel = new Label
                            {
                                Text = timeText,
                                Width = 440,
                                Height = 20,
                                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                                ForeColor = TextGray,
                                BackColor = CardBackground,
                                Margin = new Padding(0, 0, 0, 2)
                            };
                            cardLayout.Controls.Add(timeLabel);
                        }
                    }
                }
            }

            cardPanel.Controls.Add(cardLayout);
            
            // Add disabled overlay if feature is disabled
            if (!IsFeatureEnabled(s => s.TradingTimesEnabled))
            {
                AddDisabledOverlay(cardPanel);
            }
            
            return cardPanel;
        }

        // Helper method to format TimeSpan as human-readable time
        private string FormatTimeSpan(TimeSpan time)
        {
            int hour = time.Hours;
            int minute = time.Minutes;
            string ampm = hour >= 12 ? "PM" : "AM";
            
            if (hour > 12) hour -= 12;
            if (hour == 0) hour = 12;
            
            return $"{hour}:{minute:D2} {ampm}";
        }

        // Helper methods to get risk setting values for Risk Overview
        private string GetAccountLockStatus()
        {
            var accountNumber = GetSelectedAccountNumber();
            if (string.IsNullOrEmpty(accountNumber)) return "⚠️ No account selected";

            var settingsService = RiskManagerSettingsService.Instance;
            if (!settingsService.IsInitialized) return "⚠️ Service not initialized";

            var lockStatus = settingsService.GetLockStatusString(accountNumber);
            return lockStatus == "Unlocked" ? "🔓 Unlocked" : "🔒 " + lockStatus;
        }

        private string GetSettingsLockStatus()
        {
            var accountNumber = GetSelectedAccountNumber();
            if (string.IsNullOrEmpty(accountNumber)) return "⚠️ No account selected";

            var settingsService = RiskManagerSettingsService.Instance;
            if (!settingsService.IsInitialized) return "⚠️ Service not initialized";

            var isLocked = settingsService.AreSettingsLocked(accountNumber);
            return isLocked ? "🔒 Locked" : "🔓 Unlocked";
        }

        private bool IsFeatureEnabled(Func<AccountSettings, bool> featureGetter)
        {
            var accountNumber = GetSelectedAccountNumber();
            // Default to enabled (no overlay) if no account selected - avoid misleading disabled state
            if (string.IsNullOrEmpty(accountNumber)) return true;

            var settingsService = RiskManagerSettingsService.Instance;
            // Default to enabled (no overlay) if service not ready - avoid misleading disabled state
            if (!settingsService.IsInitialized) return true;

            var settings = settingsService.GetSettings(accountNumber);
            // If settings exist, use the feature flag; otherwise default to enabled
            return settings != null ? featureGetter(settings) : true;
        }

        private void UpdateCardOverlay(Panel cardPanel)
        {
            if (cardPanel == null) return;
            
            // Get the feature checker from Tag (might be wrapped in anonymous object)
            Func<bool> featureChecker = null;
            
            if (cardPanel.Tag != null)
            {
                // Try to get FeatureChecker from anonymous object
                var featureCheckerProp = cardPanel.Tag.GetType().GetProperty("FeatureChecker");
                if (featureCheckerProp != null)
                {
                    featureChecker = featureCheckerProp.GetValue(cardPanel.Tag) as Func<bool>;
                }
                else
                {
                    // Tag is directly the feature checker
                    featureChecker = cardPanel.Tag as Func<bool>;
                }
            }
            
            // Check if this card has a feature checker
            if (featureChecker != null)
            {
                // Determine if overlay should be shown
                bool shouldShowOverlay = !featureChecker();
                
                // Check if overlay panel exists
                var existingOverlay = cardPanel.Controls.OfType<Panel>()
                    .FirstOrDefault(p => p.Name == "DisabledOverlay");
                bool hasOverlay = existingOverlay != null;
                
                if (shouldShowOverlay && !hasOverlay)
                {
                    // Add overlay
                    AddDisabledOverlay(cardPanel);
                }
                else if (!shouldShowOverlay && hasOverlay)
                {
                    // Remove overlay panel
                    cardPanel.Controls.Remove(existingOverlay);
                    existingOverlay?.Dispose();
                    cardPanel.Tag = featureChecker;
                    cardPanel.Cursor = Cursors.Default;
                }
            }
        }

        private void AddDisabledOverlay(Panel cardPanel)
        {
            // Check if overlay already exists
            var existingOverlay = cardPanel.Controls.OfType<Panel>()
                .FirstOrDefault(p => p.Name == "DisabledOverlay");
            if (existingOverlay != null)
            {
                return; // Already has overlay
            }
            
            // Create a semi-transparent overlay panel (20% opacity for better visibility)
            var overlay = new Panel
            {
                Name = "DisabledOverlay", // Identify this as the overlay panel
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(51, 40, 40, 40), // 20% opacity (51/255 ≈ 0.2)
                Cursor = Cursors.No
            };

            // Create the red X label
            var disabledLabel = new Label
            {
                Text = "✖",
                Font = new Font("Segoe UI", 72, FontStyle.Bold),
                ForeColor = Color.FromArgb(220, 50, 50), // Bright red color
                BackColor = Color.Transparent,
                TextAlign = ContentAlignment.MiddleCenter,
                Dock = DockStyle.Fill,
                UseCompatibleTextRendering = false
            };

            overlay.Controls.Add(disabledLabel);
            cardPanel.Controls.Add(overlay);
            overlay.BringToFront();
        }

        private string GetPositionLossLimit()
        {
            var accountNumber = GetSelectedAccountNumber();
            if (string.IsNullOrEmpty(accountNumber)) return "Not set";

            var settingsService = RiskManagerSettingsService.Instance;
            if (!settingsService.IsInitialized) return "Not set";

            var settings = settingsService.GetSettings(accountNumber);
            if (settings?.PositionLossLimit.HasValue == true)
                return $"💵 ${settings.PositionLossLimit.Value:N2} per position";
            
            return "❌ Not enabled";
        }

        private string GetPositionProfitTarget()
        {
            var accountNumber = GetSelectedAccountNumber();
            if (string.IsNullOrEmpty(accountNumber)) return "Not set";

            var settingsService = RiskManagerSettingsService.Instance;
            if (!settingsService.IsInitialized) return "Not set";

            var settings = settingsService.GetSettings(accountNumber);
            if (settings?.PositionProfitTarget.HasValue == true)
                return $"💵 ${settings.PositionProfitTarget.Value:N2} per position";
            
            return "❌ Not enabled";
        }

        private string GetDailyLossLimit()
        {
            var accountNumber = GetSelectedAccountNumber();
            if (string.IsNullOrEmpty(accountNumber)) return "Not set";

            var settingsService = RiskManagerSettingsService.Instance;
            if (!settingsService.IsInitialized) return "Not set";

            var settings = settingsService.GetSettings(accountNumber);
            if (settings?.DailyLossLimit.HasValue == true)
                return $"💵 ${settings.DailyLossLimit.Value:N2} per day";
            
            return "❌ Not enabled";
        }

        private string GetDailyProfitTarget()
        {
            var accountNumber = GetSelectedAccountNumber();
            if (string.IsNullOrEmpty(accountNumber)) return "Not set";

            var settingsService = RiskManagerSettingsService.Instance;
            if (!settingsService.IsInitialized) return "Not set";

            var settings = settingsService.GetSettings(accountNumber);
            if (settings?.DailyProfitTarget.HasValue == true)
                return $"💵 ${settings.DailyProfitTarget.Value:N2} per day";
            
            return "❌ Not enabled";
        }

        private string GetBlockedSymbols()
        {
            var accountNumber = GetSelectedAccountNumber();
            if (string.IsNullOrEmpty(accountNumber)) return "Not set";

            var settingsService = RiskManagerSettingsService.Instance;
            if (!settingsService.IsInitialized) return "Not set";

            var settings = settingsService.GetSettings(accountNumber);
            if (settings?.BlockedSymbols != null && settings.BlockedSymbols.Any())
                return $"⛔ {string.Join(", ", settings.BlockedSymbols)}";
            
            return "✅ None";
        }

        private string GetDefaultContractLimit()
        {
            var accountNumber = GetSelectedAccountNumber();
            if (string.IsNullOrEmpty(accountNumber)) return "Not set";

            var settingsService = RiskManagerSettingsService.Instance;
            if (!settingsService.IsInitialized) return "Not set";

            var settings = settingsService.GetSettings(accountNumber);
            if (settings?.DefaultContractLimit.HasValue == true)
                return $"📊 {settings.DefaultContractLimit.Value} contracts";
            
            return "❌ Not set";
        }

        private string GetSymbolContractLimits()
        {
            var accountNumber = GetSelectedAccountNumber();
            if (string.IsNullOrEmpty(accountNumber)) return "Not set";

            var settingsService = RiskManagerSettingsService.Instance;
            if (!settingsService.IsInitialized) return "Not set";

            var settings = settingsService.GetSettings(accountNumber);
            if (settings?.SymbolContractLimits != null && settings.SymbolContractLimits.Any())
            {
                var limits = string.Join(", ", settings.SymbolContractLimits.Select(kvp => $"{kvp.Key}:{kvp.Value}"));
                return $"📊 {limits}";
            }
            
            return "✅ None";
        }

        private string GetTradingTimeRestrictions()
        {
            var accountNumber = GetSelectedAccountNumber();
            if (string.IsNullOrEmpty(accountNumber)) return "Not set";

            var settingsService = RiskManagerSettingsService.Instance;
            if (!settingsService.IsInitialized) return "Not set";

            var settings = settingsService.GetSettings(accountNumber);
            if (settings?.TradingTimeRestrictions != null && settings.TradingTimeRestrictions.Any())
            {
                var allowedCount = settings.TradingTimeRestrictions.Count(r => r.IsAllowed);
                return $"✅ {allowedCount} time slot(s) configured";
            }
            
            return "⚠️ No restrictions (24/7 trading)";
        }

        /// <summary>
        /// Refreshes the Risk Overview panel with current account data.
        /// </summary>
        private void RefreshRiskOverviewPanel(Control panel)
        {
            if (panel == null) return;

            // Find all labels in the panel that display values
            RefreshLabelsInControl(panel);
        }

        /// <summary>
        /// Recursively refreshes all value labels in a control.
        /// </summary>
        private void RefreshLabelsInControl(Control control)
        {
            if (control == null) return;

            // Check if this is a card panel with feature overlay support
            if (control is Panel panel && panel.Tag is Func<bool>)
            {
                // Update the overlay state for this card
                UpdateCardOverlay(panel);
            }

            // Check if this is the Trading Times card - needs special refresh
            if (control is Panel tradingPanel && tradingPanel.Tag as string == "TradingTimesCard")
            {
                var parent = tradingPanel.Parent;
                var index = parent?.Controls.GetChildIndex(tradingPanel) ?? -1;
                if (parent != null && index >= 0)
                {
                    parent.Controls.Remove(tradingPanel);
                    var newCard = CreateTradingTimesOverviewCard();
                    parent.Controls.Add(newCard);
                    parent.Controls.SetChildIndex(newCard, index);
                }
                return;
            }

            // Check if this is a value label (has a Tag with getter function)
            if (control is Label label && label.Tag is Func<string> getter)
            {
                try
                {
                    var rawVal = getter() ?? string.Empty;
                    var val = rawVal.Trim();

                    // Dispose previously created image if needed
                    if (label.Image != null && label.Image is Bitmap bmpPrev)
                    {
                        try { bmpPrev.Dispose(); } catch { }
                    }

                    if (string.IsNullOrEmpty(val))
                    {
                        label.Text = "Not set";
                        label.Image = null;
                        label.ForeColor = TextGray;
                    }
                    else
                    {
                        // Detect special emoji tokens and choose icons accordingly
                        Image overrideIcon = null;
                        bool isDollar = false;

                        if (val.Contains("💵"))
                        {
                            overrideIcon = DollarImage;
                            isDollar = true;
                        }
                        else if (val.Contains("⛔"))
                        {
                            overrideIcon = GetIconForTitle("Symbols") ?? (IconMap.TryGetValue("Symbols", out var img) ? img : null);
                        }
                        else if (val.Contains("📊"))
                        {
                            overrideIcon = GetIconForTitle("Limits") ?? (IconMap.TryGetValue("Limits", out var img) ? img : null);
                        }

                        // Default display text (may be modified below)
                        string display = val;
                        Color labelColor = TextGray;

                        // Check if this is a lock status value (contains lock/unlock emojis)
                        bool isLockStatus = IsLockStatusValue(val);
                        
                        if (isLockStatus)
                        {
                            // Extract and apply lock status color (Green for Unlocked, Red for Locked)
                            var lockStatusText = ExtractLockStatusText(val);
                            labelColor = GetLockStatusColor(lockStatusText);
                        }
                        else
                        {
                            // If theme requires special numeric coloring, detect numeric content and sign
                            bool applySpecialThemeColoring = currentTheme == Theme.YellowBlueBlack;
                            bool hasDigit = display.Any(char.IsDigit);
                            bool isNegative = IsNegativeNumericString(display);

                            if (applySpecialThemeColoring && hasDigit)
                            {
                                // Use yellow for negative, blue for positive (per theme)
                                labelColor = isNegative ? NegativeValueColor : PositiveValueColor;
                            }
                            else
                            {
                                labelColor = TextGray;
                            }
                        }

                        if (overrideIcon != null)
                        {
                            int targetHeight = Math.Max(14, (int)(label.Font.Height * 1.2));
                            label.Image = ScaleImageToFit(overrideIcon, targetHeight, targetHeight);
                            label.ImageAlign = ContentAlignment.MiddleLeft;
                            label.Padding = new Padding(targetHeight + 8, 0, 0, 0);

                            // Remove emoji/currency tokens for display text
                            if (isDollar)
                            {
                                display = display.Replace("💵", "").Trim();
                                if (display.StartsWith("$"))
                                    display = display.Substring(1).Trim();
                            }
                            else
                            {
                                var idx = display.IndexOf(' ');
                                if (idx >= 0 && idx + 1 < display.Length)
                                    display = display.Substring(idx + 1).Trim();
                                else if (display.Length > 1 && (char.IsSurrogate(display[0]) || char.IsSymbol(display[0]) || char.IsPunctuation(display[0])))
                                    display = display.Length > 1 ? display.Substring(1).Trim() : display;
                            }

                            label.Text = "       " + display;
                            label.ForeColor = labelColor;
                        }
                        else if (val.StartsWith("$") && DollarImage != null)
                        {
                            int targetHeight = Math.Max(14, (int)(label.Font.Height * 1.2));
                            label.Image = ScaleImageToFit(DollarImage, targetHeight, targetHeight);
                            label.ImageAlign = ContentAlignment.MiddleLeft;
                            label.Padding = new Padding(targetHeight + 8, 0, 0, 0);
                            label.Text = display.TrimStart('$', ' ');
                            label.ForeColor = labelColor;
                        }
                        else
                        {
                            var icon = GetIconForTitle(display);
                            if (icon != null)
                            {
                                int targetHeight = Math.Max(14, (int)(label.Font.Height * 1.2));
                                label.Image = ScaleImageToFit(icon, targetHeight, targetHeight);
                                label.ImageAlign = ContentAlignment.MiddleLeft;
                                // Increased padding for better spacing between icon and text
                                label.Padding = new Padding(targetHeight + 12, 0, 0, 0);

                                var trimmed = display.Trim();
                                var idx = trimmed.IndexOf(' ');
                                label.Text = (idx >= 0) ? trimmed.Substring(idx + 1) : (trimmed.Length > 1 ? trimmed.Substring(1) : trimmed);
                                label.ForeColor = labelColor;
                            }
                            else
                            {
                                // Plain text fallback
                                label.Image = null;
                                label.Text = display;
                                label.ForeColor = labelColor;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    label.Text = "⚠️ Error loading data";
                    System.Diagnostics.Debug.WriteLine($"Error refreshing Risk Overview label: {ex.Message}");
                }
            }
            // Recursively refresh child controls
            foreach (Control child in control.Controls)
            {
                RefreshLabelsInControl(child);
            }
        }


        /// <summary>
        /// Creates the consolidated "Positions" panel combining Position Loss Limit and Position Profit Target.
        /// </summary>
        private Control CreatePositionsPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title with emoji rendering
            var positionsHeader = new CustomHeaderControl("Positions", GetIconForTitle("Positions"));
            positionsHeader.Dock = DockStyle.Top;
            positionsHeader.Margin = new Padding(10, 0, 0, 0);
            contentPanel.Controls.Add(positionsHeader);

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Configure position limits and targets:",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(10, 0, 10, 0),
                BackColor = DarkBackground,
                ForeColor = TextGray,
                AutoSize = false
            };

            // Content area
            var contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBackground,
                Padding = new Padding(15),
                AutoScroll = true
            };

            // Position Loss Limit section
            var lossSection = CreatePositionSection("Position Loss Limit", "USD per position:", 10, 
                out positionLossLimitEnabled, out positionLossLimitInput);
            contentArea.Controls.Add(lossSection);

            // Position Profit Target section
            var profitSection = CreatePositionSection("Position Profit Target", "USD per position:", 120,
                out positionProfitTargetEnabled, out positionProfitTargetInput);
            contentArea.Controls.Add(profitSection);

            var saveButton = CreateDarkSaveButton();

            // Add controls in correct order: Bottom first, Fill second, Top last
            mainPanel.Controls.Add(saveButton);
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(positionsHeader);

            return mainPanel;
        }

        /// <summary>
        /// Helper method to create a position section with toggle and USD input.
        /// Renders an icon (if available) to the left of the checkbox header.
        /// </summary>
        private Panel CreatePositionSection(string sectionTitle, string inputLabel, int topPosition,
            out CheckBox enabledCheckbox, out TextBox valueInput)
        {
            var sectionPanel = new Panel
            {
                Left = 0,
                Top = topPosition,
                Width = 500,
                Height = 100,
                BackColor = CardBackground
            };

            // Section header with toggle
            var sectionHeader = new CheckBox
            {
                Text = sectionTitle,
                Left = 0,
                Top = 0,
                Width = 300,
                Height = 30,
                Checked = false,
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                AutoSize = false,
                UseCompatibleTextRendering = false
            };

            // Prepare scaled icon for header (use title key)
            Image headerIcon = GetIconForTitle(sectionTitle);
            Bitmap scaledHeaderIcon = null;
            if (headerIcon != null)
            {
                int targetHeight = Math.Max(14, (int)(sectionHeader.Font.Height * 1.2));
                scaledHeaderIcon = ScaleImageToFit(headerIcon, targetHeight, targetHeight) as Bitmap;
                sectionHeader.Disposed += (s, e) =>
                {
                    try { scaledHeaderIcon?.Dispose(); } catch { }
                };
            }

            // Custom paint to draw icon + checkbox + text similar to limits section
            sectionHeader.Paint += (s, e) =>
            {
                var cb = (CheckBox)s;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                e.Graphics.Clear(cb.BackColor);

                int x = 0;

                // Draw icon if available
                if (scaledHeaderIcon != null)
                {
                    int iconY = (cb.Height - scaledHeaderIcon.Height) / 2;
                    var iconRect = new Rectangle(x, iconY, scaledHeaderIcon.Width, scaledHeaderIcon.Height);
                    e.Graphics.DrawImage(scaledHeaderIcon, iconRect);
                    x += scaledHeaderIcon.Width + 6;
                }

                // Draw checkbox
                var checkBoxSize = 13;
                var checkBoxRect = new Rectangle(x, (cb.Height - checkBoxSize) / 2, checkBoxSize, checkBoxSize);
                ControlPaint.DrawCheckBox(e.Graphics, checkBoxRect, cb.Checked ? ButtonState.Checked : ButtonState.Normal);
                x += checkBoxSize + 6;

                // Draw text
                using (var brush = new SolidBrush(cb.ForeColor))
                {
                    var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near };
                    var textRect = new RectangleF(x, 0, cb.Width - x, cb.Height);
                    e.Graphics.DrawString(cb.Text, cb.Font, brush, textRect, sf);
                }
            };

            sectionPanel.Controls.Add(sectionHeader);
            enabledCheckbox = sectionHeader;

            // Input label
            var label = new Label
            {
                Text = inputLabel,
                Left = 0,
                Top = 40,
                Width = 120,
                Height = 25,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                TextAlign = ContentAlignment.MiddleLeft
            };
            sectionPanel.Controls.Add(label);

            // Input textbox
            var input = new TextBox
            {
                Left = 130,
                Top = 40,
                Width = 150,
                Height = 25,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                BackColor = DarkerBackground,
                ForeColor = TextWhite,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "0"
            };
            sectionPanel.Controls.Add(input);
            valueInput = input;

            return sectionPanel;
        }

        /// <summary>
        /// Creates the consolidated "Limits" panel combining Daily Loss Limit and Daily Profit Target.
        /// </summary>
        private Control CreateLimitsPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title with emoji rendering
            var limitsHeader = new CustomHeaderControl("Limits", GetIconForTitle("Limits"));
            limitsHeader.Dock = DockStyle.Top;
            limitsHeader.Margin = new Padding(10, 0, 0, 0);
            contentPanel.Controls.Add(limitsHeader);

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Configure daily trading limits:",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(10, 0, 10, 0),
                BackColor = DarkBackground,
                ForeColor = TextGray,
                AutoSize = false
            };

            // Content area
            var contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBackground,
                Padding = new Padding(15),
                AutoScroll = true
            };

            // Daily Loss Limit section
            var lossSection = CreateLimitSection("📉 Daily Loss Limit", 10, out dailyLossLimitEnabled, out dailyLossLimitInput);
            contentArea.Controls.Add(lossSection);

            // Daily Profit Target section
            var profitSection = CreateLimitSection("📈 Daily Profit Target", 120, out dailyProfitTargetEnabled, out dailyProfitTargetInput);
            contentArea.Controls.Add(profitSection);

            var saveButton = CreateDarkSaveButton();

            // Add controls in correct order: Bottom first, Fill second, Top last
            mainPanel.Controls.Add(saveButton);
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(limitsHeader);

            return mainPanel;
        }

        /// <summary>
        /// Helper method to create a limit section with toggle and USD input.
        /// Renders an icon (if available) to the left of the checkbox header.
        /// </summary>
        private Panel CreateLimitSection(string sectionTitle, int topPosition, out CheckBox enabledCheckbox, out TextBox valueInput)
        {
            var sectionPanel = new Panel
            {
                Left = 0,
                Top = topPosition,
                Width = 500,
                Height = 100,
                BackColor = CardBackground
            };

            // Section header with toggle
            var sectionHeader = new CheckBox
            {
                Text = sectionTitle,
                Left = 0,
                Top = 0,
                Width = 300,
                Height = 30,
                Checked = false,
                Font = new Font("Segoe UI Emoji", 11, FontStyle.Bold),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                UseCompatibleTextRendering = false
            };

            // Prepare scaled icon for header using the title key (handles emoji-prefixed titles)
            Image headerIcon = GetIconForTitle(sectionTitle);
            Bitmap scaledHeaderIcon = null;
            if (headerIcon != null)
            {
                int targetHeight = Math.Max(14, (int)(sectionHeader.Font.Height * 1.2));
                scaledHeaderIcon = ScaleImageToFit(headerIcon, targetHeight, targetHeight) as Bitmap;
                // Ensure disposal when control is disposed
                sectionHeader.Disposed += (s, e) =>
                {
                    try { scaledHeaderIcon?.Dispose(); } catch { }
                };
            }

            // Custom paint for colored emoji and icon rendering in checkbox
            sectionHeader.Paint += (s, e) =>
            {
                var cb = (CheckBox)s;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Draw background
                e.Graphics.Clear(cb.BackColor);

                // Layout metrics
                int x = 0;

                // Draw icon if available
                if (scaledHeaderIcon != null)
                {
                    int iconY = (cb.Height - scaledHeaderIcon.Height) / 2;
                    var iconRect = new Rectangle(x, iconY, scaledHeaderIcon.Width, scaledHeaderIcon.Height);
                    e.Graphics.DrawImage(scaledHeaderIcon, iconRect);
                    x += scaledHeaderIcon.Width + 6; // gap after icon
                }

                // Draw checkbox next to icon
                var checkBoxSize = 13;
                var checkBoxRect = new Rectangle(x, (cb.Height - checkBoxSize) / 2, checkBoxSize, checkBoxSize);
                ControlPaint.DrawCheckBox(e.Graphics, checkBoxRect, cb.Checked ? ButtonState.Checked : ButtonState.Normal);
                x += checkBoxSize + 6; // gap after checkbox

                // Determine display text (strip leading emoji/token if present)
                var raw = (cb.Text ?? string.Empty).Trim();
                string displayText = raw;
                var idx = raw.IndexOf(' ');
                if (idx >= 0 && idx + 1 < raw.Length)
                    displayText = raw.Substring(idx + 1).Trim();

                // Draw text
                using (var brush = new SolidBrush(cb.ForeColor))
                {
                    var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near };
                    var textRect = new RectangleF(x, 0, cb.Width - x, cb.Height);
                    e.Graphics.DrawString(displayText, cb.Font, brush, textRect, sf);
                }
            };

            sectionPanel.Controls.Add(sectionHeader);
            enabledCheckbox = sectionHeader;

            // Input textbox
            var input = new TextBox
            {
                Left = 0,
                Top = 40,
                Width = 150,
                Height = 25,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                BackColor = DarkerBackground,
                ForeColor = TextWhite,
                BorderStyle = BorderStyle.FixedSingle,
                Text = "0"
            };
            sectionPanel.Controls.Add(input);
            valueInput = input;

            // USD label
            var usdLabel = new Label
            {
                Text = "USD",
                Left = 160,
                Top = 40,
                Width = 50,
                Height = 25,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                TextAlign = ContentAlignment.MiddleLeft
            };
            sectionPanel.Controls.Add(usdLabel);

            return sectionPanel;
        }

        /// <summary>
        /// Creates the consolidated "Symbols" panel combining Symbol Blacklist and Symbol Contract Limits.
        /// </summary>
        private Control CreateSymbolsPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title with emoji rendering
            var symbolsHeader = new CustomHeaderControl("Symbols", GetIconForTitle("Symbols"));
            symbolsHeader.Dock = DockStyle.Top;
            symbolsHeader.Margin = new Padding(10, 0, 0, 0);
            contentPanel.Controls.Add(symbolsHeader);

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Configure symbol blacklist and contract limits:",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(10, 0, 10, 0),
                BackColor = DarkBackground,
                ForeColor = TextGray,
                AutoSize = false
            };

            // Content area
            var contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBackground,
                Padding = new Padding(15),
                AutoScroll = true
            };

            // Symbol Blacklist section
            var blacklistSection = CreateSymbolBlacklistSection(10, out blockedSymbolsEnabled, out blockedSymbolsInput);
            contentArea.Controls.Add(blacklistSection);

            // Symbol Contract Limits section
            var contractSection = CreateSymbolContractLimitsSection(150, out symbolContractLimitsEnabled, out defaultContractLimitInput, out symbolContractLimitsInput);
            contentArea.Controls.Add(contractSection);

            var saveButton = CreateDarkSaveButton();

            // Add controls in correct order: Bottom first, Fill second, Top last
            mainPanel.Controls.Add(saveButton);
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(symbolsHeader);

            return mainPanel;
        }

        /// <summary>
        /// Helper method to create the Symbol Blacklist section.
        /// </summary>
        private Panel CreateSymbolBlacklistSection(int topPosition, out CheckBox enabledCheckbox, out TextBox symbolsTextBox)
        {
            var sectionPanel = new Panel
            {
                Left = 0,
                Top = topPosition,
                Width = 500,
                Height = 130,
                BackColor = CardBackground
            };

            // Section header with toggle
            var sectionHeader = new CheckBox
            {
                Text = "Symbol Blacklist",
                Left = 0,
                Top = 0,
                Width = 300,
                Height = 30,
                Checked = false,
                Font = new Font("Segoe UI Emoji", 11, FontStyle.Bold),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                UseCompatibleTextRendering = false
            };

            // Prepare scaled icon for header (blocked.png -> IconMap["Symbols"])
            Image headerIcon = GetIconForTitle("Symbols");
            Bitmap scaledHeaderIcon = null;
            if (headerIcon != null)
            {
                int targetHeight = Math.Max(14, (int)(sectionHeader.Font.Height * 1.2));
                scaledHeaderIcon = ScaleImageToFit(headerIcon, targetHeight, targetHeight) as Bitmap;
                // Ensure disposal when control is disposed
                sectionHeader.Disposed += (s, e) =>
                {
                    try { scaledHeaderIcon?.Dispose(); } catch { }
                };
            }

            // Custom paint for colored emoji and icon rendering in checkbox
            sectionHeader.Paint += (s, e) =>
            {
                var cb = (CheckBox)s;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Draw background
                e.Graphics.Clear(cb.BackColor);

                // Layout metrics
                int yCenter = cb.Height / 2;

                // Draw icon if available
                int x = 0;
                if (scaledHeaderIcon != null)
                {
                    int iconY = (cb.Height - scaledHeaderIcon.Height) / 2;
                    var iconRect = new Rectangle(x, iconY, scaledHeaderIcon.Width, scaledHeaderIcon.Height);
                    e.Graphics.DrawImage(scaledHeaderIcon, iconRect);
                    x += scaledHeaderIcon.Width + 6; // gap after icon
                }

                // Draw checkbox next to icon
                var checkBoxSize = 13;
                var checkBoxRect = new Rectangle(x, (cb.Height - checkBoxSize) / 2, checkBoxSize, checkBoxSize);
                ControlPaint.DrawCheckBox(e.Graphics, checkBoxRect, cb.Checked ? ButtonState.Checked : ButtonState.Normal);
                x += checkBoxSize + 6; // gap after checkbox

                // Draw text
                using (var brush = new SolidBrush(cb.ForeColor))
                {
                    var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near };
                    var textRect = new RectangleF(x, 0, cb.Width - x, cb.Height);
                    e.Graphics.DrawString(cb.Text, cb.Font, brush, textRect, sf);
                }
            };

            sectionPanel.Controls.Add(sectionHeader);
            enabledCheckbox = sectionHeader;

            // Description label
            var descLabel = new Label
            {
                Text = "Enter symbols to block (comma separated):",
                Left = 0,
                Top = 35,
                Width = 300,
                Height = 20,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = TextGray,
                BackColor = CardBackground
            };
            sectionPanel.Controls.Add(descLabel);

            // Symbols input textbox
            var symbolsInput = new TextBox
            {
                Left = 0,
                Top = 60,
                Width = 400,
                Height = 60,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                BackColor = DarkerBackground,
                ForeColor = TextWhite,
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = ""
            };
            sectionPanel.Controls.Add(symbolsInput);
            symbolsTextBox = symbolsInput;

            return sectionPanel;
        }

        /// <summary>
        /// Helper method to create the Symbol Contract Limits section.
        /// </summary>
        private Panel CreateSymbolContractLimitsSection(int topPosition, out CheckBox enabledCheckbox, out TextBox defaultLimitInput, out TextBox specificLimitsInput)
        {
            var sectionPanel = new Panel
            {
                Left = 0,
                Top = topPosition,
                Width = 500,
                Height = 200,
                BackColor = CardBackground
            };

            // Section header with toggle
            var sectionHeader = new CheckBox
            {
                Text = "Symbol Contract Limits",
                Left = 0,
                Top = 0,
                Width = 300,
                Height = 30,
                Checked = false,
                Font = new Font("Segoe UI Emoji", 11, FontStyle.Bold),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                UseCompatibleTextRendering = false
            };

            // Prepare scaled icon for header (blocked.png -> IconMap["Symbols"])
            Image contractHeaderIcon = GetIconForTitle("Symbols");
            Bitmap scaledContractIcon = null;
            if (contractHeaderIcon != null)
            {
                int targetHeight = Math.Max(14, (int)(sectionHeader.Font.Height * 1.2));
                scaledContractIcon = ScaleImageToFit(contractHeaderIcon, targetHeight, targetHeight) as Bitmap;
                sectionHeader.Disposed += (s, e) =>
                {
                    try { scaledContractIcon?.Dispose(); } catch { }
                };
            }

            // Custom paint for colored emoji and icon rendering in checkbox
            sectionHeader.Paint += (s, e) =>
            {
                var cb = (CheckBox)s;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

                // Draw background
                e.Graphics.Clear(cb.BackColor);

                int x = 0;
                // Draw icon if available
                if (scaledContractIcon != null)
                {
                    int iconY = (cb.Height - scaledContractIcon.Height) / 2;
                    var iconRect = new Rectangle(x, iconY, scaledContractIcon.Width, scaledContractIcon.Height);
                    e.Graphics.DrawImage(scaledContractIcon, iconRect);
                    x += scaledContractIcon.Width + 6;
                }

                // Draw checkbox next to icon
                var checkBoxSize = 13;
                var checkBoxRect = new Rectangle(x, (cb.Height - checkBoxSize) / 2, checkBoxSize, checkBoxSize);
                ControlPaint.DrawCheckBox(e.Graphics, checkBoxRect, cb.Checked ? ButtonState.Checked : ButtonState.Normal);
                x += checkBoxSize + 6;

                // Draw text
                using (var brush = new SolidBrush(cb.ForeColor))
                {
                    var sf = new StringFormat { LineAlignment = StringAlignment.Center, Alignment = StringAlignment.Near };
                    var textRect = new RectangleF(x, 0, cb.Width - x, cb.Height);
                    e.Graphics.DrawString(cb.Text, cb.Font, brush, textRect, sf);
                }
            };

            sectionPanel.Controls.Add(sectionHeader);
            enabledCheckbox = sectionHeader;

            // Default contract limit label
            var defaultLabel = new Label
            {
                Text = "Default contract limit:",
                Left = 0,
                Top = 40,
                Width = 150,
                Height = 25,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                TextAlign = ContentAlignment.MiddleLeft
            };
            sectionPanel.Controls.Add(defaultLabel);

            // Default contract limit input
            var defaultInput = new TextBox
            {
                Left = 160,
                Top = 40,
                Width = 100,
                Height = 25,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                BackColor = DarkerBackground,
                ForeColor = TextWhite,
                BorderStyle = BorderStyle.FixedSingle,
                Text = DEFAULT_CONTRACT_LIMIT.ToString()
            };
            sectionPanel.Controls.Add(defaultInput);
            defaultLimitInput = defaultInput;

            // Symbol-specific limits label
            var specificLabel = new Label
            {
                Text = "Symbol-specific limits (Symbol:Limit, comma separated):",
                Left = 0,
                Top = 75,
                Width = 400,
                Height = 20,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = TextGray,
                BackColor = CardBackground
            };
            sectionPanel.Controls.Add(specificLabel);

            // Symbol-specific limits input
            var specificInput = new TextBox
            {
                Left = 0,
                Top = 100,
                Width = 400,
                Height = 60,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                BackColor = DarkerBackground,
                ForeColor = TextWhite,
                BorderStyle = BorderStyle.FixedSingle,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical,
                Text = ""
            };
            sectionPanel.Controls.Add(specificInput);
            specificLimitsInput = specificInput;

            return sectionPanel;
        }

        private void ShowPage(string name)
        {
            contentPanel.SuspendLayout();
            contentPanel.Controls.Clear();

            if (pageContents.TryGetValue(name, out var ctrl))
            {
                ctrl.Dock = DockStyle.Fill;
                contentPanel.Controls.Add(ctrl);
                
                // Refresh Risk Overview tab when it's shown
                if (name.EndsWith("Risk Overview"))
                {
                    RefreshRiskOverviewPanel(ctrl);
                }
                
                // Reload Trading Time Restrictions when Allowed Trading Times tab is shown
                if (name.EndsWith("Allowed Trading Times"))
                {
                    System.Diagnostics.Debug.WriteLine("=== ShowPage: Allowed Trading Times tab clicked ===");
                    var accountNumber = GetSelectedAccountNumber();
                    System.Diagnostics.Debug.WriteLine($"ShowPage: accountNumber = '{accountNumber}'");
                    if (!string.IsNullOrEmpty(accountNumber))
                    {
                        var settingsService = RiskManagerSettingsService.Instance;
                        System.Diagnostics.Debug.WriteLine($"ShowPage: settingsService.IsInitialized = {settingsService.IsInitialized}");
                        if (settingsService.IsInitialized)
                        {
                            var settings = settingsService.GetSettings(accountNumber);
                            System.Diagnostics.Debug.WriteLine($"ShowPage: settings = {(settings != null ? "Found" : "NULL")}");
                            if (settings != null)
                            {
                                System.Diagnostics.Debug.WriteLine($"ShowPage: settings.TradingTimeRestrictions count = {settings.TradingTimeRestrictions?.Count ?? 0}");
                                LoadTradingTimeRestrictions(settings);
                            }
                            else
                            {
                                System.Diagnostics.Debug.WriteLine("ShowPage: WARNING - settings is NULL, cannot load trading times");
                            }
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("ShowPage: WARNING - settingsService not initialized");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("ShowPage: WARNING - accountNumber is null or empty");
                    }
                }
            }
            else
            {
                // Fallback: show page name as a placeholder when page content is not found
                var lbl = new Label
                {
                    Text = name,
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleCenter,
                    Font = new Font("Segoe UI", 12, FontStyle.Bold),
                    ForeColor = TextWhite,
                    BackColor = DarkBackground
                };
                contentPanel.Controls.Add(lbl);
            }

            contentPanel.ResumeLayout();
        }

        // Update Dispose to free scaled images (replace existing Dispose(bool) body)
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                statsRefreshTimer?.Stop();
                statsRefreshTimer?.Dispose();
                statsRefreshTimer = null;

                statsDetailRefreshTimer?.Stop();
                statsDetailRefreshTimer?.Dispose();
                statsDetailRefreshTimer = null;

                typeSummaryRefreshTimer?.Stop();
                typeSummaryRefreshTimer?.Dispose();
                typeSummaryRefreshTimer = null;

                lockExpirationCheckTimer?.Stop();
                lockExpirationCheckTimer?.Dispose();
                lockExpirationCheckTimer = null;

                pnlMonitorTimer?.Stop();
                pnlMonitorTimer?.Dispose();
                pnlMonitorTimer = null;

                badgeRefreshTimer?.Stop();
                badgeRefreshTimer?.Dispose();
                badgeRefreshTimer = null;

                alertSoundPlayer?.Dispose();
                alertSoundPlayer = null;

                shutdownSoundPlayer?.Dispose();
                shutdownSoundPlayer = null;

                titleToolTip?.Dispose();
                titleToolTip = null;

                // Dispose scaled images created at runtime
                try { themeButtonScaledImage?.Dispose(); } catch { }
                themeButtonScaledImage = null;
                try { cautionButtonScaledImage?.Dispose(); } catch { }
                cautionButtonScaledImage = null;
                try { cautionButtonBgImage?.Dispose(); } catch { }
                cautionButtonBgImage = null;
                try { navToggleButtonScaledImage?.Dispose(); } catch { }
                navToggleButtonScaledImage = null;
                
                // Dispose shutdown-related resources
                try { shutdownButtonScaledImage?.Dispose(); } catch { }
                shutdownButtonScaledImage = null;
                
                shutdownTimer?.Stop();
                shutdownTimer?.Dispose();
                shutdownTimer = null;
                
                try { shutdownCountdownForm?.Dispose(); } catch { }
                shutdownCountdownForm = null;
                
                // Dispose status table view
                try { statusTableView?.Dispose(); } catch { }
                statusTableView = null;
            }
            base.Dispose(disposing);
        }

        private Control CreateWeeklyLossDarkPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title with emoji
            var titleLabel = new Label
            {
                Text = "📊 Weekly Loss",
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Padding = new Padding(10, 0, 0, 0),
                BackColor = DarkBackground,
                ForeColor = TextWhite
            };

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Set the maximum loss allowed per week.",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(10, 0, 10, 0),
                BackColor = DarkBackground,
                ForeColor = TextGray,
                AutoSize = false
            };

            var contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBackground,
                Padding = new Padding(15)
            };

            // Enable checkbox
            var enableCheckbox = new CheckBox
            {
                Text = "Enable Weekly Loss Limit",
                Left = 0,
                Top = 0,
                Width = 300,
                Height = 30,
                Checked = false,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                AutoSize = false
            };
            contentArea.Controls.Add(enableCheckbox);
            weeklyLossLimitEnabled = enableCheckbox;

            // USD per week label and textbox
            var usdLabel = new Label
            {
                Text = "USD per week:",
                Left = 0,
                Top = 40,
                Width = 150,
                Height = 24,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                AutoSize = false
            };
            contentArea.Controls.Add(usdLabel);

            var usdInput = new TextBox
            {
                Left = 160,
                Top = 40,
                Width = 150,
                Height = 24,
                Text = DEFAULT_WEEKLY_LOSS_LIMIT.ToString(),
                Font = new Font("Segoe UI", 9),
                BackColor = CardBackground,
                ForeColor = TextWhite,
                BorderStyle = BorderStyle.FixedSingle
            };
            contentArea.Controls.Add(usdInput);
            weeklyLossLimitInput = usdInput;

            var saveButton = CreateDarkSaveButton();

            // Add controls in correct order: Bottom first, Fill second, Top last
            mainPanel.Controls.Add(saveButton);
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(titleLabel);

            return mainPanel;
        }

        private Control CreateWeeklyProfitTargetDarkPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title with emoji
            var titleLabel = new Label
            {
                Text = "🎯 Weekly Profit Target",
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Padding = new Padding(10, 0, 0, 0),
                BackColor = DarkBackground,
                ForeColor = TextWhite
            };

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Set the profit target for each week.",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(10, 0, 10, 0),
                BackColor = DarkBackground,
                ForeColor = TextGray,
                AutoSize = false
            };

            var contentArea = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBackground,
                Padding = new Padding(15)
            };

            // Enable checkbox
            var enableCheckbox = new CheckBox
            {
                Text = "Enable Weekly Profit Target",
                Left = 0,
                Top = 0,
                Width = 300,
                Height = 30,
                Checked = false,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                AutoSize = false
            };
            contentArea.Controls.Add(enableCheckbox);
            weeklyProfitTargetEnabled = enableCheckbox;

            // USD per week label and textbox
            var usdLabel = new Label
            {
                Text = "USD per week:",
                Left = 0,
                Top = 40,
                Width = 150,
                Height = 24,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                ForeColor = TextWhite,
                BackColor = CardBackground,
                AutoSize = false
            };
            contentArea.Controls.Add(usdLabel);

            var usdInput = new TextBox
            {
                Left = 160,
                Top = 40,
                Width = 150,
                Height = 24,
                Text = DEFAULT_WEEKLY_PROFIT_TARGET.ToString(),
                Font = new Font("Segoe UI", 9),
                BackColor = CardBackground,
                ForeColor = TextWhite,
                BorderStyle = BorderStyle.FixedSingle
            };
            contentArea.Controls.Add(usdInput);
            weeklyProfitTargetInput = usdInput;

            var saveButton = CreateDarkSaveButton();

            // Add controls in correct order: Bottom first, Fill second, Top last
            mainPanel.Controls.Add(saveButton);
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(titleLabel);

            return mainPanel;
        }

        /// <summary>
        /// Centers a control horizontally and vertically within its container panel
        /// </summary>
        private void CenterControlInContainer(Panel container, Control control)
        {
            if (container == null || control == null) return;
            
            // Calculate centered position with proper rounding to avoid truncation issues
            int centerX = (int)Math.Round((container.Width - control.Width) / 2.0);
            int centerY = (int)Math.Round((container.Height - control.Height) / 2.0);
            
            // Set the control's location to center it
            control.Location = new Point(centerX, centerY);
        }

        /// <summary>
        /// Enables dragging functionality for a control to move the parent window
        /// </summary>
        private void EnableDragging(Control control)
        {
            Point lastPoint = Point.Empty;
            
            control.MouseDown += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    lastPoint = e.Location;
                }
            };
            
            control.MouseMove += (sender, e) =>
            {
                if (e.Button == MouseButtons.Left && lastPoint != Point.Empty)
                {
                    // Try WinForms Form first
                    Form parentForm = control.FindForm();
                    if (parentForm != null)
                    {
                        // Calculate the new position
                        int newX = parentForm.Left + (e.X - lastPoint.X);
                        int newY = parentForm.Top + (e.Y - lastPoint.Y);
                        
                        // Move the form
                        parentForm.Location = new Point(newX, newY);
                    }
                    else
                    {
                        // Try to find WPF Window via WindowsFormsHost
                        try
                        {
                            // Use cached WPF window if available
                            if (cachedWpfWindow != null && cachedLeftProperty != null && cachedTopProperty != null)
                            {
                                double currentLeft = (double)cachedLeftProperty.GetValue(cachedWpfWindow);
                                double currentTop = (double)cachedTopProperty.GetValue(cachedWpfWindow);
                                
                                // Calculate new position
                                double newX = currentLeft + (e.X - lastPoint.X);
                                double newY = currentTop + (e.Y - lastPoint.Y);
                                
                                // Move the WPF window
                                cachedLeftProperty.SetValue(cachedWpfWindow, newX);
                                cachedTopProperty.SetValue(cachedWpfWindow, newY);
                            }
                            else
                            {
                                // Find and cache WPF window
                                var parent = control.Parent;
                                while (parent != null)
                                {
                                    // Check if parent is a WindowsFormsHost using type comparison
                                    var parentType = parent.GetType();
                                    if (parentType.FullName == "System.Windows.Forms.Integration.WindowsFormsHost" ||
                                        parentType.Name == "WindowsFormsHost")
                                    {
                                        // Get the WPF Window from the WindowsFormsHost
                                        var windowProp = parentType.GetProperty("Window", 
                                            System.Reflection.BindingFlags.Instance | 
                                            System.Reflection.BindingFlags.Public);
                                        
                                        if (windowProp != null)
                                        {
                                            var wpfWindow = windowProp.GetValue(parent);
                                            if (wpfWindow != null)
                                            {
                                                var wpfWindowType = wpfWindow.GetType();
                                                
                                                // Cache the window and properties for future use
                                                cachedWpfWindow = wpfWindow;
                                                cachedLeftProperty = wpfWindowType.GetProperty("Left");
                                                cachedTopProperty = wpfWindowType.GetProperty("Top");
                                                
                                                if (cachedLeftProperty != null && cachedTopProperty != null)
                                                {
                                                    double currentLeft = (double)cachedLeftProperty.GetValue(cachedWpfWindow);
                                                    double currentTop = (double)cachedTopProperty.GetValue(cachedWpfWindow);
                                                    
                                                    // Calculate new position
                                                    double newX = currentLeft + (e.X - lastPoint.X);
                                                    double newY = currentTop + (e.Y - lastPoint.Y);
                                                    
                                                    // Move the WPF window
                                                    cachedLeftProperty.SetValue(cachedWpfWindow, newX);
                                                    cachedTopProperty.SetValue(cachedWpfWindow, newY);
                                                }
                                            }
                                        }
                                        break;
                                    }
                                    parent = parent.Parent;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Log exception for debugging but don't crash
                            System.Diagnostics.Debug.WriteLine($"WPF window dragging error: {ex.Message}");
                        }
                    }
                }
            };
            
            control.MouseUp += (sender, e) =>
            {
                lastPoint = Point.Empty;
            };
        }

        // Helper method to find a control by its Tag property (recursive search)
        private Control FindControlByTag(Control parent, string tagValue)
        {
            if (parent == null || string.IsNullOrEmpty(tagValue))
                return null;

            // Check if this control has the matching tag
            if (parent.Tag is string tag && tag == tagValue)
                return parent;

            // Recursively search children
            foreach (Control child in parent.Controls)
            {
                var found = FindControlByTag(child, tagValue);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void ApplyValueLabelColoring(Control root)
        {
            if (root == null) return;
            foreach (Control child in root.Controls)
            {
                if (child is Label lbl && lbl.Tag is Func<string> getter)
                {
                    try
                    {
                        var display = (getter() ?? string.Empty).Trim();

                        // Default label color for value labels
                        Color colorToApply = TextGray;

                        // Check if this is a lock status getter
                        var methodName = getter.Method.Name;
                        bool isLockStatusGetter = methodName == nameof(GetAccountLockStatus) || 
                                                   methodName == nameof(GetSettingsLockStatus);
                        
                        if (isLockStatusGetter)
                        {
                            // Extract lock status and apply appropriate color
                            var lockStatusText = ExtractLockStatusText(display);
                            colorToApply = GetLockStatusColor(lockStatusText);
                        }
                        else if (currentTheme == Theme.YellowBlueBlack)
                        {
                            // Only these getters should get the positive/negative color mapping
                            if (methodName == nameof(GetPositionLossLimit) ||
                                methodName == nameof(GetDailyLossLimit) ||
                                methodName == nameof(GetDailyProfitTarget) ||
                                methodName == nameof(GetPositionProfitTarget))
                            {
                                // Determine sign from formatted display (FormatNumeric/FormatLossLimit use parentheses for negatives)
                                if (IsNegativeNumericString(display))
                                    colorToApply = NegativeValueColor;
                                else
                                    colorToApply = PositiveValueColor;
                            }
                        }

                        // Assign and ensure label is redrawn; do not change other label properties
                        if (lbl.ForeColor != colorToApply)
                        {
                            lbl.ForeColor = colorToApply;
                            lbl.Invalidate();
                        }
                    }
                    catch
                    {
                        // swallow - best effort coloring only
                    }
                }

                // recurse
                ApplyValueLabelColoring(child);
            }
        }
    }
}
