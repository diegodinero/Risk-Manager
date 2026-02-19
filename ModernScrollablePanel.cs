using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

/// <summary>
/// A Panel subclass that replaces both native Win32 scrollbars with modern,
/// macOS-style thin scrollbars. Features:
///   - Separate vertical (right edge) and horizontal (bottom edge) overlays
///   - Thin (6 px) thumbs that widen to 10 px on hover
///   - Fully rounded (pill-shaped) thumbs
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
    private const int ThumbMargin      = 3;    // gap from edge
    private const int ThumbMinLength   = 28;   // minimum thumb length (both axes)
    private const int ThumbRadius      = 3;

    private static readonly Color ThumbNormal = Color.FromArgb(110, 160, 160, 160);
    private static readonly Color ThumbHover  = Color.FromArgb(190, 180, 180, 180);
    private static readonly Color ThumbDrag   = Color.FromArgb(220, 200, 200, 200);

    // ── Vertical scrollbar state ───────────────────────────────────────────
    private float _vAlpha;
    private bool  _vHovered;
    private bool  _vDragging;
    private int   _vDragStartY;
    private int   _vDragStartScrollY;

    // ── Horizontal scrollbar state ─────────────────────────────────────────
    private float _hAlpha;
    private bool  _hHovered;
    private bool  _hDragging;
    private int   _hDragStartX;
    private int   _hDragStartScrollX;

    // ── Child controls / timers ────────────────────────────────────────────
    private readonly ScrollbarOverlay _vOverlay;  // vertical scrollbar (right edge)
    private readonly ScrollbarOverlay _hOverlay;  // horizontal scrollbar (bottom edge)
    private readonly Timer _hideTimer;
    private readonly Timer _fadeTimer;

    // ── Nested transparent overlay ─────────────────────────────────────────
    private sealed class ScrollbarOverlay : Panel
    {
        public ScrollbarOverlay()
        {
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
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);

        // ── vertical overlay ──
        _vOverlay = new ScrollbarOverlay();
        _vOverlay.Paint      += OnVOverlayPaint;
        _vOverlay.MouseEnter += OnVOverlayMouseEnter;
        _vOverlay.MouseLeave += OnVOverlayMouseLeave;
        _vOverlay.MouseDown  += OnVOverlayMouseDown;
        _vOverlay.MouseMove  += OnVOverlayMouseMove;
        _vOverlay.MouseUp    += OnVOverlayMouseUp;
        Controls.Add(_vOverlay);

        // ── horizontal overlay ──
        _hOverlay = new ScrollbarOverlay();
        _hOverlay.Paint      += OnHOverlayPaint;
        _hOverlay.MouseEnter += OnHOverlayMouseEnter;
        _hOverlay.MouseLeave += OnHOverlayMouseLeave;
        _hOverlay.MouseDown  += OnHOverlayMouseDown;
        _hOverlay.MouseMove  += OnHOverlayMouseMove;
        _hOverlay.MouseUp    += OnHOverlayMouseUp;
        Controls.Add(_hOverlay);

        // ── shared timers ──
        _hideTimer = new Timer { Interval = 1500 };
        _hideTimer.Tick += (_, __) => { _hideTimer.Stop(); _fadeTimer.Start(); };

        _fadeTimer = new Timer { Interval = 16 };  // ~60 fps
        _fadeTimer.Tick += (_, __) =>
        {
            _vAlpha = Math.Max(0f, _vAlpha - 0.07f);
            _hAlpha = Math.Max(0f, _hAlpha - 0.07f);
            _vOverlay.Invalidate();
            _hOverlay.Invalidate();
            if (_vAlpha <= 0f && _hAlpha <= 0f) _fadeTimer.Stop();
        };

        Scroll += (_, __) => ShowThumbs();
    }

    // ── Override WM_NCCALCSIZE to "eat" both native scrollbars ────────────
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_NCCALCSIZE && m.WParam.ToInt32() == 1)
        {
            base.WndProc(ref m);
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

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        ShowThumbs();
        _vOverlay.Invalidate();
        _hOverlay.Invalidate();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        PositionOverlays();
    }

    protected override void OnLayout(LayoutEventArgs levent)
    {
        base.OnLayout(levent);
        PositionOverlays();
    }

    protected override void OnControlAdded(ControlEventArgs e)
    {
        base.OnControlAdded(e);
        if (e.Control != _vOverlay && e.Control != _hOverlay)
        {
            _hOverlay?.BringToFront();
            _vOverlay?.BringToFront();
        }
    }

    private void PositionOverlays()
    {
        if (_vOverlay == null || _hOverlay == null) return;
        int ow = ThumbHoverWidth + ThumbMargin * 2;  // overlay thickness
        // Vertical: right edge, full height
        _vOverlay.SetBounds(ClientSize.Width - ow, 0, ow, ClientSize.Height);
        // Horizontal: bottom edge, full width
        _hOverlay.SetBounds(0, ClientSize.Height - ow, ClientSize.Width, ow);
        _hOverlay.BringToFront();
        _vOverlay.BringToFront();
    }

    // ── Vertical painting ──────────────────────────────────────────────────
    private void OnVOverlayPaint(object sender, PaintEventArgs e)
    {
        if (_vAlpha <= 0f) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int trackH = _vOverlay.Height;
        var (thumbY, thumbH) = ComputeVThumb(trackH);
        if (thumbH >= trackH) return;

        int   w         = _vHovered ? ThumbHoverWidth : ThumbNormalWidth;
        int   x         = _vOverlay.Width - w - ThumbMargin;
        int   alpha     = (int)(_vAlpha * 255);
        Color baseColor = _vDragging ? ThumbDrag : _vHovered ? ThumbHover : ThumbNormal;
        var   color     = Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);

        using var brush = new SolidBrush(color);
        using var path  = MakeRoundedRect(new Rectangle(x, thumbY, w, thumbH), ThumbRadius);
        g.FillPath(brush, path);
    }

    private (int thumbY, int thumbH) ComputeVThumb(int trackH)
    {
        int contentH = DisplayRectangle.Height;
        int clientH  = ClientSize.Height;
        if (contentH <= clientH) return (0, trackH);

        float ratio  = (float)clientH / contentH;
        int   thumbH = Math.Max(ThumbMinLength, (int)(trackH * ratio));

        int   scrollY        = Math.Abs(AutoScrollPosition.Y);
        int   maxScroll      = contentH - clientH;
        float scrollFraction = maxScroll > 0 ? (float)scrollY / maxScroll : 0f;
        int   thumbY         = (int)((trackH - thumbH) * scrollFraction);

        return (thumbY, thumbH);
    }

    // ── Horizontal painting ────────────────────────────────────────────────
    private void OnHOverlayPaint(object sender, PaintEventArgs e)
    {
        if (_hAlpha <= 0f) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int trackW = _hOverlay.Width;
        var (thumbX, thumbW) = ComputeHThumb(trackW);
        if (thumbW >= trackW) return;

        int   h         = _hHovered ? ThumbHoverWidth : ThumbNormalWidth;
        int   y         = _hOverlay.Height - h - ThumbMargin;
        int   alpha     = (int)(_hAlpha * 255);
        Color baseColor = _hDragging ? ThumbDrag : _hHovered ? ThumbHover : ThumbNormal;
        var   color     = Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);

        using var brush = new SolidBrush(color);
        using var path  = MakeRoundedRect(new Rectangle(thumbX, y, thumbW, h), ThumbRadius);
        g.FillPath(brush, path);
    }

    private (int thumbX, int thumbW) ComputeHThumb(int trackW)
    {
        int contentW = DisplayRectangle.Width;
        int clientW  = ClientSize.Width;
        if (contentW <= clientW) return (0, trackW);

        float ratio  = (float)clientW / contentW;
        int   thumbW = Math.Max(ThumbMinLength, (int)(trackW * ratio));

        int   scrollX        = Math.Abs(AutoScrollPosition.X);
        int   maxScroll      = contentW - clientW;
        float scrollFraction = maxScroll > 0 ? (float)scrollX / maxScroll : 0f;
        int   thumbX         = (int)((trackW - thumbW) * scrollFraction);

        return (thumbX, thumbW);
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

    // ── Vertical overlay mouse events ──────────────────────────────────────
    private void OnVOverlayMouseEnter(object sender, EventArgs e)
    {
        _vHovered = true;
        _hideTimer.Stop();
        _fadeTimer.Stop();
        _vAlpha = 1f;
        _vOverlay.Invalidate();
    }

    private void OnVOverlayMouseLeave(object sender, EventArgs e)
    {
        _vHovered = false;
        _vOverlay.Invalidate();
        StartHideTimer();
    }

    private void OnVOverlayMouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        var (thumbY, thumbH) = ComputeVThumb(_vOverlay.Height);
        if (e.Y < thumbY || e.Y > thumbY + thumbH) return;

        _vDragging         = true;
        _vDragStartY       = e.Y;
        _vDragStartScrollY = Math.Abs(AutoScrollPosition.Y);
        _vOverlay.Capture  = true;
        _vOverlay.Invalidate();
    }

    private void OnVOverlayMouseMove(object sender, MouseEventArgs e)
    {
        if (!_vDragging) return;

        int contentH   = DisplayRectangle.Height;
        int clientH    = ClientSize.Height;
        int trackH     = _vOverlay.Height;
        var (_, thumbH) = ComputeVThumb(trackH);
        int thumbRange  = trackH - thumbH;
        if (thumbRange <= 0) return;

        int scrollRange = contentH - clientH;
        int delta       = e.Y - _vDragStartY;
        int newScrollY  = Math.Max(0, Math.Min(scrollRange,
                              _vDragStartScrollY + (int)((float)delta * scrollRange / thumbRange)));

        AutoScrollPosition = new Point(Math.Abs(AutoScrollPosition.X), newScrollY);
        _vOverlay.Invalidate();
    }

    private void OnVOverlayMouseUp(object sender, MouseEventArgs e)
    {
        _vDragging        = false;
        _vOverlay.Capture = false;
        _vOverlay.Invalidate();
        StartHideTimer();
    }

    // ── Horizontal overlay mouse events ────────────────────────────────────
    private void OnHOverlayMouseEnter(object sender, EventArgs e)
    {
        _hHovered = true;
        _hideTimer.Stop();
        _fadeTimer.Stop();
        _hAlpha = 1f;
        _hOverlay.Invalidate();
    }

    private void OnHOverlayMouseLeave(object sender, EventArgs e)
    {
        _hHovered = false;
        _hOverlay.Invalidate();
        StartHideTimer();
    }

    private void OnHOverlayMouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) return;
        var (thumbX, thumbW) = ComputeHThumb(_hOverlay.Width);
        if (e.X < thumbX || e.X > thumbX + thumbW) return;

        _hDragging         = true;
        _hDragStartX       = e.X;
        _hDragStartScrollX = Math.Abs(AutoScrollPosition.X);
        _hOverlay.Capture  = true;
        _hOverlay.Invalidate();
    }

    private void OnHOverlayMouseMove(object sender, MouseEventArgs e)
    {
        if (!_hDragging) return;

        int contentW    = DisplayRectangle.Width;
        int clientW     = ClientSize.Width;
        int trackW      = _hOverlay.Width;
        var (_, thumbW) = ComputeHThumb(trackW);
        int thumbRange  = trackW - thumbW;
        if (thumbRange <= 0) return;

        int scrollRange = contentW - clientW;
        int delta       = e.X - _hDragStartX;
        int newScrollX  = Math.Max(0, Math.Min(scrollRange,
                              _hDragStartScrollX + (int)((float)delta * scrollRange / thumbRange)));

        AutoScrollPosition = new Point(newScrollX, Math.Abs(AutoScrollPosition.Y));
        _hOverlay.Invalidate();
    }

    private void OnHOverlayMouseUp(object sender, MouseEventArgs e)
    {
        _hDragging        = false;
        _hOverlay.Capture = false;
        _hOverlay.Invalidate();
        StartHideTimer();
    }

    // ── Shared fade helpers ────────────────────────────────────────────────
    private void ShowThumbs()
    {
        _fadeTimer.Stop();
        _vAlpha = 1f;
        _hAlpha = 1f;
        _vOverlay.Invalidate();
        _hOverlay.Invalidate();
        StartHideTimer();
    }

    private void StartHideTimer()
    {
        if (!_vHovered && !_vDragging && !_hHovered && !_hDragging)
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
