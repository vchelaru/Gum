using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;
using FlatRedBall.SpecializedXnaControls;
using Gum.Avalonia.Canvas;
using Gum.Avalonia.Services;
using Gum.Input;
using TextureCoordinateSelectionPlugin.Models;
using TextureCoordinateSelectionPlugin.ViewModels;
using TextureCoordinateSelectionPlugin.Views;

namespace Gum.Avalonia.Plugins.TextureCoordinates;

/// <summary>
/// The Avalonia texture-coordinates tab: a code-built toolbar over the shared
/// <see cref="MainControlViewModel"/>, the region-selection canvas, and its scroll bars.
/// </summary>
public sealed class TextureCoordinateView : DockPanel, ITextureCoordinateView
{
    private readonly ImageRegionCanvasControl _canvasControl;
    private readonly Button _minusButton;
    private readonly Button _plusButton;

    /// <summary>Builds the view.</summary>
    public TextureCoordinateView()
    {
        _canvasControl = new ImageRegionCanvasControl();
        // we are going to do our own handling of events
        _canvasControl.Core.DisableHotkeyPanning();
        _canvasControl.SizeChanged += (_, _) => CanvasResized?.Invoke();
        _canvasControl.AddHandler(KeyDownEvent, HandleCanvasKeyDown, global::Avalonia.Interactivity.RoutingStrategies.Tunnel);

        ScrollBar vertical = new ScrollBar { Orientation = Orientation.Vertical, AllowAutoHide = false };
        ScrollBar horizontal = new ScrollBar { Orientation = Orientation.Horizontal, AllowAutoHide = false };
        VerticalScrollBar = new AvaloniaCameraScrollBar(vertical);
        HorizontalScrollBar = new AvaloniaCameraScrollBar(horizontal);

        _minusButton = ToolButton("-");
        _minusButton.Click += (_, _) => (DataContext as MainControlViewModel)?.ZoomOut();
        _plusButton = ToolButton("+");
        _plusButton.Click += (_, _) => (DataContext as MainControlViewModel)?.ZoomIn();

        StackPanel toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 4, Margin = new Thickness(4, 2) };
        toolbar.Children.Add(_minusButton);
        toolbar.Children.Add(new ComboBox
        {
            MinWidth = 70,
            [!ItemsControl.ItemsSourceProperty] = new Binding(nameof(MainControlViewModel.AvailableZoomLevels)),
            [!SelectingItemsControl.SelectedItemProperty] = new Binding(nameof(MainControlViewModel.SelectedZoomLevel)) { Mode = BindingMode.TwoWay },
        });
        toolbar.Children.Add(_plusButton);
        toolbar.Children.Add(new CheckBox
        {
            Content = "Snap to grid",
            Margin = new Thickness(16, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            [!ToggleButton.IsCheckedProperty] = new Binding(nameof(MainControlViewModel.IsSnapToGridChecked)) { Mode = BindingMode.TwoWay },
        });
        toolbar.Children.Add(new ComboBox
        {
            Width = 100,
            Margin = new Thickness(10, 0, 0, 0),
            [!InputElement.IsEnabledProperty] = new Binding(nameof(MainControlViewModel.IsSnapToGridComboBoxEnabled)),
            [!ItemsControl.ItemsSourceProperty] = new Binding(nameof(MainControlViewModel.AvailableSnapToGridValues)),
            [!SelectingItemsControl.SelectedItemProperty] = new Binding(nameof(MainControlViewModel.SelectedSnapToGridValue)) { Mode = BindingMode.TwoWay },
        });
        toolbar.Children.Add(new ComboBox
        {
            MinWidth = 80,
            Margin = new Thickness(16, 0, 0, 0),
            DisplayMemberBinding = new Binding(nameof(ExposedTextureCoordinateSet.SourceObjectName)),
            [!Visual.IsVisibleProperty] = new Binding(nameof(MainControlViewModel.IsExposedSourceDropdownVisible)),
            [!ItemsControl.ItemsSourceProperty] = new Binding(nameof(MainControlViewModel.AvailableExposedSources)),
            [!SelectingItemsControl.SelectedItemProperty] = new Binding(nameof(MainControlViewModel.SelectedExposedSource)) { Mode = BindingMode.TwoWay },
        });

        Grid canvasGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            RowDefinitions = new RowDefinitions("*,Auto"),
        };
        canvasGrid.Children.Add(_canvasControl);
        Grid.SetColumn(vertical, 1);
        canvasGrid.Children.Add(vertical);
        Grid.SetRow(horizontal, 1);
        canvasGrid.Children.Add(horizontal);

