using Gum.Managers;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Headless port of ElementTreeViewManager's WinForms-coupled IsPartOfScreensFolderStructure/
/// IsPartOfComponentsFolderStructure (ADR-0005 Phase 3), needed so AddScreenDialogViewModel/
/// AddComponentDialogViewModel can move into Gum.Presentation. The old TreeNode-typed overloads
/// identified the structure's root by reference-equality against ElementTreeViewManager's live
/// TreeNode instance; this version reuses IsTopScreenContainerTreeNode/IsTopComponentContainerTreeNode's
/// existing text-based root check instead, so it works for any ITreeNode implementation.
/// </summary>
public class TreeNodeFolderExtensionsTests
{
    [Fact]
    public void IsPartOfScreensFolderStructure_ReturnsTrue_ForNodeUnderScreensRoot()
    {
        Mock<ITreeNode> screensRoot = new();
        screensRoot.Setup(x => x.Text).Returns("Screens");
        screensRoot.Setup(x => x.Parent).Returns((ITreeNode?)null);

        Mock<ITreeNode> subfolder = new();
        subfolder.Setup(x => x.Parent).Returns(screensRoot.Object);

        subfolder.Object.IsPartOfScreensFolderStructure().ShouldBeTrue();
    }

    [Fact]
    public void IsPartOfScreensFolderStructure_ReturnsFalse_ForNodeUnderComponentsRoot()
    {
        Mock<ITreeNode> componentsRoot = new();
        componentsRoot.Setup(x => x.Text).Returns("Components");
        componentsRoot.Setup(x => x.Parent).Returns((ITreeNode?)null);

        Mock<ITreeNode> subfolder = new();
        subfolder.Setup(x => x.Parent).Returns(componentsRoot.Object);

        subfolder.Object.IsPartOfScreensFolderStructure().ShouldBeFalse();
    }

    [Fact]
    public void IsPartOfScreensFolderStructure_ReturnsFalse_ForNull()
    {
        ITreeNode? node = null;

        node.IsPartOfScreensFolderStructure().ShouldBeFalse();
    }

    [Fact]
    public void IsPartOfComponentsFolderStructure_ReturnsTrue_ForNodeUnderComponentsRoot()
    {
        Mock<ITreeNode> componentsRoot = new();
        componentsRoot.Setup(x => x.Text).Returns("Components");
        componentsRoot.Setup(x => x.Parent).Returns((ITreeNode?)null);

        Mock<ITreeNode> subfolder = new();
        subfolder.Setup(x => x.Parent).Returns(componentsRoot.Object);

        subfolder.Object.IsPartOfComponentsFolderStructure().ShouldBeTrue();
    }
}
