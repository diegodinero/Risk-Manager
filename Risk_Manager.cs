// Copyright QUANTOWER LLC. © 2017-2023. All rights reserved.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TradingPlatform.BusinessLayer;
using TradingPlatform.PresentationLayer.Plugins;
using TradingPlatform.PresentationLayer.Renderers.Table;

namespace Risk_Manager
{
    public class Risk_Manager : TablePlugin
    {
        public static PluginInfo GetInfo()
        {
            return new PluginInfo
            {
                Name = "RiskManager.Panel",
                Title = loc.key("Risk Manager UI"),
                Group = PluginGroup.Portfolio,
                ShortName = "RMMUI",
                SortIndex = 34,
                WindowParameters = new NativeWindowParameters(NativeWindowParameters.Panel)
                {
                    BrowserUsageType = BrowserUsageType.None,
                    WindowStyle = NativeWindowStyle.SingleBorderWindow,
                    ResizeMode = ResizeMode.CanResizeWithGrip,
                    ShowInTaskbar = true,
                    AllowMaximize = true,
                    AllowMinimize = true
                },
                CustomProperties = new Dictionary<string, object>
                {
                    { PluginInfo.Const.ALLOW_MANUAL_CREATION, true }
                }
            };
        }

        public override Size DefaultSize => new Size(this.UnitSize.Width * 4, this.UnitSize.Height * 2);
        protected override TableItem AssociatedTableItem => new RiskManagerTableItem();

        public override void Initialize()
        {
            base.Initialize();
            this.AllowDataExport = true;
            this.table.RowsLimit = 1000;
            this.table.AllowFilter = true;
            this.table.AllowSorting = true;
            this.table.AllowHeaderContextMenu = true;
        }

