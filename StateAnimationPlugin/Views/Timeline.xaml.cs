using StateAnimationPlugin.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace StateAnimationPlugin.Controls;

public partial class TimelineControl : UserControl
{
    public TimelineControl()
    {
        InitializeComponent();
        Loaded += (_, __) => WireKeyframeTracking();
    }

    #region Dependency Properties
    public AnimationViewModel? Animation
    {
        get => (AnimationViewModel?)GetValue(AnimationProperty);
        set => SetValue(AnimationProperty, value);
    }

    public static readonly DependencyProperty AnimationProperty =
        DependencyProperty.Register(nameof(Animation), typeof(AnimationViewModel), typeof(TimelineControl),
            new PropertyMetadata(null, OnAnimationChanged));

    private static void OnAnimationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctl = (TimelineControl)d;
        ctl.WireKeyframeTracking();
        ctl.RebuildSubRows();
        ctl.RefreshStateEventView(); // reapply filter
    }

    public static readonly DependencyProperty CurrentTimeProperty =
        DependencyProperty.Register(nameof(CurrentTime), typeof(double), typeof(TimelineControl),
            new PropertyMetadata(0.0));

    public double CurrentTime
    {
        get => (double)GetValue(CurrentTimeProperty);
        set => SetValue(CurrentTimeProperty, value);
    }
    #endregion

    public ObservableCollection<SubRow> SubRows { get; } = new();

    private void RebuildSubRows()
    {
        SubRows.Clear();

        var frames = Animation?.Keyframes;
        if (frames is null) return;

        foreach (var k in frames.Where(k => !string.IsNullOrEmpty(k.AnimationName))
                     .OrderBy(k => k.Time))
        {
            SubRows.Add(new SubRow(k.AnimationName!, [k]));
        }
    }

    private void WireKeyframeTracking()
    {
        // detach previous
        if (_currentHooked != null)
        {
            _currentHooked.CollectionChanged -= OnKeyframesChanged;
            foreach (var k in _currentHooked.OfType<INotifyPropertyChanged>())
                k.PropertyChanged -= OnKeyframePropertyChanged;
        }

        _currentHooked = Animation?.Keyframes;

        if (_currentHooked != null)
        {
            _currentHooked.CollectionChanged += OnKeyframesChanged;
            foreach (var k in _currentHooked.OfType<INotifyPropertyChanged>())
                k.PropertyChanged += OnKeyframePropertyChanged;
        }
    }

    private void OnKeyframesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (var k in e.NewItems.OfType<INotifyPropertyChanged>())
                k.PropertyChanged += OnKeyframePropertyChanged;
        }
        if (e.OldItems != null)
        {
            foreach (var k in e.OldItems.OfType<INotifyPropertyChanged>())
                k.PropertyChanged -= OnKeyframePropertyChanged;
        }

        RebuildSubRows();
        RefreshStateEventView();
    }

    private void OnKeyframePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AnimatedKeyframeViewModel.AnimationName) ||
            e.PropertyName is nameof(AnimatedKeyframeViewModel.StateName) ||
            e.PropertyName is nameof(AnimatedKeyframeViewModel.EventName) ||
            e.PropertyName is nameof(AnimatedKeyframeViewModel.Time) ||
            e.PropertyName is nameof(AnimatedKeyframeViewModel.Length))
        {
            // animation name changes can move items between rows
            RebuildSubRows();
            RefreshStateEventView();
        }
    }

    private void RefreshStateEventView()
    {
        if (Resources["StateEventView"] is CollectionViewSource cvs)
        {
            cvs.View?.Refresh();
        }
    }

    private ObservableCollection<AnimatedKeyframeViewModel>? _currentHooked;

    // XAML Filter handler (for the State+Event row)
    private void OnStateEventFilter(object sender, FilterEventArgs e)
    {
        if (e.Item is AnimatedKeyframeViewModel kf)
        {
            e.Accepted = !string.IsNullOrEmpty(kf.StateName) || !string.IsNullOrEmpty(kf.EventName);
        }
        else e.Accepted = false;
    }

    public class SubRow
    {
        public string Name { get; }
        public IReadOnlyList<AnimatedKeyframeViewModel> Items { get; }
        public SubRow(string name, IReadOnlyList<AnimatedKeyframeViewModel> items)
        {
            Name = name;
            Items = items;
        }
    }

    private void Keyframe_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: AnimatedKeyframeViewModel frame } && Animation is { } animation &&
            animation.Keyframes.Contains(frame))
        {
            animation.SelectedKeyframe = frame;
        }
    }

    private void OnKeyframeMouseEnter(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: AnimatedKeyframeViewModel frame })
        {
            frame.IsTimelineVisualHovered = true;
        }
    }

    private void OnKeyframeMouseLeave(object sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: AnimatedKeyframeViewModel frame })
        {
            frame.IsTimelineVisualHovered = false;
        }
    }
}