        SetDock(toolbar, global::Avalonia.Controls.Dock.Top);
        Children.Add(toolbar);
        Children.Add(canvasGrid);
    }

    /// <inheritdoc/>
    public ImageRegionSelectionCore Canvas => _canvasControl.Core;

    /// <inheritdoc/>
    public object View => this;

    /// <inheritdoc/>
    public ICameraScrollBar VerticalScrollBar { get; }

    /// <inheritdoc/>
    public ICameraScrollBar HorizontalScrollBar { get; }

    /// <inheritdoc/>
    public event Action? CanvasResized;

    /// <inheritdoc/>
    public new event Action<GumKeyEventArgs>? KeyDown;

    /// <inheritdoc/>
    public void InvokeWhenLoaded(Action action) => Dispatcher.UIThread.Post(action, DispatcherPriority.Loaded);

    /// <inheritdoc/>
    public void UpdateButtonSizes(double baseFontSize)
    {
        const double defaultBaseFontSize = 12.0;
        const double defaultMinWidth = 24.0;
        double minWidth = defaultMinWidth * baseFontSize / defaultBaseFontSize;
        _minusButton.MinWidth = minWidth;
        _minusButton.FontSize = baseFontSize;
        _plusButton.MinWidth = minWidth;
        _plusButton.FontSize = baseFontSize;
    }

    private void HandleCanvasKeyDown(object? sender, KeyEventArgs e)
    {
        GumKeyEventArgs keyArgs = e.ToGumKeyEventArgs();
        KeyDown?.Invoke(keyArgs);
        e.Handled = keyArgs.Handled;
    }

    private static Button ToolButton(string content) => new Button
    {
        Content = content,
        MinWidth = 24,
        Padding = new Thickness(0),
        HorizontalContentAlignment = HorizontalAlignment.Center,
    };
}

/// <summary>
/// The Avalonia texture-coordinate canvas: an <see cref="AvaloniaGraphicsDeviceControl"/> that
/// hosts an <see cref="ImageRegionSelectionCore"/> and translates its wheel and double-click input.
/// </summary>
public sealed class ImageRegionCanvasControl : AvaloniaGraphicsDeviceControl
{
    /// <summary>The framework-neutral canvas this control renders.</summary>
    public ImageRegionSelectionCore Core { get; }

    /// <summary>Creates the control and its core.</summary>
    public ImageRegionCanvasControl()
    {
        // Ctrl+= / Ctrl+- zoom this canvas's camera, not the app-wide font size.
        CameraZoomScope.SetOwnsCameraZoom(this, true);
        Core = new ImageRegionSelectionCore(this);
    }

    /// <inheritdoc/>
    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        // WPF reports 120 per notch; Avalonia reports 1.
        if (Core.HandleMouseWheel((int)(e.Delta.Y * 120)))
        {
            e.Handled = true;
        }
    }

    /// <inheritdoc/>
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        PointerPoint point = e.GetCurrentPoint(this);
        if (point.Properties.IsLeftButtonPressed && e.ClickCount == 2)
        {
            Core.RaiseDoubleClick();
        }
    }

    /// <inheritdoc/>
    protected override void Draw() => Core.Draw();
}

/// <summary>Adapts an Avalonia <see cref="ScrollBar"/> to <see cref="ICameraScrollBar"/>.</summary>
public sealed class AvaloniaCameraScrollBar : ICameraScrollBar
{
    private readonly ScrollBar _scrollBar;

    /// <summary>Creates the adapter over <paramref name="scrollBar"/>.</summary>
    public AvaloniaCameraScrollBar(ScrollBar scrollBar)
    {
        _scrollBar = scrollBar ?? throw new ArgumentNullException(nameof(scrollBar));
        _scrollBar.ValueChanged += (_, _) => ValueChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc/>
    public double Minimum { get => _scrollBar.Minimum; set => _scrollBar.Minimum = value; }

    /// <inheritdoc/>
    public double Maximum { get => _scrollBar.Maximum; set => _scrollBar.Maximum = value; }

    /// <inheritdoc/>
    public double ViewportSize { get => _scrollBar.ViewportSize; set => _scrollBar.ViewportSize = value; }

    /// <inheritdoc/>
    public double Value { get => _scrollBar.Value; set => _scrollBar.Value = value; }

    /// <inheritdoc/>
    public event EventHandler? ValueChanged;
}
