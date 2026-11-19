using Avalonia.Headless.XUnit;
using Gum.Avalonia.Plugins.TreeView;
using Gum.Managers;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>When the Project panel counts as showing a project, which the startup timing's time-to-tree mark waits for.</summary>
public class ElementTreeViewProjectNodesTests
{
    [AvaloniaFact]
    public void HasProjectNodes_IsTrue_OnlyOnceARootFolderHasChildren()
    {
        AvaloniaElementTreeView view = new AvaloniaElementTreeView();
        view.HasProjectNodes().ShouldBeFalse();

        GumTreeNode screens = new GumTreeNode("Screens");
        view.Nodes.Add(new GumTreeNode("Components"));
        view.Nodes.Add(screens);
        view.HasProjectNodes().ShouldBeFalse();

        screens.Nodes.Add(new GumTreeNode("MainMenu"));
        view.HasProjectNodes().ShouldBeTrue();
    }
}
