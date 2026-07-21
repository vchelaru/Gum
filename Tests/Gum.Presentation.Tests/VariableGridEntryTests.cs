using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Reflection;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

public class VariableGridEntryTests : BaseTestClass
{
    private readonly AutoMocker _mocker = new();

    private VariableGridEntry CreateSut(
        string variableName,
        StateSave stateSave,
        IStateContainer container,
        InstanceSave? instanceSave = null,
        bool isVariable = true)
    {
        return new VariableGridEntry(
            Array.Empty<Attribute>(),
            converter: null,
            componentType: typeof(object),
            isReadOnly: false,
            isAssignedByReference: false,
            isVariable,
            stateSave,
            stateSaveCategory: null,
            variableName,
            instanceSave,
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

    private static ComponentSave CreateComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name };
        component.States.Add(new StateSave());
        component.DefaultState.ParentContainer = component;

        // The ctor's standard-variable lookup walks ObjectFinder.Self.GumProjectSave.AllElements,
        // so the component needs to be registered there for a custom variable's ObjectFinder.GetContainerOf
        // call to resolve instead of throwing.
        ObjectFinder.Self.GumProjectSave ??= new GumProjectSave();
        ObjectFinder.Self.GumProjectSave.Components.Add(component);

        return component;
    }

    [Fact]
    public void BuildContextMenuActions_ShouldCopyQualifiedVariableName_WhenClicked()
    {
        ComponentSave component = CreateComponent("MyComponent");
        component.DefaultState.SetValue("X", 5f);

        VariableGridEntry sut = CreateSut("X", component.DefaultState, component);

        List<VariableContextMenuAction> actions = sut.BuildContextMenuActions();
        VariableContextMenuAction copyAction = actions.Single(a => a.Label == "Copy Qualified Variable Name");

        copyAction.Execute();

        _mocker.GetMock<IClipboardService>()
            .Verify(x => x.SetText("Components/MyComponent.X"), Times.Once);
    }

    [Fact]
    public void GetValue_ShouldReturnNameWithoutFolderPrefix_WhenRootVariableIsNameAndElementHasFolder()
    {
        ComponentSave component = CreateComponent("MyFolder/MyComponent");
        VariableGridEntry sut = CreateSut("Name", component.DefaultState, component);

        object? value = sut.GetValue(component);

        value.ShouldBe("MyComponent");
    }

    [Fact]
    public void GetValue_ShouldReturnValueFromState_WhenVariableIsSetInState()
    {
        ComponentSave component = CreateComponent("MyComponent");
        component.DefaultState.SetValue("X", 5f);

        VariableGridEntry sut = CreateSut("X", component.DefaultState, component);

        object? value = sut.GetValue(component);

        value.ShouldBe(5f);
    }

    [Fact]
    public void ResetToDefault_ShouldRemoveVariableAndRecordUndo_WhenElementIsNotStandardElement()
    {
        ComponentSave component = CreateComponent("MyComponent");
        component.DefaultState.SetValue("X", 5f);

        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedElement).Returns(component);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedStateSave).Returns(component.DefaultState);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedInstance).Returns((InstanceSave?)null);

        VariableGridEntry sut = CreateSut("X", component.DefaultState, component);

        sut.ResetToDefault();

        component.DefaultState.GetVariableSave("X").ShouldBeNull();
        _mocker.GetMock<IUndoManager>().Verify(x => x.RecordUndo(), Times.Once);
        _mocker.GetMock<IWireframeObjectManager>().Verify(x => x.RefreshAll(true, false), Times.Once);
        _mocker.GetMock<IPluginManager>().Verify(x => x.VariableSet(component, null, "X", 5f), Times.Once);
    }

    [Fact]
    public void RootVariableName_ShouldReturnPortionAfterLastDot_WhenNameIsDotted()
    {
        ComponentSave component = CreateComponent("MyComponent");
        VariableGridEntry sut = CreateSut("SpriteInstance.X", component.DefaultState, component);

        sut.RootVariableName.ShouldBe("X");
    }

    [Fact]
    public void SetValue_ShouldStoreLastOldFullCommitValue_WhenCommitTypeIsFull()
    {
        ComponentSave component = CreateComponent("MyComponent");
        component.DefaultState.SetValue("X", 3);

        _mocker.GetMock<ISetVariableLogic>()
            .Setup(x => x.PropertyValueChanged(
                It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<InstanceSave>(), It.IsAny<StateSave>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(GeneralResponse.SuccessfulResponse);

        VariableGridEntry sut = CreateSut("X", component.DefaultState, component);

        sut.SetValue(component, 1, VariablePropertyCommitType.Full);

        sut.LastOldFullCommitValue.ShouldBe(3);
    }
}
