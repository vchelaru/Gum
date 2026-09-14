using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.VisualTree;
using FluentIcons.Avalonia;
using Gum.Avalonia.Panels;
using Gum.Avalonia.Themes;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The Output tab's chrome, as in the WPF MainOutputPluginView.</summary>
public class ToolPanelViewsTests
{
    [AvaloniaFact]
    public void AlignmentView_UsesTheWpfIconsAndButtonLook()
    {
        AlignmentView view = new AlignmentView();
        Window window = new Window { Content = view, Width = 400, Height = 400 };
        window.Show();
        window.UpdateLayout();

        Button[] buttons = view.GetVisualDescendants().OfType<Button>().ToArray();
        buttons.Length.ShouldBe(19);
        string?[] icons = buttons.Select(button => (button.Content as GumIcon)?.Icon).ToArray();
        foreach (string expected in new[] { "AnchorTopLeft", "AnchorCenter", "AnchorBottomRight", "AnchorCenterHorizontal", "AnchorCenterVertical", "DockLeft", "DockFill", "DockLeftRight", "DockTopBottom" })
        {
            icons.ShouldContain(expected);
        }
        // Size to Children keeps the WPF DockControl's Fluent arrow.
        Button sizeToChildren = buttons.Single(button => button.Content is FluentIcon);
        ToolTip.GetTip(sizeToChildren).ShouldBe("Size to Children");
        foreach (Button button in buttons)
        {
            button.Classes.ShouldContain(GumChromeStyles.AlignmentButtonClass);
            button.Background.ShouldBeSameAs(ThemeBrushes.Get(window, "Frb.Brushes.Contrast.Subtle", Brushes.Red));
        }
        window.Close();
    }

    [AvaloniaFact]
    public void OutputView_ClearsWithTheWpfIconButton()
    {
        OutputView view = new OutputView();
        Window window = new Window { Content = view, Width = 400, Height = 300 };
        window.Show();
        window.UpdateLayout();

        Button clear = view.GetVisualDescendants().OfType<Button>().First();
        clear.Content.ShouldBeOfType<FluentIcon>();
        ToolTip.GetTip(clear).ShouldBe("Clear Output");
        clear.Classes.ShouldContain(GumChromeStyles.IconButtonClass);
        window.Close();
    }
}
