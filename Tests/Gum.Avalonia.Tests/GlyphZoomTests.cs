using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FluentIcons.Avalonia;
using Gum.Avalonia.Panels;
using Gum.Avalonia.Plugins.TreeView;
using Gum.Avalonia.Shell;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Radio button, check box and menu check glyphs, icon buttons and the status dot follow the base font
/// size like the text beside them, and are unchanged at the default 12 (#7343).
/// </summary>
public class GlyphZoomTests
{
    [AvaloniaFact]
    public void RadioButton_GlyphFollowsTheBaseFontSize()
    {
        RadioButton radio = new RadioButton { Content = "Option" };
        AssertFollowsZoom(radio, () => GlyphSizes(radio), expectedAtDefault: new List<double> { 20, 20, 8 });
    }

    [AvaloniaFact]
    public void CheckBox_CheckMarkAndMixedSquareFollowTheBaseFontSize()
    {
        CheckBox check = new CheckBox { Content = "Option", IsChecked = true, IsThreeState = true };
        AssertFollowsZoom(check, () => CheckBoxSizes(check), expectedAtDefault: new List<double> { 12, 8, 6 });
    }

    [AvaloniaFact]
    public void ErrorTabHeader_DotFollowsTheBaseFontSize()
    {
        ErrorTabHeaderView header = new ErrorTabHeaderView();
        AssertFollowsZoom(header, () => header.GetVisualDescendants().OfType<Ellipse>().Select(dot => dot.Width).ToList(),
            expectedAtDefault: new List<double> { 10 });
    }

    [AvaloniaFact]
    public void ProjectPanel_SearchAndCollapseButtonsFollowTheBaseFontSize()
    {
        AvaloniaElementTreeView panel = new AvaloniaElementTreeView();
        Control content = (Control)panel.Content;
        AssertFollowsZoom(content, () => ButtonIconSizes(content), expectedAtDefault: new List<double> { 12, 24, 16, 24, 16 });
    }

    // The panel's buttons: each button's height and its icon's size, in tree order.
    private static List<double> ButtonIconSizes(Control root)
    {
        List<double> sizes = new List<double>();
        foreach (Button button in root.GetVisualDescendants().OfType<Button>())
        {
            if (button.Content is FluentIcon icon)
            {
                if (!double.IsNaN(button.Height))
                {
                    sizes.Add(button.Height);
                }
                sizes.Add(icon.Width);
            }
        }
        return sizes;
    }

    private static List<double> GlyphSizes(RadioButton radio) =>
        new[] { "OuterEllipse", "CheckOuterEllipse", "CheckGlyph" }
            .Select(name => radio.GetVisualDescendants().OfType<Ellipse>().First(part => part.Name == name).Width).ToList();

    // The box, the check mark's drawn width and the mixed square, as drawn (render transforms included).
    private static List<double> CheckBoxSizes(CheckBox check)
    {
        Visual root = check.GetVisualDescendants().OfType<Grid>().First();
        double box = check.GetVisualDescendants().OfType<Border>().First(part => part.Name == "PART_Box").Width;
        global::Avalonia.Controls.Shapes.Path mark = check.GetVisualDescendants().OfType<global::Avalonia.Controls.Shapes.Path>().First();
        double markWidth = DrawnWidth(mark, root);
        Border mixed = check.GetVisualDescendants().OfType<Border>().First(part => part.Name == "PART_Mixed");
        return new List<double> { box, Math.Round(markWidth, 3), mixed.Width };
    }

    private static double DrawnWidth(Visual part, Visual root) =>
        new Rect(part.Bounds.Size).TransformToAABB(part.TransformToVisual(root)!.Value).Width;

    private static void AssertFollowsZoom(Control content, Func<List<double>> sizes, List<double> expectedAtDefault)
    {
        // Its own window: the container's one MainWindow must not be left shown.
        Window window = new Window { Content = content, Width = 600, Height = 300 };
        IAppScaleProvider scale = TestAppBuilder.Services.GetRequiredService<IAppScaleProvider>();
        double originalSize = scale.BaseFontSize;
        using IDisposable follower = AppWideWindowInput.FollowBaseFontSize(window, scale);
        try
        {
            window.Show();
            scale.BaseFontSize = 12;
            Dispatcher.UIThread.RunJobs();
            List<double> atDefault = sizes();
            atDefault.ShouldBe(expectedAtDefault);

            scale.BaseFontSize = 24;
            Dispatcher.UIThread.RunJobs();
            sizes().ShouldBe(atDefault.Select(size => size * 2).ToList());

            scale.BaseFontSize = 6;
            Dispatcher.UIThread.RunJobs();
            sizes().ShouldBe(atDefault.Select(size => size * 0.5).ToList());
        }
        finally
        {
            scale.BaseFontSize = originalSize;
            window.Close();
        }
    }
}
