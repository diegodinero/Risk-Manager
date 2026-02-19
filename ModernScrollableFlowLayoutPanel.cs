using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

/// <summary>
/// A FlowLayoutPanel subclass that replaces the native Win32 scrollbar with a
/// modern, macOS-style thin scrollbar. Identical behaviour to
/// <see cref="ModernScrollablePanel"/> but inherits from
/// <see cref="FlowLayoutPanel"/> so that <c>FlowDirection</c>,
/// <c>WrapContents</c> and other flow properties work as normal.
/// Drop-in replacement for FlowLayoutPanel anywhere AutoScroll = true is needed.
/// </summary>
public class ModernScrollableFlowLayoutPanel : FlowLayoutPanel
{
    // ── Win32 constants ────────────────────────────────────────────────────
    private const int WM_NCCALCSIZE = 0x0083;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct NCCALCSIZE_PARAMS
    {
        public RECT rgrc0, rgrc1, rgrc2;
        public IntPtr lppos;
    }

    private readonly ModernScrollbarCore _scrollbar;

    public ModernScrollableFlowLayoutPanel()
    {
        AutoScroll = true;
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        _scrollbar = new ModernScrollbarCore(this);
        Scroll += (_, __) => _scrollbar.ShowThumb();
    }

    // ── Override WM_NCCALCSIZE to "eat" the native vertical scrollbar ──────
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NCCALCSIZE && m.WParam.ToInt32() == 1)
        {
            base.WndProc(ref m);
            var p = (NCCALCSIZE_PARAMS)Marshal.PtrToStructure(
                        m.LParam, typeof(NCCALCSIZE_PARAMS));
            p.rgrc0.Right += SystemInformation.VerticalScrollBarWidth;
            Marshal.StructureToPtr(p, m.LParam, false);
            m.Result = IntPtr.Zero;
            return;
        }
        base.WndProc(ref m);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        _scrollbar.OnMouseWheel();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        _scrollbar.OnResize();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        _scrollbar.OnLayout();
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        _scrollbar.OnControlAdded(e.Control);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _scrollbar?.Dispose();
        base.Dispose(disposing);
    }
}
