using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Undo;
using Moq;
using Shouldly;

namespace GumToolUnitTests.Plugins.InternalPlugins.VariableGrid;

public class DeleteVariableServiceTests : BaseTestClass
{
    private readonly Mock<IUndoManager> _undoManager;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IRenameLogic> _renameLogic;
    private readonly Mock<IDialogService> _dialogService;
    private readonly Mock<IPluginManager> _pluginManager;
    private readonly DeleteVariableService _service;

    public DeleteVariableServiceTests()
    {
        _undoManager = new Mock<IUndoManager>();
        _undoManager.Setup(x => x.RequestLock()).Returns(new UndoLock(() => { }));
        _fileCommands = new Mock<IFileCommands>();
        _guiCommands = new Mock<IGuiCommands>();
        _renameLogic = new Mock<IRenameLogic>();
        _dialogService = new Mock<IDialogService>();
        _pluginManager = new Mock<IPluginManager>();

        _service = new DeleteVariableService(
            _undoManager.Object,
            _fileCommands.Object,
            _guiCommands.Object,
            _renameLogic.Object,
            _dialogService.Object,
            _pluginManager.Object);
    }

    private static (ComponentSave Owner, VariableSave Variable) MakeOwnerWithCustomVariable()
    {
        var owner = new ComponentSave { Name = "Component1" };
        owner.States.Add(new StateSave { Name = "Default", ParentContainer = owner });
        var variable = new VariableSave { Name = "Variable1", Type = "float", IsCustomVariable = true, Value = 5f };
        owner.DefaultState.Variables.Add(variable);
        return (owner, variable);
    }

    [Fact]
    public void DeleteVariable_WhenReferencedOnlyByInstanceValueOverride_ShouldRemoveFromBothElements()
    {
        var (owner, variable) = MakeOwnerWithCustomVariable();

        var otherScreen = new ScreenSave { Name = "Screen1" };
        otherScreen.States.Add(new StateSave { Name = "Default", ParentContainer = otherScreen });
        var instance = new InstanceSave { Name = "Variable1Instance", BaseType = "Component1", ParentContainer = otherScreen };
        otherScreen.Instances.Add(instance);
        var instanceVariable = new VariableSave { Name = "Variable1Instance.Variable1", Type = "float", Value = 7f };
        otherScreen.DefaultState.Variables.Add(instanceVariable);

        _renameLogic.Setup(x => x.GetChangesForRenamedVariable(owner, variable.Name, variable.GetRootName()))
            .Returns(new VariableChangeResponse
            {
                VariableChanges =
                {
                    new VariableChange { Container = otherScreen, State = otherScreen.DefaultState, Variable = instanceVariable }
                }
            });

        _service.DeleteVariable(variable, owner);

        owner.DefaultState.Variables.ShouldNotContain(variable);
        otherScreen.DefaultState.Variables.ShouldNotContain(instanceVariable);
        _dialogService.Verify(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()), Times.Never);
    }

    [Fact]
    public void DeleteVariable_WhenReferencedOnlyByInstanceValueOverride_ShouldAttachCrossElementRemovalForUndo()
    {
        var (owner, variable) = MakeOwnerWithCustomVariable();

        var otherScreen = new ScreenSave { Name = "Screen1" };
        otherScreen.States.Add(new StateSave { Name = "Default", ParentContainer = otherScreen });
        var instance = new InstanceSave { Name = "Variable1Instance", BaseType = "Component1", ParentContainer = otherScreen };
        otherScreen.Instances.Add(instance);
        var instanceVariable = new VariableSave { Name = "Variable1Instance.Variable1", Type = "float", Value = 7f };
        otherScreen.DefaultState.Variables.Add(instanceVariable);

        _renameLogic.Setup(x => x.GetChangesForRenamedVariable(owner, variable.Name, variable.GetRootName()))
            .Returns(new VariableChangeResponse
            {
                VariableChanges =
                {
                    new VariableChange { Container = otherScreen, State = otherScreen.DefaultState, Variable = instanceVariable }
                }
            });

        _service.DeleteVariable(variable, owner);

        _undoManager.Verify(x => x.AttachCrossElementVariableRemovals(
            It.Is<System.Collections.Generic.IEnumerable<CrossElementVariableChange>>(removals =>
                System.Linq.Enumerable.Single(removals).Container == otherScreen &&
                System.Linq.Enumerable.Single(removals).Instance == instance &&
                System.Linq.Enumerable.Single(removals).Variable == instanceVariable)),
            Times.Once);
    }

    [Fact]
    public void DeleteVariable_WhenReferencedByVariableReferenceBinding_ShouldStillBlock()
    {
        // A VariableReferences binding pointing at the deleted variable is a different, harder problem
        // than a plain instance value override (ADR 0016 scopes it out) - it must keep blocking rather
        // than silently leaving a dangling reference.
        var (owner, variable) = MakeOwnerWithCustomVariable();

        var otherScreen = new ScreenSave { Name = "Screen1" };
        var variableReferenceList = new VariableListSave<string> { Name = "VariableReferences" };
        variableReferenceList.ValueAsIList.Add("SomeInstance.X = Components/Component1.Variable1");

        _renameLogic.Setup(x => x.GetChangesForRenamedVariable(owner, variable.Name, variable.GetRootName()))
            .Returns(new VariableChangeResponse
            {
                VariableReferenceChanges =
                {
                    new VariableReferenceChange { Container = otherScreen, VariableReferenceList = variableReferenceList, LineIndex = 0, ChangedSide = SideOfEquals.Right }
                }
            });

        _service.DeleteVariable(variable, owner);

        owner.DefaultState.Variables.ShouldContain(variable);
        _dialogService.Verify(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<MessageDialogStyle?>()), Times.Once);
    }
}
