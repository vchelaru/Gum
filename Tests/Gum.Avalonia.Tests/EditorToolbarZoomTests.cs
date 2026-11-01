using Avalonia.Controls;
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
        MainWindow window = TestAppBuilder.Services.GetRequiredService<MainWindow>();
        IAppScaleProvider scale = TestAppBuilder.Services.GetRequiredService<IAppScaleProvider>();
        EditorToolbar toolbar = TestAppBuilder.Services.GetRequiredService<PluginManager>().InitializedPlugins
            .OfType<AvaloniaEditorTabPlugin>().Single().Toolbar.ShouldNotBeNull();
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
