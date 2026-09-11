using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Gum.Avalonia.Services;
using Gum.Avalonia.Shell;
using StateAnimationPlugin;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Plugins.StateAnimation;

/// <summary>
/// The Animations tab: play/stop, game speed and the timeline on top; below, the element's
/// animations, the selected animation's keyframes, and the selected keyframe's details. Bound to an
/// <see cref="ElementAnimationsViewModel"/>, which is replaced when the selected element changes.
/// Twin of the WPF <c>MainWindow</c> (the plugin's view, not the application window).
/// </summary>
public sealed class AnimationsView : Grid
{
    private static readonly IValueConverter PlayStopText =
        new FuncValueConverter<bool, string>(isPlaying => isPlaying ? "■ Stop" : "▶ Play");

    private static readonly IValueConverter LoopText =
        new FuncValueConverter<bool, string>(loops => loops ? "∞" : "1");

    private static readonly IValueConverter HoverBackground =
        new FuncValueConverter<bool, IBrush>(hovered => hovered ? new SolidColorBrush(Color.FromArgb(60, 30, 144, 255)) : Brushes.Transparent);

    // The kind glyph: a warning for a keyframe whose state or animation is gone (issue #3386), then
    // state, sub-animation, or named event.
    private static readonly IMultiValueConverter KeyframeGlyph = new FuncMultiValueConverter<object?, string>(values =>
    {
        object?[] parts = values.ToArray();
        if (parts.Length > 0 && parts[0] is true)
        {
            return "⚠";
        }
        if (parts.Length > 1 && parts[1] is string { Length: > 0 })
        {
            return "◆";
        }
        if (parts.Length > 2 && parts[2] is string { Length: > 0 })
        {
            return "▶";
        }
        return "⚑";
    });

    // The detail column's header: "State" for a state keyframe, otherwise the keyframe's name.
    private static readonly IMultiValueConverter DetailTitle = new FuncMultiValueConverter<object?, string>(values =>
    {
        object?[] parts = values.ToArray();
        if (parts.Length < 3 || parts[0] is not AnimatedKeyframeViewModel)
        {
            return "No Keyframe Selected";
        }
        return parts[1] is string { Length: > 0 } ? "State" : parts[2] as string ?? "";
    });

    private readonly AnimationTabKeyHandler _keyHandler;
    private readonly Grid _columns;

    /// <summary>Raised when the user picks Add &gt; State; the plugin opens the state picker.</summary>
    public event Action? AddStateKeyframeRequested;

    /// <summary>Raised after a keyframe is pasted into the keyframe list.</summary>
    public event Action<AnimatedKeyframeViewModel>? KeyframePasted;

    /// <summary>Raised after the user resizes the columns, with the animation and keyframe column widths.</summary>
    public event Action<double, double>? ColumnsResized;

