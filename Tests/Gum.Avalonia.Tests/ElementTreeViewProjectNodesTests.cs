using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Threading;
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

    [AvaloniaFact]
    public void StartupMark_IsNotWatched_WhenTimingIsOff()
    {
        AvaloniaElementTreeView view = new AvaloniaElementTreeView(startupMark: null);

        view.IsWaitingForFirstPopulatedFrame.ShouldBeFalse();
    }

    [AvaloniaFact]
    public void StartupMark_FiresOnce_OnTheFirstFrameAfterARootFolderGetsChildren()
    {
        List<string> marks = new List<string>();
        AvaloniaElementTreeView view = new AvaloniaElementTreeView(marks.Add);
        Window window = new Window { Content = view.Content, Width = 300, Height = 400 };
        window.Show();
        GumTreeNode screens = new GumTreeNode("Screens");
        view.Nodes.Add(screens);
        Render(window);
        marks.ShouldBeEmpty();
        view.IsWaitingForFirstPopulatedFrame.ShouldBeTrue();

        screens.Nodes.Add(new GumTreeNode("MainMenu"));
        Render(window);
        screens.Nodes.Add(new GumTreeNode("Options"));
        Render(window);

        marks.ShouldBe(new[] { "Project tree: first populated frame" });
        view.IsWaitingForFirstPopulatedFrame.ShouldBeFalse();
        window.Close();
    }

    private static void Render(Window window)
    {
        window.UpdateLayout();
        Dispatcher.UIThread.RunJobs();
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }
}
