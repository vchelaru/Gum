using System;
using System.Windows;
using System.Windows.Media;

namespace StateAnimationPlugin.Views;

public class TimelineOverlay : FrameworkElement
{
    #region DPs
    public double Length
    {
        get => (double)GetValue(LengthProperty);
        set => SetValue(LengthProperty, value);
    }
    public static readonly DependencyProperty LengthProperty =
        DependencyProperty.Register(nameof(Length), typeof(double), typeof(TimelineOverlay),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public double CurrentTime
    {
        get => (double)GetValue(CurrentTimeProperty);
        set => SetValue(CurrentTimeProperty, value);
    }
    public static readonly DependencyProperty CurrentTimeProperty =
        DependencyProperty.Register(nameof(CurrentTime), typeof(double), typeof(TimelineOverlay),
            new FrameworkPropertyMetadata(0d, FrameworkPropertyMetadataOptions.AffectsRender));

    public double MajorTickInterval
    {
        get => (double)GetValue(MajorTickIntervalProperty);
        set => SetValue(MajorTickIntervalProperty, value);
    }
    public static readonly DependencyProperty MajorTickIntervalProperty =
        DependencyProperty.Register(nameof(MajorTickInterval), typeof(double), typeof(TimelineOverlay),
            new FrameworkPropertyMetadata(1d, FrameworkPropertyMetadataOptions.AffectsRender));

    public double MinorTickInterval
    {
        get => (double)GetValue(MinorTickIntervalProperty);
        set => SetValue(MinorTickIntervalProperty, value);
    }
    public static readonly DependencyProperty MinorTickIntervalProperty =
        DependencyProperty.Register(nameof(MinorTickInterval), typeof(double), typeof(TimelineOverlay),
            new FrameworkPropertyMetadata(0.25d, FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush MajorTickBrush
    {
        get => (Brush)GetValue(MajorTickBrushProperty);
        set => SetValue(MajorTickBrushProperty, value);
    }
    public static readonly DependencyProperty MajorTickBrushProperty =
        DependencyProperty.Register(nameof(MajorTickBrush), typeof(Brush), typeof(TimelineOverlay),
            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(0x55, 0x00, 0x00, 0x00)), FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush MinorTickBrush
    {
        get => (Brush)GetValue(MinorTickBrushProperty);
        set => SetValue(MinorTickBrushProperty, value);
    }
    public static readonly DependencyProperty MinorTickBrushProperty =
        DependencyProperty.Register(nameof(MinorTickBrush), typeof(Brush), typeof(TimelineOverlay),
            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromArgb(0x22, 0x00, 0x00, 0x00)), FrameworkPropertyMetadataOptions.AffectsRender));

    public Brush CurrentTimeBrush
    {
        get => (Brush)GetValue(CurrentTimeBrushProperty);
        set => SetValue(CurrentTimeBrushProperty, value);
    }
    public static readonly DependencyProperty CurrentTimeBrushProperty =
        DependencyProperty.Register(nameof(CurrentTimeBrush), typeof(Brush), typeof(TimelineOverlay),
            new FrameworkPropertyMetadata(Brushes.Red, FrameworkPropertyMetadataOptions.AffectsRender));

    public double MajorTickThickness
    {
        get => (double)GetValue(MajorTickThicknessProperty);
        set => SetValue(MajorTickThicknessProperty, value);
    }
    public static readonly DependencyProperty MajorTickThicknessProperty =
        DependencyProperty.Register(nameof(MajorTickThickness), typeof(double), typeof(TimelineOverlay),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double MinorTickThickness
    {
        get => (double)GetValue(MinorTickThicknessProperty);
        set => SetValue(MinorTickThicknessProperty, value);
    }
    public static readonly DependencyProperty MinorTickThicknessProperty =
        DependencyProperty.Register(nameof(MinorTickThickness), typeof(double), typeof(TimelineOverlay),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public double CurrentTimeThickness
    {
        get => (double)GetValue(CurrentTimeThicknessProperty);
        set => SetValue(CurrentTimeThicknessProperty, value);
    }
    public static readonly DependencyProperty CurrentTimeThicknessProperty =
        DependencyProperty.Register(nameof(CurrentTimeThickness), typeof(double), typeof(TimelineOverlay),
            new FrameworkPropertyMetadata(1.5, FrameworkPropertyMetadataOptions.AffectsRender));
    #endregion

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        double w = ActualWidth;
        double h = ActualHeight;
        if (w <= 0 || h <= 0 || Length <= 0) return;

        double pxPerSec = w / Length;

        // Draw ticks
        void DrawVLine(double x, Pen pen)
        {
            // Snap for crisp 1px lines
            double t = pen.Thickness;
            double offset = (t % 2 == 1) ? 0.5 : 0.0;
            x = Math.Round(x) + offset;
            dc.DrawLine(pen, new Point(x, 0), new Point(x, h));
        }

        var minorPen = new Pen(MinorTickBrush, MinorTickThickness);
        var majorPen = new Pen(MajorTickBrush, MajorTickThickness);
        var nowPen = new Pen(CurrentTimeBrush, CurrentTimeThickness);

        // minor ticks
        if (MinorTickInterval > 0 && MinorTickInterval < Length)
        {
            double t = 0.0;
            // avoid cumulative floating-point error by stepping with integer counter
            int count = (int)Math.Floor(Length / MinorTickInterval);
            for (int i = 0; i <= count; i++)
            {
                t = i * MinorTickInterval;
                double x = t * pxPerSec;
                DrawVLine(x, minorPen);
            }
        }

        // major ticks on top of minors
        if (MajorTickInterval > 0 && MajorTickInterval < Length)
        {
            int count = (int)Math.Floor(Length / MajorTickInterval);
            for (int i = 0; i <= count; i++)
            {
                double t = i * MajorTickInterval;
                double x = t * pxPerSec;
                DrawVLine(x, majorPen);
            }
        }

        // current time line (behind items — this element lives under them in the visual tree)
        if (CurrentTime >= 0 && CurrentTime <= Length)
        {
            double x = CurrentTime * pxPerSec;
            DrawVLine(x, nowPen);
        }
    }

    protected override HitTestResult HitTestCore(PointHitTestParameters hitTestParameters)
    {
        // Let clicks pass through
        return null!;
    }
}