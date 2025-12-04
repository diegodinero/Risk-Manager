using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TradingPlatform.BusinessLayer;
using TradingPlatform.PresentationLayer.Renderers.Chart;
using DockStyle = System.Windows.Forms.DockStyle;

namespace Risk_Manager
{
    public class RiskManagerControl : UserControl
    {
        private readonly TabControl leftTabControl;
        private readonly Panel contentPanel;
        private readonly Panel leftPanel;
        private readonly Dictionary<string, Control> pageContents = new();
        private DataGridView statsGrid;
        private System.Windows.Forms.Timer statsRefreshTimer;

        private static readonly string[] TabNames = new[]
        {
            "Stats", "Feature Toggles", "Block Symbols", "Allowed Trading Times",
            "Daily Loss", "Daily Profit Target", "Position Size", "Position Win",
            "Position Loss", "Weekly Loss", "Weekly Profit Target", "Lock Settings",
            "Manual Lock", "Copy Settings"
        };

        private const int LeftPanelWidth = 160;

        public RiskManagerControl()
        {
            Dock = DockStyle.Fill;
            BackColor = Color.SteelBlue;

            // Left panel (fixed width) — prevent auto-sizing
            leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = LeftPanelWidth,
                MinimumSize = new Size(LeftPanelWidth, 0),
                AutoSize = false,  // Prevent panel from resizing when tab content changes
                BackColor = SystemColors.Control
            };

            leftTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Alignment = TabAlignment.Top,
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(100, 23),
                DrawMode = TabDrawMode.OwnerDrawFixed,
                Multiline = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Regular),
                BackColor = SystemColors.Control
            };
            leftTabControl.DrawItem += LeftTabControl_DrawItem;
            leftTabControl.SelectedIndexChanged += LeftTabControl_SelectedIndexChanged;

            contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,  // Prevent content panel from resizing
                BackColor = Color.SteelBlue
            };

            // Create pages and contents
            foreach (var name in TabNames)
            {
                Control placeholder = string.Equals(name, "Stats", StringComparison.OrdinalIgnoreCase)
                    ? CreateStatsPanel()
                    : CreatePlaceholderPanel(name);

                var page = new TabPage(name) { BackColor = SystemColors.Control };
                leftTabControl.TabPages.Add(page);
                pageContents[name] = placeholder;
            }

            leftPanel.Controls.Add(leftTabControl);
            Controls.Add(contentPanel);
            Controls.Add(leftPanel);

            // Show first page
            if (TabNames.Length > 0)
                ShowPage(TabNames[0]);
        }

        private Control CreateStatsPanel()
        {
            statsGrid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                BackgroundColor = Color.SteelBlue
            };

            statsGrid.Columns.Add("Provider", "Provider");
            statsGrid.Columns.Add("Connection", "Connection");
            statsGrid.Columns.Add("Account", "Account");
            statsGrid.Columns.Add("Balance", "Balance");
            statsGrid.Columns.Add("OpenPnL", "Open P&L");
            statsGrid.Columns.Add("DailyPnL", "Daily P&L");
            statsGrid.Columns.Add("GrossPnL", "Gross P&L");
            statsGrid.Columns.Add("Positions", "Positions");
            statsGrid.Columns.Add("Status", "Status");

            RefreshStats();
            statsRefreshTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            statsRefreshTimer.Tick += (s, e) => RefreshStats();
            statsRefreshTimer.Start();

            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.SteelBlue };
            panel.Controls.Add(statsGrid);
            return panel;
        }

        private void RefreshStats()
        {
            if (InvokeRequired) { BeginInvoke(new Action(RefreshStats)); return; }
            if (statsGrid == null) return;

            try
            {
                statsGrid.SuspendLayout();
                statsGrid.Rows.Clear();

                var core = Core.Instance;
                if (core == null || core.Accounts == null || !core.Accounts.Any())
                {
                    statsGrid.Rows.Add("DemoProvider", "DemoConn", "ACC123", "1000.00", "12.34", "5.67", "18.01", "1", "Connected");
                    statsGrid.Rows.Add("DemoProvider", "DemoConn2", "ACC456", "2500.50", "-8.20", "-2.00", "-10.20", "2", "Connected");
                    statsGrid.Rows.Add("DemoProvider", "DemoConn3", "ACC789", "500.00", "0.00", "0.00", "0", "Disconnected");
                    return;
                }

                foreach (var account in core.Accounts)
                {
                    if (account == null) continue;

                    var provider = account.Connection?.VendorName ?? account.Connection?.Name ?? "Unknown";
                    var connectionName = account.Connection?.Name ?? "Unknown";
                    var accountId = account.Id ?? account.Name ?? "Unknown";
                    var balance = account.Balance;

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

                    double dailyPnL = 0, grossPnL = 0;
                    // AdditionalInfo collection may not support LINQ; iterate safely
                    if (account.AdditionalInfo != null)
                    {
                        foreach (var info in account.AdditionalInfo)
                        {
                            if (info?.Id == null) continue;
                            var id = info.Id;
                            if (string.Equals(id, "Daily Net P&L", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(id, "TotalPnL", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(id, "DailyPnL", StringComparison.OrdinalIgnoreCase))
                            {
                                if (info.Value is double dv) dailyPnL = dv;
                            }

                            if (string.Equals(id, "Gross P&L", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(id, "GrossPnL", StringComparison.OrdinalIgnoreCase) ||
                                string.Equals(id, "Total P&L", StringComparison.OrdinalIgnoreCase))
                            {
                                if (info.Value is double gv) grossPnL = gv;
                            }
                        }
                    }

                    var status = account.Connection == null ? "Disconnected" : account.Connection.State.ToString();
                    statsGrid.Rows.Add(provider, connectionName, accountId, balance.ToString("N2"), openPnL.ToString("N2"), dailyPnL.ToString("N2"), grossPnL.ToString("N2"), positionsCount.ToString(), status);
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

        private Control CreateFeatureTogglesPanel()
        {
            var mainPanel = new Panel { BackColor = Color.SteelBlue, Dock = DockStyle.Fill };

            // Title
            var titleLabel = new Label
            {
                Text = "Feature Toggles",
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                Padding = new Padding(8, 0, 0, 0),
                BackColor = Color.SteelBlue
            };
            mainPanel.Controls.Add(titleLabel);

            // Subtitle
            var subtitleLabel = new Label
            {
                Text = "Enable/disable features without expanding the page. Scroll if there are many toggles.",
                Dock = DockStyle.Top,
                Height = 40,
                TextAlign = ContentAlignment.TopLeft,
                Font = new Font("Segoe UI", 9, FontStyle.Regular),
                Padding = new Padding(8, 4, 8, 4),
                BackColor = Color.SteelBlue,
                AutoSize = false    
            };
            mainPanel.Controls.Add(subtitleLabel);

            // Scrollable panel for checkboxes
            var scrollPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BackColor = SystemColors.Window,
                Padding = new Padding(8)
            };
            mainPanel.Controls.Add(scrollPanel);

            // Feature list (in order)
            var features = new[]
            {
                "Enable All Features",
                "Block Symbols",
                "Allowed Sessions",
                "Daily Loss",
                "Daily Profit",
                "Position Size",
                "Position Win",
                "Position Loss",
                "Weekly Loss",
                "Weekly Profit"
            };

            var checkboxes = new Dictionary<string, CheckBox>();
            int y = 0;

            foreach (var feature in features)
            {
                var checkbox = new CheckBox
                {
                    Text = feature,
                    Left = 0,
                    Top = y,
                    Width = scrollPanel.Width - 20, // Account for scrollbar
                    Height = 24,
                    Checked = true, // Default all to checked
                    Font = new Font("Segoe UI", 9, FontStyle.Regular),
                    BackColor = SystemColors.Window
                };
                checkbox.AutoSize = false;
                scrollPanel.Controls.Add(checkbox);
                checkboxes[feature] = checkbox;
                y += 28;
            }

            // Save Settings button at bottom
            var saveButton = new Button
            {
                Text = "SAVE SETTINGS",
                Dock = DockStyle.Bottom,
                Height = 36,
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = SystemColors.Control,
                Cursor = Cursors.Hand
            };
            saveButton.Click += (s, e) =>
            {
                // Collect all toggle states
                var settings = new Dictionary<string, bool>();
                foreach (var feature in features)
                {
                    settings[feature] = checkboxes[feature].Checked;
                }

                // Log or save settings (example: write to file or plugin storage)
                try
                {
                    var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    var settingsPath = System.IO.Path.Combine(desktop, "FeatureToggles_Settings.txt");
                    var lines = new List<string>();
                    foreach (var kvp in settings)
                    {
                        lines.Add($"{kvp.Key}={kvp.Value}");
                    }
                    System.IO.File.WriteAllLines(settingsPath, lines);
                    MessageBox.Show("Feature toggles saved successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to save settings: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            mainPanel.Controls.Add(saveButton);

            return mainPanel;
        }

        private Control CreatePlaceholderPanel(string title)
        {
            // Handle Feature Toggles specially
            if (string.Equals(title, "Feature Toggles", StringComparison.OrdinalIgnoreCase))
            {
                return CreateFeatureTogglesPanel();
            }

            // Default placeholder for other tabs
            var pnl = new Panel { BackColor = Color.SteelBlue };
            var lbl = new Label
            {
                Text = $"{title} (placeholder)",
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                Padding = new Padding(8, 0, 0, 0)
            };
            pnl.Controls.Add(lbl);

            var flow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(8)
            };
            pnl.Controls.Add(flow);
            return pnl;
        }

        private void LeftTabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (leftTabControl.SelectedIndex >= 0 && leftTabControl.SelectedIndex < leftTabControl.TabPages.Count)
                ShowPage(leftTabControl.TabPages[leftTabControl.SelectedIndex].Text);
        }

        private void ShowPage(string name)
        {
            contentPanel.Controls.Clear();
            if (pageContents.TryGetValue(name, out var ctrl))
            {
                ctrl.Dock = DockStyle.Fill;
                contentPanel.Controls.Add(ctrl);
            }
            else
            {
                var lbl = new Label { Text = name, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 12, FontStyle.Bold) };
                contentPanel.Controls.Add(lbl);
            }
        }

        private void LeftTabControl_DrawItem(object sender, DrawItemEventArgs e)
        {
            var g = e.Graphics;
            var rc = e.Bounds;
            var text = leftTabControl.TabPages[e.Index].Text;
            var isSelected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            
            using var backBrush = new SolidBrush(isSelected ? SystemColors.ControlLight : leftTabControl.BackColor);
            g.FillRectangle(backBrush, rc);

            // No rotation — draw text normally for horizontal tabs
            using var textBrush = new SolidBrush(SystemColors.ControlText);
            var textSize = g.MeasureString(text, leftTabControl.Font);
            var x = rc.Left + (rc.Width - textSize.Width) / 2;
            var y = rc.Top + (rc.Height - textSize.Height) / 2;
            g.DrawString(text, leftTabControl.Font, textBrush, x, y);

            if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
                ControlPaint.DrawFocusRectangle(g, rc);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                statsRefreshTimer?.Stop();
                statsRefreshTimer?.Dispose();
                statsRefreshTimer = null;
            }
            base.Dispose(disposing);
        }
    }
}