        public override void Populate(PluginParameters args = null)
        {
            base.Populate(args);

            try
            {
                var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                var log = System.IO.Path.Combine(desktop, "RiskManager_attach_log.txt");

                System.IO.File.AppendAllText(log, $"Populate called at {DateTime.Now:O}{Environment.NewLine}");
                System.Diagnostics.Debug.WriteLine($"RiskManager: Populate called at {DateTime.Now:O}");
                System.Diagnostics.Trace.WriteLine($"RiskManager: Populate called at {DateTime.Now:O}");

                // Show an unobtrusive MessageBox for immediate feedback (hosts may suppress it)
                try
                {
                    MessageBox.Show("Risk_Manager.Populate executed. Check your Desktop for RiskManager_attach_log.txt", "Risk Manager - Debug", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch
                {
                    // ignore if host prevents UI from plugins
                }

                // Keep minimal attach attempt so behavior is unchanged (optional)
                // var control = new RiskManagerControl { Dock = System.Windows.Forms.DockStyle.Fill };
                // ... attach logic omitted for brevity
            }
            catch (Exception ex)
            {
                try
                {
                    var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                    var log = System.IO.Path.Combine(desktop, "RiskManager_attach_log.txt");
                    System.IO.File.AppendAllText(log, $"Populate exception: {ex}{Environment.NewLine}");
                    System.Diagnostics.Debug.WriteLine("RiskManager: Populate exception: " + ex);
                }
                catch { /* give up if writing fails */ }
            }

            PerformFullUpdate();
        }

        public override void OnAsyncTimer()
        {
            base.OnAsyncTimer();
            UpdateAccountValues();
        }

        private void PerformFullUpdate()
        {
            if (this.table == null)
                return;

            this.table.ClearAll();

            var core = Core.Instance;
            if (core == null || core.Accounts == null)
                return;

            foreach (var account in core.Accounts)
            {
                if (account == null || account.Connection == null)
                    continue;

                var item = new RiskManagerTableItem();
                item.Account = account;
                this.table.AddItem(item);
            }
        }

        private void UpdateAccountValues()
        {
            if (this.table == null)
                return;

            try
            {
                this.table.SuspendDrawing = true;
                this.table.UpdateAllItems(false, null);
            }
            finally
            {
                this.table.SuspendDrawing = false;
            }
        }
    }

    public class RiskManagerTableItem : TableItem
    {
        public Account Account { get; set; }

        public override List<TableColumnDefinition> ColumnsDefinition => new()
        {
            new TableColumnDefinition(loc.key("Provider"), TableComparingType.String, 100, true, false),
            new TableColumnDefinition(loc.key("Connection"), TableComparingType.String, 120, true, false),
            new TableColumnDefinition(loc.key("Account"), TableComparingType.String, 150, true, false),
            new TableColumnDefinition(loc.key("Balance"), TableComparingType.Double, 100, true, false),
            new TableColumnDefinition(loc.key("Open P&L"), TableComparingType.Double, 90, true, false),
            new TableColumnDefinition(loc.key("Daily P&L"), TableComparingType.Double, 90, true, false),
            new TableColumnDefinition(loc.key("Gross P&L"), TableComparingType.Double, 90, true, false),
            new TableColumnDefinition(loc.key("Positions"), TableComparingType.Int, 70, true, false),
            new TableColumnDefinition(loc.key("Status"), TableComparingType.String, 80, true, false)
        };

        public override ValueTuple<object, string> GetCellValue(int columnIndex, bool RequireFormattedValue = true)
        {
            if (Account == null)
                return new ValueTuple<object, string>(null, "");

            var core = Core.Instance;
            var provider = Account.Connection?.VendorName ?? Account.Connection?.Name ?? "Unknown";
            var connectionName = Account.Connection?.Name ?? "Unknown";
            var accountId = Account.Id ?? Account.Name ?? "Unknown";

            // Compute Open P&L from positions
            double openPnL = 0;
            int positionsCount = 0;
            if (core?.Positions != null)
            {
                foreach (var pos in core.Positions)
                {
                    if (pos?.Account == Account && pos.Quantity != 0)
                    {
                        var pnlItem = pos.NetPnL ?? pos.GrossPnL;
                        if (pnlItem != null)
                            openPnL += pnlItem.Value;
                        positionsCount++;
                    }
                }
            }

            // Get Daily/Gross P&L from AdditionalInfo
            double dailyPnL = 0, grossPnL = 0;
            if (Account.AdditionalInfo?.Any() == true)
            {
                var dailyItem = Account.AdditionalInfo.FirstOrDefault(x =>
                    x.Id.Equals("Daily Net P&L", StringComparison.OrdinalIgnoreCase) ||
                    x.Id.Equals("TotalPnL", StringComparison.OrdinalIgnoreCase));
                if (dailyItem?.Value is double dv) dailyPnL = dv;

                var grossItem = Account.AdditionalInfo.FirstOrDefault(x =>
                    x.Id.Equals("Gross P&L", StringComparison.OrdinalIgnoreCase) ||
                    x.Id.Equals("GrossPnL", StringComparison.OrdinalIgnoreCase));
                if (grossItem?.Value is double gv) grossPnL = gv;
            }

            object value = null;
            string formatted = null;

            switch (ColumnsDefinition[columnIndex].HeaderText)
            {
                case var x when x == loc.key("Provider"):
                    value = provider;
                    break;
                case var x when x == loc.key("Connection"):
                    value = connectionName;
                    break;
                case var x when x == loc.key("Account"):
                    value = accountId;
                    break;
                case var x when x == loc.key("Balance"):
                    value = Account.Balance;
                    formatted = Account.Balance.ToString("N2");
                    break;
                case var x when x == loc.key("Open P&L"):
                    value = openPnL;
                    formatted = openPnL.ToString("N2");
                    break;
                case var x when x == loc.key("Daily P&L"):
                    value = dailyPnL;
                    formatted = dailyPnL.ToString("N2");
                    break;
                case var x when x == loc.key("Gross P&L"):
                    value = grossPnL;
                    formatted = grossPnL.ToString("N2");
                    break;
                case var x when x == loc.key("Positions"):
                    value = positionsCount;
                    formatted = positionsCount.ToString();
                    break;
                case var x when x == loc.key("Status"):
                    value = Account.Connection?.State.ToString() ?? "Unknown";
                    break;
            }

            return new ValueTuple<object, string>(value, formatted ?? value?.ToString() ?? "");
        }

        public override Symbol CurrentSymbol => null;
    }
}
