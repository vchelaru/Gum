using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using EditorTabPlugin_XNA.ViewModels;
using Gum.Avalonia.Plugins.TreeView;
using Gum.Avalonia.Themes;
using Gum.DataTypes;

namespace Gum.Avalonia.Plugins.EditorTab;

/// <summary>
/// The editor tab's toolbar: the zoom, canvas size, font scale, and grid-snap controls, plus the
/// icon-only Preview button anchored at the far right (issue #4697) so it reads as a run control,
/// not a view setting. Binds to the <see cref="EditorViewModel"/> in its data context.
/// </summary>
internal sealed class EditorToolbar : DockPanel
{
    private const double DefaultButtonWidth = 20;
    private const string PreviewIdleGlyph = "▶";

    /// <summary>The grid sizes offered in the toolbar's Grid Size drop-down; any other size can still be typed.</summary>
    internal static readonly IReadOnlyList<int> GridSizePresets = new[] { 8, 16, 32, 64 };

    private readonly List<Button> _sizedButtons;

    public EditorToolbar()
    {
        _sizedButtons = new List<Button>();
        LastChildFill = true;

        StackPanel panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 0, Margin = new Thickness(4, 2) };

        panel.Children.Add(SizedButton("-", nameof(EditorViewModel.ZoomOutCommand)));
        panel.Children.Add(Scaled(new ComboBox
        {
            DisplayMemberBinding = new Binding(nameof(ZoomLevel.ZoomDisplay)),
            [!ItemsControl.ItemsSourceProperty] = new Binding(nameof(EditorViewModel.ZoomLevels)),
            [!SelectingItemsControl.SelectedItemProperty] = new Binding(nameof(EditorViewModel.PercentZoomLevel)) { Mode = BindingMode.TwoWay },
        }, width: 100));
        panel.Children.Add(SizedButton("+", nameof(EditorViewModel.ZoomInCommand)));

        panel.Children.Add(Scaled(new ComboBox
        {
            DisplayMemberBinding = new Binding(nameof(CustomCanvasSize.FriendlyName)),
            [!ItemsControl.ItemsSourceProperty] = new Binding(nameof(EditorViewModel.CustomCanvasSizes)),
            [!SelectingItemsControl.SelectedItemProperty] = new Binding(nameof(EditorViewModel.SelectedCustomCanvasSize)) { Mode = BindingMode.TwoWay },
        }, width: 180, marginLeft: 10));

