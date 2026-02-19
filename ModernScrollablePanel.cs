using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

/// <summary>
/// A Panel subclass that replaces the native Win32 scrollbar with a modern,
/// macOS-style thin scrollbar. Features:
///   - Thin (6 px) thumb that widens to 10 px on hover
///   - Fully rounded (pill-shaped) thumb
///   - Smooth fade-in on scroll / mouse-enter, fade-out after idle
///   - Dark-theme compatible semi-transparent gray
/// Drop-in replacement for Panel anywhere AutoScroll = true is needed.
/// </summary>
public class ModernScrollablePanel : Panel
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

    // ── Appearance ─────────────────────────────────────────────────────────
    private const int ThumbNormalWidth = 6;
    private const int ThumbHoverWidth  = 10;
    private const int ThumbMargin      = 3;   // gap from right edge
    private const int ThumbMinHeight   = 28;
    private const int ThumbRadius      = 3;

    private static readonly Color ThumbNormal = Color.FromArgb(110, 160, 160, 160);
    private static readonly Color ThumbHover  = Color.FromArgb(190, 180, 180, 180);
    private static readonly Color ThumbDrag   = Color.FromArgb(220, 200, 200, 200);

    // ── State ──────────────────────────────────────────────────────────────
    private float _alpha;          // 0 = invisible, 1 = fully visible
    private bool  _hovered;        // mouse over the overlay
    private bool  _dragging;
    private int   _dragStartY;
    private int   _dragStartScrollY;

    // ── Child controls / timers ────────────────────────────────────────────
    private readonly ScrollbarOverlay _overlay;   // transparent panel that paints the thumb
    private readonly Timer _hideTimer; // idle → begin fade-out
    private readonly Timer _fadeTimer; // animate opacity down

    // ── Nested transparent overlay panel ──────────────────────────────────
    private sealed class ScrollbarOverlay : Panel
    {
        public ScrollbarOverlay()
        {
            // Enable truly transparent background and flicker-free painting
            SetStyle(
                ControlStyles.SupportsTransparentBackColor |
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.AllPaintingInWmPaint,
                true);
            BackColor = Color.Transparent;
        }
    }

    // ──────────────────────────────────────────────────────────────────────
    public ModernScrollablePanel()
    {
        AutoScroll = true;
        // Enable flicker-free, double-buffered painting for this panel
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

        // ── overlay ──
        _overlay = new ScrollbarOverlay();
        _overlay.Paint      += OnOverlayPaint;
        _overlay.MouseEnter += OnOverlayMouseEnter;
        _overlay.MouseLeave += OnOverlayMouseLeave;
        _overlay.MouseDown  += OnOverlayMouseDown;
        _overlay.MouseMove  += OnOverlayMouseMove;
        _overlay.MouseUp    += OnOverlayMouseUp;
        Controls.Add(_overlay);

        // ── timers ──
        _hideTimer = new Timer { Interval = 1500 };
        _hideTimer.Tick += (_, __) => { _hideTimer.Stop(); _fadeTimer.Start(); };

        _fadeTimer = new Timer { Interval = 16 };  // ~60 fps
        _fadeTimer.Tick += (_, __) =>
        {
            _alpha = Math.Max(0f, _alpha - 0.07f);
            _overlay.Invalidate();
            if (_alpha <= 0f) _fadeTimer.Stop();
        };

        // ── hook scroll / wheel events ──
        Scroll += (_, __) => ShowThumb();
    }

    // ── Override WM_NCCALCSIZE to "eat" the native scrollbars ────────────
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NCCALCSIZE && m.WParam.ToInt32() == 1)
        {
            base.WndProc(ref m);
            // Expand the client rect to absorb both native scrollbars,
            // making them invisible while keeping AutoScroll functionality.
            var p = (NCCALCSIZE_PARAMS)Marshal.PtrToStructure(
                        m.LParam, typeof(NCCALCSIZE_PARAMS));
            p.rgrc0.Right  += SystemInformation.VerticalScrollBarWidth;
            p.rgrc0.Bottom += SystemInformation.HorizontalScrollBarHeight;
            Marshal.StructureToPtr(p, m.LParam, false);
            m.Result = IntPtr.Zero;
            return;
        }
        base.WndProc(ref m);
    }

    // ── Mouse-wheel: show thumb then start the idle timer ──────────────────
    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        ShowThumb();
        _overlay.Invalidate();
    }

    // ── Layout: keep the overlay flush to the right edge ───────────────────
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        PositionOverlay();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        PositionOverlay();
    }

    // Keep overlay on top when new controls are added
    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control != _overlay) _overlay?.BringToFront();
    }

    private void PositionOverlay()
    {
        if (_overlay == null) return;
        int w = ThumbHoverWidth + ThumbMargin * 2;
        _overlay.SetBounds(ClientSize.Width - w, 0, w, ClientSize.Height);
        _overlay.BringToFront();
    }

    // ── Scrollbar painting ──────────────────────────────────────────────────
    private void OnOverlayPaint(object sender, PaintEventArgs e)
    {
        if (_alpha <= 0f) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int trackH = _overlay.Height;
        var (thumbY, thumbH) = ComputeThumb(trackH);
        if (thumbH >= trackH) return;   // content fits; nothing to show

        int w     = _hovered ? ThumbHoverWidth : ThumbNormalWidth;
        int x     = _overlay.Width - w - ThumbMargin;
        int alpha = (int)(_alpha * 255);

        Color baseColor = _dragging ? ThumbDrag : _hovered ? ThumbHover : ThumbNormal;
        var   color     = Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);

        using var brush = new SolidBrush(color);
        using var path  = MakeRoundedRect(new Rectangle(x, thumbY, w, thumbH), ThumbRadius);
        g.FillPath(brush, path);
    }

    private (int thumbY, int thumbH) ComputeThumb(int trackH)
    {
        int contentH = DisplayRectangle.Height;
        int clientH  = ClientSize.Height;
        if (contentH <= clientH) return (0, trackH);

        float ratio  = (float)clientH / contentH;
        int   thumbH = Math.Max(ThumbMinHeight, (int)(trackH * ratio));

        int   scrollY        = Math.Abs(AutoScrollPosition.Y);
        int   maxScroll      = contentH - clientH;
        float scrollFraction = maxScroll > 0 ? (float)scrollY / maxScroll : 0f;
        int   thumbY         = (int)((trackH - thumbH) * scrollFraction);

        return (thumbY, thumbH);
    }

    private static GraphicsPath MakeRoundedRect(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(r.X,          r.Y,           d, d, 180, 90);
        path.AddArc(r.Right - d,  r.Y,           d, d, 270, 90);
        path.AddArc(r.Right - d,  r.Bottom - d,  d, d,   0, 90);
        path.AddArc(r.X,          r.Bottom - d,  d, d,  90, 90);
        path.CloseFigure();
        return path;
    }

    // ── Overlay mouse events ────────────────────────────────────────────────
    private void OnOverlayMouseEnter(object sender, EventArgs e)
    {
        _hovered = true;
        _hideTimer.Stop();
        _fadeTimer.Stop();
        _alpha = 1f;
        _overlay.Invalidate();
    }

    private void OnOverlayMouseLeave(object sender, EventArgs e)
    {
        _hovered = false;
        _overlay.Invalidate();
        StartHideTimer();
    }

    private void OnOverlayMouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        var (thumbY, thumbH) = ComputeThumb(_overlay.Height);
        if (e.Y < thumbY || e.Y > thumbY + thumbH) return;

        _dragging         = true;
        _dragStartY       = e.Y;
        _dragStartScrollY = Math.Abs(AutoScrollPosition.Y);
        _overlay.Capture  = true;
        _overlay.Invalidate();
    }

    private void OnOverlayMouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;

        int contentH = DisplayRectangle.Height;
        int clientH  = ClientSize.Height;
        int trackH   = _overlay.Height;
        var (_, thumbH) = ComputeThumb(trackH);
        int thumbRange   = trackH - thumbH;
        if (thumbRange <= 0) return;

        int scrollRange = contentH - clientH;
        int delta       = e.Y - _dragStartY;
        int newScrollY  = Math.Max(0, Math.Min(scrollRange,
                              _dragStartScrollY + (int)((float)delta * scrollRange / thumbRange)));

        AutoScrollPosition = new Point(0, newScrollY);
        _overlay.Invalidate();
    }

    private void OnOverlayMouseUp(object sender, MouseEventArgs e)
    {
        _dragging        = false;
        _overlay.Capture = false;
        _overlay.Invalidate();
        StartHideTimer();
    }

    // ── Fade helpers ────────────────────────────────────────────────────────
    private void ShowThumb()
    {
        _fadeTimer.Stop();
        _alpha = 1f;
        _overlay.Invalidate();
        StartHideTimer();
    }

    private void StartHideTimer()
    {
        if (!_hovered && !_dragging)
        {
            _hideTimer.Stop();
            _hideTimer.Start();
        }
    }

    // ── Cleanup ─────────────────────────────────────────────────────────────
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _hideTimer?.Dispose();
            _fadeTimer?.Dispose();
        }
        base.Dispose(disposing);
    }
}
