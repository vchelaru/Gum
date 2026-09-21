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
    public void GetValue_ShouldReturnNull_WhenIsVariableFalseAndStateSaveIsNull()
    {
        // Issue #4643: entries built for a behavior's required instance pass a null StateSave.
        // isVariable=false (VariableList-backed) entries must not crash if that ever happens.
        ComponentSave component = CreateComponent("MyComponent");
        VariableGridEntry sut = CreateSut("SomeList", stateSave: null!, component, isVariable: false);

        object? value = sut.GetValue(component);

        value.ShouldBeNull();
    }

    [Fact]
    public void SetValue_ShouldNotThrow_WhenIsVariableFalseAndStateSaveIsNull()
    {
        // Issue #4643: the isVariable=false write path dereferenced the (nullable-in-practice)
        // StateSave unguarded; assert it degrades to a no-op instead of throwing.
        ComponentSave component = CreateComponent("MyComponent");
        VariableGridEntry sut = CreateSut("SomeList", stateSave: null!, component, isVariable: false);

        Should.NotThrow(() => sut.SetValue(component, new List<string> { "a" }, VariablePropertyCommitType.Full));
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
    public void NotifyVariableLogic_ShouldNotRefreshOrRecordUndo_WhenDeferringToMultiSelect()
    {
        // A multi-select edit sets every wrapped row in a loop; each row must write its value and
        // run the per-variable reaction, but the grid rebuild and undo belong to the batch owner.
        ComponentSave component = CreateComponent("DeferredRefreshComponent");
        component.DefaultState.SetValue("X", 3);
        VariableGridEntry sut = CreateSut("X", component.DefaultState, component);
        sut.IsCallingRefresh = false;

        sut.SetValue(component, 1, VariablePropertyCommitType.Full);

        _mocker.GetMock<ISetVariableLogic>().Verify(x => x.PropertyValueChanged(
            "X", 3, null, component.DefaultState, false, false, true, true), Times.Once);
    }

    [Fact]
    public void NotifyVariableLogic_ShouldRefreshAndRecordUndo_WhenSingleRowCommitsFully()
    {
        ComponentSave component = CreateComponent("SingleRowFullCommitComponent");
        component.DefaultState.SetValue("X", 3);
        VariableGridEntry sut = CreateSut("X", component.DefaultState, component);

        sut.SetValue(component, 1, VariablePropertyCommitType.Full);

        _mocker.GetMock<ISetVariableLogic>().Verify(x => x.PropertyValueChanged(
            "X", 3, null, component.DefaultState, true, true, true, true), Times.Once);
    }

    [Fact]
    public void NotifyVariableLogic_ShouldRefreshWithoutUndoOrSave_WhenSingleRowCommitIsIntermediate()
    {
        ComponentSave component = CreateComponent("SingleRowIntermediateComponent");
        component.DefaultState.SetValue("X", 3);
        VariableGridEntry sut = CreateSut("X", component.DefaultState, component);

        sut.SetValue(component, 1, VariablePropertyCommitType.Intermediate);

        _mocker.GetMock<ISetVariableLogic>().Verify(x => x.PropertyValueChanged(
            "X", It.IsAny<object?>(), null, component.DefaultState, true, false, false, false), Times.Once);
    }

    [Fact]
    public void PreferredDisplayerKind_ShouldReturnKindOverride_WhenSet()
    {
        // ElementSaveDisplayer (headless) can't reference a concrete WpfDataUi control Type, so it
        // forces a displayer kind directly (e.g. defaulting variable lists to ListBox) rather than
        // going through PreferredDisplayerOverride's raw Type.
        ComponentSave component = CreateComponent("MyComponent");
        VariableGridEntry sut = CreateSut("X", component.DefaultState, component);

        sut.PreferredDisplayerKind.ShouldBe(VariableDisplayerKind.Default);

        sut.PreferredDisplayerKindOverride = VariableDisplayerKind.ListBox;

        sut.PreferredDisplayerKind.ShouldBe(VariableDisplayerKind.ListBox);
    }

    [Fact]
    public void PreferredDisplayerKind_ShouldClassifyNeutralDisplayerKeys()
    {
        // StandardElementsManagerGumTool assigns neutral keys, not head controls, to variables.
        ComponentSave component = CreateComponent("MyComponent");
        VariableGridEntry sut = CreateSut("X", component.DefaultState, component);

        sut.PreferredDisplayerOverride = typeof(WpfDataUi.DataTypes.StandardDisplayers.FileSelection);
        sut.PreferredDisplayerKind.ShouldBe(VariableDisplayerKind.FileSelection);

        sut.PreferredDisplayerOverride = typeof(WpfDataUi.DataTypes.StandardDisplayers.MultiLineTextBox);
        sut.PreferredDisplayerKind.ShouldBe(VariableDisplayerKind.MultiLineTextBox);
    }

    [Fact]
    public void RecomputeDetailTextOnValueChanged_ShouldUpdateDetailText_WhenInvoked()
    {
        // Mirrors the WPF adapter's usage: a headless caller (ElementSaveDisplayer's XUnits/YUnits
        // subtext) stores a recompute delegate here; the adapter invokes it and re-reads DetailText
        // whenever its own "Value" change notification fires.
        ComponentSave component = CreateComponent("MyComponent");
        VariableGridEntry sut = CreateSut("X", component.DefaultState, component);
        sut.DetailText = "initial";

        sut.RecomputeDetailTextOnValueChanged = () => sut.DetailText = "recomputed";
        sut.RecomputeDetailTextOnValueChanged();

        sut.DetailText.ShouldBe("recomputed");
    }

    [Fact]
    public void GetMakeDefaultPreviewValue_ShouldReturnBaseValue_WhenVariableIsOverriddenOnDerivedComponent()
    {
        // #4893: "Make Default" should preview the value it would restore - here, the derived
        // component's explicit override of X should fall away to the base component's value.
        ComponentSave baseComponent = CreateComponent("MakeDefaultPreviewBase");
        baseComponent.DefaultState.SetValue("X", 1f);

        ComponentSave derivedComponent = new() { Name = "MakeDefaultPreviewDerived", BaseType = "MakeDefaultPreviewBase" };
        derivedComponent.States.Add(new StateSave { Name = "Default", ParentContainer = derivedComponent });
        derivedComponent.DefaultState.SetValue("X", 2f);
        ObjectFinder.Self.GumProjectSave!.Components.Add(derivedComponent);

        VariableGridEntry sut = CreateSut("X", derivedComponent.DefaultState, derivedComponent);

        sut.GetValue(derivedComponent).ShouldBe(2f, "the explicit override should still be the displayed value.");
        sut.GetMakeDefaultPreviewValue().ShouldBe(1f, "Make Default should preview the base component's value.");
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
