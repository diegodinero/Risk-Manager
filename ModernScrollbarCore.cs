using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

/// <summary>
/// Shared overlay-based scrollbar logic that can be composed into any
/// <see cref="ScrollableControl"/> subclass to give it a macOS-style
/// thin, auto-hiding scrollbar thumb.
/// </summary>
internal sealed class ModernScrollbarCore : IDisposable
{
    // ── Appearance constants (used by host classes too) ────────────────────
    internal const int ThumbNormalWidth = 6;
    internal const int ThumbHoverWidth  = 10;
    internal const int ThumbMargin      = 3;    // gap from right edge
    internal const int OverlayWidth     = ThumbHoverWidth + ThumbMargin * 2;
    private  const int ThumbMinHeight   = 28;
    private  const int ThumbRadius      = 3;

    private static readonly Color ThumbNormal = Color.FromArgb(110, 160, 160, 160);
    private static readonly Color ThumbHover  = Color.FromArgb(190, 180, 180, 180);
    private static readonly Color ThumbDrag   = Color.FromArgb(220, 200, 200, 200);

    // ── State ──────────────────────────────────────────────────────────────
    private float _alpha;
    private bool  _hovered;
    private bool  _dragging;
    private int   _dragStartY;
    private int   _dragStartScrollY;

    // ── References ─────────────────────────────────────────────────────────
    private readonly ScrollableControl _parent;
    private readonly Panel             _overlay;
    private readonly Timer             _hideTimer;
    private readonly Timer             _fadeTimer;

    // ── Nested overlay control with proper WinForms transparency ───────────
    internal sealed class ScrollbarOverlay : Panel
    {
        internal ScrollbarOverlay()
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
    internal ModernScrollbarCore(ScrollableControl parent)
    {
        _parent  = parent;
        _overlay = new ScrollbarOverlay();

        _overlay.Paint      += OnOverlayPaint;
        _overlay.MouseEnter += OnOverlayMouseEnter;
        _overlay.MouseLeave += OnOverlayMouseLeave;
        _overlay.MouseDown  += OnOverlayMouseDown;
        _overlay.MouseMove  += OnOverlayMouseMove;
        _overlay.MouseUp    += OnOverlayMouseUp;

        _parent.Controls.Add(_overlay);

        _hideTimer = new Timer { Interval = 1500 };
        _hideTimer.Tick += (_, __) => { _hideTimer.Stop(); _fadeTimer.Start(); };

        _fadeTimer = new Timer { Interval = 16 };  // ~60 fps
        _fadeTimer.Tick += (_, __) =>
        {
            _alpha = Math.Max(0f, _alpha - 0.07f);
            _overlay.Invalidate();
            if (_alpha <= 0f) _fadeTimer.Stop();
        };
    }

    // ── Called from the host's OnControlAdded ─────────────────────────────
    internal void OnControlAdded(Control added)
    {
        if (added != _overlay) _overlay?.BringToFront();
    }

    // ── Called from host's OnResize / OnLayout ────────────────────────────
    internal void OnResize()  => PositionOverlay();
    internal void OnLayout()  => PositionOverlay();

    // ── Called from host's OnMouseWheel ───────────────────────────────────
    internal void OnMouseWheel()
    {
        ShowThumb();
        _overlay.Invalidate();
    }

    // ── Called from host's Scroll event ───────────────────────────────────
    internal void ShowThumb()
    {
        _fadeTimer.Stop();
        _alpha = 1f;
        _overlay.Invalidate();
        StartHideTimer();
    }

    // ──────────────────────────────────────────────────────────────────────
    private void PositionOverlay()
    {
        if (_overlay == null) return;
        _overlay.SetBounds(
            _parent.ClientSize.Width - OverlayWidth, 0,
            OverlayWidth, _parent.ClientSize.Height);
        _overlay.BringToFront();
    }

    private void OnOverlayPaint(object sender, PaintEventArgs e)
    {
        if (_alpha <= 0f) return;

        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;

        int trackH = _overlay.Height;
        var (thumbY, thumbH) = ComputeThumb(trackH);
        if (thumbH >= trackH) return;   // content fits; nothing to show

        int   w         = _hovered ? ThumbHoverWidth : ThumbNormalWidth;
        int   x         = _overlay.Width - w - ThumbMargin;
        int   alpha     = (int)(_alpha * 255);
        Color baseColor = _dragging ? ThumbDrag : _hovered ? ThumbHover : ThumbNormal;
        var   color     = Color.FromArgb(alpha, baseColor.R, baseColor.G, baseColor.B);

        using var brush = new SolidBrush(color);
        using var path  = MakeRoundedRect(new Rectangle(x, thumbY, w, thumbH), ThumbRadius);
        g.FillPath(brush, path);
    }

    private (int thumbY, int thumbH) ComputeThumb(int trackH)
    {
        int contentH = _parent.DisplayRectangle.Height;
        int clientH  = _parent.ClientSize.Height;
        if (contentH <= clientH) return (0, trackH);

        float ratio  = (float)clientH / contentH;
        int   thumbH = Math.Max(ThumbMinHeight, (int)(trackH * ratio));

        int   scrollY        = Math.Abs(_parent.AutoScrollPosition.Y);
        int   maxScroll      = contentH - clientH;
        float scrollFraction = maxScroll > 0 ? (float)scrollY / maxScroll : 0f;
        int   thumbY         = (int)((trackH - thumbH) * scrollFraction);

        return (thumbY, thumbH);
    }

    private static GraphicsPath MakeRoundedRect(Rectangle r, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(r.X,         r.Y,          d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y,          d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d,   0, 90);
        path.AddArc(r.X,         r.Bottom - d, d, d,  90, 90);
        path.CloseFigure();
        return path;
    }

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
        _dragStartScrollY = Math.Abs(_parent.AutoScrollPosition.Y);
        _overlay.Capture  = true;
        _overlay.Invalidate();
    }

    private void OnOverlayMouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging) return;

        int contentH = _parent.DisplayRectangle.Height;
        int clientH  = _parent.ClientSize.Height;
        int trackH   = _overlay.Height;
        var (_, thumbH) = ComputeThumb(trackH);
        int thumbRange   = trackH - thumbH;
        if (thumbRange <= 0) return;

        int scrollRange = contentH - clientH;
        int delta       = e.Y - _dragStartY;
        int newScrollY  = Math.Max(0, Math.Min(scrollRange,
                              _dragStartScrollY + (int)((float)delta * scrollRange / thumbRange)));

        _parent.AutoScrollPosition = new Point(0, newScrollY);
        _overlay.Invalidate();
    }

    private void OnOverlayMouseUp(object sender, MouseEventArgs e)
    {
        _dragging        = false;
        _overlay.Capture = false;
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

    public void Dispose()
    {
        _hideTimer?.Dispose();
        _fadeTimer?.Dispose();
    }
}
