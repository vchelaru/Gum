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
        using EngineProfileScope scope = new EngineProfileScope(Services);
        EngineProfile profile = scope.Profile;
        ElementTreeViewManager treeViewManager = Services.GetRequiredService<ElementTreeViewManager>();
        treeViewManager.RefreshUi();
        profile.DisableComponents = true;
        profile.DisableBehaviors = true;

        treeViewManager.ApplyElementKindVisibility();

        treeViewManager.RootTreeNodes.ShouldNotContain(treeViewManager.RootComponentsTreeNode);
        treeViewManager.RootTreeNodes.ShouldNotContain(treeViewManager.RootBehaviorsTreeNode);
        treeViewManager.RootTreeNodes.ShouldContain(treeViewManager.RootScreensTreeNode);
    }

    [AvaloniaFact]
    public void Reload_ProfileNowDisablesComponentsAndBehaviors_RemovesTheirRootFolders()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumTreeElementKindTests");
        using EngineProfileScope scope = new EngineProfileScope(Services);
        ElementTreeViewManager treeViewManager = Services.GetRequiredService<ElementTreeViewManager>();
        treeViewManager.RefreshUi();
        treeViewManager.RootTreeNodes.ShouldContain(treeViewManager.RootComponentsTreeNode);
        scope.Profile.DisableComponents = true;
        scope.Profile.DisableBehaviors = true;

        Services.GetRequiredService<IProjectRestrictions>().Reload();

        treeViewManager.RootTreeNodes.ShouldNotContain(treeViewManager.RootComponentsTreeNode);
        treeViewManager.RootTreeNodes.ShouldNotContain(treeViewManager.RootBehaviorsTreeNode);
    }

    [AvaloniaFact]
    public void ApplyElementKindVisibility_ProjectReenablesComponentsAndBehaviors_RestoresRootFoldersInOrder()
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumTreeElementKindTests");
        using EngineProfileScope scope = new EngineProfileScope(Services);
        EngineProfile profile = scope.Profile;
        ElementTreeViewManager treeViewManager = Services.GetRequiredService<ElementTreeViewManager>();
        treeViewManager.RefreshUi();
        profile.DisableComponents = true;
        profile.DisableBehaviors = true;
        treeViewManager.ApplyElementKindVisibility();
        profile.DisableComponents = false;
        profile.DisableBehaviors = false;

        treeViewManager.ApplyElementKindVisibility();

        List<object> roots = treeViewManager.RootTreeNodes.Cast<object>().ToList();
        int screensIndex = roots.IndexOf(treeViewManager.RootScreensTreeNode);
        int componentsIndex = roots.IndexOf(treeViewManager.RootComponentsTreeNode);
        int behaviorsIndex = roots.IndexOf(treeViewManager.RootBehaviorsTreeNode);
        componentsIndex.ShouldBe(screensIndex + 1);
        behaviorsIndex.ShouldBe(roots.Count - 1);
    }

    [AvaloniaTheory]
    [InlineData(false)]
    [InlineData(true)]
    public void ElementKindVisibilityAndStandardsPalette_Combined_KeepRootFolderOrder(bool usePalette)
    {
        using ToolProjectFixture fixture = new ToolProjectFixture("GumTreeElementKindTests");
        using EngineProfileScope scope = new EngineProfileScope(Services);
        EngineProfile profile = scope.Profile;
        IProjectManager projectManager = Services.GetRequiredService<IProjectManager>();
        bool? originalPalette = projectManager.UseStandardsPalette;
        try
        {
            projectManager.UseStandardsPalette = usePalette;
            ElementTreeViewManager treeViewManager = Services.GetRequiredService<ElementTreeViewManager>();
            treeViewManager.RefreshUi();
            profile.DisableComponents = true;
            profile.DisableBehaviors = true;
            treeViewManager.ApplyElementKindVisibility();
            treeViewManager.ApplyStandardsPaletteMode();
            profile.DisableComponents = false;
            profile.DisableBehaviors = false;
            treeViewManager.ApplyElementKindVisibility();
            treeViewManager.ApplyStandardsPaletteMode();

            List<object> roots = treeViewManager.RootTreeNodes.Cast<object>().ToList();
            List<object> expected = new List<object>
            {
                treeViewManager.RootScreensTreeNode,
                treeViewManager.RootComponentsTreeNode,
            };
            if (!usePalette)
            {
                expected.Add(treeViewManager.RootStandardElementsTreeNode);
            }
            expected.Add(treeViewManager.RootBehaviorsTreeNode);
            roots.ShouldBe(expected);
        }
        finally
        {
            projectManager.UseStandardsPalette = originalPalette;
        }
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
