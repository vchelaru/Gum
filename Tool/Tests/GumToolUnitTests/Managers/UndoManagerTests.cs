using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Plugins;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GumToolUnitTests.Managers;
public class UndoManagerTests
{
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IRenameLogic> _renameLogic;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IMessenger> _messenger;
    private readonly UndoManager _undoManager;

    public UndoManagerTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _renameLogic = new Mock<IRenameLogic>();
        _guiCommands = new Mock<IGuiCommands>();
        _fileCommands = new Mock<IFileCommands>();
        _messenger = new Mock<IMessenger>();

        ComponentSave component = new();
        component.States.Add(new Gum.DataTypes.Variables.StateSave { });


        _selectedState
            .Setup(x => x.SelectedElement)
            .Returns(component);

        _selectedState
            .Setup(x => x.SelectedComponent)
            .Returns(component);

        _selectedState
            .Setup(x => x.SelectedStateSave)
            .Returns(component.DefaultState);


        _undoManager = new UndoManager(
            _selectedState.Object,
            _renameLogic.Object,
            _guiCommands.Object,
            _fileCommands.Object,
            _messenger.Object
            );
    }

    [Fact]
    public void PerformUndo_ShouldRestoreValue()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent;

        component.DefaultState.SetValue("X", 10f);

        _undoManager.RecordState();

        component.DefaultState.SetValue("X", 11f);

        _undoManager.RecordUndo();

        component.DefaultState.GetValueOrDefault<float>("X").ShouldBe(11.0f);

        _undoManager.PerformUndo();

        component.DefaultState.GetValueOrDefault<float>("X").ShouldBe(10.0f);
    }


    [Fact]
    public void CurrentElementHistory_ShouldReportVariableChanges()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent;

        component.DefaultState.SetValue("X", 10f);

        _undoManager.RecordState();

        component.DefaultState.SetValue("X", 11f);

        _undoManager.RecordUndo();

        var elementHistory = _undoManager.CurrentElementHistory;
        elementHistory.Actions.Count.ShouldBe(1);
    }


}
