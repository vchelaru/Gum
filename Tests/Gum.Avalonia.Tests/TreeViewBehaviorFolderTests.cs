using Avalonia.Headless.XUnit;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// Behaviors in subfolders get the same folder handling in the tree as screens and components.
/// </summary>
public class TreeViewBehaviorFolderTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    public void RefreshUi_BehaviorInFolderMissingOnDisk_ShowsItUnderAPlaceholderFolder()
    {
        // A hand-edited project can name a behavior in a folder that isn't on disk. The refresh
        // found no parent node for it and threw, aborting the whole tree refresh.
        using ToolProjectFixture fixture = new ToolProjectFixture("GumTreeBehaviorFolderTests");
        ElementTreeViewManager treeViewManager = Services.GetRequiredService<ElementTreeViewManager>();
        BehaviorSave behavior = new BehaviorSave { Name = "Sub/MyBehavior" };
        fixture.Project.Behaviors.Add(behavior);

        treeViewManager.RefreshUi();

        GumTreeNode behaviorNode = treeViewManager.GetTreeNodeFor(behavior).ShouldNotBeNull();
        GumTreeNode folderNode = behaviorNode.Parent.ShouldNotBeNull();
        folderNode.Text.ShouldBe("Sub");
        folderNode.Parent.ShouldBeSameAs(treeViewManager.RootBehaviorsTreeNode);
    }

    [AvaloniaFact]
    public void ApplyElementKindVisibility_ProjectDisablesComponentsAndBehaviors_RemovesTheirRootFolders()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumTreeElementKindTests");
        ElementTreeViewManager treeViewManager = Services.GetRequiredService<ElementTreeViewManager>();
        treeViewManager.RefreshUi();
        fixture.Project.DisableComponents = true;
        fixture.Project.DisableBehaviors = true;

        treeViewManager.ApplyElementKindVisibility();

        treeViewManager.RootTreeNodes.ShouldNotContain(treeViewManager.RootComponentsTreeNode);
        treeViewManager.RootTreeNodes.ShouldNotContain(treeViewManager.RootBehaviorsTreeNode);
        treeViewManager.RootTreeNodes.ShouldContain(treeViewManager.RootScreensTreeNode);
    }

    [AvaloniaFact]
    public void ApplyElementKindVisibility_ProjectReenablesComponentsAndBehaviors_RestoresRootFoldersInOrder()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumTreeElementKindTests");
        ElementTreeViewManager treeViewManager = Services.GetRequiredService<ElementTreeViewManager>();
        treeViewManager.RefreshUi();
        fixture.Project.DisableComponents = true;
        fixture.Project.DisableBehaviors = true;
        treeViewManager.ApplyElementKindVisibility();
        fixture.Project.DisableComponents = false;
        fixture.Project.DisableBehaviors = false;

        treeViewManager.ApplyElementKindVisibility();

        List<object> roots = treeViewManager.RootTreeNodes.Cast<object>().ToList();
        int screensIndex = roots.IndexOf(treeViewManager.RootScreensTreeNode);
        int componentsIndex = roots.IndexOf(treeViewManager.RootComponentsTreeNode);
        int behaviorsIndex = roots.IndexOf(treeViewManager.RootBehaviorsTreeNode);
        componentsIndex.ShouldBe(screensIndex + 1);
        behaviorsIndex.ShouldBe(roots.Count - 1);
    }

    [AvaloniaFact]
    public void RefreshUi_BehaviorInSubfolderRemovedFromProject_RemovesItsNode()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumTreeBehaviorFolderTests");
        ElementTreeViewManager treeViewManager = Services.GetRequiredService<ElementTreeViewManager>();
        Directory.CreateDirectory(Path.Combine(fixture.ProjectFolder, "Behaviors", "Sub"));
        BehaviorSave behavior = new BehaviorSave { Name = "Sub/MyBehavior" };
        fixture.Project.Behaviors.Add(behavior);
        treeViewManager.RefreshUi();
        treeViewManager.GetTreeNodeFor(behavior).ShouldNotBeNull();

        fixture.Project.Behaviors.Remove(behavior);
        treeViewManager.RefreshUi();

        treeViewManager.GetTreeNodeFor(behavior).ShouldBeNull();
    }
}
