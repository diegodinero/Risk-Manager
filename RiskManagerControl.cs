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
            BackColor = Color.LimeGreen;

            // Left panel (fixed width) instead of SplitContainer to avoid SplitterDistance issues
            leftPanel = new Panel
            {
                Dock = DockStyle.Left,
                Width = LeftPanelWidth,
                MinimumSize = new Size(LeftPanelWidth, 0),
                BackColor = SystemColors.Control
            };

            leftTabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Alignment = TabAlignment.Left,
                SizeMode = TabSizeMode.Fixed,
                ItemSize = new Size(32, 140),
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
                BackColor = Color.LimeGreen
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
                BackgroundColor = Color.LimeGreen
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

            var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.LimeGreen };
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
                    statsGrid.Rows.Add("DemoProvider", "DemoConn3", "ACC789", "500.00", "0.00", "0.00", "0.00", "0", "Disconnected");
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

        private Control CreatePlaceholderPanel(string title)
        {
            var pnl = new Panel { BackColor = Color.LimeGreen };
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

            g.TranslateTransform(rc.Left + rc.Width / 2f, rc.Top + rc.Height / 2f);
            g.RotateTransform(-90f);
            using var textBrush = new SolidBrush(SystemColors.ControlText);
            var textSize = g.MeasureString(text, leftTabControl.Font);
            g.DrawString(text, leftTabControl.Font, textBrush, -textSize.Width / 2f, -textSize.Height / 2f);
            g.ResetTransform();

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