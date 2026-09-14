using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using StateAnimationPlugin.Timeline;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Plugins.StateAnimation;

/// <summary>
/// The Animations tab's timeline: the current time and a scrubber on top, then one row per state
/// category or sub-animation. Each row's <see cref="TimelineTrack"/> draws ticks, the interpolation
/// curve, the keyframe markers and the current-time line natively in Avalonia (the WPF tool used a
/// mix of overlays, templates and a Skia surface). Row grouping and geometry come from the shared
/// <see cref="TimelineLayout"/> and <see cref="InterpolationCurve"/>. Twin of the WPF <c>TimelineControl</c>.
/// </summary>
public sealed class TimelineView : Grid
{
    /// <summary>The animation shown.</summary>
    public static readonly StyledProperty<AnimationViewModel?> AnimationProperty =
        AvaloniaProperty.Register<TimelineView, AnimationViewModel?>(nameof(Animation));

    /// <summary>The time the editor displays; the scrubber and time box change it.</summary>
    public static readonly StyledProperty<double> CurrentTimeProperty =
        AvaloniaProperty.Register<TimelineView, double>(nameof(CurrentTime), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>Whether overshooting easings are drawn clamped to the row.</summary>
    public static readonly StyledProperty<bool> ClampInterpolationVisualsProperty =
        AvaloniaProperty.Register<TimelineView, bool>(nameof(ClampInterpolationVisuals), defaultValue: true);

    private readonly TextBox _timeBox;
    private readonly TextBlock _lengthText;
    private readonly Slider _scrubber;
    private readonly Grid _rows;
    private AnimationViewModel? _hookedAnimation;
    private ObservableCollection<AnimatedKeyframeViewModel>? _hookedKeyframes;
    private readonly List<AnimatedKeyframeViewModel> _hookedItems;

    /// <summary>Builds the timeline.</summary>
    public TimelineView()
    {
        _hookedItems = new List<AnimatedKeyframeViewModel>();
        Margin = new Thickness(8, 0);
        ColumnDefinitions = new ColumnDefinitions("Auto,*");
        RowDefinitions = new RowDefinitions("Auto,*");

        StackPanel timeRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 12, 4), VerticalAlignment = VerticalAlignment.Center };
        _timeBox = new TextBox { MinWidth = 64 };
        _timeBox.LostFocus += (_, _) => CommitTimeBox();
        _timeBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                CommitTimeBox();
                e.Handled = true;
            }
        };
        _lengthText = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0), Opacity = 0.7 };
        timeRow.Children.Add(_timeBox);
        timeRow.Children.Add(_lengthText);
        Children.Add(timeRow);

        _scrubber = new Slider { Minimum = 0, SmallChange = 0.01, LargeChange = 0.25, Margin = new Thickness(0, 0, 20, 0), VerticalAlignment = VerticalAlignment.Center };
        _scrubber.Bind(RangeBase.ValueProperty, new Binding(nameof(CurrentTime)) { Source = this, Mode = BindingMode.TwoWay });
        SetColumn(_scrubber, 1);
        Children.Add(_scrubber);

        _rows = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), Margin = new Thickness(0, 0, 12, 0), VerticalAlignment = VerticalAlignment.Top };
        ScrollViewer scroller = new ScrollViewer { Content = _rows, VerticalScrollBarVisibility = ScrollBarVisibility.Visible };
        SetRow(scroller, 1);
        SetColumnSpan(scroller, 2);
        Children.Add(scroller);

        UpdateTimeText();
    }

    /// <inheritdoc cref="AnimationProperty"/>
    public AnimationViewModel? Animation { get => GetValue(AnimationProperty); set => SetValue(AnimationProperty, value); }

    /// <inheritdoc cref="CurrentTimeProperty"/>
    public double CurrentTime { get => GetValue(CurrentTimeProperty); set => SetValue(CurrentTimeProperty, value); }

    /// <inheritdoc cref="ClampInterpolationVisualsProperty"/>
    public bool ClampInterpolationVisuals { get => GetValue(ClampInterpolationVisualsProperty); set => SetValue(ClampInterpolationVisualsProperty, value); }

    /// <summary>The rows currently shown, state and event categories first.</summary>
    internal IReadOnlyList<TimelineRow> Rows { get; private set; } = Array.Empty<TimelineRow>();

    /// <summary>Selects <paramref name="keyframe"/> when it belongs to the shown animation.</summary>
    internal void Select(AnimatedKeyframeViewModel keyframe)
    {
        if (Animation is { } animation && animation.Keyframes.Contains(keyframe))
        {
            animation.SelectedKeyframe = keyframe;
        }
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == AnimationProperty)
        {
            Hook(Animation);
            RebuildRows();
            UpdateTimeText();
        }
        else if (change.Property == CurrentTimeProperty)
        {
            UpdateTimeText();
            InvalidateTracks();
        }
        else if (change.Property == ClampInterpolationVisualsProperty)
        {
            InvalidateTracks();
        }
    }

    private void Hook(AnimationViewModel? animation)
    {
        if (_hookedAnimation != null)
        {
            _hookedAnimation.PropertyChanged -= HandleAnimationPropertyChanged;
        }
        if (_hookedKeyframes != null)
        {
            _hookedKeyframes.CollectionChanged -= HandleKeyframesChanged;
        }
        HookItems(Array.Empty<AnimatedKeyframeViewModel>());

        _hookedAnimation = animation;
        _hookedKeyframes = animation?.Keyframes;
        if (_hookedAnimation != null)
        {
            _hookedAnimation.PropertyChanged += HandleAnimationPropertyChanged;
        }
        if (_hookedKeyframes != null)
        {
            _hookedKeyframes.CollectionChanged += HandleKeyframesChanged;
            HookItems(_hookedKeyframes);
        }
    }

    private void HookItems(IEnumerable<AnimatedKeyframeViewModel> keyframes)
    {
        foreach (AnimatedKeyframeViewModel keyframe in _hookedItems)
        {
            keyframe.PropertyChanged -= HandleKeyframePropertyChanged;
        }
        _hookedItems.Clear();
        foreach (AnimatedKeyframeViewModel keyframe in keyframes)
        {
            keyframe.PropertyChanged += HandleKeyframePropertyChanged;
            _hookedItems.Add(keyframe);
        }
    }

    private void HandleAnimationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AnimationViewModel.SelectedKeyframe))
        {
            InvalidateTracks();
            BringSelectedRowIntoView();
        }
        else if (e.PropertyName == nameof(AnimationViewModel.Length))
        {
            UpdateTimeText();
            InvalidateTracks();
        }
    }

    private void HandleKeyframesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        HookItems(_hookedKeyframes ?? Enumerable.Empty<AnimatedKeyframeViewModel>());
        RebuildRows();
    }

    private void HandleKeyframePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            // Name and time changes can move a keyframe between rows or within one.
            case nameof(AnimatedKeyframeViewModel.AnimationName):
            case nameof(AnimatedKeyframeViewModel.StateName):
            case nameof(AnimatedKeyframeViewModel.EventName):
            case nameof(AnimatedKeyframeViewModel.Time):
            case nameof(AnimatedKeyframeViewModel.Length):
                RebuildRows();
                break;
            case nameof(AnimatedKeyframeViewModel.InterpolationType):
            case nameof(AnimatedKeyframeViewModel.Easing):
            case nameof(AnimatedKeyframeViewModel.IsTimelineVisualHovered):
                InvalidateTracks();
                break;
        }
    }

    private void RebuildRows()
    {
        List<(TimelineRow Row, bool IsSubAnimation)> rows = TimelineLayout.StateAndEventRows(Animation?.Keyframes).Select(row => (row, false))
            .Concat(TimelineLayout.SubAnimationRows(Animation?.Keyframes).Select(row => (row, true)))
            .ToList();
        Rows = rows.Select(entry => entry.Row).ToList();

        _rows.Children.Clear();
        _rows.RowDefinitions.Clear();
        for (int i = 0; i < rows.Count; i++)
        {
            _rows.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            TextBlock label = new TextBlock { Text = rows[i].Row.Name, Margin = new Thickness(0, 0, 12, 0), Padding = new Thickness(0, 1), VerticalAlignment = VerticalAlignment.Center };
            SetRow(label, i);
            _rows.Children.Add(label);

            TimelineTrack track = new TimelineTrack(this, rows[i].Row, rows[i].IsSubAnimation);
            SetRow(track, i);
            SetColumn(track, 1);
            _rows.Children.Add(track);
        }
    }

    private void BringSelectedRowIntoView()
    {
        if (Animation?.SelectedKeyframe is not { } selected)
        {
            return;
        }
        string rowName = TimelineLayout.RowName(selected);
        _rows.Children.OfType<TimelineTrack>().FirstOrDefault(track => track.Row.Name == rowName)?.BringIntoView();
    }

    private void InvalidateTracks()
    {
        foreach (TimelineTrack track in _rows.Children.OfType<TimelineTrack>())
        {
            track.InvalidateVisual();
        }
    }

    private void UpdateTimeText()
    {
        double length = Animation?.Length ?? 0;
        _scrubber.Maximum = Math.Max(length, 0.0001);
        _lengthText.Text = "/" + length.ToString("0.##", CultureInfo.CurrentCulture);
        if (!_timeBox.IsFocused)
        {
            _timeBox.Text = CurrentTime.ToString("0.###", CultureInfo.CurrentCulture);
        }
    }

    private void CommitTimeBox()
    {
        if (double.TryParse(_timeBox.Text, NumberStyles.Float, CultureInfo.CurrentCulture, out double time))
        {
            CurrentTime = time;
        }
        UpdateTimeText();
    }
}

