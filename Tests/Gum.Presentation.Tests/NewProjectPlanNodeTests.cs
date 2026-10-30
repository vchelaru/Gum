using System.IO;
using System.Linq;
using Gum.Dialogs;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>The Files tree the New Project dialog builds from a plan.</summary>
public class NewProjectPlanNodeTests
{
    private static readonly string Sep = Path.DirectorySeparatorChar.ToString();

    private static NewProjectPlan Plan() => new NewProjectPlan(
        "root",
        new[] { "Game1.cs", "Content/GumProject/GumProject.gumj", "Content/GumProject/Fonts/F.ttf", "Other/GumProject.gumj" },
        new[] { "Content/GumProject/Behaviors" });

    private static NewProjectPlanNode Child(NewProjectPlanNode parent, string name) => parent.Children.Single(child => child.Name == name);

    [Fact]
    public void Build_NamesTheRoot_AndListsFoldersBeforeFiles_EachSortedByName()
    {
        NewProjectPlanNode root = NewProjectPlanNode.Build(Plan(), "/games/MyGame/", "Content/GumProject/GumProject.gumj", openDepth: 1);

        root.IsRoot.ShouldBeTrue();
        root.DisplayName.ShouldBe("/games/MyGame/");
        root.Children.Select(child => child.DisplayName).ShouldBe(new[] { "Content" + Sep, "Other" + Sep, "Game1.cs" });
        NewProjectPlanNode gumProject = Child(Child(root, "Content"), "GumProject");
        gumProject.IsRoot.ShouldBeFalse();
        gumProject.Children.Select(child => child.Name).ShouldBe(new[] { "Behaviors", "Fonts", "GumProject.gumj" });
    }

    [Fact]
    public void Build_CountsFilesAtAnyDepth_AndAFolderWithoutFilesCountsZero()
    {
        NewProjectPlanNode root = NewProjectPlanNode.Build(Plan(), "root", null, openDepth: 1);

        root.CountText.ShouldBe("4");
        Child(root, "Content").CountText.ShouldBe("2");
        Child(Child(Child(root, "Content"), "GumProject"), "Behaviors").CountText.ShouldBe("0");
        Child(root, "Game1.cs").CountText.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(1, true, false, false, false)]
    [InlineData(3, true, true, true, false)]
    [InlineData(0, false, false, false, false)]
    public void Build_OpensOnlyTheFirstOpenDepthLevelsOfFolders(int openDepth, bool root, bool content, bool gumProject, bool fonts)
    {
        NewProjectPlanNode tree = NewProjectPlanNode.Build(Plan(), "root", null, openDepth);

        NewProjectPlanNode contentNode = Child(tree, "Content");
        NewProjectPlanNode gumProjectNode = Child(contentNode, "GumProject");
        tree.IsExpanded.ShouldBe(root);
        contentNode.IsExpanded.ShouldBe(content);
        gumProjectNode.IsExpanded.ShouldBe(gumProject);
        Child(gumProjectNode, "Fonts").IsExpanded.ShouldBe(fonts);
    }

    [Theory]
    [InlineData("Content/GumProject/GumProject.gumj")]
    [InlineData("Content\\GumProject\\GumProject.gumj")]
    public void Build_MarksOnlyTheMainFile_WhateverSeparatorTheCallerUses(string mainFile)
    {
        NewProjectPlanNode root = NewProjectPlanNode.Build(Plan(), "root", mainFile, openDepth: 1);

        Child(Child(Child(root, "Content"), "GumProject"), "GumProject.gumj").IsMainFile.ShouldBeTrue();
        Child(Child(root, "Other"), "GumProject.gumj").IsMainFile.ShouldBeFalse();
    }

    [Fact]
    public void SetExpandedRecursive_OpensAndClosesEveryFolder_AndLeavesFilesAlone()
    {
        NewProjectPlanNode root = NewProjectPlanNode.Build(Plan(), "root", null, openDepth: 0);

        root.SetExpandedRecursive(true);

        root.IsExpanded.ShouldBeTrue();
        Child(Child(Child(root, "Content"), "GumProject"), "Fonts").IsExpanded.ShouldBeTrue();
        Child(root, "Game1.cs").IsExpanded.ShouldBeFalse();
        root.SetExpandedRecursive(false);
        Child(Child(root, "Content"), "GumProject").IsExpanded.ShouldBeFalse();
    }
}
