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
/// Adding or deleting an instance rebuilds only the owning element's tree nodes, so the cost does not
/// grow with the number of screens and components in the project.
/// </summary>
public class MainTreeViewPluginInstanceRefreshScopeTests : BaseTestClass
{
    private readonly ScreenSave _screen;
    private readonly ScreenSave _otherScreen;
    private readonly GumTreeNode _screenNode;
    private readonly GumTreeNode _otherScreenNode;
    private readonly MainTreeViewPlugin _plugin;

    public MainTreeViewPluginInstanceRefreshScopeTests()
    {
        AutoMocker mocker = new AutoMocker();

        GumProjectSave project = new GumProjectSave();
        _screen = new ScreenSave { Name = "MyScreen" };
        _otherScreen = new ScreenSave { Name = "OtherScreen" };
        _screen.States.Add(new StateSave { Name = "Default", ParentContainer = _screen });
        _otherScreen.States.Add(new StateSave { Name = "Default", ParentContainer = _otherScreen });
        project.Screens.Add(_screen);
        project.Screens.Add(_otherScreen);

        mocker.GetMock<IErrorChecker>()
            .Setup(c => c.GetErrorsFor(It.IsAny<ElementSave>(), It.IsAny<GumProjectSave>()))
            .Returns(new ErrorViewModel[0]);

        GumTreeNodeCollection nodes = new GumTreeNodeCollection();
        Mock<IElementTreeView> view = mocker.GetMock<IElementTreeView>();
        view.Setup(x => x.Nodes).Returns(nodes);
        view.Setup(x => x.Selection).Returns(new TreeSelectionModel(nodes, () => TreeModifierKeys.None, _ => { }));
        mocker.GetMock<IElementTreeViewFactory>().Setup(x => x.Create()).Returns(view.Object);
        mocker.Use(new TreeNodeImageLogic());

        ElementTreeViewManager treeViewManager = mocker.CreateInstance<ElementTreeViewManager>();
        treeViewManager.Initialize();
        ITreeNodeMutable screens = (ITreeNodeMutable)((IElementTreeRoots)treeViewManager).Screens!;
        _screenNode = new GumTreeNode { Tag = _screen, Text = "MyScreen" };
        _otherScreenNode = new GumTreeNode { Tag = _otherScreen, Text = "OtherScreen" };
        screens.AddChild(_screenNode);
        screens.AddChild(_otherScreenNode);
        mocker.GetMock<IProjectState>().Setup(p => p.GumProjectSave).Returns(project);
        mocker.GetMock<ISelectedState>().Setup(s => s.SelectedElement).Returns(_screen);
        mocker.Use(treeViewManager);
        _plugin = mocker.CreateInstance<MainTreeViewPlugin>();
        _plugin.StartUp();
    }

    [Fact]
    public void InstanceAdd_ShowsTheInstanceUnderItsElement_WithoutRefreshingOtherElements()
    {
        InstanceSave added = new InstanceSave { Name = "NewText", BaseType = "Text", ParentContainer = _screen };
        _screen.Instances.Add(added);
        // Only a full refresh would give this one a node, so a node for it would prove other elements were walked.
        _otherScreen.Instances.Add(new InstanceSave { Name = "Untouched", BaseType = "Text", ParentContainer = _otherScreen });

        _plugin.CallInstanceAdd(_screen, added);

        _screenNode.Children.Select(c => c.Text).ShouldContain("NewText");
        _otherScreenNode.Children.ShouldBeEmpty();
    }

    [Fact]
    public void InstanceDelete_RemovesTheInstanceNode_WithoutRefreshingOtherElements()
    {
        InstanceSave existing = new InstanceSave { Name = "OldText", BaseType = "Text", ParentContainer = _screen };
        ((ITreeNodeMutable)_screenNode).AddChild(new GumTreeNode { Tag = existing, Text = "OldText" });
        _otherScreen.Instances.Add(new InstanceSave { Name = "Untouched", BaseType = "Text", ParentContainer = _otherScreen });

        _plugin.CallInstanceDelete(_screen, existing);

        _screenNode.Children.ShouldBeEmpty();
        _otherScreenNode.Children.ShouldBeEmpty();
    }
}