/// <summary>
/// One timeline row drawn in <see cref="Render"/>: a background, tick marks, the interpolation curve
/// between state keyframes, the keyframe markers (diamond: state, circle: event, bar: sub-animation),
/// and the current-time line. Hovering a marker highlights its keyframe in the list and clicking
/// selects it.
/// </summary>
internal sealed class TimelineTrack : Control
{
    private const double MajorTickSeconds = 1;
    private const double MinorTickSeconds = 0.25;

    private static readonly IBrush TrackBackground = new SolidColorBrush(Color.FromArgb(24, 128, 128, 128));
    private static readonly IPen MinorTickPen = new Pen(new SolidColorBrush(Color.FromArgb(64, 128, 128, 128)), 1);
    private static readonly IPen MajorTickPen = new Pen(new SolidColorBrush(Color.FromArgb(160, 128, 128, 128)), 1);
    private static readonly IPen NowPen = new Pen(new SolidColorBrush(Color.Parse("#c3e3ed")), 2);
    private static readonly IPen CurvePen = new Pen(new SolidColorBrush(Color.Parse("#3a8fd6")), 1);
    private static readonly IBrush CurveFill = new SolidColorBrush(Color.FromArgb(80, 58, 143, 214));
    private static readonly IBrush Deselected = new SolidColorBrush(Color.Parse("#2a5c8a"));
    private static readonly IBrush Selected = new SolidColorBrush(Color.Parse("#8cc8f5"));
    private static readonly IBrush Hovered = new SolidColorBrush(Color.Parse("#b9defa"));
    private static readonly IPen MarkerOutline = new Pen(Brushes.Black, 1);

