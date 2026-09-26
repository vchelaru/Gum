using Gum.Controls;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.Services;
using Gum.ToolStates;
using Gum.ViewModels;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// Tree view gestures that must not throw when the project is missing or was never saved (#4997).
/// </summary>
public class ElementTreeViewManagerNullSafetyTests : BaseTestClass
{
    private static (ElementTreeViewManager manager, GumTreeNodeCollection nodes) CreateManager(AutoMocker mocker)
    {
        GumTreeNodeCollection nodes = new GumTreeNodeCollection();
        TreeSelectionModel selection = new TreeSelectionModel(nodes, () => TreeModifierKeys.None, _ => { });

        Mock<IElementTreeView> view = mocker.GetMock<IElementTreeView>();
        view.Setup(x => x.Nodes).Returns(nodes);
        view.Setup(x => x.Selection).Returns(selection);
        mocker.GetMock<IElementTreeViewFactory>().Setup(x => x.Create()).Returns(view.Object);

        ElementTreeViewManager manager = mocker.CreateInstance<ElementTreeViewManager>();
        manager.Initialize();

        return (manager, nodes);
    }

    [Fact]
    public void FilterText_WithNoProjectLoaded_DoesNotThrow()
    {
        AutoMocker mocker = new AutoMocker();
        (ElementTreeViewManager manager, _) = CreateManager(mocker);
        mocker.GetMock<IProjectState>().Setup(x => x.GumProjectSave).Returns((GumProjectSave?)null);

        Should.NotThrow(() => manager.FilterText = "Button");
    }

    [Fact]
    public void ViewInExplorer_OnStandardElementOfUnsavedProject_DoesNotThrowOrReveal()
    {
        AutoMocker mocker = new AutoMocker();
        (ElementTreeViewManager manager, GumTreeNodeCollection nodes) = CreateManager(mocker);

        StandardElementSave textStandard = new StandardElementSave { Name = "Text" };
        GumTreeNode textNode = new GumTreeNode { Tag = textStandard };
        nodes.Add(textNode);
        manager.SelectedNode = textNode;

        Mock<ISelectedState> selectedState = mocker.GetMock<ISelectedState>();
        selectedState.Setup(x => x.SelectedStandardElement).Returns(textStandard);
        selectedState.Setup(x => x.SelectedTreeNode).Returns(textNode);
        selectedState.Setup(x => x.SelectedTreeNodes).Returns(new List<ITreeNode> { textNode });
        // An unsaved project has no folder, so the element has no file path.
        mocker.GetMock<Gum.Commands.IFileCommands>()
            .Setup(x => x.GetFullPathXmlFile(textStandard, "Text"))
            .Returns((ToolsUtilities.FilePath?)null);

        IReadOnlyList<ContextMenuItemViewModel> items = manager.BuildContextMenuItems();
        ContextMenuItemViewModel viewInExplorer = items.Single(item => item.Text == "View in explorer");

        Should.NotThrow(() => viewInExplorer.Action!());

        mocker.GetMock<IFileSystemRevealService>().Verify(x => x.RevealFile(It.IsAny<string>()), Times.Never);
    }
}
