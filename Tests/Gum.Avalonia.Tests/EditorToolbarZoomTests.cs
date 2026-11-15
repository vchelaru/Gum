using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.EditorTab;
using Gum.Avalonia.Shell;
using Gum.Plugins;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The Editor toolbar's combo boxes, label and buttons follow the base font size, so zooming out
/// shrinks the strip with its text instead of leaving it sparse (#7332).
/// </summary>
public class EditorToolbarZoomTests
{
    [AvaloniaFact]
    public void Toolbar_SizesFollowTheBaseFontSize()
    {
        // The toolbar gets its own window: showing the container's one MainWindow leaves the tab content
        // parented to it, which breaks the tests that host that content themselves.
        EditorToolbar toolbar = new EditorToolbar();
        Window window = new Window { Content = toolbar };
        IAppScaleProvider scale = TestAppBuilder.Services.GetRequiredService<IAppScaleProvider>();
        double originalSize = scale.BaseFontSize;
        using IDisposable follower = AppWideWindowInput.FollowBaseFontSize(window, scale);
        try
        {
            window.Show();
            scale.BaseFontSize = 12;
            Dispatcher.UIThread.RunJobs();
            List<double> atDefault = ScaledSizes(toolbar);
            atDefault.Count.ShouldBeGreaterThan(8);

            scale.BaseFontSize = 24;
            Dispatcher.UIThread.RunJobs();
            ScaledSizes(toolbar).ShouldBe(atDefault.Select(size => size * 2).ToList());

            scale.BaseFontSize = 9;
            Dispatcher.UIThread.RunJobs();
            ScaledSizes(toolbar).ShouldBe(atDefault.Select(size => size * 0.75).ToList());
        }
        finally
        {
            scale.BaseFontSize = originalSize;
            window.Close();
        }
    }

    [AvaloniaFact]
    public void ComboBoxes_HeightChevronAndEditableTextFollowTheBaseFontSize()
    {
        // The toolbar gets its own window: showing the container's one MainWindow leaves the tab content
        // parented to it, which breaks the tests that host that content themselves.
        EditorToolbar toolbar = new EditorToolbar();
        Window window = new Window { Content = toolbar };
        IAppScaleProvider scale = TestAppBuilder.Services.GetRequiredService<IAppScaleProvider>();
        double originalSize = scale.BaseFontSize;
        using IDisposable follower = AppWideWindowInput.FollowBaseFontSize(window, scale);
        try
        {
            window.Show();
            scale.BaseFontSize = 12;
            Dispatcher.UIThread.RunJobs();
            ComboBox editable = toolbar.GetVisualDescendants().OfType<ComboBox>().Single(combo => combo.IsEditable);
            ComboBox closed = toolbar.GetVisualDescendants().OfType<ComboBox>().First(combo => !combo.IsEditable);
            closed.Bounds.Height.ShouldBe(22);
            PathIcon chevron = closed.GetVisualDescendants().OfType<PathIcon>().Single(icon => icon.Name == "DropDownGlyph");
            chevron.Width.ShouldBe(12);
            ((Grid)chevron.Parent!).ColumnDefinitions[1].Width.Value.ShouldBe(32);

            scale.BaseFontSize = 6;
            Dispatcher.UIThread.RunJobs();
            closed.Bounds.Height.ShouldBeLessThanOrEqualTo(12);
            chevron.Width.ShouldBe(6);
            ((Grid)chevron.Parent!).ColumnDefinitions[1].Width.Value.ShouldBe(16);

            scale.BaseFontSize = 24;
            Dispatcher.UIThread.RunJobs();
            closed.Bounds.Height.ShouldBeGreaterThanOrEqualTo(44);
            chevron.Width.ShouldBe(24);

            TextPresenter text = editable.GetVisualDescendants().OfType<TextPresenter>().Single();
            text.VerticalAlignment.ShouldBe(global::Avalonia.Layout.VerticalAlignment.Center);
        }
        finally
        {
            scale.BaseFontSize = originalSize;
            window.Close();
        }
    }

    // Each strip item's width or minimum width (whichever the toolbar sets) plus its horizontal margin.
    private static List<double> ScaledSizes(EditorToolbar toolbar) => toolbar.GetVisualDescendants()
        .OfType<StackPanel>().First().Children
        .SelectMany(control => new[]
        {
            double.IsNaN(control.Width) ? control.MinWidth : control.Width,
            control.Margin.Left + control.Margin.Right,
        })
        .ToList();
}