    private readonly TimelineView _owner;
    private readonly bool _isSubAnimationRow;
    private AnimatedKeyframeViewModel? _hovered;

    public TimelineTrack(TimelineView owner, TimelineRow row, bool isSubAnimationRow)
    {
        _owner = owner;
        Row = row;
        _isSubAnimationRow = isSubAnimationRow;
        Height = 22;
        Cursor = new Cursor(StandardCursorType.Hand);
    }

    public TimelineRow Row { get; }

    public override void Render(DrawingContext context)
    {
        double width = Bounds.Width;
        double height = Bounds.Height;
        double length = _owner.Animation?.Length ?? 0;
        context.FillRectangle(TrackBackground, new Rect(0, 0, width, height));
        if (width <= 0 || height <= 0 || length <= 0)
        {
            return;
        }

        foreach (double x in TimelineLayout.TickPositions(length, MinorTickSeconds, width))
        {
            context.DrawLine(MinorTickPen, new Point(x, 0), new Point(x, height));
        }
        foreach (double x in TimelineLayout.TickPositions(length, MajorTickSeconds, width))
        {
            context.DrawLine(MajorTickPen, new Point(x, 0), new Point(x, height));
        }

        if (!_isSubAnimationRow)
        {
            foreach (InterpolationSegment segment in InterpolationCurve.Segments(Row.Items, length, width, height, _owner.ClampInterpolationVisuals))
            {
                context.DrawGeometry(CurveFill, null, CreateCurve(segment, height, closed: true));
                context.DrawGeometry(null, CurvePen, CreateCurve(segment, height, closed: false));
            }
        }

        foreach (AnimatedKeyframeViewModel keyframe in Row.Items)
        {
            DrawMarker(context, keyframe, length, width, height);
        }

        if (_owner.CurrentTime >= 0 && _owner.CurrentTime <= length)
        {
            double x = TimelineLayout.TimeToX(_owner.CurrentTime, length, width);
            context.DrawLine(NowPen, new Point(x, 0), new Point(x, height));
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        SetHovered(KeyframeAt(e.GetPosition(this)));
    }

    protected override void OnPointerExited(PointerEventArgs e)
    {
        base.OnPointerExited(e);
        SetHovered(null);
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        if (KeyframeAt(e.GetPosition(this)) is { } keyframe)
        {
            _owner.Select(keyframe);
            e.Handled = true;
        }
    }

    private void SetHovered(AnimatedKeyframeViewModel? keyframe)
    {
        if (keyframe == _hovered)
        {
            return;
        }
        if (_hovered != null)
        {
            _hovered.IsTimelineVisualHovered = false;
        }
        _hovered = keyframe;
        if (_hovered != null)
        {
            _hovered.IsTimelineVisualHovered = true;
        }
        ToolTip.SetTip(this, _hovered?.DisplayName);
        InvalidateVisual();
    }

    private AnimatedKeyframeViewModel? KeyframeAt(Point point)
    {
        double length = _owner.Animation?.Length ?? 0;
        for (int i = Row.Items.Count - 1; i >= 0; i--)
        {
            AnimatedKeyframeViewModel keyframe = Row.Items[i];
            if (MarkerBounds(keyframe, length, Bounds.Width, Bounds.Height).Inflate(2).Contains(point))
            {
                return keyframe;
            }
        }
        return null;
    }

    private Rect MarkerBounds(AnimatedKeyframeViewModel keyframe, double length, double width, double height)
    {
        double size = height * 0.6;
        double top = (height - size) / 2;
        if (_isSubAnimationRow)
        {
            double left = TimelineLayout.TimeToX(keyframe.Time, length, width);
            return new Rect(left, top, TimelineLayout.LengthToWidth(keyframe.Length, length, width), size);
        }
        double centerLeft = TimelineLayout.CenteredLeft(keyframe.Time, length, width, size);
        return new Rect(centerLeft, top, size, size);
    }

    private void DrawMarker(DrawingContext context, AnimatedKeyframeViewModel keyframe, double length, double width, double height)
    {
        IBrush fill = keyframe == _owner.Animation?.SelectedKeyframe ? Selected
            : keyframe.IsTimelineVisualHovered ? Hovered
            : Deselected;
        Rect bounds = MarkerBounds(keyframe, length, width, height);

        if (_isSubAnimationRow)
        {
            context.DrawRectangle(fill, MarkerOutline, bounds);
        }
        else if (!string.IsNullOrEmpty(keyframe.EventName))
        {
            context.DrawEllipse(fill, MarkerOutline, bounds);
        }
        else
        {
            // A diamond: the square rotated 45 degrees.
            Point center = bounds.Center;
            double half = bounds.Width / 2;
            StreamGeometry diamond = new StreamGeometry();
            using (StreamGeometryContext geometry = diamond.Open())
            {
                geometry.BeginFigure(new Point(center.X, center.Y - half), isFilled: true);
                geometry.LineTo(new Point(center.X + half, center.Y));
                geometry.LineTo(new Point(center.X, center.Y + half));
                geometry.LineTo(new Point(center.X - half, center.Y));
                geometry.EndFigure(isClosed: true);
            }
            context.DrawGeometry(fill, MarkerOutline, diamond);
        }
    }

    private static StreamGeometry CreateCurve(InterpolationSegment segment, double height, bool closed)
    {
        StreamGeometry curve = new StreamGeometry();
        using (StreamGeometryContext geometry = curve.Open())
        {
            geometry.BeginFigure(new Point(segment.Points[0].X, segment.Points[0].Y), isFilled: closed);
            for (int i = 1; i < segment.Points.Count; i++)
            {
                geometry.LineTo(new Point(segment.Points[i].X, segment.Points[i].Y));
            }
            if (closed)
            {
                geometry.LineTo(new Point(segment.EndX, height));
                geometry.LineTo(new Point(segment.StartX, height));
            }
            geometry.EndFigure(isClosed: closed);
        }
        return curve;
    }
}
