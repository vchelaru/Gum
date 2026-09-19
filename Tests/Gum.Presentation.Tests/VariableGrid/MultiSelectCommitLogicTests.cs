using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
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
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests.VariableGrid;

/// <summary>
/// A multi-select edit writes one value to every selected instance. The per-instance reaction
/// (circular-reference check, inheritance propagation, plugin notification) still runs per
/// instance, but the grid/tree rebuild and the undo record happen once for the batch - a rebuild
/// per instance is what froze the tool on a many-instance BaseType change (#4842).
/// </summary>
public class MultiSelectCommitLogicTests : BaseTestClass
{
    private readonly AutoMocker _mocker;
    private int _undoLockDisposals;

    public MultiSelectCommitLogicTests()
    {
        _mocker = new AutoMocker();
        _undoLockDisposals = 0;
        _mocker.GetMock<IUndoManager>().Setup(x => x.RequestLock()).Returns(new UndoLock(() => _undoLockDisposals++));
        _mocker.GetMock<ISetVariableLogic>()
            .Setup(x => x.PropertyValueChanged(
                It.IsAny<string>(), It.IsAny<object?>(), It.IsAny<InstanceSave>(), It.IsAny<StateSave>(),
                It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns(GeneralResponse.SuccessfulResponse);
    }

    /// <summary>Builds the selected screen; rows write into the selected state, so both are set on the mock.</summary>
    private ScreenSave CreateScreenWithTextInstances(int instanceCount)
    {
        ScreenSave screen = new ScreenSave { Name = "MultiSelectScreen" };
        screen.States.Add(new StateSave { Name = "Default" });
        screen.DefaultState.ParentContainer = screen;
        for (int i = 0; i < instanceCount; i++)
        {
            screen.Instances.Add(new InstanceSave { Name = $"Text{i}", BaseType = "Text", ParentContainer = screen });
        }

        StandardElementSave text = new StandardElementSave { Name = "Text" };
        text.States.Add(new StateSave { Name = "Default", ParentContainer = text });

        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave.Screens.Add(screen);
        ObjectFinder.Self.GumProjectSave.StandardElements.Add(text);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedElement).Returns(screen);
        _mocker.GetMock<ISelectedState>().Setup(x => x.SelectedStateSave).Returns(screen.DefaultState);
        return screen;
    }

    private StateReferencingInstanceMember CreateRow(ScreenSave screen, InstanceSave instance, string rootVariableName)
    {
        return new StateReferencingInstanceMember(
            Array.Empty<Attribute>(),
            converter: null,
            componentType: typeof(string),
            isReadOnly: false,
            isAssignedByReference: false,
            isVariable: true,
            screen.DefaultState,
            stateSaveCategory: null,
            $"{instance.Name}.{rootVariableName}",
            instance,
            screen,
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

    private MultiSelectInstanceMember CreateAttachedMultiSelect(ScreenSave screen, string rootVariableName)
    {
        List<InstanceMember> rows = screen.Instances
            .Select(instance => (InstanceMember)CreateRow(screen, instance, rootVariableName))
            .ToList();
        MultiSelectInstanceMember multiSelect = new MultiSelectInstanceMember
        {
            Name = rootVariableName,
            DisplayName = rootVariableName,
            InstanceMembers = rows,
        };

        MultiSelectCommitLogic sut = _mocker.CreateInstance<MultiSelectCommitLogic>();
        sut.Attach(multiSelect);

        return multiSelect;
    }

    [Fact]
    public void Attach_ShouldNotLockRefreshOrRecordUndo_WhenCommitIsIntermediate()
    {
        ScreenSave screen = CreateScreenWithTextInstances(instanceCount: 3);
        MultiSelectInstanceMember multiSelect = CreateAttachedMultiSelect(screen, "X");

        multiSelect.SetValue(12f, SetPropertyCommitType.Intermediate);

        screen.DefaultState.GetValue("Text0.X").ShouldBe(12f);
        screen.DefaultState.GetValue("Text2.X").ShouldBe(12f);
        _mocker.GetMock<IUndoManager>().Verify(x => x.RequestLock(), Times.Never);
        _mocker.GetMock<IUndoManager>().Verify(x => x.RecordUndo(), Times.Never);
        _mocker.GetMock<ISetVariableLogic>().Verify(
            x => x.RefreshInResponseToVariableChange(It.IsAny<string>(), It.IsAny<ElementSave>(), It.IsAny<InstanceSave?>()),
            Times.Never);
    }

    [Fact]
    public void Attach_ShouldReactPerInstanceWithoutRefresh_ThenRefreshOnce_WhenCommitIsFull()
    {
        ScreenSave screen = CreateScreenWithTextInstances(instanceCount: 3);
        MultiSelectInstanceMember multiSelect = CreateAttachedMultiSelect(screen, "BaseType");

        multiSelect.SetValue("Label", SetPropertyCommitType.Full);

        screen.Instances.ShouldAllBe(instance => instance.BaseType == "Label");
        foreach (InstanceSave instance in screen.Instances)
        {
            _mocker.GetMock<ISetVariableLogic>().Verify(x => x.PropertyValueChanged(
                "BaseType", "Text", instance, screen.DefaultState, false, false, true, true), Times.Once);
        }
        _mocker.GetMock<ISetVariableLogic>().Verify(
            x => x.RefreshInResponseToVariableChange("BaseType", screen, screen.Instances[0]), Times.Once);
        _mocker.GetMock<ISetVariableLogic>().Verify(
            x => x.RefreshInResponseToVariableChange(It.IsAny<string>(), It.IsAny<ElementSave>(), It.IsAny<InstanceSave?>()),
            Times.Once);
    }

    [Fact]
    public void Attach_ShouldRecordOneUndoForTheBatch_WhenCommitIsFull()
    {
        ScreenSave screen = CreateScreenWithTextInstances(instanceCount: 2);
        MultiSelectInstanceMember multiSelect = CreateAttachedMultiSelect(screen, "BaseType");
        int lockDisposalsBeforeRecord = -1;
        _mocker.GetMock<IUndoManager>()
            .Setup(x => x.RecordUndo())
            .Callback(() => lockDisposalsBeforeRecord = _undoLockDisposals);

        multiSelect.SetValue("Label", SetPropertyCommitType.Full);

        _mocker.GetMock<IUndoManager>().Verify(x => x.RequestLock(), Times.Once);
        _undoLockDisposals.ShouldBe(1);
        lockDisposalsBeforeRecord.ShouldBe(1);
    }
}
