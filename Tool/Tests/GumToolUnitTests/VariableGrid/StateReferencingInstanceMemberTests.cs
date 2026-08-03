using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.PropertyGridHelpers.Converters;
using Gum.Reflection;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using System.ComponentModel;
using ToolsUtilities;
using WpfDataUi.Controls;
using WpfDataUi.DataTypes;

namespace GumToolUnitTests.VariableGrid;

public class StateReferencingInstanceMemberTests
{
    private readonly AutoMocker _mocker;

    private StateReferencingInstanceMember CreateSut(
        Attribute[] attributes,
        TypeConverter? converter,
        Type? componentType,
        StateSave stateSave,
        string variableName,
        IStateContainer container,
        bool isReadOnly = false)
    {
        return new StateReferencingInstanceMember(
            attributes,
            converter,
            componentType,
            isReadOnly,
            false,
            true,
            stateSave,
            null,
            variableName,
            null,
            container,
            _mocker.Get<ISelectedState>(),
            _mocker.Get<IUndoManager>(),
            _mocker.Get<IGuiCommands>(),
            _mocker.Get<IFileCommands>(),
            _mocker.Get<ISetVariableLogic>(),
            _mocker.Get<IWireframeObjectManager>(),
            _mocker.Get<IPluginManager>(),
            _mocker.Get<IHotkeyManager>(),
            _mocker.Get<IDeleteVariableService>(),
            _mocker.Get<IExposeVariableService>(),
            _mocker.Get<IEditVariableService>(),
            _mocker.Get<ITypeManager>(),
            _mocker.Get<IClipboardService>());
    }

    public StateReferencingInstanceMemberTests()
    {
        _mocker = new();
    }

