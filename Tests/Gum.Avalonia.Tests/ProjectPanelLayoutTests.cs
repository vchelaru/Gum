using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Gum.Avalonia.Plugins.TreeView;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The Project panel's layout: its tool buttons above the search box and tree.</summary>
public class ProjectPanelLayoutTests
{
    [AvaloniaFact]
    public void ToolButtons_KeepTheirSize_AndTheTreeFillsThePanel()
    {
        AvaloniaElementTreeView view = new AvaloniaElementTreeView();
        DockPanel content = (DockPanel)view.Content;
        Window window = new Window { Content = content, Width = 300, Height = 800 };
        window.Show();
        window.UpdateLayout();

        StackPanel toolButtons = content.Children.OfType<StackPanel>().First();
        Grid treeHost = content.Children.OfType<Grid>().Last();
        toolButtons.Bounds.Height.ShouldBeLessThan(40);
        treeHost.Bounds.Height.ShouldBeGreaterThan(600);
        window.Close();
    }
}
