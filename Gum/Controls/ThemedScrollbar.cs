using System;
using System.Drawing;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Dialogs;
using Gum.Services;

namespace Gum.Controls;

public enum ScrollOrientationEx { Horizontal, Vertical }

public sealed class ThemedScrollBar : Control
{
    public ScrollOrientationEx Orientation { get; set; } = ScrollOrientationEx.Vertical;

    // Range
    private int _minimum = 0, _maximum = 100, _value = 0, _largeChange = 10, _smallChange = 20;
    public int Minimum
    {
        get => _minimum;
        set { _minimum = value; ClampValue(); Invalidate(); }
    }

    public int Maximum
    {
        get => _maximum;
        set { _maximum = Math.Max(value, _minimum + 1); ClampValue(); Invalidate(); }
    }

    public int LargeChange
    {
        get => _largeChange;
        set { _largeChange = Math.Max(1, value); ClampValue(); Invalidate(); }
    }

    // Optional: keep as-is
    public int SmallChange
    {
        get => _smallChange;
        set { _smallChange = Math.Max(1, value); Invalidate(); }
    }

    public int Value
    {
        get => _value;
        set
        {
            int newVal = Clamp(value, Minimum, MaxThumbStart());
            if (newVal != _value)
            {
                _value = newVal;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private int MaxThumbStart()
    {
        // last starting value that keeps the thumb fully visible
        // ensure not less than Minimum even if LargeChange > range
        return Math.Max(Minimum, Maximum - LargeChange + 1);
    }
    private void ClampValue()
    {
        int clamped = Clamp(_value, Minimum, MaxThumbStart());
        if (clamped != _value)
        {
            _value = clamped;
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? ValueChanged;

    // Appearance
    public int Thickness { get; set; } = 12;
    public int ThumbMinLength { get; set; } = 24;
    public int Radius { get; set; } = 6;
    public Color TrackColor { get; set; } = Color.FromArgb(32, 0, 0, 0);
    public Color TrackHoverColor { get; set; } = Color.FromArgb(48, 0, 0, 0);
    public Color ThumbColor { get; set; } = Color.FromArgb(160, 120, 120, 120);
    public Color ThumbHoverColor { get; set; } = Color.FromArgb(200, 120, 120, 120);
    public Color ThumbActiveColor { get; set; } = Color.FromArgb(220, 120, 120, 120);

    private bool _hover, _dragging, _hoverTrack;
    private int _dragOffsetPx; // mouse-pos within thumb at drag start

    public ThemedScrollBar()
    {
        SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint |
                 ControlStyles.OptimizedDoubleBuffer, true);
        TabStop = false;
        Width = Thickness; Height = 100;
        MouseWheel += OnMouseWheel;

        bool isDarkMode = Locator.GetRequiredService<IThemingService>().EffectiveSettings.IsSystemInDarkMode;
        ApplyTheme(isDarkMode);
        Locator.GetRequiredService<IMessenger>().Register<ThemeChangedMessage>(this, (recipient, message) =>
        {
            bool dark = message.settings.Mode is ThemeMode.Dark;
            ApplyTheme(dark);
            Invalidate();
        });
    }

    public void ApplyTheme(bool isDarkMode)
    {
        BackColor = (System.Windows.Application.Current.TryFindResource("Frb.Surface01") as
                        System.Windows.Media.SolidColorBrush) is { Color: { } c }
                        ? Color.FromArgb(c.A, c.R, c.G, c.B)
                        : Color.Transparent;
    }

    protected override void OnMouseEnter(EventArgs e) { base.OnMouseEnter(e); _hover = true; Invalidate(); }
    protected override void OnMouseLeave(EventArgs e) { base.OnMouseLeave(e); _hover = _hoverTrack = false; Invalidate(); }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        if (_dragging)
        {
            // Convert mouse coordinate to value via track geometry
            var (trackStart, trackLen, thumbLen) = TrackMetrics();
            int mouse = (Orientation == ScrollOrientationEx.Vertical) ? e.Y : e.X;
            int thumbStartPx = mouse - _dragOffsetPx;
            thumbStartPx = Clamp(thumbStartPx, trackStart, trackStart + trackLen - thumbLen);
            Value = PxToValue(thumbStartPx, trackStart, trackLen, thumbLen);
        }
        else
        {
            _hoverTrack = ThumbRect().Contains(e.Location);
            Invalidate();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;

        var thumb = ThumbRect();
        if (thumb.Contains(e.Location))
        {
            _dragging = true;
            _dragOffsetPx = (Orientation == ScrollOrientationEx.Vertical) ? e.Y - thumb.Y : e.X - thumb.X;
            Capture = true;
            Invalidate();
        }
        else
        {
            // Page up/down on track click
            if (Orientation == ScrollOrientationEx.Vertical)
                Value += (e.Y < thumb.Y ? -LargeChange : LargeChange);
            else
                Value += (e.X < thumb.X ? -LargeChange : LargeChange);
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (_dragging)
        {
            _dragging = false;
            Capture = false;
            Invalidate();
        }
    }

    private void OnMouseWheel(object? s, MouseEventArgs e)
    {
        int delta = Math.Sign(e.Delta) * -SmallChange;
        Value += delta;
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        if (Orientation == ScrollOrientationEx.Vertical) Width = Thickness; else Height = Thickness;
        Invalidate();
    }

    protected override void OnPaintBackground(PaintEventArgs e)
    {
        // Fill the whole control with its BackColor
        using var brush = new SolidBrush(this.BackColor);
        e.Graphics.FillRectangle(brush, this.ClientRectangle);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

        // track
        using var trackBrush = new SolidBrush(_hover ? TrackHoverColor : TrackColor);
        var trackRect = ClientRectangle;
        if (Orientation == ScrollOrientationEx.Vertical)
        {
            trackRect.X = (trackRect.Width - Thickness) / 2;
            trackRect.Width = Thickness;
        }
        else
        {
            trackRect.Y = (trackRect.Height - Thickness) / 2;
            trackRect.Height = Thickness;
        }
        FillRound(g, trackBrush, trackRect, Radius);

        // thumb
        var thumb = ThumbRect();
        var thumbColor = _dragging ? ThumbActiveColor : (_hoverTrack ? ThumbHoverColor : ThumbColor);
        using var thumbBrush = new SolidBrush(thumbColor);
        FillRound(g, thumbBrush, thumb, Radius);
    }

    private Rectangle ThumbRect()
    {
        var (trackStart, trackLen, thumbLen) = TrackMetrics();
        if (trackLen <= 0) return Rectangle.Empty;

        int thumbStartPx = ValueToPx(Value, trackStart, trackLen, thumbLen);
        if (Orientation == ScrollOrientationEx.Vertical)
            return new Rectangle((Width - Thickness) / 2, thumbStartPx, Thickness, thumbLen);
        else
            return new Rectangle(thumbStartPx, (Height - Thickness) / 2, thumbLen, Thickness);
    }

    // Layout math
    private (int trackStart, int trackLen, int thumbLen) TrackMetrics()
    {
        int trackBreadth = Thickness;
        if (Orientation == ScrollOrientationEx.Vertical)
        {
            int start = Radius; // small padding
            int len = Math.Max(0, Height - 2 * Radius);
            int thumbLen = ThumbLengthFromRange(len);
            return (start, len, thumbLen);
        }
        else
        {
            int start = Radius;
            int len = Math.Max(0, Width - 2 * Radius);
            int thumbLen = ThumbLengthFromRange(len);
            return (start, len, thumbLen);
        }
    }

    private int ThumbLengthFromRange(int trackLen)
    {
        // proportional thumb: LargeChange represents viewport; Maximum is extent
        int range = Math.Max(1, Maximum - Minimum + 1);
        int len = (int)Math.Round(trackLen * Math.Min(1.0, LargeChange / (double)range));
        return Math.Max(ThumbMinLength, Math.Min(trackLen, len));
    }

    private int ValueToPx(int value, int trackStart, int trackLen, int thumbLen)
    {
        int movable = Math.Max(1, trackLen - thumbLen);
        int maxVal = Math.Max(0, MaxThumbStart() - Minimum);
        double t = maxVal == 0 ? 0 : (value - Minimum) / (double)maxVal;
        return trackStart + (int)Math.Round(t * movable);
    }

    private int PxToValue(int px, int trackStart, int trackLen, int thumbLen)
    {
        int movable = Math.Max(1, trackLen - thumbLen);
        int rel = Clamp(px - trackStart, 0, movable);
        int maxVal = Math.Max(0, MaxThumbStart() - Minimum);
        int val = Minimum + (movable == 0 ? 0 : (int)Math.Round(rel * (maxVal / (double)movable)));
        return Clamp(val, Minimum, MaxThumbStart());
    }

    private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

    private static void FillRound(System.Drawing.Graphics g, Brush brush, Rectangle r, int radius)
    {
        if (radius <= 0) { g.FillRectangle(brush, r); return; }
        using var gp = new System.Drawing.Drawing2D.GraphicsPath();
        int d = radius * 2;
        gp.AddArc(r.X, r.Y, d, d, 180, 90);
        gp.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        gp.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        gp.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        gp.CloseFigure();
        g.FillPath(brush, gp);
    }
}