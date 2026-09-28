using Gum.Commands;
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
using ToolsUtilities;
using Xunit;

namespace Gum.Presentation.Tests.Managers;

/// <summary>Files dropped from the file manager onto the element tree.</summary>
public class DragDropManagerTreeFileDropTests
{
    [Theory]
    [InlineData(false, 1)]
    [InlineData(true, 0)]
    public void ScreenFileDroppedOnScreens_IsImportedUnlessTheDropCarriesAProject(bool alsoDropsProject, int expectedImports)
    {
        // A drop that carries a project file opens that project from the main window; the tree
        // leaves the whole drop alone rather than importing into the project being replaced.
        Mock<ITreeNode> screensNode = new Mock<ITreeNode>();
        screensNode.SetupGet(node => node.Text).Returns("Screens");
        Mock<IImportLogic> importLogic = new Mock<IImportLogic>();
        DragDropManager manager = CreateManager(Mock.Of<IPluginManager>(), importLogic.Object);
        string[] files = alsoDropsProject
            ? new[] { "C:/Game/Screens/Title.gusx", "C:/Game/Game.gumx" }
            : new[] { "C:/Game/Screens/Title.gusx" };

        manager.OnFilesDroppedInTreeView(files, screensNode.Object);

        importLogic.Verify(logic => logic.ImportScreen(It.IsAny<FilePath>(), null, true), Times.Exactly(expectedImports));
    }

    private static DragDropManager CreateManager(IPluginManager pluginManager, IImportLogic importLogic) =>
        new DragDropManager(
            Mock.Of<IAddInstanceLogic>(),
            Mock.Of<ICircularReferenceManager>(),
            Mock.Of<ISelectedState>(),
            Mock.Of<IElementCommands>(),
            Mock.Of<IRenameLogic>(),
            Mock.Of<IUndoManager>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<ISetVariableLogic>(),
            Mock.Of<ICopyPasteLogic>(),
            importLogic,
            Mock.Of<IWireframeObjectManager>(),
            pluginManager,
            Mock.Of<IReorderLogic>(),
            Mock.Of<IProjectManager>(),
            Mock.Of<IProjectState>(),
            new PathCaseSensitivity());
}
