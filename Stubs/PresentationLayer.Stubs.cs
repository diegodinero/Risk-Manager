// Stub implementations of TradingPlatform.PresentationLayer types.
// Used only when the real Quantower DLLs are not present (e.g., CI/sandbox builds).

using System;
using System.Collections.Generic;
using System.Drawing;
using TradingPlatform.BusinessLayer;

namespace TradingPlatform.PresentationLayer.Plugins
{
    // ── Enums ────────────────────────────────────────────────────────────────
    public enum PluginGroup { None, Portfolio, Trading, Analysis }
    public enum BrowserUsageType { None, Full }
    public enum NativeWindowStyle { None, SingleBorderWindow, ToolWindow }
    public enum NativeResizeMode { NoResize, CanResize, CanResizeWithGrip }

    // ── NativeWindowParameters ───────────────────────────────────────────────
    public class NativeWindowParameters
    {
        /// <summary>Sentinel value used as a constructor argument to request a Panel-style host.</summary>
        public static readonly NativeWindowParameters Panel = new NativeWindowParameters();

        public NativeWindowParameters() { }
        public NativeWindowParameters(NativeWindowParameters template) { }

        public BrowserUsageType BrowserUsageType { get; set; }
        public NativeWindowStyle WindowStyle { get; set; }
        public NativeResizeMode ResizeMode { get; set; }
        public bool AllowActionsButton { get; set; }
        public bool AllowCloseButton { get; set; }
        public bool AllowMaximizeButton { get; set; }
        public bool AllowFullScreenButton { get; set; }
        public bool ShowInTaskbar { get; set; }
    }

    // ── PluginInfo ───────────────────────────────────────────────────────────
    public class PluginInfo
    {
        public string? Name { get; set; }
        public string? Title { get; set; }
        public PluginGroup Group { get; set; }
        public string? ShortName { get; set; }
        public int SortIndex { get; set; }
        public NativeWindowParameters? WindowParameters { get; set; }
        public Dictionary<string, object>? CustomProperties { get; set; }

        public static class Const
        {
            public const string ALLOW_MANUAL_CREATION = "AllowManualCreation";
        }
    }

    // ── PluginParameters ─────────────────────────────────────────────────────
    public class PluginParameters { }

    // ── Plugin (base class) ──────────────────────────────────────────────────
    public abstract class Plugin
    {
        public virtual void Initialize() { }
        public virtual void Populate(PluginParameters? args = null) { }
    }

    // ── TableCollection (the 'table' field on TablePlugin) ───────────────────
    public class TableCollection
    {
        public int RowsLimit { get; set; }
        public bool AllowFilter { get; set; }
        public bool AllowSorting { get; set; }
        public bool AllowHeaderContextMenu { get; set; }
        public bool SuspendDrawing { get; set; }

        public void ClearAll() { }
        public void AddItem(object item) { }
        public void UpdateAllItems(bool flag, object? context) { }
    }

    // ── TablePlugin (base class for table-style plugins) ─────────────────────
    public abstract class TablePlugin : Plugin
    {
        protected readonly TableCollection table = new TableCollection();
        public bool AllowDataExport { get; set; }
        public virtual Size DefaultSize => new Size(400, 200);
        public virtual Size UnitSize => new Size(100, 50);
        protected abstract object AssociatedTableItem { get; }

        public virtual void OnAsyncTimer() { }
    }
}

// ── Renderers.Table types ─────────────────────────────────────────────────
namespace TradingPlatform.PresentationLayer.Renderers.Table
{
    using TradingPlatform.BusinessLayer;

    public class TableColumnDefinition
    {
        public string HeaderText { get; }

        public TableColumnDefinition(
            string headerText,
            TableComparingType comparingType,
            int width,
            bool isVisible,
            bool isSortable)
        {
            HeaderText = headerText;
        }
    }

    public abstract class TableItem
    {
        public abstract System.Collections.Generic.List<TableColumnDefinition> ColumnsDefinition { get; }

        public abstract (object?, string?) GetCellValue(int columnIndex, bool requireFormattedValue = true);

        public abstract Symbol? CurrentSymbol { get; }
    }
}

// ── Renderers.Chart – placeholder type so the 'using' in RiskManagerControl compiles ──
namespace TradingPlatform.PresentationLayer.Renderers.Chart
{
    /// <summary>Stub placeholder; no members are used by Risk Manager.</summary>
    internal static class _Stub { }
}
