using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using TradingPlatform.BusinessLayer;
using TradingPlatform.PresentationLayer.Renderers.Chart;
using Risk_Manager.Data;
using DockStyle = System.Windows.Forms.DockStyle;

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
        private DataGridView statsDetailGrid;
        private System.Windows.Forms.Timer statsDetailRefreshTimer;
        private DataGridView typeSummaryGrid;
        private System.Windows.Forms.Timer typeSummaryRefreshTimer;
        private System.Windows.Forms.Timer lockExpirationCheckTimer;
        private System.Windows.Forms.Timer pnlMonitorTimer; // Timer to monitor P&L limits
        private ComboBox typeSummaryFilterComboBox;
        private string selectedNavItem = null;
        private readonly List<Button> navButtons = new();
        private Label settingsStatusBadge;
        private Label tradingStatusBadge;
        private ComboBox accountSelector;
        private Label accountNumberDisplay; // Display current account number in UI
        private Button lockTradingButton; // Lock Trading button reference
        private Button unlockTradingButton; // Unlock Trading button reference
        private ComboBox lockDurationComboBox; // Lock duration selector

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
        
        // Copy Settings controls
        private ComboBox copySettingsSourceComboBox;
        private FlowLayoutPanel copySettingsTargetPanel;
        
        private SoundPlayer alertSoundPlayer;
        
        // Tooltip for draggable title
        private ToolTip titleToolTip;
        
        // Cache for WPF window dragging to avoid repeated reflection
        private object cachedWpfWindow;
        private PropertyInfo cachedLeftProperty;
        private PropertyInfo cachedTopProperty;

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
            White
        }
        
        private Theme currentTheme = Theme.Blue;  // Default theme
        private bool isInitializing = true;  // Flag to prevent saving during initialization

        // Default values for settings
        private const decimal DEFAULT_WEEKLY_LOSS_LIMIT = 1000m;
        private const decimal DEFAULT_WEEKLY_PROFIT_TARGET = 2000m;
        private const int DEFAULT_CONTRACT_LIMIT = 10;

        // P&L monitoring constants
        private const int PNL_MONITOR_INTERVAL_MS = 500; // Check P&L every half second
        private const int FALLBACK_LOCK_HOURS = 8; // Fallback lock duration if timezone calculation fails

        // Account type constants
        private const string ACCOUNT_TYPE_PA = "PA";
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

        // Regex patterns for account type detection (compiled for performance)
        // Using word boundaries to avoid false positives (e.g., "space" won't match "pa", "evaluate" won't match "eval")
        private static readonly Regex PAPattern = new Regex(@"\bpa\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex EvalPattern = new Regex(@"\beval\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
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
            "📊 Accounts Summary", "📈 Stats", "📋 Type", "⚙️ Feature Toggles", "📋 Copy Settings", "📈 Positions", "📊 Limits", "🛡️ Symbols", "🕐 Allowed Trading Times",
            "🔒 Lock Settings", "🔒 Manual Lock"
        };

        private const int LeftPanelWidth = 200;

        public RiskManagerControl()
        {
            // Load saved theme preference or use default (Blue)
            var savedTheme = LoadThemePreference();
            ApplyTheme(savedTheme);
            
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
            
            // Set theme colors based on selection
            switch (theme)
            {
                case Theme.Blue:
                    // Blue theme (original dark theme)
                    DarkBackground = Color.FromArgb(45, 62, 80);      // #2D3E50
                    DarkerBackground = Color.FromArgb(35, 52, 70);    // Slightly darker for sidebar
                    CardBackground = Color.FromArgb(55, 72, 90);      // Card/panel background
                    AccentGreen = Color.FromArgb(39, 174, 96);        // #27AE60 - Green for badges
                    AccentAmber = Color.FromArgb(243, 156, 18);       // #F39C12 - Amber for warnings
                    TextWhite = Color.White;
                    TextGray = Color.FromArgb(189, 195, 199);         // #BDC3C7
                    HoverColor = Color.FromArgb(65, 82, 100);         // Hover state
                    SelectedColor = Color.FromArgb(75, 92, 110);      // Selected state
                    break;
                    
                case Theme.Black:
                    // Black theme (pure dark)
                    DarkBackground = Color.FromArgb(20, 20, 20);      // Very dark gray
                    DarkerBackground = Color.FromArgb(10, 10, 10);    // Almost black sidebar
                    CardBackground = Color.FromArgb(30, 30, 30);      // Dark gray for cards
                    AccentGreen = Color.FromArgb(0, 200, 83);         // Brighter green
                    AccentAmber = Color.FromArgb(255, 185, 0);        // Bright amber
                    TextWhite = Color.White;
                    TextGray = Color.FromArgb(160, 160, 160);         // Medium gray
                    HoverColor = Color.FromArgb(50, 50, 50);          // Hover state
                    SelectedColor = Color.FromArgb(60, 60, 60);       // Selected state
                    break;
                    
                case Theme.White:
                    // White theme (light)
                    DarkBackground = Color.FromArgb(245, 245, 245);   // Light gray
                    DarkerBackground = Color.FromArgb(220, 220, 220); // Slightly darker sidebar
                    CardBackground = Color.White;                      // White cards
                    AccentGreen = Color.FromArgb(39, 174, 96);        // Keep green accent
                    AccentAmber = Color.FromArgb(243, 156, 18);       // Keep amber accent
                    TextWhite = Color.FromArgb(30, 30, 30);           // Dark text for contrast
                    TextGray = Color.FromArgb(90, 90, 90);            // Dark gray for secondary text
                    HoverColor = Color.FromArgb(230, 230, 230);       // Light hover
                    SelectedColor = Color.FromArgb(210, 210, 210);    // Light selected
                    break;
            }
            
            // Save theme preference
            SaveThemePreference();
            
            // Apply theme to all controls
            UpdateAllControlColors();
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
            if (control is Panel)
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
                // Update text color for labels
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
                
                // Update background
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
                    UpdateAccountNumberDisplay(); // Update display
                    LoadAccountSettings();
                }
            }
        }

        private void AccountSelectorOnSelectedIndexChanged(object sender, EventArgs e)
        {
            if (accountSelector.SelectedItem is Account account)
            {
                selectedAccount = account;
                selectedAccountIndex = accountSelector.SelectedIndex; // Store the index
                
                // Debug logging to help identify account selection issues
                var accountId = account.Id ?? "NULL";
                var accountName = account.Name ?? "NULL";
                System.Diagnostics.Debug.WriteLine($"Account selected at index {selectedAccountIndex}: Id='{accountId}', Name='{accountName}'");
                
                // Update the account number display for both Limits and Manual Lock tabs
                UpdateAccountNumberDisplay();
                UpdateAllLockAccountDisplays();
                
                // Refresh Stats tab if visible
                if (statsDetailGrid != null)
                    RefreshAccountStats();
                
                // Load settings for the selected account
                LoadAccountSettings();
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

                // Load Daily Limits
                if (dailyLossLimitEnabled != null && dailyLossLimitInput != null)
                {
                    dailyLossLimitEnabled.Checked = settings.DailyLossLimit.HasValue;
                    dailyLossLimitInput.Text = settings.DailyLossLimit?.ToString() ?? "0";
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
                    positionLossLimitInput.Text = settings.PositionLossLimit?.ToString() ?? "0";
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
                    weeklyLossLimitInput.Text = settings.WeeklyLossLimit?.ToString() ?? DEFAULT_WEEKLY_LOSS_LIMIT.ToString();
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

                // Update status displays
                UpdateTradingStatusBadge();
                if (settingsLockCheckBox?.Tag is Label statusLabel)
                {
                    UpdateSettingsLockStatus(statusLabel);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading account settings: {ex.Message}");
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
        }

        private Panel CreateTopPanel()
        {
            topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = DarkBackground,
                Padding = new Padding(15, 10, 15, 10),
                Cursor = Cursors.SizeAll  // Show move cursor to indicate draggability
            };

            // Make the top panel draggable to move the parent window
            EnableDragging(topPanel);

            // Title label
            var titleLabel = new Label
            {
                Text = "Risk Manager",
                AutoSize = true,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(15, 8),
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
                Location = new Point(15, 40)
            };
            topPanel.Controls.Add(accountLabel);

            accountSelector = new ComboBox
            {
                Location = new Point(80, 37),
                Width = 250,
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 9),
                BackColor = CardBackground,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat
            };
            accountSelector.SelectedIndexChanged += AccountSelectorOnSelectedIndexChanged;
            topPanel.Controls.Add(accountSelector);

            // Emergency Flatten button next to Account Selector
            var emergencyFlattenButton = new Button
            {
                Text = "⚠️ EMERGENCY FLATTEN ⚠️",
                Location = new Point(340, 37),
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

            // Settings Unlocked badge
            settingsStatusBadge = CreateStatusBadge("Settings Unlocked", AccentGreen);
            badgesPanel.Controls.Add(settingsStatusBadge);

            // Trading Unlocked badge
            tradingStatusBadge = CreateStatusBadge("Trading Unlocked", AccentGreen);
            badgesPanel.Controls.Add(tradingStatusBadge);

            // Theme Changer button (replaces the X button)
            var themeButton = new Button
            {
                Text = "🎨",
                Width = 40,
                Height = 32,
                Font = new Font("Segoe UI Emoji", 16, FontStyle.Bold),
                BackColor = Color.FromArgb(52, 152, 219),  // Nice blue color
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 0, 0),
                Padding = new Padding(0),
                UseCompatibleTextRendering = true  // Better emoji support
            };
            themeButton.FlatAppearance.BorderSize = 0;
            themeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(41, 128, 185);
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
                        ApplyTheme(Theme.Blue);
                        break;
                }
            };
            badgesPanel.Controls.Add(themeButton);

            // Position the badges panel initially and on resize
            PositionBadgesPanel(topPanel, badgesPanel);
            topPanel.Controls.Add(badgesPanel);

            topPanel.Resize += (s, e) => PositionBadgesPanel(topPanel, badgesPanel);

            return topPanel;
        }

        private static void PositionBadgesPanel(Panel topPanel, FlowLayoutPanel badgesPanel)
        {
            badgesPanel.Location = new Point(topPanel.Width - badgesPanel.PreferredSize.Width - 20, 15);
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
            var sidebarPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = LeftPanelWidth,
                MinimumSize = new Size(LeftPanelWidth, 0),
                AutoSize = false,
                AutoScroll = true,
                BackColor = DarkerBackground,
                Padding = new Padding(0, 10, 0, 10)
            };

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
                
                // Draw background
                e.Graphics.Clear(btn.BackColor);
                
                // Use Graphics.DrawString for colored emoji support (GDI+)
                using (var brush = new SolidBrush(btn.ForeColor))
                {
                    var sf = new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Near
                    };
                    e.Graphics.DrawString(btn.Text, btn.Font, brush, 
                        new RectangleF(btn.Padding.Left, 0, btn.Width, btn.Height), sf);
                }
            };

            button.Click += (s, e) =>
            {
                selectedNavItem = text;
                UpdateNavButtonStates();
                ShowPage(text);
            };

            return button;
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

        /// <summary>
        /// Creates a label with colored emoji support using custom painting
        /// </summary>
        private Label CreateEmojiLabel(string text, int fontSize, FontStyle fontStyle = FontStyle.Regular)
        {
            var label = new Label
            {
                Text = text,
                Font = new Font("Segoe UI Emoji", fontSize, fontStyle),
                ForeColor = TextWhite,
                BackColor = Color.Transparent,
                AutoSize = false
            };

            // Custom paint for colored emoji rendering
            label.Paint += (s, e) =>
            {
                var lbl = (Label)s;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Draw text with GDI+ for colored emoji support
                using (var brush = new SolidBrush(lbl.ForeColor))
                {
                    var sf = new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Near
                    };
                    e.Graphics.DrawString(lbl.Text, lbl.Font, brush, 
                        new RectangleF(lbl.Padding.Left, 0, lbl.Width, lbl.Height), sf);
                }
            };

            return label;
        }

        private Control CreateAccountsSummaryPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title with colored emoji rendering
            var titleLabel = CreateEmojiLabel("📊 Accounts Summary", 14, FontStyle.Bold);
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 40;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleLabel.Padding = new Padding(10, 0, 0, 0);
            titleLabel.BackColor = DarkBackground;

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

            // Allow row selection to populate Stats tab
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

            RefreshAccountsSummary();
            statsRefreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            statsRefreshTimer.Tick += (s, e) => RefreshAccountsSummary();
            statsRefreshTimer.Start();

            // Add controls in correct order: Fill control first, then Top controls
            // In WinForms, docking is processed in reverse Z-order
            mainPanel.Controls.Add(statsGrid);
            mainPanel.Controls.Add(titleLabel);
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
            }
        }

        private Control CreateAccountStatsPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title
            var titleLabel = new Label
            {
                Text = "Account Stats",
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Padding = new Padding(10, 0, 0, 0),
                BackColor = DarkBackground,
                ForeColor = TextWhite
            };

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
            mainPanel.Controls.Add(titleLabel);
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
            }
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

            // Title with colored emoji rendering
            var titleLabel = CreateEmojiLabel("📋 Type Summary", 14, FontStyle.Bold);
            titleLabel.Dock = DockStyle.Left;
            titleLabel.Width = 200;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleLabel.BackColor = DarkBackground;

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
            topPanel.Controls.Add(titleLabel);

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

            RefreshTypeSummary();
            typeSummaryRefreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            typeSummaryRefreshTimer.Tick += (s, e) => RefreshTypeSummary();
            typeSummaryRefreshTimer.Start();

            // Add controls in correct order: Fill control first, then Top controls
            mainPanel.Controls.Add(typeSummaryGrid);
            mainPanel.Controls.Add(topPanel);
            return mainPanel;
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

                // Dictionary to store aggregated data by type or firm
                var aggregatedData = new Dictionary<string, TypeSummaryData>();

                foreach (var account in core.Accounts)
                {
                    if (account == null) continue;

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
                }

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
                typeSummaryGrid.ResumeLayout();
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

            // Title with emoji rendering
            var titleLabel = CreateEmojiLabel("🕐 Trading Time Restrictions", 14, FontStyle.Bold);
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 40;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleLabel.Padding = new Padding(10, 0, 0, 0);
            titleLabel.BackColor = DarkBackground;

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Select which sessions the trader is allowed to participate in:",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(10, 0, 10, 0),
                BackColor = DarkBackground,
                ForeColor = TextGray,
                AutoSize = false
            };

            // Content area with proper scrolling
            var contentArea = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = CardBackground,
                Padding = new Padding(15),
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };

            var sessions = new[]
            {
                ("NY Session", "8 AM - 5 PM EST"),
                ("London Session", "3 AM - 12 PM EST"),
                ("Asia Session", "7 PM - 4 AM EST")
            };

            foreach (var (sessionName, timeRange) in sessions)
            {
                var checkbox = new CheckBox
                {
                    Text = $"{sessionName} ({timeRange})",
                    AutoSize = true,
                    Checked = true,
                    Font = new Font("Segoe UI", 10, FontStyle.Regular),
                    ForeColor = TextWhite,
                    BackColor = CardBackground,
                    Margin = new Padding(0, 5, 0, 5)
                };
                contentArea.Controls.Add(checkbox);
            }

            var saveButton = CreateDarkSaveButton();

            // Add controls in correct order: Bottom first, Fill second, Top last
            // In WinForms, docking is processed in reverse Z-order
            mainPanel.Controls.Add(saveButton);
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(titleLabel);

            return mainPanel;
        }

        private Control CreateLockSettingsDarkPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title with emoji rendering
            var titleLabel = CreateEmojiLabel("⚙️ Settings Lock", 14, FontStyle.Bold);
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 40;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleLabel.Padding = new Padding(10, 0, 0, 0);
            titleLabel.BackColor = DarkBackground;

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Prevent changes to settings.",
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

            var lockCheckbox = new CheckBox
            {
                Text = "Enable Settings Lock",
                Left = 0,
                Top = 0,
                Width = 250,
                Height = 30,
                Checked = false,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = TextWhite,
                BackColor = CardBackground
            };
            contentArea.Controls.Add(lockCheckbox);
            settingsLockCheckBox = lockCheckbox;

            // Status label to show lock state with color
            var lblSettingsStatus = new Label
            {
                Text = "Settings Unlocked",
                Left = 270,
                Top = 5,
                Width = 150,
                Height = 30,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = AccentGreen,
                BackColor = CardBackground,
                TextAlign = ContentAlignment.MiddleLeft,
                Tag = "SettingsStatus" // Tag for identification
            };
            contentArea.Controls.Add(lblSettingsStatus);

            // Initialize status from settings service
            UpdateSettingsLockStatus(lblSettingsStatus);

            // Store reference to status label for use in save button
            lockCheckbox.Tag = lblSettingsStatus;

            var saveButton = CreateDarkSaveButton();

            // Add controls in correct order: Bottom first, Fill second, Top last
            // In WinForms, docking is processed in reverse Z-order
            mainPanel.Controls.Add(saveButton);
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(titleLabel);

            return mainPanel;
        }

        private Control CreateManualLockDarkPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title
            var titleLabel = CreateEmojiLabel("🔒 Trading Lock", 14, FontStyle.Bold);
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 40;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleLabel.Padding = new Padding(10, 0, 0, 0);
            titleLabel.BackColor = DarkBackground;

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

            // Lock Duration Section
            var durationLabel = new Label
            {
                Text = "Lock Duration:",
                Left = 0,
                Top = 10,
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
                Top = 10,
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
                Top = 60,
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
                Top = 60,
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

            // Add controls in correct order: Fill first, then Top
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(lockAccountDisplay);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(titleLabel);

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
                    "This will disable all Buy/Sell buttons until the lock expires.",
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
                        settingsService.SetTradingLock(accountNumber, true, reason, duration);
                    }
                    
                    // Always update the trading status badge immediately (no conditional check)
                    UpdateTradingStatusBadgeUI(true);
                    
                    // Update button states - Lock button should now be disabled
                    // Do this BEFORE refresh to avoid race conditions
                    UpdateLockButtonStates();
                    
                    RefreshAccountsSummary();
                    RefreshAccountStats();
                    
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
                    }
                    
                    // Always update the trading status badge immediately (no conditional check)
                    UpdateTradingStatusBadgeUI(false);
                    
                    // Update button states - Unlock button should now be disabled
                    // Do this BEFORE refresh to avoid race conditions
                    UpdateLockButtonStates();
                    
                    RefreshAccountsSummary();
                    RefreshAccountStats();
                    
                    MessageBox.Show($"Account '{accountNumber}' has been unlocked successfully. Buy/Sell buttons are now enabled.", "Trading Unlocked", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show("UnLockAccount method not available in Core API.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error unlocking the account: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
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
                        
                        // IsTradingLocked checks expiration and auto-unlocks if expired
                        var isLocked = settingsService.IsTradingLocked(uniqueAccountId);
                        
                        // If was locked but now unlocked, the lock expired and was auto-unlocked
                        if (wasLocked && !isLocked)
                        {
                            anyUnlocked = true;
                            
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
                    }

                    accountIndex++;
                }

                // Update button states and refresh displays only if state changed
                if (anyUnlocked || anyLocked)
                {
                    UpdateLockButtonStates();
                    
                    // Refresh account summary and stats displays
                    if (anyUnlocked)
                    {
                        RefreshAccountsSummary();
                        RefreshAccountStats();
                    }
                    
                    // Update badge only when state changes are detected
                    var selectedAccountNumber = GetSelectedAccountNumber();
                    if (!string.IsNullOrEmpty(selectedAccountNumber))
                    {
                        bool selectedIsLocked = settingsService.IsTradingLocked(selectedAccountNumber);
                        System.Diagnostics.Debug.WriteLine($"CheckExpiredLocks: State changed - updating badge. Account='{selectedAccountNumber}', IsLocked={selectedIsLocked}");
                        UpdateTradingStatusBadgeUI(selectedIsLocked);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking expired locks: {ex.Message}");
            }
        }

        /// <summary>
        /// Monitors P&L limits for all accounts and enforces automatic lockouts and position closures.
        /// Checks daily P&L against daily limits and position P&L against position limits.
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

                    // Skip if account is already locked
                    if (settingsService.IsTradingLocked(uniqueAccountId))
                        continue;

                    // Check Allowed Trading Times - close positions if outside trading hours
                    CheckTradingTimeRestrictions(item.account, uniqueAccountId, settings, settingsService, core);

                    // Check Symbol Blacklist - close positions for blacklisted symbols
                    CheckSymbolBlacklist(item.account, uniqueAccountId, settings, settingsService, core);

                    // Check Symbol Contract Limits - close excess positions
                    CheckSymbolContractLimits(item.account, uniqueAccountId, settings, settingsService, core);

                    // Check Daily P&L limits
                    CheckDailyPnLLimits(item.account, uniqueAccountId, settings, core);

                    // Check Position P&L limits
                    CheckPositionPnLLimits(item.account, uniqueAccountId, settings, core);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error monitoring P&L limits: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if daily P&L has exceeded limits and locks account if necessary.
        /// </summary>
        private void CheckDailyPnLLimits(Account account, string accountId, AccountSettings settings, Core core)
        {
            try
            {
                // Get daily P&L from account
                double dailyPnL = GetAccountDailyPnL(account);

                // Check Daily Loss Limit (negative value)
                if (settings.DailyLossLimit.HasValue && settings.DailyLossLimit.Value < 0)
                {
                    decimal lossLimit = settings.DailyLossLimit.Value;
                    if ((decimal)dailyPnL <= lossLimit)
                    {
                        // Loss limit exceeded - lock account until 5 PM ET
                        string reason = $"Daily Loss Limit reached: P&L ${dailyPnL:F2} ≤ Limit ${lossLimit:F2}";
                        LockAccountUntil5PMET(accountId, reason, core, account);
                        CloseAllPositionsForAccount(account, core);
                        System.Diagnostics.Debug.WriteLine($"Account {accountId} locked due to daily loss limit");
                        return; // Exit after locking
                    }
                }

                // Check Daily Profit Target (positive value)
                if (settings.DailyProfitTarget.HasValue && settings.DailyProfitTarget.Value > 0)
                {
                    decimal profitTarget = settings.DailyProfitTarget.Value;
                    if ((decimal)dailyPnL >= profitTarget)
                    {
                        // Profit target reached - lock account until 5 PM ET
                        string reason = $"Daily Profit Target reached: P&L ${dailyPnL:F2} ≥ Target ${profitTarget:F2}";
                        LockAccountUntil5PMET(accountId, reason, core, account);
                        CloseAllPositionsForAccount(account, core);
                        System.Diagnostics.Debug.WriteLine($"Account {accountId} locked due to daily profit target");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking daily P&L limits for account {accountId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Checks if weekly P&L has exceeded limits and locks account if necessary.
        /// </summary>
        private void CheckWeeklyPnLLimits(Account account, string accountId, AccountSettings settings, Core core)
        {
            try
            {
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
        /// </summary>
        private void CheckPositionPnLLimits(Account account, string accountId, AccountSettings settings, Core core)
        {
            try
            {
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
                            // Position loss limit exceeded - close position
                            ClosePosition(position, core);
                            System.Diagnostics.Debug.WriteLine($"Position closed due to loss limit: {position.Symbol} OpenPnL ${openPnL:F2} ≤ Limit ${lossLimit:F2}");
                        }
                    }

                    // Check Position Profit Target (positive value)
                    if (settings.PositionProfitTarget.HasValue && settings.PositionProfitTarget.Value > 0)
                    {
                        decimal profitTarget = settings.PositionProfitTarget.Value;
                        if ((decimal)openPnL >= profitTarget)
                        {
                            // Position profit target reached - close position
                            ClosePosition(position, core);
                            System.Diagnostics.Debug.WriteLine($"Position closed due to profit target: {position.Symbol} OpenPnL ${openPnL:F2} ≥ Target ${profitTarget:F2}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking position P&L limits for account {accountId}: {ex.Message}");
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
                    UpdateTradingStatusBadgeUI(true);
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
                    UpdateTradingStatusBadgeUI(true);
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

                var accountPositions = core.Positions
                    .Where(p => p != null && p.Account == account)
                    .ToList();

                foreach (var position in accountPositions)
                {
                    ClosePosition(position, core);
                }

                System.Diagnostics.Debug.WriteLine($"Closed {accountPositions.Count} positions for account");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error closing all positions for account: {ex.Message}");
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
            //   - FFNX: PA (Personal Account)
            //   - FFN: Eval (Evaluation/Funded Account)
            //   - BX: PA (Personal Account)
            //   - APEX: Eval (Evaluation/Funded Account)
            
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

        private void UpdateTradingStatusBadge()
        {
            try
            {
                var accountNumber = GetSelectedAccountNumber();
                if (string.IsNullOrEmpty(accountNumber))
                {
                    return;
                }

                var settingsService = RiskManagerSettingsService.Instance;
                if (!settingsService.IsInitialized)
                {
                    return;
                }

                bool isLocked = settingsService.IsTradingLocked(accountNumber);
                System.Diagnostics.Debug.WriteLine($"UpdateTradingStatusBadge: Account='{accountNumber}', IsLocked={isLocked}");

                UpdateTradingStatusBadgeUI(isLocked);
            }
            catch (Exception ex)
            {
                // Log error but don't interrupt UI flow
                System.Diagnostics.Debug.WriteLine($"Error updating trading status badge: {ex.Message}");
            }
        }

        private void UpdateTradingStatusBadgeUI(bool isLocked)
        {
            try
            {
                if (tradingStatusBadge != null)
                {
                    string newState = isLocked ? "Locked (Red)" : "Unlocked (Green)";
                    System.Diagnostics.Debug.WriteLine($"UpdateTradingStatusBadgeUI: Setting badge to {newState}");
                    
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
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating trading status badge UI: {ex.Message}");
            }
        }

        private void UpdateSettingsStatusBadge(bool isLocked)
        {
            try
            {
                if (settingsStatusBadge != null)
                {
                    if (isLocked)
                    {
                        settingsStatusBadge.Text = "  Settings Locked  ";
                        settingsStatusBadge.BackColor = Color.Red;
                    }
                    else
                    {
                        settingsStatusBadge.Text = "  Settings Unlocked  ";
                        settingsStatusBadge.BackColor = AccentGreen;
                    }
                    settingsStatusBadge.Invalidate();
                }
            }
            catch (Exception ex)
            {
                // Log error but don't interrupt UI flow
                System.Diagnostics.Debug.WriteLine($"Error updating settings status badge: {ex.Message}");
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
                    lblSettingsStatus.Text = "Settings Locked";
                    lblSettingsStatus.ForeColor = Color.Red;
                }
                else
                {
                    lblSettingsStatus.Text = "Settings Unlocked";
                    lblSettingsStatus.ForeColor = AccentGreen;
                }

                // Update top badge as well
                UpdateSettingsStatusBadge(isLocked);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating settings lock status: {ex.Message}");
                lblSettingsStatus.Text = "Status Error";
                lblSettingsStatus.ForeColor = TextGray;
            }
        }

        private void UpdateAccountNumberDisplay()
        {
            try
            {
                if (accountNumberDisplay == null)
                    return;
                
                var accountNumber = GetSelectedAccountNumber();
                
                // Cache the account number so save operation uses exactly what's displayed
                displayedAccountNumber = accountNumber;
                
                if (string.IsNullOrEmpty(accountNumber))
                {
                    accountNumberDisplay.Text = "Account: Not Selected";
                    accountNumberDisplay.ForeColor = Color.Orange;
                }
                else
                {
                    accountNumberDisplay.Text = $"Account: {accountNumber}";
                    accountNumberDisplay.ForeColor = Color.Transparent;
                }
                
                System.Diagnostics.Debug.WriteLine($"UpdateAccountNumberDisplay: Displaying and caching account='{accountNumber}'");
                accountNumberDisplay.Invalidate();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating account number display: {ex.Message}");
            }
        }

        private void EmergencyFlattenButton_Click(object sender, EventArgs e)
        {
            FlattenAllTrades();
            PlayAlertSound();
            MessageBox.Show("Emergency Flatten Triggered!", "Emergency Flatten", MessageBoxButtons.OK, MessageBoxIcon.Information);
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
                    // Use the cached account number that's displayed in the UI
                    // This ensures we save to exactly what the user sees
                    var accountNumber = displayedAccountNumber;
                    
                    // Debug logging
                    System.Diagnostics.Debug.WriteLine($"Save button clicked for account: '{accountNumber}' (using displayed value)");
                    
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
                            service.UpdateDailyLossLimit(accountNumber, lossLimit);
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
                            service.UpdatePositionLossLimit(accountNumber, posLossLimit);
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
                            service.UpdateWeeklyLossLimit(accountNumber, weeklyLossLimit);
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

            // Title with emoji rendering
            var titleLabel = CreateEmojiLabel("⚙️ Advanced", 14, FontStyle.Bold);
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 40;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleLabel.Padding = new Padding(10, 0, 0, 0);
            titleLabel.BackColor = DarkBackground;

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
                "Allowed Trading Times",
                "Weekly Loss",
                "Weekly Profit Target"
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
                
                // Store reference to the master toggle checkbox
                if (i == 0 && feature == "Enable All Features")
                {
                    featureToggleEnabledCheckbox = checkbox;
                }
            }

            var saveButton = CreateDarkSaveButton();

            // Add controls in correct order: Bottom first, Fill second, Top last
            // In WinForms, docking is processed in reverse Z-order
            mainPanel.Controls.Add(saveButton);
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(titleLabel);

            return mainPanel;
        }

        /// <summary>
        /// Creates the "Copy Settings" panel for copying settings from one account to multiple accounts.
        /// </summary>
        private Control CreateCopySettingsPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title
            var titleLabel = CreateEmojiLabel("📋 Copy Settings", 14, FontStyle.Bold);
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 40;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleLabel.Padding = new Padding(10, 0, 0, 0);
            titleLabel.BackColor = DarkBackground;

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
                Margin = new Padding(0, 0, 0, 20)
            };
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

                        var checkbox = new CheckBox
                        {
                            Text = $"{account.Name} ({GetAccountIdentifier(account)})",
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
            mainPanel.Controls.Add(titleLabel);

            return mainPanel;
        }

        /// <summary>
        /// Creates the consolidated "Positions" panel combining Position Loss Limit and Position Profit Target.
        /// </summary>
        private Control CreatePositionsPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title with emoji rendering
            var titleLabel = CreateEmojiLabel("📈 Positions", 14, FontStyle.Bold);
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 40;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleLabel.Padding = new Padding(10, 0, 0, 0);
            titleLabel.BackColor = DarkBackground;

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
            mainPanel.Controls.Add(titleLabel);

            return mainPanel;
        }

        /// <summary>
        /// Helper method to create a position section with toggle and USD input.
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
                BackColor = CardBackground
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
            var titleLabel = CreateEmojiLabel("📊 Limits", 14, FontStyle.Bold);
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 40;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleLabel.Padding = new Padding(10, 0, 0, 0);
            titleLabel.BackColor = DarkBackground;

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

            // Account Number Display - shows which account settings will be saved to
            // This is hidden but functionality is retained for settings persistence
            accountNumberDisplay = new Label
            {
                Text = "Account: Not Selected",
                Dock = DockStyle.Top,
                Height = 30,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                Padding = new Padding(10, 5, 10, 0),
                BackColor = CardBackground,
                ForeColor = TextWhite,  // Set proper color in case it's made visible later
                AutoSize = false,
                BorderStyle = BorderStyle.FixedSingle,
                Visible = false  // Hide the control while retaining functionality
            };
            
            // Update the display with current account
            UpdateAccountNumberDisplay();

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
            mainPanel.Controls.Add(accountNumberDisplay);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(titleLabel);

            return mainPanel;
        }

        /// <summary>
        /// Helper method to create a limit section with toggle and USD input.
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
            
            // Custom paint for colored emoji rendering in checkbox
            sectionHeader.Paint += (s, e) =>
            {
                var cb = (CheckBox)s;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Draw background
                e.Graphics.Clear(cb.BackColor);
                
                // Draw checkbox
                var checkBoxSize = 13;
                var checkBoxRect = new Rectangle(0, (cb.Height - checkBoxSize) / 2, checkBoxSize, checkBoxSize);
                ControlPaint.DrawCheckBox(e.Graphics, checkBoxRect, cb.Checked ? ButtonState.Checked : ButtonState.Normal);
                
                // Draw text with GDI+ for colored emoji support
                using (var brush = new SolidBrush(cb.ForeColor))
                {
                    var sf = new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Near
                    };
                    var textRect = new RectangleF(checkBoxSize + 5, 0, cb.Width - checkBoxSize - 5, cb.Height);
                    e.Graphics.DrawString(cb.Text, cb.Font, brush, textRect, sf);
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
            var titleLabel = CreateEmojiLabel("🛡️ Symbols", 14, FontStyle.Bold);
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 40;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleLabel.Padding = new Padding(10, 0, 0, 0);
            titleLabel.BackColor = DarkBackground;

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
            mainPanel.Controls.Add(titleLabel);

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
                Text = "🛡️ Symbol Blacklist",
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
            
            // Custom paint for colored emoji rendering in checkbox
            sectionHeader.Paint += (s, e) =>
            {
                var cb = (CheckBox)s;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Draw background
                e.Graphics.Clear(cb.BackColor);
                
                // Draw checkbox
                var checkBoxSize = 13;
                var checkBoxRect = new Rectangle(0, (cb.Height - checkBoxSize) / 2, checkBoxSize, checkBoxSize);
                ControlPaint.DrawCheckBox(e.Graphics, checkBoxRect, cb.Checked ? ButtonState.Checked : ButtonState.Normal);
                
                // Draw text with GDI+ for colored emoji support
                using (var brush = new SolidBrush(cb.ForeColor))
                {
                    var sf = new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Near
                    };
                    var textRect = new RectangleF(checkBoxSize + 5, 0, cb.Width - checkBoxSize - 5, cb.Height);
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
                Text = "🛡️ Symbol Contract Limits",
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
            
            // Custom paint for colored emoji rendering in checkbox
            sectionHeader.Paint += (s, e) =>
            {
                var cb = (CheckBox)s;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
                e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                
                // Draw background
                e.Graphics.Clear(cb.BackColor);
                
                // Draw checkbox
                var checkBoxSize = 13;
                var checkBoxRect = new Rectangle(0, (cb.Height - checkBoxSize) / 2, checkBoxSize, checkBoxSize);
                ControlPaint.DrawCheckBox(e.Graphics, checkBoxRect, cb.Checked ? ButtonState.Checked : ButtonState.Normal);
                
                // Draw text with GDI+ for colored emoji support
                using (var brush = new SolidBrush(cb.ForeColor))
                {
                    var sf = new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Near
                    };
                    var textRect = new RectangleF(checkBoxSize + 5, 0, cb.Width - checkBoxSize - 5, cb.Height);
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

                alertSoundPlayer?.Dispose();
                alertSoundPlayer = null;
                
                titleToolTip?.Dispose();
                titleToolTip = null;
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
    }
}