    /// <summary>
    /// The ctor's standard-variable lookup walks ObjectFinder.Self.GumProjectSave.AllElements, so a
    /// real ComponentSave used as the row's container needs to be registered there for a
    /// custom-variable's ObjectFinder.GetContainerOf call to resolve instead of throwing.
    /// </summary>
    private static ComponentSave CreateComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name };
        component.States.Add(new StateSave());
        component.DefaultState.ParentContainer = component;

        ObjectFinder.Self.GumProjectSave ??= new GumProjectSave();
        ObjectFinder.Self.GumProjectSave.Components.Add(component);

        return component;
    }

    [Fact]
    public void CanRetargetTo_ShouldReturnFalse_WhenPreferredDisplayerDiffers()
    {
        ComponentSave componentSave = CreateComponent("CanRetargetToDisplayerMismatchComponent");
        StateSave stateSave = componentSave.DefaultState;
        stateSave.SetValue("Instance1.Foo", "A");
        stateSave.SetValue("Instance2.Foo", "A");

        StateReferencingInstanceMember comboBoxMember = CreateSut(
            Array.Empty<Attribute>(),
            new AvailableStatesConverter(category: "", _mocker.Get<ISelectedState>()),
            typeof(string), stateSave, "Instance1.Foo", componentSave);
        StateReferencingInstanceMember plainMember = CreateSut(
            Array.Empty<Attribute>(), null, typeof(string), stateSave, "Instance2.Foo", componentSave);

        comboBoxMember.CanRetargetTo(plainMember).ShouldBeFalse();
    }

    [Fact]
    public void CanRetargetTo_ShouldReturnTrue_WhenRootVariableNameAndDisplayerMatch()
    {
        ComponentSave componentSave = CreateComponent("CanRetargetToMatchComponent");
        StateSave stateSave = componentSave.DefaultState;
        stateSave.SetValue("Instance1.Foo", "A");
        stateSave.SetValue("Instance2.Foo", "A");

        StateReferencingInstanceMember first = CreateSut(
            Array.Empty<Attribute>(), null, typeof(string), stateSave, "Instance1.Foo", componentSave);
        StateReferencingInstanceMember second = CreateSut(
            Array.Empty<Attribute>(), null, typeof(string), stateSave, "Instance2.Foo", componentSave);

        first.CanRetargetTo(second).ShouldBeTrue();
    }

    [Fact]
    public void DoCategoriesDiffer_ShouldReturnFalse_WhenMembersOnlyDifferByInstanceQualifier()
    {
        ComponentSave componentSave = CreateComponent("DoCategoriesDifferSameShapeComponent");
        StateSave stateSave = componentSave.DefaultState;
        stateSave.SetValue("Instance1.Visible", true);
        stateSave.SetValue("Instance2.Visible", true);

        StateReferencingInstanceMember first = CreateSut(
            Array.Empty<Attribute>(), null, typeof(bool), stateSave, "Instance1.Visible", componentSave);
        StateReferencingInstanceMember second = CreateSut(
            Array.Empty<Attribute>(), null, typeof(bool), stateSave, "Instance2.Visible", componentSave);

        bool result = PropertyGridManager.DoCategoriesDiffer(
            new InstanceMember[] { first }, new InstanceMember[] { second });

        result.ShouldBeFalse();
    }

    [Fact]
    public void DoCategoriesDiffer_ShouldReturnTrue_WhenRootVariableNamesDiffer()
    {
        ComponentSave componentSave = CreateComponent("DoCategoriesDifferDifferentShapeComponent");
        StateSave stateSave = componentSave.DefaultState;
        stateSave.SetValue("Instance1.Visible", true);
        stateSave.SetValue("Instance2.Width", 10);

        StateReferencingInstanceMember first = CreateSut(
            Array.Empty<Attribute>(), null, typeof(bool), stateSave, "Instance1.Visible", componentSave);
        StateReferencingInstanceMember second = CreateSut(
            Array.Empty<Attribute>(), null, typeof(int), stateSave, "Instance2.Width", componentSave);

        bool result = PropertyGridManager.DoCategoriesDiffer(
            new InstanceMember[] { first }, new InstanceMember[] { second });

        result.ShouldBeTrue();
    }

    [Fact]
    public void PreferredDisplayer_ShouldMapToComboBoxDisplay_WhenConverterProvidesStandardValues()
    {
        ComponentSave componentSave = CreateComponent("PreferredDisplayerComboBoxComponent");
        StateSave stateSave = componentSave.DefaultState;
        stateSave.SetValue("testVariableName", "A");

        var sut = CreateSut(
            Array.Empty<Attribute>(),
            new AvailableStatesConverter(category: "", _mocker.Get<ISelectedState>()),
            typeof(string),
            stateSave,
            "testVariableName",
            componentSave);

        sut.PreferredDisplayer.ShouldBe(typeof(ComboBoxDisplay));
    }

    [Fact]
    public void PreferredDisplayer_ShouldPassThroughExplicitOverride_WhenSet()
    {
        ComponentSave componentSave = CreateComponent("PreferredDisplayerOverrideComponent");
        StateSave stateSave = componentSave.DefaultState;
        stateSave.SetValue("testVariableName", 3);

        var sut = CreateSut(Array.Empty<Attribute>(), null, typeof(int), stateSave, "testVariableName", componentSave);

        // SliderDisplay isn't one of VariableDisplayerKind's known mapped types, so an explicit
        // override must pass through unchanged rather than collapse to Default:
        sut.PreferredDisplayer = typeof(SliderDisplay);

        sut.PreferredDisplayer.ShouldBe(typeof(SliderDisplay));
    }

    [Fact]
    public void Retarget_ShouldEnableEditing_WhenNewEntryIsNotReadOnly()
    {
        ComponentSave componentSave = CreateComponent("RetargetEnablesEditingComponent");
        StateSave stateSave = componentSave.DefaultState;
        stateSave.SetValue("Instance1.Visible", true);
        stateSave.SetValue("Instance2.Visible", true);

        StateReferencingInstanceMember lockedMember = CreateSut(
            Array.Empty<Attribute>(), null, typeof(bool), stateSave, "Instance1.Visible", componentSave, isReadOnly: true);
        StateReferencingInstanceMember unlockedMember = CreateSut(
            Array.Empty<Attribute>(), null, typeof(bool), stateSave, "Instance2.Visible", componentSave, isReadOnly: false);

        Mock<ISetVariableLogic> setVariableLogic = _mocker.GetMock<ISetVariableLogic>();
        setVariableLogic
            .Setup(x => x.PropertyValueChanged(It.IsAny<string>(), It.IsAny<object?>(),
                It.IsAny<InstanceSave>(), It.IsAny<StateSave>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(GeneralResponse.SuccessfulResponse);

        lockedMember.Retarget(unlockedMember.Entry);
        lockedMember.Instance = componentSave;

        lockedMember.SetValue(false, SetPropertyCommitType.Full);

        setVariableLogic.Verify(x => x.PropertyValueChanged(
            It.IsAny<string>(), It.IsAny<object?>(), null, stateSave, true, true, true, true), Times.Once);
    }

    [Fact]
    public void SetValue_ShouldMapFullCommitType_WhenCommitTypeIsFull()
    {
        ComponentSave componentSave = CreateComponent("SetValueFullCommitComponent");
        StateSave stateSave = componentSave.DefaultState;
        stateSave.SetValue("testVariableName", 3);

        Mock<ISetVariableLogic> setVariableLogic = _mocker.GetMock<ISetVariableLogic>();
        setVariableLogic
            .Setup(x => x.PropertyValueChanged(It.IsAny<string>(), It.IsAny<object?>(),
                It.IsAny<InstanceSave>(), It.IsAny<StateSave>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(GeneralResponse.SuccessfulResponse);

        var sut = CreateSut(Array.Empty<Attribute>(), null, typeof(int), stateSave, "testVariableName", componentSave);
        sut.Instance = componentSave;

        sut.SetValue(1, SetPropertyCommitType.Full);

        setVariableLogic.Verify(x => x.PropertyValueChanged(
            "testVariableName", It.IsAny<object?>(), null, stateSave, true, true, true, true), Times.Once);
    }

    [Fact]
    public void SetValue_ShouldMapIntermediateCommitType_WhenCommitTypeIsIntermediate()
    {
        ComponentSave componentSave = CreateComponent("SetValueIntermediateCommitComponent");
        StateSave stateSave = componentSave.DefaultState;
        stateSave.SetValue("testVariableName", 3);

        Mock<ISetVariableLogic> setVariableLogic = _mocker.GetMock<ISetVariableLogic>();
        setVariableLogic
            .Setup(x => x.PropertyValueChanged(It.IsAny<string>(), It.IsAny<object?>(),
                It.IsAny<InstanceSave>(), It.IsAny<StateSave>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(GeneralResponse.SuccessfulResponse);

        var sut = CreateSut(Array.Empty<Attribute>(), null, typeof(int), stateSave, "testVariableName", componentSave);
        sut.Instance = componentSave;

        sut.SetValue(1, SetPropertyCommitType.Intermediate);

        setVariableLogic.Verify(x => x.PropertyValueChanged(
            "testVariableName", It.IsAny<object?>(), null, stateSave, true, false, false, false), Times.Once);
    }

    [Fact]
    public void SetValue_ShouldStoreLastValue()
    {
        StateSave stateSave = new StateSave();
        stateSave.SetValue("testVariableName", 3);

        Mock<ISetVariableLogic> setVariableLogic = _mocker.GetMock<ISetVariableLogic>();
        setVariableLogic
            .Setup(x => x.PropertyValueChanged(It.IsAny<string>(), It.IsAny<object?>(),
                It.IsAny<InstanceSave>(), It.IsAny<StateSave>(), It.IsAny<bool>(), It.IsAny<bool>(),
                It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(GeneralResponse.SuccessfulResponse);

        var sut = CreateSut(Array.Empty<Attribute>(), null, typeof(int), stateSave, "testVariableName", _mocker.Get<IStateContainer>());

        ComponentSave componentSave = new ComponentSave();
        componentSave.States.Add(new StateSave
        {

        });

        sut.Instance = componentSave;

        sut.SetValue(1, WpfDataUi.DataTypes.SetPropertyCommitType.Full);
        sut.LastOldFullCommitValue.ShouldBe(3);
    }
}
