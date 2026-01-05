using Gum.DataTypes.ComponentModel;
using Gum.DataTypes.Variables;
using Gum.PropertyGridHelpers;
using Moq.AutoMock;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
        _mocker.Use<InstanceSavePropertyDescriptor>(new InstanceSavePropertyDescriptor("testVariableName", typeof(int), new Attribute[0]));
        StateSave stateSave = new StateSave();
        stateSave.SetValue("testVariableName", 3);
        _mocker.Use<StateSave>(stateSave);
        _mocker.Use<string>("testVariableName"); // for the variableName parameter

        StateReferencingInstanceMember _sut = _mocker.CreateInstance<StateReferencingInstanceMember>(true);
        _sut.SetValue(1, WpfDataUi.DataTypes.SetPropertyCommitType.Full);
        _sut.LastOldFullCommitValue.ShouldBe(3);
    }
}
