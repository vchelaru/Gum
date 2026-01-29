using StateAnimationPlugin.ViewModels;
using System;
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
    const string DefaultCategoryName = "Default";

    public TimelineControl()
    {
        InitializeComponent();
        Loaded += (_, __) => WireKeyframeTracking();
    }

    #region Dependency Properties

    public DependencyProperty ClampInterpolationVisualsProperty =
        InterpolationTrackControl.ClampInterpolationVisualsProperty.AddOwner(typeof(TimelineControl));

    public bool ClampInterpolationVisuals
    {
        get => (bool)GetValue(ClampInterpolationVisualsProperty);
        set => SetValue(ClampInterpolationVisualsProperty, value);
    }

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
        ctl.RebuildStateEventRows();

        if (e.OldValue is AnimationViewModel old)
        {
            old.PropertyChanged -= ctl.OnAnimationPropertyChanged;
        }

        if (e.NewValue is AnimationViewModel newAnimation)
        {
            newAnimation.PropertyChanged += ctl.OnAnimationPropertyChanged;
        }
    }

    private void OnAnimationPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AnimationViewModel.SelectedKeyframe) && sender is AnimationViewModel
            {
                SelectedKeyframe: { } frame
            })
        {
            FrameworkElement? row = null;
            if (SubRows.FirstOrDefault(r => r.Name == RowName(frame)) is { } subRow)
            {
                row = SubAnimationsItemsControl.ItemContainerGenerator.ContainerFromItem(subRow) as FrameworkElement;
            }
            else if (StateEventRows.FirstOrDefault(r => r.Name == RowName(frame)) is { } kfRow)
            {
                row = StateEventItemsControl.ItemContainerGenerator.ContainerFromItem(kfRow) as FrameworkElement;
            }
            row?.BringIntoView();
        }
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

    public ObservableCollection<KeyframeGroupRow> StateEventRows { get; } = new();

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

    private void RebuildStateEventRows()
    {
        StateEventRows.Clear();

        if (Animation?.Keyframes is not { } frames)
        {
            return;
        }

        

        // Only state + event keyframes
        var stateEventFrames = frames
            .Where(k => !string.IsNullOrEmpty(k.StateName) ||
                        !string.IsNullOrEmpty(k.EventName));

        var grouped = stateEventFrames
            .GroupBy(RowName)
            .OrderBy(g => g.Key == DefaultCategoryName ? 0 : 1)
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var group in grouped)
        {
            var ordered = group.OrderBy(k => k.Time).ToList();
            StateEventRows.Add(new KeyframeGroupRow(group.Key, ordered));
        }

        
    }

    static string RowName(AnimatedKeyframeViewModel kf)
    {
        if (kf is { AnimationName.Length: > 0 })
        {
            return kf.AnimationName;
        }
        return kf.DisplayName.IndexOf('/') is var i and > 0
            ? kf.DisplayName.Substring(0, i)
            : DefaultCategoryName;
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
        RebuildStateEventRows();
    }

    private void OnKeyframePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(AnimatedKeyframeViewModel.AnimationName) ||
            e.PropertyName is nameof(AnimatedKeyframeViewModel.StateName) ||
            e.PropertyName is nameof(AnimatedKeyframeViewModel.EventName) ||
            e.PropertyName is nameof(AnimatedKeyframeViewModel.Time) ||
            e.PropertyName is nameof(AnimatedKeyframeViewModel.Length))
        {
            // name/time changes can move items between rows or within them
            RebuildSubRows();
            RebuildStateEventRows();
        }
    }

    private ObservableCollection<AnimatedKeyframeViewModel>? _currentHooked;


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

    public class KeyframeGroupRow
    {
        public string Name { get; }
        public IReadOnlyList<AnimatedKeyframeViewModel> Items { get; }

        public KeyframeGroupRow(string name, IReadOnlyList<AnimatedKeyframeViewModel> items)
        {
            Name = name;
            Items = items;
        }
    }

    private void Keyframe_MouseDown(object? sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: AnimatedKeyframeViewModel frame } && Animation is { } animation &&
            animation.Keyframes.Contains(frame))
        {
            animation.SelectedKeyframe = frame;
        }
    }

    private void OnKeyframeMouseEnter(object? sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: AnimatedKeyframeViewModel frame })
        {
            frame.IsTimelineVisualHovered = true;
        }
    }

    private void OnKeyframeMouseLeave(object? sender, MouseEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: AnimatedKeyframeViewModel frame })
        {
            frame.IsTimelineVisualHovered = false;
        }
    }
}
