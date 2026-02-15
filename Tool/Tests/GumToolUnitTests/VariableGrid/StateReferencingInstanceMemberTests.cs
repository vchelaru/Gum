using FlatRedBall.Glue.StateInterpolation;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
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

namespace GumToolUnitTests.VariableGrid;

public class StateReferencingInstanceMemberTests
{
    private readonly AutoMocker _mocker;

    StateReferencingInstanceMember _sut;

    public StateReferencingInstanceMemberTests()
    {
        _mocker = new();
        StateSave stateSave = new StateSave();
        stateSave.SetValue("testVariableName", 3);

        Mock<ISetVariableLogic> setVariableLogic = _mocker.GetMock<ISetVariableLogic>();
        setVariableLogic
            .Setup(x => x.PropertyValueChanged(It.IsAny<string>(), It.IsAny<object?>(),
                It.IsAny<InstanceSave>(), It.IsAny<StateSave>() , It.IsAny<bool>() , It.IsAny<bool>(),
                It.IsAny<bool>()))
            .Returns(GeneralResponse.SuccessfulResponse);

        _sut = new StateReferencingInstanceMember(
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
            _mocker.Get<IStateContainer>(),
            _mocker.Get<IUndoManager>(),
            _mocker.Get<IEditVariableService>(),
            _mocker.Get<IExposeVariableService>(),
            _mocker.Get<IHotkeyManager>(),
            _mocker.Get<IDeleteVariableService>(),
            _mocker.Get<ISelectedState>(),
            _mocker.Get<IGuiCommands>(),
            _mocker.Get<IFileCommands>(),
            setVariableLogic.Object,
            _mocker.Get<IWireframeObjectManager>());
    }

    [Fact]
    public void SetValue_ShouldStoreLastValue()
    {

        ComponentSave componentSave = new ComponentSave();
        componentSave.States.Add(new StateSave
        {

        });

        _sut.Instance = componentSave;

        _sut.SetValue(1, WpfDataUi.DataTypes.SetPropertyCommitType.Full);
        _sut.LastOldFullCommitValue.ShouldBe(3);
    }
}
