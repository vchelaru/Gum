using Gum.Controls;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace Gum.Presentation.Tests.TreeView;

/// <summary>
/// The tree's "!" indicator follows every error check of an element rather than running its own
/// check on the notifications the Errors tab already checks for (issue #4950).
/// </summary>
public class MainTreeViewPluginErrorIndicatorTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private readonly Mock<IErrorChecker> _errorChecker;
    private readonly GumProjectSave _project;
    private readonly ScreenSave _screen;
    private readonly ElementTreeViewManager _treeViewManager;
    private readonly MainTreeViewPlugin _plugin;

    public MainTreeViewPluginErrorIndicatorTests()
    {
        _mocker = new AutoMocker();

        _project = new GumProjectSave();
        _screen = new ScreenSave { Name = "MyScreen" };
        _project.Screens.Add(_screen);

        _errorChecker = _mocker.GetMock<IErrorChecker>();
        _errorChecker.Setup(c => c.GetErrorsFor(It.IsAny<ElementSave>(), It.IsAny<GumProjectSave>()))
            .Returns(new ErrorViewModel[0]);

        GumTreeNodeCollection nodes = new GumTreeNodeCollection();
        Mock<IElementTreeView> view = _mocker.GetMock<IElementTreeView>();
        view.Setup(x => x.Nodes).Returns(nodes);
        view.Setup(x => x.Selection).Returns(new TreeSelectionModel(nodes, () => TreeModifierKeys.None, _ => { }));
        _mocker.GetMock<IElementTreeViewFactory>().Setup(x => x.Create()).Returns(view.Object);
        _mocker.Use(new TreeNodeImageLogic());

        _treeViewManager = _mocker.CreateInstance<ElementTreeViewManager>();
        // Initialize builds the empty root nodes before any project is loaded; the screen's node is
        // added by hand because a project refresh needs a saved project on disk.
        _treeViewManager.Initialize();
        ((ITreeNodeMutable)((IElementTreeRoots)_treeViewManager).Screens!).AddChild(new GumTreeNode { Tag = _screen });
        _mocker.GetMock<IProjectState>().Setup(p => p.GumProjectSave).Returns(_project);
        _mocker.Use(_treeViewManager);
        _plugin = _mocker.CreateInstance<MainTreeViewPlugin>();
        _plugin.StartUp();
    }

    [Fact]
    public void ErrorsChecked_ShowsTheIndicator_WhenTheCheckFoundErrors()
    {
        GumTreeNode screenNode = _treeViewManager.GetTreeNodeFor(_screen).ShouldNotBeNull();

        _errorChecker.Raise(c => c.ErrorsChecked += null, _screen, new[] { new ErrorViewModel { Message = "Broken" } });

        screenNode.ImageIndex.ShouldBe(TreeNodeImageIndices.ExclamationIndex);
    }

    [Theory]
    [InlineData("VariableSet")]
    [InlineData("ElementReloaded")]
    [InlineData("VariableRemovedFromCategory")]
    [InlineData("BehaviorReferencesChanged")]
    public void ANotificationTheErrorsTabChecksFor_DoesNotCheckForErrorsAgain(string notification)
    {
        _mocker.GetMock<ISelectedState>().Setup(s => s.SelectedElement).Returns(_screen);
        InstanceSave instance = new InstanceSave { Name = "Child", BaseType = "Sprite", ParentContainer = _screen };

        switch (notification)
        {
            case "VariableSet":
                _plugin.CallVariableSet(_screen, null, "Red", 0, isFullCommit: true);
                break;
            case "InstanceAdd":
                _plugin.CallInstanceAdd(_screen, instance);
                break;
            case "InstanceDelete":
                _plugin.CallInstanceDelete(_screen, instance);
                break;
            case "ElementReloaded":
                _plugin.CallElementReloaded(_screen);
                break;
            case "VariableRemovedFromCategory":
                _plugin.CallVariableRemovedFromCategory("Red", new StateSaveCategory { Name = "Category" });
                break;
            case "BehaviorReferencesChanged":
                _plugin.CallBehaviorReferencesChanged(_screen);
                break;
        }

        _errorChecker.Verify(
            c => c.GetErrorsFor(It.IsAny<ElementSave>(), It.IsAny<GumProjectSave>()),
            Times.Never);
    }
}