        panel.Children.Add(GlyphLabel("Aa", "Font Scale", 20));
        panel.Children.Add(SizedButton("-", nameof(EditorViewModel.FontScaleDecreaseCommand)));
        panel.Children.Add(Scaled(new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            [!TextBlock.TextProperty] = new Binding(nameof(EditorViewModel.GlobalFontScaleDisplay)),
        }, width: 40));
        panel.Children.Add(SizedButton("+", nameof(EditorViewModel.FontScaleIncreaseCommand)));

        panel.Children.Add(Scaled(new CheckBox
        {
            Content = "Snap to Grid",
            VerticalAlignment = VerticalAlignment.Center,
            [!ToggleButton.IsCheckedProperty] = new Binding(nameof(EditorViewModel.SnapToGrid)) { Mode = BindingMode.TwoWay },
        }, marginLeft: 20));
        panel.Children.Add(GlyphLabel("▦", "Grid Size", 10));
        panel.Children.Add(Scaled(new ComboBox
        {
            IsEditable = true,
            VerticalAlignment = VerticalAlignment.Center,
            ItemsSource = GridSizePresets,
            [!ComboBox.TextProperty] = new Binding(nameof(EditorViewModel.GridSize)) { Mode = BindingMode.TwoWay },
        }, width: 80));

        RotateTransform previewSpinnerRotation = new RotateTransform();
        TextBlock previewIcon = new TextBlock
        {
            Text = PreviewIdleGlyph,
            TextAlignment = TextAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        // A dashed-stroke ring, not a solid circle: rotating a solid circle is a visual no-op, but
        // the gaps in the dash pattern give the rotation something asymmetric to actually show
        // (issue #4717). Explicit Center alignment (rather than relying on Panel's default Stretch)
        // keeps it pinned to the same spot the ▶ glyph occupies, so swapping between the two doesn't
        // shift position.
        Ellipse previewSpinner = new Ellipse
        {
            StrokeThickness = 2,
            Stroke = Brushes.White,
            StrokeDashArray = new AvaloniaList<double> { 2, 1.5 },
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            RenderTransform = previewSpinnerRotation,
            RenderTransformOrigin = RelativePoint.Center,
            IsVisible = false,
        };
        AvaloniaTreeIcons.FollowBaseFontSize(previewSpinner, 12);
        Panel previewIconHost = new Panel();
        previewIconHost.Children.Add(previewIcon);
        previewIconHost.Children.Add(previewSpinner);

        PreviewButton = new Button
        {
            Classes = { GumChromeStyles.FlatButtonClass },
            Content = previewIconHost,
            Margin = new Thickness(0, 0, 4, 0),
            Padding = new Thickness(6, 0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            [!Button.CommandProperty] = new Binding(nameof(EditorViewModel.PreviewCommand)),
            [ToolTip.TipProperty] = "Preview in runtime",
        };
        PreviewButton.Click += (_, _) => ShowPreviewLaunchSpinner(previewIcon, previewSpinner, previewSpinnerRotation);
        SetDock(PreviewButton, global::Avalonia.Controls.Dock.Right);

        PinPreviewButton = new ToggleButton
        {
            Content = CreatePinIcon(),
            Margin = new Thickness(0, 0, 4, 0),
            Padding = new Thickness(6, 0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            [!IsVisibleProperty] = new Binding(nameof(EditorViewModel.IsPreviewRunning)),
            [!ToggleButton.IsCheckedProperty] = new Binding(nameof(EditorViewModel.IsPreviewPinned)) { Mode = BindingMode.TwoWay },
            [ToolTip.TipProperty] = "Pin the preview to its current element so it stops following the selection",
        };
        SetDock(PinPreviewButton, global::Avalonia.Controls.Dock.Right);

        Children.Add(PreviewButton);
        Children.Add(PinPreviewButton);
        WatchForPreviewClosing();
        // The controls scroll horizontally in the space beside the Preview button rather than
        // drawing over it when the window is narrow (#5695).
        Children.Add(new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = panel,
        });
    }

    // The tool isn't told when the preview window closes, so look while the toolbar is showing.
    private void WatchForPreviewClosing()
    {
        DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        timer.Tick += (_, _) => (DataContext as EditorViewModel)?.RefreshPreviewRunning();
        AttachedToVisualTree += (_, _) => timer.Start();
        DetachedFromVisualTree += (_, _) => timer.Stop();
    }

    /// <summary>The "Preview in runtime" button at the right end.</summary>
    internal Button PreviewButton { get; }

    /// <summary>The toggle beside <see cref="PreviewButton"/> that pins the preview to its element.</summary>
    internal ToggleButton PinPreviewButton { get; }

    /// <summary>The zoom and font scale +/- buttons, whose width follows the UI base font size.</summary>
    internal IReadOnlyList<Button> SizedButtons => _sizedButtons;

    /// <summary>
    /// Hides the Preview button's ▶ icon behind a spinning ring while the GumPreview process
    /// spawns, then restores the icon. Only the ring rotates - the button itself (its
    /// border/background/hit area) never moves (issue #4717).
    /// </summary>
    private static void ShowPreviewLaunchSpinner(TextBlock icon, Ellipse spinner, RotateTransform rotation)
    {
        TimeSpan rotationDuration = TimeSpan.FromMilliseconds(1400);
        DateTime startUtc = DateTime.UtcNow;
        icon.IsVisible = false;
        spinner.IsVisible = true;
        DispatcherTimer timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) =>
        {
            double elapsedMs = (DateTime.UtcNow - startUtc).TotalMilliseconds;
            if (elapsedMs >= rotationDuration.TotalMilliseconds)
            {
                timer.Stop();
                rotation.Angle = 0;
                spinner.IsVisible = false;
                icon.IsVisible = true;
                return;
            }

            const double degreesPerSecond = 360;
            rotation.Angle = degreesPerSecond * elapsedMs / 1000;
        };
        timer.Start();
    }

    private Button SizedButton(string content, string commandPath)
    {
        Button button = new Button
        {
            Classes = { GumChromeStyles.FlatButtonClass },
            Content = content,
            Padding = new Thickness(0),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            [!Button.CommandProperty] = new Binding(commandPath),
        };
        AvaloniaTreeIcons.FollowBaseFontSize(button, DefaultButtonWidth, widthOnly: true);
        _sizedButtons.Add(button);
        return button;
    }

    private static TextBlock GlyphLabel(string glyph, string tooltip, double leftMargin) => Scaled(new TextBlock
    {
        Text = glyph,
        VerticalAlignment = VerticalAlignment.Center,
        [ToolTip.TipProperty] = tooltip,
    }, marginLeft: leftMargin, marginRight: 4);

    private static Control CreatePinIcon()
    {
        Control icon = GumFluentIcons.Create(FluentIcons.Common.Icon.Pin, 14);
        AvaloniaTreeIcons.FollowBaseFontSize(icon, 14);
        return icon;
    }

    /// <summary>
    /// Gives <paramref name="control"/> its width and left/right margin in pixels at the default zoom and in
    /// proportion to the UI base font size after, so the strip shrinks and grows with its text.
    /// </summary>
    private static T Scaled<T>(T control, double? width = null, double marginLeft = 0, double marginRight = 0) where T : Control
    {
        if (width is double w)
        {
            AvaloniaTreeIcons.FollowBaseFontSize(control, w, widthOnly: true);
        }
        if (marginLeft != 0 || marginRight != 0)
        {
            control.Bind(Layoutable.MarginProperty, control.GetResourceObservable(
                FrbThemeResources.BaseFontSizeKey,
                value =>
                {
                    double factor = (value is double size ? size : FrbThemeResources.DefaultBaseFontSize) / FrbThemeResources.DefaultBaseFontSize;
                    return (object)new Thickness(marginLeft * factor, 0, marginRight * factor, 0);
                }));
        }
        return control;
    }
}
