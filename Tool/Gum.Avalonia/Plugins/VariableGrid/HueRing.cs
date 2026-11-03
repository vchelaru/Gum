using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Gum.ColorPicking;

namespace Gum.Avalonia.Plugins.VariableGrid;

/// <summary>
/// A ring of hues for the color picker. Red is at the top and the hue grows clockwise, so dragging
/// from purple to orange passes through red with no seam to cross. Dragging works anywhere once it
/// starts on the band, and the arrow keys nudge the hue.
/// </summary>
public class HueRing : Control
{
    /// <summary>The hue in degrees, 0 to under 360; 0 is at the top.</summary>
    public static readonly StyledProperty<double> HueProperty =
        AvaloniaProperty.Register<HueRing, double>(nameof(Hue), defaultValue: 0, coerce: (_, value) => HueRingGeometry.NudgeHue(value, 0));

    /// <summary>The ring's thickness as a fraction of its width.</summary>
    internal const double ThicknessFraction = 0.1;

    private const double PointerTolerance = 2;
    private const double KeyStep = 1;
    private const double ShiftKeyStep = 10;

    private static readonly IBrush RingBrush = CreateRingBrush();

    private bool _isDragging;

    static HueRing()
    {
        AffectsRender<HueRing>(HueProperty);
        FocusableProperty.OverrideDefaultValue<HueRing>(true);
        AutomationProperties.NameProperty.OverrideDefaultValue<HueRing>("Hue");
    }

    /// <summary>The hue in degrees, 0 to under 360.</summary>
    public double Hue
    {
        get => GetValue(HueProperty);
        set => SetValue(HueProperty, value);
    }

    /// <summary>Raised when the user moves the hue by the pointer or keyboard, with the new hue. Setting <see cref="Hue"/> does not raise it.</summary>
    public event EventHandler<double>? HueEdited;

    /// <summary>The ring's outer radius at the current size.</summary>
    internal double OuterRadius => Math.Min(Bounds.Width, Bounds.Height) / 2;

    /// <summary>The ring's inner radius at the current size.</summary>
    internal double InnerRadius => OuterRadius - Math.Min(Bounds.Width, Bounds.Height) * ThicknessFraction;

    /// <summary>The point at the hue on the middle of the ring, in this control's coordinates.</summary>
    internal Point ThumbCenterFor(double hue)
    {
        (double x, double y) = HueRingGeometry.PointAt(Bounds.Width / 2, Bounds.Height / 2, (OuterRadius + InnerRadius) / 2, hue);
        return new Point(x, y);
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        // Transparent fill so the whole box takes the pointer; presses off the band are ignored.
        context.DrawRectangle(Brushes.Transparent, null, new Rect(Bounds.Size));

        double outer = OuterRadius;
        if (outer <= 0)
        {
            return;
        }

        Point center = new Point(Bounds.Width / 2, Bounds.Height / 2);
        GeometryGroup band = new GeometryGroup { FillRule = FillRule.EvenOdd };
        band.Children.Add(new EllipseGeometry(new Rect(center.X - outer, center.Y - outer, outer * 2, outer * 2)));
        band.Children.Add(new EllipseGeometry(new Rect(center.X - InnerRadius, center.Y - InnerRadius, InnerRadius * 2, InnerRadius * 2)));
        context.DrawGeometry(RingBrush, null, band);

        double thickness = outer - InnerRadius;
        double thumbRadius = thickness * 0.42;
        Point thumb = ThumbCenterFor(Hue);
        (byte red, byte green, byte blue) = HsvRgb.ToRgb(Hue, 1, 1);
        double outline = Math.Max(1, thickness * 0.1);
        context.DrawEllipse(new SolidColorBrush(Color.FromRgb(red, green, blue)), new Pen(Brushes.Black, outline * 3), thumb, thumbRadius, thumbRadius);
        context.DrawEllipse(null, new Pen(Brushes.White, outline * (IsKeyboardFocusWithin ? 2.5 : 1.5)), thumb, thumbRadius, thumbRadius);
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Point point = e.GetPosition(this);
        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed ||
            !HueRingGeometry.IsOnRing(Bounds.Width / 2, Bounds.Height / 2, point.X, point.Y, OuterRadius, InnerRadius, PointerTolerance))
        {
            return;
        }

        _isDragging = true;
        e.Pointer.Capture(this);
        Focus();
        SetHueFrom(point);
        e.Handled = true;
    }

    /// <inheritdoc/>
    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (_isDragging)
        {
            SetHueFrom(e.GetPosition(this));
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (_isDragging)
        {
            _isDragging = false;
            e.Pointer.Capture(null);
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerCaptureLost(PointerCaptureLostEventArgs e)
    {
        base.OnPointerCaptureLost(e);
        _isDragging = false;
    }

    /// <inheritdoc/>
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        double step = e.KeyModifiers.HasFlag(KeyModifiers.Shift) ? ShiftKeyStep : KeyStep;
        double delta = e.Key switch
        {
            Key.Right or Key.Up => step,
            Key.Left or Key.Down => -step,
            _ => 0,
        };
        if (delta != 0)
        {
            SetHueByUser(HueRingGeometry.NudgeHue(Hue, delta));
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnGotFocus(GotFocusEventArgs e)
    {
        base.OnGotFocus(e);
        InvalidateVisual();
    }

    /// <inheritdoc/>
    protected override void OnLostFocus(RoutedEventArgs e)
    {
        base.OnLostFocus(e);
        InvalidateVisual();
    }

    /// <summary>Sets the hue as the user would and raises <see cref="HueEdited"/> when it changed.</summary>
    internal void SetHueByUser(double hue)
    {
        if (hue != Hue)
        {
            Hue = hue;
            HueEdited?.Invoke(this, Hue);
        }
    }

    private void SetHueFrom(Point point) =>
        SetHueByUser(HueRingGeometry.HueAt(Bounds.Width / 2, Bounds.Height / 2, point.X, point.Y));

    private static IBrush CreateRingBrush()
    {
        ConicGradientBrush brush = new ConicGradientBrush { Center = new RelativePoint(0.5, 0.5, RelativeUnit.Relative), Angle = 0 };
        for (int step = 0; step <= 6; step++)
        {
            (byte red, byte green, byte blue) = HsvRgb.ToRgb(step * 60, 1, 1);
            brush.GradientStops.Add(new GradientStop(Color.FromRgb(red, green, blue), step / 6d));
        }
        return brush;
    }
}
