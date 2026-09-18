using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Moq.AutoMock;
using Shouldly;
using Xunit;

namespace Gum.Presentation.Tests.Logic;

public class AddInstanceLogicTests : BaseTestClass
{
    private readonly AutoMocker _mocker = new();
    private readonly AddInstanceLogic _sut;
    private readonly ComponentSave _component;
    private readonly InstanceSave _container;
    private readonly StandardElementSave _text;

    public AddInstanceLogicTests()
    {
        ObjectFinder.Self.GumProjectSave = new GumProjectSave();

        _component = new ComponentSave { Name = "MyComponent" };
        _component.States.Add(new StateSave { Name = "Default", ParentContainer = _component });
        _container = new InstanceSave { Name = "Container", BaseType = "Container", ParentContainer = _component };
        _component.Instances.Add(_container);
        _text = new StandardElementSave { Name = "Text" };

        _mocker.GetMock<ICircularReferenceManager>()
            .Setup(x => x.CanTypeBeAddedToElement(It.IsAny<ElementSave>(), It.IsAny<string>()))
            .Returns(true);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedStateSave).Returns(_component.DefaultState);
        _mocker.GetMock<IUndoManager>().Setup(x => x.RequestLock()).Returns((UndoLock)null!);
        _mocker.GetMock<IElementCommands>()
            .Setup(x => x.GetUniqueNameForNewInstance(It.IsAny<ElementSave>(), It.IsAny<ElementSave>()))
            .Returns("TextInstance");
        _mocker.GetMock<IElementCommands>()
            .Setup(x => x.AddInstance(It.IsAny<ElementSave>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>()))
            .Returns((ElementSave element, string name, string? type, string? parent, int? index) =>
                new InstanceSave { Name = name, BaseType = type ?? "", ParentContainer = element });