    /// <summary>Builds the view; <paramref name="animationColumnRatio"/> is the saved width ratio of the first two columns.</summary>
    public AnimationsView(AnimationTabKeyHandler keyHandler, double animationColumnRatio)
    {
        _keyHandler = keyHandler;
        RowDefinitions = new RowDefinitions("*,Auto,*");

        Children.Add(CreatePlaybackArea());

        GridSplitter rowSplitter = new GridSplitter { ResizeDirection = GridResizeDirection.Rows, Height = 4, HorizontalAlignment = HorizontalAlignment.Stretch };
        SetRow(rowSplitter, 1);
        Children.Add(rowSplitter);

        _columns = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions
            {
                new ColumnDefinition(new GridLength(animationColumnRatio > 0 ? animationColumnRatio : 1, GridUnitType.Star)),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
                new ColumnDefinition(GridLength.Auto),
                new ColumnDefinition(new GridLength(1, GridUnitType.Star)),
            },
        };
        _columns.Children.Add(CreateAnimationsColumn());
        _columns.Children.Add(CreateColumnSplitter(1, reportResize: true));
        Control keyframes = CreateKeyframesColumn();
        SetColumn(keyframes, 2);
        _columns.Children.Add(keyframes);
        _columns.Children.Add(CreateColumnSplitter(3, reportResize: false));
        Control details = CreateDetailColumn();
        SetColumn(details, 4);
        _columns.Children.Add(details);
        SetRow(_columns, 2);
        Children.Add(_columns);
    }

    private ElementAnimationsViewModel? ViewModel => DataContext as ElementAnimationsViewModel;

    private Control CreatePlaybackArea()
    {
        Grid area = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), RowDefinitions = new RowDefinitions("Auto,*") };

        ToggleButton play = new ToggleButton { Margin = new Thickness(8, 0, 4, 0), MinWidth = 72, HorizontalContentAlignment = HorizontalAlignment.Center };
        play.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(ElementAnimationsViewModel.IsPlaying)) { Mode = BindingMode.TwoWay });
        play.Bind(ContentControl.ContentProperty,new Binding(nameof(ElementAnimationsViewModel.IsPlaying)) { Converter = PlayStopText });
        play.Bind(IsVisibleProperty, new Binding(nameof(ElementAnimationsViewModel.SelectedAnimation)) { Converter = ObjectConverters.IsNotNull });
        area.Children.Add(play);

        StackPanel speed = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2, VerticalAlignment = VerticalAlignment.Center };
        speed.Children.Add(CreateButton("−", "Slow Down: reduces game speed (makes it run in slow-motion)", vm => vm.DecreaseGameSpeed()));
        TextBlock speedText = new TextBlock { MinWidth = 48, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        speedText.Bind(TextBlock.TextProperty, new Binding(nameof(ElementAnimationsViewModel.CurrentGameSpeed)) { FallbackValue = "100%" });
        speed.Children.Add(speedText);
        speed.Children.Add(CreateButton("+", "Speed Up: increases game speed (makes it run in fast-forward)", vm => vm.IncreaseGameSpeed()));
        SetColumn(speed, 1);
        area.Children.Add(speed);

        TimelineView timeline = new TimelineView { Margin = new Thickness(0, 4, 0, 0) };
        timeline.Bind(TimelineView.AnimationProperty, new Binding(nameof(ElementAnimationsViewModel.SelectedAnimation)));
        timeline.Bind(TimelineView.CurrentTimeProperty, new Binding(nameof(ElementAnimationsViewModel.DisplayedAnimationTime)) { Mode = BindingMode.TwoWay });
        timeline.Bind(TimelineView.ClampInterpolationVisualsProperty, new Binding(nameof(ElementAnimationsViewModel.ClampInterpolationVisuals)));
        SetRow(timeline, 1);
        SetColumnSpan(timeline, 2);
        area.Children.Add(timeline);

        return area;
    }

    private Control CreateAnimationsColumn()
    {
        TextBlock title = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
        title.Bind(TextBlock.TextProperty, new Binding(nameof(ElementAnimationsViewModel.AnimationColumnTitle)));
        Button add = CreateButton("+", "Add Animation", vm => vm.AddAnimation());

        ListBox animations = new ListBox
        {
            Padding = new Thickness(4),
            ItemTemplate = new FuncDataTemplate<AnimationViewModel>((_, _) => CreateAnimationRow()),
        };
        animations.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(ElementAnimationsViewModel.Animations)));
        animations.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(ElementAnimationsViewModel.SelectedAnimation)) { Mode = BindingMode.TwoWay });
        animations.ContextMenu = AvaloniaContextMenus.CreateRebuildingMenu(() => ViewModel?.AnimationRightClickItems);
        animations.KeyDown += (_, e) =>
        {
            if (ViewModel is { } viewModel && _keyHandler.HandleAnimationListKey(e.ToGumKeyEventArgs(), viewModel))
            {
                e.Handled = true;
            }
        };

        return CreateColumn(title, add, animations);
    }

    private static Control CreateAnimationRow()
    {
        StackPanel row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };

        ToggleButton loops = new ToggleButton { MinWidth = 26, Padding = new Thickness(4, 0), HorizontalContentAlignment = HorizontalAlignment.Center };
        loops.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(AnimationViewModel.Loops)) { Mode = BindingMode.TwoWay });
        loops.Bind(ContentControl.ContentProperty,new Binding(nameof(AnimationViewModel.Loops)) { Converter = LoopText });
        loops.Bind(ToolTip.TipProperty, new Binding(nameof(AnimationViewModel.Loops)) { Converter = new FuncValueConverter<bool, string>(value => value ? "Loops forever" : "Plays once") });
        row.Children.Add(loops);

        TextBlock broken = new TextBlock { Text = "⚠", Foreground = Brushes.OrangeRed, VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(broken, "This animation contains a keyframe that references a missing state or animation.");
        broken.Bind(IsVisibleProperty, new Binding(nameof(AnimationViewModel.HasBrokenKeyframe)));
        row.Children.Add(broken);

        TextBlock name = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        name.Bind(TextBlock.TextProperty, new Binding(nameof(AnimationViewModel.Name)));
        row.Children.Add(name);
        return row;
    }

    private Control CreateKeyframesColumn()
    {
        TextBlock title = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
        title.Bind(TextBlock.TextProperty, new Binding("SelectedAnimation.Name") { FallbackValue = "No Animation Selected", TargetNullValue = "No Animation Selected" });

        Button add = new Button { Content = "+", Padding = new Thickness(6, 0) };
        ToolTip.SetTip(add, "Add a keyframe");
        MenuFlyout addMenu = new MenuFlyout();
        addMenu.Items.Add(CreateMenuItem("State", () => AddStateKeyframeRequested?.Invoke()));
        addMenu.Items.Add(CreateMenuItem("Sub-Animation", () => ViewModel?.AddSubAnimation()));
        addMenu.Items.Add(CreateMenuItem("Named Event", () => ViewModel?.AddNamedEvent()));
        add.Flyout = addMenu;

        ListBox keyframes = new ListBox
        {
            Padding = new Thickness(4),
            ItemTemplate = new FuncDataTemplate<AnimatedKeyframeViewModel>((_, _) => CreateKeyframeRow()),
        };
        keyframes.Bind(ItemsControl.ItemsSourceProperty, new Binding("SelectedAnimation.Keyframes"));
        keyframes.Bind(SelectingItemsControl.SelectedItemProperty, new Binding("SelectedAnimation.SelectedKeyframe") { Mode = BindingMode.TwoWay });
        keyframes.ContextMenu = AvaloniaContextMenus.CreateRebuildingMenu(() => ViewModel?.AnimationStateRightClickItems);
        keyframes.KeyDown += (_, e) =>
        {
            if (ViewModel is { } viewModel && _keyHandler.HandleKeyframeListKey(e.ToGumKeyEventArgs(), viewModel) is { } pasted)
            {
                KeyframePasted?.Invoke(pasted);
            }
        };

        return CreateColumn(title, add, keyframes);
    }

    private static Control CreateKeyframeRow()
    {
        StackPanel row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Background = Brushes.Transparent };
        row.Bind(Panel.BackgroundProperty, new Binding(nameof(AnimatedKeyframeViewModel.IsTimelineVisualHovered)) { Converter = HoverBackground });

        TextBlock glyph = new TextBlock { MinWidth = 14, VerticalAlignment = VerticalAlignment.Center };
        glyph.Bind(TextBlock.TextProperty, new MultiBinding
        {
            Converter = KeyframeGlyph,
            Bindings =
            {
                new Binding(nameof(AnimatedKeyframeViewModel.IsMissingReference)),
                new Binding(nameof(AnimatedKeyframeViewModel.StateName)),
                new Binding(nameof(AnimatedKeyframeViewModel.AnimationName)),
            },
        });
        glyph.Bind(TextBlock.ForegroundProperty, new Binding(nameof(AnimatedKeyframeViewModel.IsMissingReference)) { Converter = ViewConvertersForAnimations.MissingForeground });
        glyph.Bind(ToolTip.TipProperty, new Binding(nameof(AnimatedKeyframeViewModel.IsMissingReference))
        {
            Converter = new FuncValueConverter<bool, string?>(missing => missing ? "This keyframe references a state or animation that no longer exists." : null),
        });
        row.Children.Add(glyph);

        TextBlock uncategorized = new TextBlock { Text = "!", Foreground = Brushes.Orange, FontWeight = FontWeight.Bold, VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(uncategorized, "This state is uncategorized which can cause unexpected results due to only some variables being assigned. Consider using only categorized states.");
        uncategorized.Bind(IsVisibleProperty, new Binding(nameof(AnimatedKeyframeViewModel.IsUncategorized)));
        row.Children.Add(uncategorized);

        TextBlock text = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
        text.Bind(TextBlock.TextProperty, new Binding(nameof(AnimatedKeyframeViewModel.DisplayString)));
        row.Children.Add(text);

        // Hovering a row highlights its marker on the timeline, and the reverse.
        row.PointerEntered += (_, _) => SetHovered(row, true);
        row.PointerExited += (_, _) => SetHovered(row, false);
        return row;
    }

    private static void SetHovered(Control row, bool hovered)
    {
        if (row.DataContext is AnimatedKeyframeViewModel keyframe)
        {
            keyframe.IsTimelineVisualHovered = hovered;
        }
    }

    private static Control CreateDetailColumn()
    {
        TextBlock title = new TextBlock { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) };
        title.Bind(TextBlock.TextProperty, new MultiBinding
        {
            Converter = DetailTitle,
            Bindings =
            {
                new Binding("SelectedAnimation.SelectedKeyframe"),
                new Binding("SelectedAnimation.SelectedKeyframe.StateName"),
                new Binding("SelectedAnimation.SelectedKeyframe.DisplayName"),
            },
        });

        KeyframeDetailView detail = new KeyframeDetailView();
        detail.Bind(DataContextProperty, new Binding("SelectedAnimation.SelectedKeyframe"));
        detail.Bind(IsVisibleProperty, new Binding("SelectedAnimation.SelectedKeyframe") { Converter = ObjectConverters.IsNotNull });

        return CreateColumn(title, action: null, detail);
    }

    private static Control CreateColumn(Control title, Control? action, Control body)
    {
        DockPanel column = new DockPanel();
        Grid header = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), MinHeight = 28, Background = new SolidColorBrush(Color.FromArgb(40, 128, 128, 128)) };
        header.Children.Add(title);
        if (action != null)
        {
            SetColumn(action, 1);
            header.Children.Add(action);
        }
        DockPanel.SetDock(header, Dock.Top);
        column.Children.Add(header);
        column.Children.Add(body);
        return column;
    }

    private GridSplitter CreateColumnSplitter(int column, bool reportResize)
    {
        GridSplitter splitter = new GridSplitter { ResizeDirection = GridResizeDirection.Columns, Width = 4, VerticalAlignment = VerticalAlignment.Stretch };
        SetColumn(splitter, column);
        if (reportResize)
        {
            splitter.DragCompleted += (_, _) =>
                ColumnsResized?.Invoke(_columns.ColumnDefinitions[0].ActualWidth, _columns.ColumnDefinitions[2].ActualWidth);
        }
        return splitter;
    }

    private Button CreateButton(string content, string tip, Action<ElementAnimationsViewModel> action)
    {
        Button button = new Button { Content = content, Padding = new Thickness(6, 0), VerticalAlignment = VerticalAlignment.Center };
        ToolTip.SetTip(button, tip);
        button.Click += (_, _) =>
        {
            if (ViewModel is { } viewModel)
            {
                action(viewModel);
            }
        };
        return button;
    }

    private static MenuItem CreateMenuItem(string header, Action action)
    {
        MenuItem item = new MenuItem { Header = header };
        item.Click += (_, _) => action();
        return item;
    }
}

/// <summary>Converters the Animations tab shares between its views.</summary>
internal static class ViewConvertersForAnimations
{
    /// <summary>Error-colored text for a keyframe that references something missing; unset otherwise.</summary>
    public static readonly IValueConverter MissingForeground =
        new FuncValueConverter<bool, object>(missing => missing ? Brushes.OrangeRed : AvaloniaProperty.UnsetValue);
}
