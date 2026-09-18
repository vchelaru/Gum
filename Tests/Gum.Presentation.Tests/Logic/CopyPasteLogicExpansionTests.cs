using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gum.Presentation.Tests.Logic;

// Pins #4833: pasting a parent instance whose tree node was expanded should leave the new pasted
// instance's node expanded too, instead of defaulting to collapsed like any newly-created node.
public class CopyPasteLogicExpansionTests
{
    private readonly AutoMocker _mocker;
    private readonly CopyPasteLogic _copyPasteLogic;
    private readonly ComponentSave _component;
    private readonly StateSave _defaultState;
    private readonly InstanceSave _panel;
    private readonly InstanceSave _panelChild;
    private readonly GumTreeNode _elementNode;

    public CopyPasteLogicExpansionTests()
    {
        _mocker = new AutoMocker();

        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        StandardElementsManager.Self.Initialize();

        _component = new ComponentSave { Name = "MyComponent" };
        _defaultState = new StateSave { Name = "Default", ParentContainer = _component };
        _component.States.Add(_defaultState);

        _panel = new InstanceSave { Name = "Panel", BaseType = "Container", ParentContainer = _component };
        _panelChild = new InstanceSave { Name = "PanelChild", BaseType = "Text", ParentContainer = _component };
        _component.Instances.Add(_panel);
        _component.Instances.Add(_panelChild);
        _defaultState.SetValue("PanelChild.Parent", "Panel", "string");

        // A minimal headless tree: Components root -> MyComponent -> Panel -> PanelChild.
        GumTreeNode componentsRoot = new GumTreeNode("Components");
        _elementNode = (GumTreeNode)componentsRoot.AddChild("MyComponent");
        _elementNode.SetTag(_component);
        GumTreeNode panelNode = (GumTreeNode)_elementNode.AddChild("Panel");
        panelNode.SetTag(_panel);
        GumTreeNode panelChildNode = (GumTreeNode)panelNode.AddChild("PanelChild");
        panelChildNode.SetTag(_panelChild);

        Mock<IElementTreeRoots> elementTreeRoots = _mocker.GetMock<IElementTreeRoots>();
        elementTreeRoots.Setup(x => x.Components).Returns(componentsRoot);
        // CopyPasteLogic takes this as Lazy<IElementTreeRoots> to break a DI construction cycle
        // (ElementTreeViewManager, the real IElementTreeRoots, takes ICopyPasteLogic itself) -
        // AutoMocker doesn't resolve Lazy<T> on its own, so wire it explicitly.
        _mocker.Use(new Lazy<IElementTreeRoots>(() => elementTreeRoots.Object));

        // Simulates the real ElementTreeViewManager.RefreshUi() that RefreshElementTreeView triggers
        // in production: it adds a node (collapsed by default) for any instance that doesn't have one
        // yet, which is exactly what newly-pasted instances need for the fix under test to act on.
        _mocker.GetMock<IGuiCommands>()
            .Setup(x => x.RefreshElementTreeView(It.IsAny<IInstanceContainer>()))
            .Callback<IInstanceContainer>(container =>
            {
                foreach (InstanceSave instance in ((ElementSave)container).Instances)
                {
                    if (_elementNode.GetTreeNodeFor(instance) == null)
                    {
                        ITreeNodeMutable node = _elementNode.AddChild(instance.Name);
                        node.SetTag(instance);
                    }
                }
            });

        Mock<ISelectedState> selectedState = _mocker.GetMock<ISelectedState>();
        selectedState.Setup(x => x.SelectedElement).Returns(_component);
        selectedState.Setup(x => x.SelectedInstances).Returns(new List<InstanceSave> { _panel });
        selectedState.Setup(x => x.SelectedInstance).Returns(_panel);
        selectedState.Setup(x => x.SelectedStateSave).Returns(_defaultState);
        selectedState.Setup(x => x.SelectedStateCategorySave).Returns((StateSaveCategory?)null);

        _copyPasteLogic = _mocker.CreateInstance<CopyPasteLogic>();
    }

    private List<InstanceSave> CopyAndPastePanel()
    {
        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        return _copyPasteLogic.PasteInstanceSaves(
            _copyPasteLogic.CopiedData.CopiedInstancesRecursive,
            _copyPasteLogic.CopiedData.CopiedStates,
            _component,
            _panel,
            baseElementDefaultStates: _copyPasteLogic.CopiedData.CopiedBaseElementDefaultStates,
            itemsOwnedByReachableStates: _copyPasteLogic.CopiedData.CopiedNamesOwnedByReachableStates,
            instancesToSelectAfterPaste: _copyPasteLogic.CopiedData.CopiedInstancesSelected,
            expandedInstanceNames: _copyPasteLogic.CopiedData.CopiedExpandedInstanceNames);
    }

    [Fact]
    public void PasteInstanceSaves_SourceNodeExpanded_LeavesPastedNodeExpanded()
    {
        GumTreeNode panelNode = (GumTreeNode)_elementNode.Children.Single(n => n.Tag == _panel);
        panelNode.Expand();

        List<InstanceSave> newInstances = CopyAndPastePanel();

        InstanceSave pastedPanel = newInstances.Single(i => i.BaseType == "Container");
        ITreeNode? pastedPanelNode = _elementNode.GetTreeNodeFor(pastedPanel);

        pastedPanelNode.ShouldNotBeNull();
        pastedPanelNode!.IsExpanded.ShouldBeTrue();
    }

    [Fact]
    public void PasteInstanceSaves_SourceNodeCollapsed_LeavesPastedNodeCollapsed()
    {
        // Panel's node starts collapsed (default) - the pasted copy should stay collapsed too.
        List<InstanceSave> newInstances = CopyAndPastePanel();

        InstanceSave pastedPanel = newInstances.Single(i => i.BaseType == "Container");
        ITreeNode? pastedPanelNode = _elementNode.GetTreeNodeFor(pastedPanel);

        pastedPanelNode.ShouldNotBeNull();
        pastedPanelNode!.IsExpanded.ShouldBeFalse();
    }
}
