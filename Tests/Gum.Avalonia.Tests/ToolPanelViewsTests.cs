using Avalonia.Controls;
using Avalonia.Headless.XUnit;
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