        _sut = _mocker.CreateInstance<AddInstanceLogic>();
    }

    [Fact]
    public void AddInstance_IntoElement_AddsAtRootSelectsElementAndRemembersElement()
    {
        InstanceSave? added = _sut.AddInstance(_text, _component);

        added.ShouldNotBeNull();
        _mocker.GetMock<IElementCommands>().Verify(x => x.AddInstance(_component, "TextInstance", "Text", null, (int?)null), Times.Once);
        _mocker.GetMock<ISelectedState>().VerifySet(x => x.SelectedElement = _component);
        _mocker.GetMock<IAddDestinationTracker>().Verify(x => x.Anchor(_component), Times.Once);
    }

    [Fact]
    public void AddInstance_UnderInstance_ParentsToItAndRaisesParentChanged()
    {
        InstanceSave? added = _sut.AddInstance(_text, _container, name: "Label");

        added.ShouldNotBeNull();
        _mocker.GetMock<IElementCommands>().Verify(x => x.AddInstance(_component, "Label", "Text", "Container", (int?)null), Times.Once);
        _mocker.GetMock<ISetVariableLogic>().Verify(x => x.PropertyValueChanged("Parent", null, added!, _component.DefaultState, true, true, true, true), Times.Once);
        // Refreshed after parenting so the child draws attached at once (#973).
        _mocker.GetMock<IWireframeObjectManager>().Verify(x => x.RefreshAll(true, false), Times.Once);
        _mocker.GetMock<IAddDestinationTracker>().Verify(x => x.Anchor(_container), Times.Once);
    }

    [Fact]
    public void AddInstance_NotRememberingContainer_KeepsThePreviousDestination()
    {
        _mocker.GetMock<IAddDestinationTracker>().Setup(x => x.Destination).Returns(_container);

        _sut.AddInstance(_text, _component, rememberContainerAsDestination: false);

        _mocker.GetMock<IAddDestinationTracker>().Verify(x => x.Anchor(_container), Times.Once);
    }

    [Fact]
    public void AddInstance_IntoStandardElement_ShowsMessageAndAddsNothing()
    {
        StandardElementSave standard = new StandardElementSave { Name = "Sprite" };

        InstanceSave? added = _sut.AddInstance(_text, standard);

        added.ShouldBeNull();
        _mocker.GetMock<IDialogService>().Verify(x => x.ShowMessage(It.IsAny<string>(), null, null), Times.Once);
        _mocker.GetMock<IElementCommands>().Verify(x => x.AddInstance(It.IsAny<ElementSave>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<int?>()), Times.Never);
        _mocker.GetMock<IAddDestinationTracker>().Verify(x => x.Anchor(It.IsAny<object?>()), Times.Never);
    }

    [Fact]
    public void AddInstance_CircularReference_ShowsMessageAndAddsNothing()
    {
        _mocker.GetMock<ICircularReferenceManager>()
            .Setup(x => x.CanTypeBeAddedToElement(_component, "Text"))
            .Returns(false);

        InstanceSave? added = _sut.AddInstance(_text, _component);

        added.ShouldBeNull();
        _mocker.GetMock<IDialogService>().Verify(x => x.ShowMessage(It.IsAny<string>(), null, null), Times.Once);
    }

    [Fact]
    public void AddInstance_NonDefaultStateSelectedOnTarget_ShowsMessageAndAddsNothing()
    {
        StateSave categorized = new StateSave { Name = "Highlighted", ParentContainer = _component };
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedElement).Returns(_component);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedStateSave).Returns(categorized);

        InstanceSave? added = _sut.AddInstance(_text, _component);

        added.ShouldBeNull();
        _mocker.GetMock<IDialogService>().Verify(x => x.ShowMessage(It.IsAny<string>(), null, null), Times.Once);
    }

    [Fact]
    public void AddInstance_NoContainer_ShowsMessageAndAddsNothing()
    {
        InstanceSave? added = _sut.AddInstance(_text, null);

        added.ShouldBeNull();
        _mocker.GetMock<IDialogService>().Verify(x => x.ShowMessage(It.IsAny<string>(), null, null), Times.Once);
    }

    [Fact]
    public void AddInstance_IntoBehavior_AddsRequiredInstanceAndRecordsBehaviorForUndo()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "MyBehavior" };
        _mocker.GetMock<IElementCommands>()
            .Setup(x => x.GetUniqueNameForNewInstance(_text, behavior))
            .Returns("TextInstance");

        _sut.AddInstance(_text, behavior);

        _mocker.GetMock<IUndoManager>().Verify(x => x.RecordBehaviorState(behavior), Times.Once);
        _mocker.GetMock<ISelectedState>().VerifySet(x => x.SelectedBehavior = behavior);
        _mocker.GetMock<IElementCommands>().Verify(x => x.AddInstance(behavior, "TextInstance", "Text", null!), Times.Once);
    }

    [Fact]
    public void AddInstanceAtDestination_RememberedDestination_WinsOverSelection()
    {
        _mocker.GetMock<IAddDestinationTracker>().Setup(x => x.Destination).Returns(_container);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedElement).Returns(_component);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedInstance).Returns((InstanceSave?)null);

        _sut.AddInstanceAtDestination(_text);

        _mocker.GetMock<IElementCommands>().Verify(x => x.AddInstance(_component, "TextInstance", "Text", "Container", (int?)null), Times.Once);
    }

    [Fact]
    public void AddInstanceAtDestination_NoDestination_UsesSelectedInstanceThenElement()
    {
        _mocker.GetMock<IAddDestinationTracker>().Setup(x => x.Destination).Returns((object?)null);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedElement).Returns(_component);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedInstance).Returns(_container);

        _sut.AddInstanceAtDestination(_text);
        _mocker.GetMock<IElementCommands>().Verify(x => x.AddInstance(_component, "TextInstance", "Text", "Container", (int?)null), Times.Once);

        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedInstance).Returns((InstanceSave?)null);

        _sut.AddInstanceAtDestination(_text);
        _mocker.GetMock<IElementCommands>().Verify(x => x.AddInstance(_component, "TextInstance", "Text", null, (int?)null), Times.Once);
    }
}
