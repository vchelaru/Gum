using Gum.Controls;
using Gum.DataTypes;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.TreeView;
using Moq;
using Moq.AutoMock;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests;

/// <summary>
/// #4882: Ctrl-click on a Standards chip and Ctrl+Shift-click on an element node add an instance
/// and select it without scrolling the tree, so users can keep adding instances one after another
/// without the view jumping.
/// </summary>
public class ElementTreeViewManagerAddInstanceScrollTests : BaseTestClass
{
    private static (ElementTreeViewManager manager, GumTreeNodeCollection nodes, TreeSelectionModel selection, Action<TreeModifierKeys> setModifiers)
        CreateManager(AutoMocker mocker)
    {
        GumTreeNodeCollection nodes = new GumTreeNodeCollection();
        TreeModifierKeys[] modifiersBox = { TreeModifierKeys.None };
        TreeSelectionModel selection = new TreeSelectionModel(nodes, () => modifiersBox[0], _ => { })
        {
            // Matches the real views (AvaloniaElementTreeView/WpfElementTreeView): selection reacts
            // on release, not push, so Ctrl+Shift-click can still see the pre-click selection.
            IsSelectingOnPush = false,
        };

        Mock<IElementTreeView> view = mocker.GetMock<IElementTreeView>();
        view.Setup(x => x.Nodes).Returns(nodes);
        view.Setup(x => x.Selection).Returns(selection);
        mocker.GetMock<IElementTreeViewFactory>().Setup(x => x.Create()).Returns(view.Object);

        ElementTreeViewManager manager = mocker.CreateInstance<ElementTreeViewManager>();
        manager.Initialize();

        return (manager, nodes, selection, modifiers => modifiersBox[0] = modifiers);
    }

    [Fact]
    public void AddStandardAtDestination_ChipCtrlClick_SuppressesEnsureVisibleOnlyDuringTheAdd()
    {
        AutoMocker mocker = new AutoMocker();
        (ElementTreeViewManager manager, _, _, _) = CreateManager(mocker);

        StandardElementSave textStandard = new StandardElementSave { Name = "Text" };
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave.StandardElements.Add(textStandard);

        bool? suppressedDuringAdd = null;
        mocker.GetMock<IAddInstanceLogic>()
            .Setup(x => x.AddInstanceAtDestination(textStandard, null))
            .Callback(() => suppressedDuringAdd = manager.SuppressNextEnsureVisible);

        mocker.GetMock<IElementTreeView>().Raise(x => x.AddStandardToCurrentRequested += null, "Text");

        suppressedDuringAdd.ShouldBe(true);
        manager.SuppressNextEnsureVisible.ShouldBeFalse();
    }

    [Fact]
    public void HandleAddAsChildOfSelectionRequested_ElementNodeCtrlShiftClick_SuppressesEnsureVisibleOnlyDuringTheAdd()
    {
        AutoMocker mocker = new AutoMocker();
        (ElementTreeViewManager manager, GumTreeNodeCollection nodes, TreeSelectionModel selection, Action<TreeModifierKeys> setModifiers) = CreateManager(mocker);

        ComponentSave initial = new ComponentSave { Name = "Initial" };
        ComponentSave target = new ComponentSave { Name = "Target" };
        GumTreeNode initialNode = new GumTreeNode { Tag = initial };
        GumTreeNode targetNode = new GumTreeNode { Tag = target };
        nodes.Add(initialNode);
        nodes.Add(targetNode);

        // Ctrl+Shift-click requires an existing selection to add the new node as a child of.
        selection.HandlePointerPressed(initialNode, TreePointerButton.Left);
        selection.HandlePointerReleased(initialNode, TreePointerButton.Left);

        bool? suppressedDuringAdd = null;
        mocker.GetMock<IAddInstanceLogic>()
            .Setup(x => x.AddInstanceAtDestination(target, null))
            .Callback(() => suppressedDuringAdd = manager.SuppressNextEnsureVisible);

        setModifiers(TreeModifierKeys.Control | TreeModifierKeys.Shift);
        selection.HandlePointerPressed(targetNode, TreePointerButton.Left);
        selection.HandlePointerReleased(targetNode, TreePointerButton.Left);

        suppressedDuringAdd.ShouldBe(true);
        manager.SuppressNextEnsureVisible.ShouldBeFalse();
    }

    [Fact]
    public void Select_SkipsEnsureVisibleWhenSuppressed_ButScrollsToItOtherwise()
    {
        AutoMocker mocker = new AutoMocker();
        (ElementTreeViewManager manager, _, _, _) = CreateManager(mocker);

        ComponentSave component = new ComponentSave { Name = "MyComponent" };
        InstanceSave first = new InstanceSave { Name = "First", ParentContainer = component };
        InstanceSave second = new InstanceSave { Name = "Second", ParentContainer = component };
        GumTreeNode componentNode = new GumTreeNode { Tag = component };
        GumTreeNode firstNode = new GumTreeNode { Tag = first };
        GumTreeNode secondNode = new GumTreeNode { Tag = second };
        componentNode.AddChild(firstNode);
        componentNode.AddChild(secondNode);
        // GetTreeNodeFor(ComponentSave) searches under the "Components" root node specifically, not
        // just any root-level node, so the component must be nested under it, not under `nodes` directly.
        ((ITreeNodeMutable)((IElementTreeRoots)manager).Components!).AddChild(componentNode);

        manager.SuppressNextEnsureVisible = true;
        manager.Select(new[] { first });
        manager.EnsureVisibleCallCount.ShouldBe(0);

        manager.SuppressNextEnsureVisible = false;
        manager.Select(new[] { second });
        manager.EnsureVisibleCallCount.ShouldBe(1);
    }
}
