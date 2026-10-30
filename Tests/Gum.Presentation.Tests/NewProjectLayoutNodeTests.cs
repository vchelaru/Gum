using System.IO;
using System.Linq;
using Gum.Dialogs;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>The folder tree the New Project dialog builds from a layout.</summary>
public class NewProjectLayoutNodeTests
{
    private static readonly string Sep = Path.DirectorySeparatorChar.ToString();

    private static NewProjectLayout CodeProject() =>
        NewProjectLayout.ForCodeProject("root", "MyGame", HostPlatform.MonoGame, useXmlFormat: false);

    private static NewProjectLayout GumOnly() => NewProjectLayout.ForGumOnly("root", "MyGame", useXmlFormat: true);

    private static NewProjectLayoutNode Child(NewProjectLayoutNode parent, string name) =>
        parent.Children.Single(child => child.Name == name);

    [Fact]
    public void Build_ForACodeProject_NamesTheRoot_AndNestsTheGumProjectFolderUnderContent()
    {
        NewProjectLayoutNode root = NewProjectLayoutNode.Build(CodeProject(), "/games/MyGame/", openDepth: 1);

        root.IsRoot.ShouldBeTrue();
        root.DisplayName.ShouldBe("/games/MyGame/");
        root.Children.Select(child => child.DisplayName).ShouldBe(new[] { "Content" + Sep });
        NewProjectLayoutNode gumProject = Child(Child(root, "Content"), "GumProject");
        gumProject.IsRoot.ShouldBeFalse();
        gumProject.Children.Select(child => child.Name)
            .ShouldBe(new[] { "Behaviors", "Components", "Fonts", "Screens", "Standards", "GumProject.gumj" });
    }

    [Fact]
    public void Build_ForAGumOnlyProject_PutsTheProjectFileDirectlyUnderTheRoot()
    {
        NewProjectLayoutNode root = NewProjectLayoutNode.Build(GumOnly(), "root", openDepth: 1);

        root.Children.Select(child => child.Name)
            .ShouldBe(new[] { "Behaviors", "Components", "Fonts", "Screens", "Standards", "MyGame.gumx" });
    }

    [Fact]
    public void Build_MarksOnlyTheGumProjectFile_AndShowsNoOtherFile()
    {
        NewProjectLayoutNode root = NewProjectLayoutNode.Build(CodeProject(), "root", openDepth: 3);

        NewProjectLayoutNode file = Child(Child(Child(root, "Content"), "GumProject"), "GumProject.gumj");
        file.IsMainFile.ShouldBeTrue();
        file.IsFolder.ShouldBeFalse();
        Flatten(root).Where(node => !node.IsFolder).ShouldBe(new[] { file });
    }

    [Theory]
    [InlineData(1, true, false, false, false)]
    [InlineData(3, true, true, true, false)]
    [InlineData(0, false, false, false, false)]
    public void Build_OpensOnlyTheFirstOpenDepthLevelsOfFolders(int openDepth, bool root, bool content, bool gumProject, bool fonts)
    {
        NewProjectLayoutNode tree = NewProjectLayoutNode.Build(CodeProject(), "root", openDepth);

        NewProjectLayoutNode contentNode = Child(tree, "Content");
        NewProjectLayoutNode gumProjectNode = Child(contentNode, "GumProject");
        tree.IsExpanded.ShouldBe(root);
        contentNode.IsExpanded.ShouldBe(content);
        gumProjectNode.IsExpanded.ShouldBe(gumProject);
        Child(gumProjectNode, "Fonts").IsExpanded.ShouldBe(fonts);
    }

    [Fact]
    public void SetExpandedRecursive_OpensAndClosesEveryFolder_AndLeavesTheFileAlone()
    {
        NewProjectLayoutNode root = NewProjectLayoutNode.Build(CodeProject(), "root", openDepth: 0);

        root.SetExpandedRecursive(true);

        Flatten(root).Where(node => node.IsFolder).ShouldAllBe(node => node.IsExpanded);
        Flatten(root).Single(node => !node.IsFolder).IsExpanded.ShouldBeFalse();
        root.SetExpandedRecursive(false);
        Flatten(root).ShouldAllBe(node => !node.IsExpanded);
    }

    private static System.Collections.Generic.IEnumerable<NewProjectLayoutNode> Flatten(NewProjectLayoutNode node) =>
        new[] { node }.Concat(node.Children.SelectMany(Flatten));
}
