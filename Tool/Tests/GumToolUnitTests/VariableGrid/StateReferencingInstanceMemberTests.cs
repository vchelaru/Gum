using Gum.Commands;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq.AutoMock;
using Shouldly;
using System;
using System.ComponentModel;

namespace GumToolUnitTests.VariableGrid;

public class StateReferencingInstanceMemberTests
{
    private readonly AutoMocker _mocker;

    public StateReferencingInstanceMemberTests()
    {
        _mocker = new();
    }

    [Fact]
    public void SetValue_ShouldStoreLastValue()
    {
        StateSave stateSave = new StateSave();
        stateSave.SetValue("testVariableName", 3);

        var sut = new StateReferencingInstanceMember(
            new Attribute[0],
            null,
            typeof(int),
            false,
            false,
            true,
            stateSave,
            null,
            "testVariableName",
            null,
            null,
            _mocker.Get<IUndoManager>(),
            _mocker.Get<IEditVariableService>(),
            _mocker.Get<IExposeVariableService>(),
            _mocker.Get<HotkeyManager>(),
            _mocker.Get<IDeleteVariableService>(),
            _mocker.Get<ISelectedState>(),
            _mocker.Get<IGuiCommands>(),
            _mocker.Get<IFileCommands>(),
            _mocker.Get<ISetVariableLogic>(),
            _mocker.Get<WireframeObjectManager>());

        sut.SetValue(1, WpfDataUi.DataTypes.SetPropertyCommitType.Full);
        sut.LastOldFullCommitValue.ShouldBe(3);
    }
}
