using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Gum.Avalonia.Plugins.TreeView;
using Gum.Avalonia.Shell;
using Avalonia.Media;
using Avalonia.VisualTree;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.DataTypes;
using Gum.Services;

using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The tree view's row icons follow the base font size, as the text beside them does (#7316).</summary>
public class TreeIconZoomTests
{
    [AvaloniaFact]
    public void TreeRowIcons_FollowBaseFontSize()
    {
        IAppScaleProvider scale = TestAppBuilder.Services.GetRequiredService<IAppScaleProvider>();
        double original = scale.BaseFontSize;
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ScreenSave screen = tree.Project.AddScreen("MainMenu");
        tree.Project.AddInstance(screen, "Background", "Sprite");
        tree.TreeManager.RefreshUi();
        tree.NodeFor(screen).IsExpanded = true;
        using IDisposable follower = AppWideWindowInput.FollowBaseFontSize(tree.Input.Window, scale);
        try
        {
            scale.BaseFontSize = 12;
            tree.Input.Layout();
            List<double> atDefault = RowIconWidths(tree);
            scale.BaseFontSize = 24;
            tree.Input.Layout();
            List<double> zoomedIn = RowIconWidths(tree);

            atDefault.ShouldNotBeEmpty();
            zoomedIn.ShouldBe(atDefault.Select(width => width * 2).ToList());
        }
        finally
        {
            scale.BaseFontSize = original;
        }
    }

    private static List<double> RowIconWidths(ProjectTreeHarness tree) =>
        tree.Input.Window.GetVisualDescendants().OfType<TreeRowView>()
            .SelectMany(row => row.GetVisualDescendants().OfType<Rectangle>())
            .Where(icon => icon.OpacityMask is ImageBrush)
            .Select(icon => icon.Bounds.Width)
            .ToList();
}
