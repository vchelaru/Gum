using CommunityToolkit.Mvvm.Messaging;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Messages;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Gum.Presentation.Tests.Logic;

// #4846: paste and Ctrl+Shift-click share one add destination, so an add that only moved the
// selection to what it created does not redirect the next paste.
public class CopyPasteLogicDestinationTests : BaseTestClass
{
    private readonly WeakReferenceMessenger _messenger = new();
    private readonly AddDestinationTracker _tracker;
    private readonly CopyPasteLogic _copyPasteLogic;
    private readonly ComponentSave _component;
    private readonly InstanceSave _container;
    private readonly InstanceSave _copied;

    public CopyPasteLogicDestinationTests()
    {
        AutoMocker mocker = new AutoMocker();
        mocker.Use<IMessenger>(_messenger);
        _tracker = new AddDestinationTracker(_messenger);
        mocker.Use<IAddDestinationTracker>(_tracker);

        ObjectFinder.Self.GumProjectSave = new GumProjectSave();

        _component = new ComponentSave { Name = "MyComponent" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = _component };
        _component.States.Add(defaultState);

        _container = new InstanceSave { Name = "Container", BaseType = "Container", ParentContainer = _component };
        _copied = new InstanceSave { Name = "Copied", BaseType = "Text", ParentContainer = _component };
        _component.Instances.Add(_container);
        _component.Instances.Add(_copied);

        GumTreeNode componentsRoot = new GumTreeNode("Components");
        GumTreeNode elementNode = (GumTreeNode)componentsRoot.AddChild("MyComponent");
        elementNode.SetTag(_component);
        Mock<IElementTreeRoots> elementTreeRoots = mocker.GetMock<IElementTreeRoots>();
        elementTreeRoots.Setup(x => x.Components).Returns(componentsRoot);
        mocker.Use(new Lazy<IElementTreeRoots>(() => elementTreeRoots.Object));

        Mock<ISelectedState> selectedState = mocker.GetMock<ISelectedState>();
        selectedState.Setup(x => x.SelectedElement).Returns(_component);
        selectedState.Setup(x => x.SelectedInstances).Returns(new List<InstanceSave> { _copied });
        selectedState.Setup(x => x.SelectedInstance).Returns(_copied);
        selectedState.Setup(x => x.SelectedStateSave).Returns(defaultState);
        selectedState.Setup(x => x.SelectedStateCategorySave).Returns((StateSaveCategory?)null);

        _copyPasteLogic = mocker.CreateInstance<CopyPasteLogic>();
    }

    [Fact]
    public void OnPaste_AfterAddIntoRememberedDestination_PastesIntoThatDestination()
    {
        _copyPasteLogic.OnCopy(CopyType.InstanceOrElement);

        // A Ctrl+Shift-click add under Container: the add selects what it created, which must
        // not count as the user picking a new paste target.
        _tracker.RunAdd(_container, () => _messenger.Send(new SelectionChangedMessage()));

        _copyPasteLogic.OnPaste(CopyType.InstanceOrElement);

        InstanceSave pasted = _component.Instances.Single(i => i != _container && i != _copied);
        _component.DefaultState.GetValue($"{pasted.Name}.Parent").ShouldBe("Container");
    }
}
