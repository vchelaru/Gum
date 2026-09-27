using Gum.Commands;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System.IO;
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests.Managers;

/// <summary>A folder node dragged onto another folder node in the element tree.</summary>
public class DragDropManagerFolderDropTests
{
    [Fact]
    public void FolderDroppedOnFolder_MovesTheDirectoryIntoTheTargetFolder()
    {
        // A backslash is a legal file name character on macOS and Linux, so a destination ending
        // in "\" moves the folder to one named "Controls\" there instead of into "Widgets". The
        // expected paths are the platform's own form, so this fails only on those systems.
        string components = new FilePath("/gumtest/Project/Components/").FullPath;
        Mock<ITreeNode> componentsRoot = new Mock<ITreeNode>();
        componentsRoot.SetupGet(node => node.Text).Returns("Components");
        Mock<ITreeNode> dragged = FolderNode("Controls", componentsRoot.Object, components + "Controls/");
        Mock<ITreeNode> target = FolderNode("Widgets", componentsRoot.Object, components + "Widgets/");
        GumProjectSave project = new GumProjectSave { FullFileName = "/gumtest/Project/Project.gumx" };
        Mock<IProjectManager> projectManager = new Mock<IProjectManager>();
        projectManager.SetupGet(manager => manager.GumProjectSave).Returns(project);
        Mock<IProjectState> projectState = new Mock<IProjectState>();
        projectState.SetupGet(state => state.GumProjectSave).Returns(project);
        Mock<IFileCommands> fileCommands = new Mock<IFileCommands>();
        DragDropManager manager = CreateManager(fileCommands.Object, projectManager.Object, projectState.Object);

        manager.OnNodeSortingDropped(new[] { dragged.Object }, target.Object, dropTarget: null);

        string expectedDestination = new FilePath(components + "Widgets/Controls/").FullPath;
        fileCommands.Verify(commands => commands.MoveDirectory(It.IsAny<string>(), expectedDestination), Times.Once);
    }

    // Where the file system keeps Foo and foo apart, foo/Bar is not inside Foo, so the drop is
    // allowed and moves only Foo's elements. Returns early where no case-sensitive directory can
    // be made (a default macOS volume).
    [Fact]
    public void FolderDroppedIntoACaseOnlyDifferentSeparateFolder_IsAllowedAndMovesOnlyItsOwnElements()
    {
        string? projectFolder = CaseSensitiveTempDirectory.TryCreate();
        if (projectFolder == null)
        {
            return;
        }
        try
        {
            string components = Path.Combine(projectFolder, "Components") + Path.DirectorySeparatorChar;
            Directory.CreateDirectory(components + "Foo");
            Directory.CreateDirectory(Path.Combine(components + "foo", "Bar"));
            Mock<ITreeNode> componentsRoot = new Mock<ITreeNode>();
            componentsRoot.SetupGet(node => node.Text).Returns("Components");
            Mock<ITreeNode> dragged = FolderNode("Foo", componentsRoot.Object, components + "Foo/");
            Mock<ITreeNode> lowerFoo = FolderNode("foo", componentsRoot.Object, components + "foo/");
            Mock<ITreeNode> target = FolderNode("Bar", lowerFoo.Object, components + "foo/Bar/");
            ComponentSave moved = new ComponentSave { Name = "Foo/Button" };
            ComponentSave untouched = new ComponentSave { Name = "foo/Label" };
            GumProjectSave project = new GumProjectSave { FullFileName = Path.Combine(projectFolder, "Project.gumx") };
            project.Components.Add(moved);
            project.Components.Add(untouched);
            Mock<IProjectManager> projectManager = new Mock<IProjectManager>();
            projectManager.SetupGet(manager => manager.GumProjectSave).Returns(project);
            Mock<IProjectState> projectState = new Mock<IProjectState>();
            projectState.SetupGet(state => state.GumProjectSave).Returns(project);
            DragDropManager manager = CreateManager(Mock.Of<IFileCommands>(), projectManager.Object, projectState.Object);

            bool isValid = manager.ValidateNodeSorting(new[] { dragged.Object }, target.Object, dropTarget: null);
            manager.OnNodeSortingDropped(new[] { dragged.Object }, target.Object, dropTarget: null);

            isValid.ShouldBeTrue();
            moved.Name.ShouldBe("foo/Bar/Foo/Button");
            untouched.Name.ShouldBe("foo/Label");
        }
        finally
        {
            Directory.Delete(projectFolder, recursive: true);
        }
    }

    private static Mock<ITreeNode> FolderNode(string text, ITreeNode parent, string fullPath)
    {
        Mock<ITreeNode> node = new Mock<ITreeNode>();
        node.SetupGet(candidate => candidate.Text).Returns(text);
        node.SetupGet(candidate => candidate.Tag).Returns((object?)null);
        node.SetupGet(candidate => candidate.Parent).Returns(parent);
        node.Setup(candidate => candidate.GetFullFilePath()).Returns(new FilePath(fullPath));
        return node;
    }

    private static DragDropManager CreateManager(IFileCommands fileCommands, IProjectManager projectManager, IProjectState projectState) =>
        new DragDropManager(
            Mock.Of<IAddInstanceLogic>(),
            Mock.Of<ICircularReferenceManager>(),
            Mock.Of<ISelectedState>(),
            Mock.Of<IElementCommands>(),
            Mock.Of<IRenameLogic>(),
            Mock.Of<IUndoManager>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IGuiCommands>(),
            fileCommands,
            Mock.Of<ISetVariableLogic>(),
            Mock.Of<ICopyPasteLogic>(),
            Mock.Of<IImportLogic>(),
            Mock.Of<IWireframeObjectManager>(),
            Mock.Of<IPluginManager>(),
            Mock.Of<IReorderLogic>(),
            projectManager,
            projectState,
            new PathCaseSensitivity());
}
