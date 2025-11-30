using System;
using FlatRedBall.Glue.StateInterpolation;
using StateAnimationPlugin.ViewModels;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace StateAnimationPlugin.Controls;

public class InterpolationTrackControl : Canvas
{
    public IReadOnlyList<AnimatedKeyframeViewModel>? Keyframes
    {
        get => (IReadOnlyList<AnimatedKeyframeViewModel>?)GetValue(KeyframesProperty);
        set => SetValue(KeyframesProperty, value);
    }

    public static readonly DependencyProperty KeyframesProperty = DependencyProperty.Register(
        nameof(Keyframes), typeof(IReadOnlyList<AnimatedKeyframeViewModel>), typeof(InterpolationTrackControl),
        new PropertyMetadata(null, OnDataChanged));

    public double AnimationLength
    {
        get => (double)GetValue(AnimationLengthProperty);
        set => SetValue(AnimationLengthProperty, value);
    }

    public static readonly DependencyProperty AnimationLengthProperty = DependencyProperty.Register(
        nameof(AnimationLength), typeof(double), typeof(InterpolationTrackControl),
        new PropertyMetadata(0d, OnDataChanged));

    public Brush LineBrush
    {
        get => (Brush)GetValue(LineBrushProperty);
        set => SetValue(LineBrushProperty, value);
    }

    public static readonly DependencyProperty LineBrushProperty = DependencyProperty.Register(
        nameof(LineBrush), typeof(Brush), typeof(InterpolationTrackControl), new PropertyMetadata(Brushes.Gray, OnDataChanged));

    public Brush? FillBrush
    {
        get => (Brush?)GetValue(FillBrushProperty);
        set => SetValue(FillBrushProperty, value);
    }

    public static readonly DependencyProperty FillBrushProperty = DependencyProperty.Register(
        nameof(FillBrush), typeof(Brush), typeof(InterpolationTrackControl), new PropertyMetadata(null, OnDataChanged));

    public bool ClampInterpolationVisuals
    {
        get => (bool)GetValue(ClampInterpolationVisualsProperty);
        set => SetValue(ClampInterpolationVisualsProperty, value);
    }

    public static readonly DependencyProperty ClampInterpolationVisualsProperty = DependencyProperty.Register(
        nameof(ClampInterpolationVisuals), typeof(bool), typeof(InterpolationTrackControl),
        new PropertyMetadata(true, OnDataChanged));

    static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if(d is InterpolationTrackControl c)
        {
            if(e.Property == KeyframesProperty)
            {
                c.HookItems();
            }
            c.InvalidateVisual();
        }
    }

    readonly List<AnimatedKeyframeViewModel> _hooked = new();

    private void HookItems()
    {
        foreach(var k in _hooked)
        {
            if(k is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged -= OnKeyframePropertyChanged;
            }
        }
        _hooked.Clear();

        if(Keyframes is { } list)
        {
            foreach(var k in list)
            {
                if(k is INotifyPropertyChanged inpc)
                {
                    inpc.PropertyChanged += OnKeyframePropertyChanged;
                    _hooked.Add(k);
                }
            }
        }
    }

    void OnKeyframePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if(e.PropertyName is nameof(AnimatedKeyframeViewModel.InterpolationType) 
           or nameof(AnimatedKeyframeViewModel.Easing) 
           or nameof(AnimatedKeyframeViewModel.Time))
        {
            InvalidateVisual();
        }
    }

    public InterpolationTrackControl()
    {
        IsHitTestVisible = false;
        SnapsToDevicePixels = true;

        Loaded += (_, _) =>
        {
            HookItems();
            InvalidateVisual();
        };

        Unloaded += (_,_) =>
        {
            foreach (var inpc in _hooked.OfType<INotifyPropertyChanged>())
            {
                inpc.PropertyChanged -= OnKeyframePropertyChanged;
            }
            _hooked.Clear();
        };
    }

    protected override void OnRender(DrawingContext dc)
    {
        base.OnRender(dc);

        if (Keyframes is not { Count: > 1 } items || AnimationLength <= 0 || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        // only states, by time
        var stateItems = items.Where(k => !string.IsNullOrEmpty(k.StateName))
                              .OrderBy(k => k.Time)
                              .ToList();
        if (stateItems.Count < 2)
        {
            return;
        }

        const int samples = 32;
        Pen pen = new(LineBrush, 1.0);
        Brush fillBrush = FillBrush ?? DeriveDefaultFillBrush(LineBrush);

        for (int i = 0; i < stateItems.Count - 1; i++)
        {
            AnimatedKeyframeViewModel current = stateItems[i];
            AnimatedKeyframeViewModel next = stateItems[i + 1];
            double startX = (current.Time / AnimationLength) * ActualWidth;
            double endX = (next.Time / AnimationLength) * ActualWidth;
            if (endX <= startX)
            {
                continue;
            }

            TweeningFunction tween = Tweener.GetInterpolationFunction(current.InterpolationType, current.Easing);

            StreamGeometry fillGeo = new();
            using(StreamGeometryContext fillContext = fillGeo.Open())
            {
                double firstProcessed = tween(0f, 0f, 1f, 1f);
                if (ClampInterpolationVisuals)
                {
                    firstProcessed = Math.Clamp(firstProcessed, 0, 1);
                }

                double firstY = (1 - firstProcessed) * ActualHeight;
                fillContext.BeginFigure(new Point(startX, firstY), true, true);

                for(int s=1; s<=samples; s++)
                {
                    float t = (float)s / samples;
                    double processed = tween(t, 0f, 1f, 1f);

                    if (ClampInterpolationVisuals)
                    {
                        processed = Math.Clamp(processed, 0, 1);
                    }

                    double x = startX + (endX - startX) * t;
                    double y = (1 - processed) * ActualHeight;
                    fillContext.LineTo(new Point(x, y), true, false);
                }

                // bottom right
                fillContext.LineTo(new Point(endX, ActualHeight), true, false);
                // bottom left
                fillContext.LineTo(new Point(startX, ActualHeight), true, false);
            }
            fillGeo.Freeze();
            dc.DrawGeometry(fillBrush, null, fillGeo);

            // stroke geometry (just the curve)
            StreamGeometry strokeGeo = new();
            using (StreamGeometryContext strokeContext = strokeGeo.Open())
            {
                for(int s = 0; s <= samples; s++)
                {
                    float t = (float)s / samples;
                    double processed = tween(t, 0f, 1f, 1f);

                    if (ClampInterpolationVisuals)
                    {
                        processed = Math.Clamp(processed, 0, 1);
                    }

                    double x = startX + (endX - startX) * t;
                    double y = (1 - processed) * ActualHeight;

                    if (s == 0)
                    {
                        strokeContext.BeginFigure(new Point(x, y), false, false);
                    }
                    else
                    {
                        strokeContext.LineTo(new Point(x, y), true, false);
                    }
                }
            }
            strokeGeo.Freeze();
            dc.DrawGeometry(null, pen, strokeGeo);
        }
    }

    static Brush DeriveDefaultFillBrush(Brush lineBrush)
    {
        if(lineBrush is SolidColorBrush scb)
        {
            Color c = scb.Color;
            return new SolidColorBrush(Color.FromArgb(80, c.R, c.G, c.B));
        }
        return new SolidColorBrush(Color.FromArgb(80, 128,128,128));
    }
}
