using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.TreeView;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.EndToEnd;
using Gum.DataTypes;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The Standards palette's text and chips, and the tree's indent, follow the base font size (#7321).</summary>
public class StandardsPaletteZoomTests
{
    [AvaloniaFact]
    public void PaletteTextAndChips_FollowBaseFontSize()
    {
        IAppScaleProvider scale = TestAppBuilder.Services.GetRequiredService<IAppScaleProvider>();
        double original = scale.BaseFontSize;
        AvaloniaStandardsPalette palette = new AvaloniaStandardsPalette();
        Window window = new Window { Content = palette, Width = 400, Height = 400 };
        window.Show();
        palette.RefreshChips(new[] { "Circle", "Text" });
        using IDisposable follower = AppWideWindowInput.FollowBaseFontSize(window, scale);
        try
        {
            scale.BaseFontSize = 12;
            window.UpdateLayout();
            List<double> atDefault = Measure(palette);
            scale.BaseFontSize = 24;
            window.UpdateLayout();
            List<double> zoomedIn = Measure(palette);

            // Header (2), then each chip's name and icon.
            atDefault.Take(4).ShouldBe(new[] { 10.5, 10, 12, 14 });
            zoomedIn.ShouldBe(atDefault.Select(size => size * 2).ToList());
        }
        finally
        {
            scale.BaseFontSize = original;
            window.Close();
        }
    }

    [AvaloniaFact]
    public void TreeIndent_FollowsBaseFontSize()
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
            double atDefault = HighlightX(tree);
            scale.BaseFontSize = 24;
            tree.Input.Layout();
            double zoomedIn = HighlightX(tree);

            atDefault.ShouldBe(2 * TreeRowView.Indent);
            zoomedIn.ShouldBe(atDefault * 2);
        }
        finally
        {
            scale.BaseFontSize = original;
        }
    }

    private static double HighlightX(ProjectTreeHarness tree)
    {
        TreeRowView row = tree.Input.Window.GetVisualDescendants().OfType<TreeRowView>()
            .First(candidate => candidate.Row?.Level == 2);
        return row.Highlight.TranslatePoint(new Point(0, 0), row)!.Value.X;
    }

    // Header text sizes, then each chip's name size and icon width, in tree order.
    private static List<double> Measure(AvaloniaStandardsPalette palette)
    {
        List<double> sizes = new List<double>();
        foreach (TextBlock header in palette.GetVisualDescendants().OfType<TextBlock>().Where(block => block.Text is "STANDARDS" or "click or drag to add"))
        {
            sizes.Add(header.FontSize);
        }
        foreach (string name in new[] { "Circle", "Text" })
        {
            TextBlock label = palette.GetVisualDescendants().OfType<TextBlock>().First(block => block.Text == name);
            sizes.Add(label.FontSize);
            Rectangle icon = label.GetVisualAncestors().OfType<Grid>().First().GetVisualChildren().OfType<Rectangle>().First();
            sizes.Add(icon.Bounds.Width);
        }
        return sizes;
    }
}
