using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Gum.Avalonia.Plugins.TreeView;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The Project panel's layout: its tool buttons above the search box and tree.</summary>
public class ProjectPanelLayoutTests
{
    [AvaloniaFact]
    public void ToolButtons_SitRightOfTheSearchBox_AndTheTreeFillsThePanel()
    {
        AvaloniaElementTreeView view = new AvaloniaElementTreeView();
        DockPanel content = (DockPanel)view.Content;
        Window window = new Window { Content = content, Width = 300, Height = 800 };
        window.Show();
        window.UpdateLayout();

        // One row: the search box shrinks to make room for the two tool buttons beside it (#4694).
        Grid searchRow = view.SearchRow;
        TextBox search = searchRow.Children.OfType<TextBox>().Single();
        Button[] buttons = searchRow.Children.OfType<Button>().ToArray();
        buttons.Length.ShouldBe(2);
        foreach (Button button in buttons)
        {
            button.Bounds.X.ShouldBeGreaterThanOrEqualTo(search.Bounds.Right);
            button.Bounds.Height.ShouldBeLessThan(40);
        }
        searchRow.Bounds.Y.ShouldBeLessThan(8);
        Grid treeHost = content.Children.OfType<Grid>().Last();
        treeHost.Bounds.Height.ShouldBeGreaterThan(700);
        window.Close();
    }

    [AvaloniaFact]
    public void ToolButtonIcons_GrowWithTheUiBaseFontSize_AndTheButtonsFitThem()
    {
        AvaloniaElementTreeView view = new AvaloniaElementTreeView();
        Window window = new Window { Content = view.Content, Width = 300, Height = 800 };
        window.Show();

        view.UpdateCollapseButtonSizes(24);
        window.UpdateLayout();

        foreach (Button button in view.SearchRow.Children.OfType<Button>())
        {
            Control icon = button.Content.ShouldBeAssignableTo<Control>().ShouldNotBeNull();
            icon.Bounds.Width.ShouldBe(32);
            icon.Bounds.Height.ShouldBe(32);
            button.Bounds.Height.ShouldBeGreaterThanOrEqualTo(32);
        }
        window.Close();
    }
}
