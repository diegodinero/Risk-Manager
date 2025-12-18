using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Media;
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
        private readonly Dictionary<string, Control> pageContents = new();
        private DataGridView statsGrid;
        private System.Windows.Forms.Timer statsRefreshTimer;
        private Account selectedAccount;
        private DataGridView statsDetailGrid;
        private System.Windows.Forms.Timer statsDetailRefreshTimer;
        private DataGridView typeSummaryGrid;
        private System.Windows.Forms.Timer typeSummaryRefreshTimer;
        private string selectedNavItem = null;
        private readonly List<Button> navButtons = new();
        private Label settingsStatusBadge;
        private Label tradingStatusBadge;
        private ComboBox accountSelector;

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

        // Dark theme colors
        private static readonly Color DarkBackground = Color.FromArgb(45, 62, 80);      // #2D3E50
        private static readonly Color DarkerBackground = Color.FromArgb(35, 52, 70);    // Slightly darker for sidebar
        private static readonly Color CardBackground = Color.FromArgb(55, 72, 90);      // Card/panel background
        private static readonly Color AccentGreen = Color.FromArgb(39, 174, 96);        // #27AE60 - Green for badges
        private static readonly Color AccentAmber = Color.FromArgb(243, 156, 18);       // #F39C12 - Amber for warnings
        private static readonly Color TextWhite = Color.White;
        private static readonly Color TextGray = Color.FromArgb(189, 195, 199);         // #BDC3C7
        private static readonly Color HoverColor = Color.FromArgb(65, 82, 100);         // Hover state
        private static readonly Color SelectedColor = Color.FromArgb(75, 92, 110);      // Selected state

        // Navigation items - includes Stats and Accounts Summary
        // Consolidated tabs: "Positions" (Position Win + Position Loss), "Limits" (Daily Loss + Daily Profit Target), "Symbols" (Block Symbols + Position Size)
        private static readonly string[] NavItems = new[]
        {
            "📊 Accounts Summary", "📈 Stats", "📋 Type", "⚙️ Feature Toggles", "📈 Positions", "📊 Limits", "🛡️ Symbols", "🕐 Allowed Trading Times",
            "📉 Weekly Loss", "📈 Weekly Profit Target", "🔒 Lock Settings", "🔒 Manual Lock"
        };

        private const int LeftPanelWidth = 200;

        public RiskManagerControl()
        {
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
                else if (name.EndsWith("Positions"))
                    placeholder = CreatePositionsPanel();
                else if (name.EndsWith("Limits"))
                    placeholder = CreateLimitsPanel();
                else if (name.EndsWith("Symbols"))
                    placeholder = CreateSymbolsPanel();
                else if (name.EndsWith("Allowed Trading Times"))
                    placeholder = CreateAllowedTradingTimesDarkPanel();
                else if (name.EndsWith("Weekly Loss"))
                    placeholder = CreateWeeklyLossDarkPanel();
                else if (name.EndsWith("Weekly Profit Target"))
                    placeholder = CreateWeeklyProfitTargetDarkPanel();
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
            dropdownRefreshTimer.Tick += (s, e) => RefreshAccountDropdown();
            dropdownRefreshTimer.Start();

            // Show Accounts Summary by default
            selectedNavItem = "📊 Accounts Summary";
            UpdateNavButtonStates();
            ShowPage("📊 Accounts Summary");
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
            }
            else if (accountSelector.Items.Count > 0)
            {
                accountSelector.SelectedIndex = 0;
            }
            else
            {
                accountSelector.SelectedIndex = -1;
            }

            // Re-enable event handling
            accountSelector.SelectedIndexChanged += AccountSelectorOnSelectedIndexChanged;
        }

        private void AccountSelectorOnSelectedIndexChanged(object sender, EventArgs e)
        {
            if (accountSelector.SelectedItem is Account account)
            {
                selectedAccount = account;
                // Refresh Stats tab if visible
                if (statsDetailGrid != null)
                    RefreshAccountStats();
            }
        }

        private Panel CreateTopPanel()
        {
            var topPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = DarkBackground,
                Padding = new Padding(15, 10, 15, 10)
            };

            // Title label
            var titleLabel = new Label
            {
                Text = "Risk Manager",
                AutoSize = true,
                ForeColor = TextWhite,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                Location = new Point(15, 8)
            };
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

            // Close button (X)
            var closeButton = new Button
            {
                Text = "✕",
                Width = 32,
                Height = 32,
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                BackColor = AccentAmber,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(5, 0, 0, 0),
                Padding = new Padding(0)
            };
            closeButton.FlatAppearance.BorderSize = 0;
            closeButton.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 140, 0);
            closeButton.Click += (s, e) =>
            {
                // Request parent to remove this control
                if (this.Parent != null)
                {
                    this.Parent.Controls.Remove(this);
                }

                var form = this.FindForm();
                if (form != null)
                {
                    form.Close();
                }
            };
            badgesPanel.Controls.Add(closeButton);

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
                using var path = GetRoundedRectPath(new Rectangle(0, 0, badge.Width, badge.Height), 4);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using var brush = new SolidBrush(badgeColor);
                e.Graphics.FillPath(brush, path);
                using var textBrush = new SolidBrush(TextWhite);
                var textSize = e.Graphics.MeasureString(text, badge.Font);
                var textX = (badge.Width - textSize.Width) / 2;
                var textY = (badge.Height - textSize.Height) / 2;
                e.Graphics.DrawString(text, badge.Font, textBrush, textX, textY);
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
            statsGrid.Columns.Add("Drawdown", "Drawdown");
            statsGrid.Columns.Add("Positions", "Positions");
            statsGrid.Columns.Add("Status", "Status");
            statsGrid.Columns.Add("LockStatus", "Lock Status");
            statsGrid.Columns.Add("LossLimit", "Loss Limit");
            statsGrid.Columns.Add("ProfitTarget", "Profit Target");
            statsGrid.Columns.Add("TrailingBalance", "Trailing Balance");

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
                    // Demo data with all new columns
                    statsGrid.Rows.Add("DemoProvider", "DemoConn", "ACC123", "Live", "1000.00", "12.34", "50.00", "5.67", "18.01", "0.00", "1", "Connected", "Unlocked", "500.00", "1000.00", "1000.00");
                    statsGrid.Rows.Add("DemoProvider", "DemoConn2", "ACC456", "Demo", "2500.50", "-8.20", "25.00", "-2.00", "-10.20", "0.00", "2", "Connected", "Unlocked", "500.00", "1000.00", "2500.50");
                    return;
                }

                foreach (var account in core.Accounts)
                {
                    if (account == null) continue;

                    var provider = account.Connection?.VendorName ?? account.Connection?.Name ?? "Unknown";
                    var connectionName = account.Connection?.Name ?? "Unknown";
                    var accountId = account.Id ?? account.Name ?? "Unknown";
                    var equity = account.Balance;

                    // Get account type from AdditionalInfo or Connection
                    var accountType = "Unknown";
                    if (account.Connection != null)
                    {
                        // Try to determine if it's a demo or live account
                        var connName = account.Connection.Name?.ToLower() ?? "";
                        if (connName.Contains("demo") || connName.Contains("simulation") || connName.Contains("paper"))
                            accountType = "Demo";
                        else if (connName.Contains("live") || connName.Contains("real"))
                            accountType = "Live";
                        // Keep as "Unknown" if we can't determine the type
                    }

                    double openPnL = 0;
                    int positionsCount = 0;
                    if (core.Positions != null)
                    {
                        foreach (var pos in core.Positions)
                        {
                            if (pos == null) continue;
                            if (pos.Account == account && pos.Quantity != 0)
                            {
                                var pnlItem = pos.NetPnL ?? pos.GrossPnL;
                                if (pnlItem != null) openPnL += pnlItem.Value;
                                positionsCount++;
                            }
                        }
                    }

                    double dailyPnL = 0, grossPnL = 0, closedPnL = 0, drawdown = 0;
                    if (account.AdditionalInfo != null)
                    {
                        foreach (var info in account.AdditionalInfo)
                        {
                            if (info?.Id == null) continue;
                            var id = info.Id;
                            
                            // Daily P&L
                            if (string.Equals(id, "Daily Net P&L", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(id, "TotalPnL", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(id, "DailyPnL", StringComparison.OrdinalIgnoreCase))
                            {
                                if (info.Value is double dv) dailyPnL = dv;
                            }

                            // Gross P&L
                            if (string.Equals(id, "Gross P&L", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(id, "GrossPnL", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(id, "Total P&L", StringComparison.OrdinalIgnoreCase))
                            {
                                if (info.Value is double gv) grossPnL = gv;
                            }

                            // Closed P&L
                            if (string.Equals(id, "Closed P&L", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(id, "ClosedPnL", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(id, "Realized P&L", StringComparison.OrdinalIgnoreCase))
                            {
                                if (info.Value is double cv) closedPnL = cv;
                            }

                            // Drawdown
                            if (string.Equals(id, "Drawdown", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(id, "MaxDrawdown", StringComparison.OrdinalIgnoreCase))
                            {
                                if (info.Value is double dd) drawdown = dd;
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

                    // Get lock status from settings service
                    var settingsService = RiskManagerSettingsService.Instance;
                    var lockStatus = "Unlocked";
                    decimal? lossLimit = null;
                    decimal? profitTarget = null;
                    if (settingsService.IsInitialized)
                    {
                        var isLocked = settingsService.IsTradingLocked(accountId);
                        lockStatus = isLocked ? "Locked" : "Unlocked";

                        // Get loss limit and profit target from settings
                        var settings = settingsService.GetSettings(accountId);
                        if (settings != null)
                        {
                            lossLimit = settings.DailyLossLimit;
                            profitTarget = settings.DailyProfitTarget;
                        }
                    }

                    // Trailing Balance - use equity as a starting point
                    var trailingBalance = equity;

                    statsGrid.Rows.Add(
                        provider, 
                        connectionName, 
                        accountId, 
                        accountType,
                        equity.ToString("N2"), 
                        openPnL.ToString("N2"), 
                        closedPnL.ToString("N2"),
                        dailyPnL.ToString("N2"), 
                        grossPnL.ToString("N2"), 
                        drawdown.ToString("N2"),
                        positionsCount.ToString(), 
                        status,
                        lockStatus,
                        lossLimit?.ToString("N2") ?? "-",
                        profitTarget?.ToString("N2") ?? "-",
                        trailingBalance.ToString("N2")
                    );
                }
            }
            catch
            {
                // ignore refresh errors
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

                // Calculate Open P&L from positions (same as Accounts Summary)
                double openPnL = 0;
                int positionsCount = 0;
                if (core?.Positions != null)
                {
                    foreach (var pos in core.Positions)
                    {
                        if (pos == null) continue;
                        if (pos.Account == accountToDisplay && pos.Quantity != 0)
                        {
                            var pnlItem = pos.NetPnL ?? pos.GrossPnL;
                            if (pnlItem != null) openPnL += pnlItem.Value;
                            positionsCount++;
                        }
                    }
                }

                // Get Daily P&L and Gross P&L from AdditionalInfo (same as Accounts Summary)
                double dailyPnL = 0, grossPnL = 0;
                if (accountToDisplay.AdditionalInfo != null)
                {
                    foreach (var info in accountToDisplay.AdditionalInfo)
                    {
                        if (info?.Id == null) continue;
                        var id = info.Id;
                        
                        // Daily P&L
                        if (string.Equals(id, "Daily Net P&L", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(id, "TotalPnL", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(id, "DailyPnL", StringComparison.OrdinalIgnoreCase))
                        {
                            if (info.Value is double dv) dailyPnL = dv;
                        }

                        // Gross P&L
                        if (string.Equals(id, "Gross P&L", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(id, "GrossPnL", StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(id, "Total P&L", StringComparison.OrdinalIgnoreCase))
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
                    var isLocked = settingsService.IsTradingLocked(accountId);
                    lockStatus = isLocked ? "Locked" : "Unlocked";
                }

                // Display all stats matching Accounts Summary data
                statsDetailGrid.Rows.Add("Provider", provider);
                statsDetailGrid.Rows.Add("Connection", connectionName);
                statsDetailGrid.Rows.Add("Account", accountId);
                statsDetailGrid.Rows.Add("Balance", balance.ToString("N2"));
                statsDetailGrid.Rows.Add("Open P&L", openPnL.ToString("N2"));
                statsDetailGrid.Rows.Add("Daily P&L", dailyPnL.ToString("N2"));
                statsDetailGrid.Rows.Add("Gross P&L", grossPnL.ToString("N2"));
                statsDetailGrid.Rows.Add("Positions", positionsCount.ToString());
                statsDetailGrid.Rows.Add("Connection Status", connectionStatus);
                statsDetailGrid.Rows.Add("Trading Status", lockStatus);
            }
            catch
            {
                // ignore refresh errors
            }
            finally
            {
                statsDetailGrid.ResumeLayout();
            }
        }

        private Control CreateTypeSummaryPanel()
        {
            var mainPanel = new Panel { BackColor = DarkBackground, Dock = DockStyle.Fill };

            // Title with colored emoji rendering
            var titleLabel = CreateEmojiLabel("📋 Type Summary", 14, FontStyle.Bold);
            titleLabel.Dock = DockStyle.Top;
            titleLabel.Height = 40;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            titleLabel.Padding = new Padding(10, 0, 0, 0);
            titleLabel.BackColor = DarkBackground;

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
            typeSummaryGrid.Columns.Add("Summary", "Summary");
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
            mainPanel.Controls.Add(titleLabel);
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
                    typeSummaryGrid.Rows.Add("Demo", "1", "2500.50", "-8.20", "25.00", "16.80", "0.00");
                    typeSummaryGrid.Rows.Add("Total", "2", "3500.50", "4.14", "75.00", "79.14", "0.00");
                    return;
                }

                // Dictionary to store aggregated data by type
                var typeData = new Dictionary<string, TypeSummaryData>();

                foreach (var account in core.Accounts)
                {
                    if (account == null) continue;

                    // Get account type (same logic as Accounts Summary)
                    var accountType = "Unknown";
                    if (account.Connection != null)
                    {
                        var connName = account.Connection.Name?.ToLower() ?? "";
                        if (connName.Contains("demo") || connName.Contains("simulation") || connName.Contains("paper"))
                            accountType = "Demo";
                        else if (connName.Contains("live") || connName.Contains("real"))
                            accountType = "Live";
                    }

                    // Check AdditionalInfo for type override
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
                                if (info.Value is string at) accountType = at;
                            }
                        }
                    }

                    // Initialize type data if not exists
                    if (!typeData.ContainsKey(accountType))
                    {
                        typeData[accountType] = new TypeSummaryData();
                    }

                    var data = typeData[accountType];
                    data.Count++;
                    data.Equity += account.Balance;

                    // Calculate Open P&L from positions
                    if (core.Positions != null)
                    {
                        foreach (var pos in core.Positions)
                        {
                            if (pos == null) continue;
                            if (pos.Account == account && pos.Quantity != 0)
                            {
                                var pnlItem = pos.NetPnL ?? pos.GrossPnL;
                                if (pnlItem != null) data.OpenPnL += pnlItem.Value;
                            }
                        }
                    }

                    // Get Closed P&L and Drawdown from AdditionalInfo
                    if (account.AdditionalInfo != null)
                    {
                        foreach (var info in account.AdditionalInfo)
                        {
                            if (info?.Id == null) continue;
                            var id = info.Id;

                            // Closed P&L
                            if (string.Equals(id, "Closed P&L", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(id, "ClosedPnL", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(id, "Realized P&L", StringComparison.OrdinalIgnoreCase))
                            {
                                if (info.Value is double cv) data.ClosedPnL += cv;
                            }

                            // Drawdown
                            if (string.Equals(id, "Drawdown", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(id, "MaxDrawdown", StringComparison.OrdinalIgnoreCase))
                            {
                                if (info.Value is double dd) data.Drawdown += dd;
                            }
                        }
                    }
                }

                // Calculate totals
                var totalData = new TypeSummaryData();
                foreach (var kvp in typeData)
                {
                    totalData.Count += kvp.Value.Count;
                    totalData.Equity += kvp.Value.Equity;
                    totalData.OpenPnL += kvp.Value.OpenPnL;
                    totalData.ClosedPnL += kvp.Value.ClosedPnL;
                    totalData.Drawdown += kvp.Value.Drawdown;
                }

                // Add rows for each type
                foreach (var kvp in typeData.OrderBy(x => x.Key))
                {
                    var type = kvp.Key;
                    var data = kvp.Value;
                    var totalPnL = data.OpenPnL + data.ClosedPnL;

                    typeSummaryGrid.Rows.Add(
                        type,
                        data.Count.ToString(),
                        data.Equity.ToString("N2"),
                        data.OpenPnL.ToString("N2"),
                        data.ClosedPnL.ToString("N2"),
                        totalPnL.ToString("N2"),
                        data.Drawdown.ToString("N2")
                    );
                }

                // Add total row
                var totalTotalPnL = totalData.OpenPnL + totalData.ClosedPnL;
                typeSummaryGrid.Rows.Add(
                    "Total",
                    totalData.Count.ToString(),
                    totalData.Equity.ToString("N2"),
                    totalData.OpenPnL.ToString("N2"),
                    totalData.ClosedPnL.ToString("N2"),
                    totalTotalPnL.ToString("N2"),
                    totalData.Drawdown.ToString("N2")
                );
            }
            catch
            {
                // ignore refresh errors
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
            public double Drawdown { get; set; }
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
                    { "Daily Net", $"${dailyNet:N2}" },
                    { "Daily Profit", $"${dailyProfit:N2}" },
                    { "Daily Loss", $"${dailyLoss:N2}" },
                    { "Weekly Net", $"${weeklyNet:N2}" },
                    { "Weekly Profit", $"${weeklyProfit:N2}" },
                    { "Weekly Loss", $"${weeklyLoss:N2}" },
                    { "Drawdown", $"${drawdown:N2}" }
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
                Text = "Manually lock or unlock trading.",
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

            var lockButton = new Button
            {
                Text = "LOCK TRADING",
                Width = 200,
                Height = 40,
                Left = 0,
                Top = 0,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = AccentAmber,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            lockButton.FlatAppearance.BorderSize = 0;
            contentArea.Controls.Add(lockButton);

            var unlockButton = new Button
            {
                Text = "UNLOCK TRADING",
                Width = 200,
                Height = 40,
                Left = 220,
                Top = 0,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = AccentGreen,
                ForeColor = TextWhite,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            unlockButton.FlatAppearance.BorderSize = 0;
            contentArea.Controls.Add(unlockButton);

            // Add controls in correct order: Fill first, then Top (no Bottom for this panel)
            // In WinForms, docking is processed in reverse Z-order
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(titleLabel);

            return mainPanel;
        }

        private void EmergencyFlattenButton_Click(object sender, EventArgs e)
        {
            FlattenAllTrades();
            MessageBox.Show("Emergency Flatten Triggered!", "Emergency Flatten", MessageBoxButtons.OK, MessageBoxIcon.Information);
            PlayAlertSound();
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
                    using (var soundPlayer = new SoundPlayer(audioStream))
                    {
                        // Use PlaySync to ensure sound completes before disposal
                        soundPlayer.PlaySync();
                    }
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
            // Placeholder for logic implementation
            // TODO: Implement trade flattening logic
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
                    // Get account number for saving
                    var accountNumber = selectedAccount?.Id ?? selectedAccount?.Name ?? "DEFAULT";
                    
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
                            service.UpdateDailyLossLimit(accountNumber, lossLimit);
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
                            service.UpdateDailyProfitTarget(accountNumber, profitTarget);
                        }
                    }
                    else
                    {
                        service.UpdateDailyProfitTarget(accountNumber, null);
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
                            service.UpdateDefaultContractLimit(accountNumber, defaultLimit);
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
                            if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out var limit))
                            {
                                limits[parts[0].Trim()] = limit;
                            }
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

            foreach (var feature in features)
            {
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
            var lossSection = CreatePositionSection("Position Loss Limit", "USD per position:", 10);
            contentArea.Controls.Add(lossSection);

            // Position Profit Target section
            var profitSection = CreatePositionSection("Position Profit Target", "USD per position:", 120);
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
        private Panel CreatePositionSection(string sectionTitle, string inputLabel, int topPosition)
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
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = TextWhite,
                BackColor = CardBackground
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
                Text = "10"
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
                Text = "1000",
                Font = new Font("Segoe UI", 9),
                BackColor = CardBackground,
                ForeColor = TextWhite,
                BorderStyle = BorderStyle.FixedSingle
            };
            contentArea.Controls.Add(usdInput);

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
                Text = "2000",
                Font = new Font("Segoe UI", 9),
                BackColor = CardBackground,
                ForeColor = TextWhite,
                BorderStyle = BorderStyle.FixedSingle
            };
            contentArea.Controls.Add(usdInput);

            var saveButton = CreateDarkSaveButton();

            // Add controls in correct order: Bottom first, Fill second, Top last
            mainPanel.Controls.Add(saveButton);
            mainPanel.Controls.Add(contentArea);
            mainPanel.Controls.Add(subtitleLabel);
            mainPanel.Controls.Add(titleLabel);

            return mainPanel;
        }
    }
}