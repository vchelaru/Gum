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

    [Fact]
    public void DeleteVariable_WhenBothInstanceOverrideAndVariableReferenceBindingExist_ShouldBlockEntirelyRatherThanPartiallyDelete()
    {
        // A mix of a cascadable override and a still-blocking VariableReferences binding must fail
        // closed: nothing gets removed anywhere, not just the binding's own instance.
        var (owner, variable) = MakeOwnerWithCustomVariable();

        var screenWithOverride = new ScreenSave { Name = "Screen1" };
        screenWithOverride.States.Add(new StateSave { Name = "Default", ParentContainer = screenWithOverride });
        var instance = new InstanceSave { Name = "Variable1Instance", BaseType = "Component1", ParentContainer = screenWithOverride };
        screenWithOverride.Instances.Add(instance);
        var instanceVariable = new VariableSave { Name = "Variable1Instance.Variable1", Type = "float", Value = 7f };
        screenWithOverride.DefaultState.Variables.Add(instanceVariable);

        var screenWithBinding = new ScreenSave { Name = "Screen2" };
        var variableReferenceList = new VariableListSave<string> { Name = "VariableReferences" };
        variableReferenceList.ValueAsIList.Add("SomeInstance.X = Components/Component1.Variable1");

        _renameLogic.Setup(x => x.GetChangesForRenamedVariable(owner, variable.Name, variable.GetRootName()))
            .Returns(new VariableChangeResponse
            {
                VariableChanges =
                {
                    new VariableChange { Container = screenWithOverride, State = screenWithOverride.DefaultState, Variable = instanceVariable }
                },
                VariableReferenceChanges =
                {
                    new VariableReferenceChange { Container = screenWithBinding, VariableReferenceList = variableReferenceList, LineIndex = 0, ChangedSide = SideOfEquals.Right }
                }
            });

        _service.DeleteVariable(variable, owner);

        owner.DefaultState.Variables.ShouldContain(variable);
        screenWithOverride.DefaultState.Variables.ShouldContain(instanceVariable);
        _undoManager.Verify(x => x.AttachCrossElementVariableRemovals(It.IsAny<System.Collections.Generic.IEnumerable<CrossElementVariableChange>>()), Times.Never);
    }

    [Fact]
    public void DeleteVariable_WhenReferencedByMultipleInstanceOverrides_ShouldCascadeAndAttachAllOfThem()
    {
        var (owner, variable) = MakeOwnerWithCustomVariable();

        var screenA = new ScreenSave { Name = "Screen1" };
        screenA.States.Add(new StateSave { Name = "Default", ParentContainer = screenA });
        var instanceA = new InstanceSave { Name = "InstanceA", BaseType = "Component1", ParentContainer = screenA };
        screenA.Instances.Add(instanceA);
        var variableA = new VariableSave { Name = "InstanceA.Variable1", Type = "float", Value = 1f };
        screenA.DefaultState.Variables.Add(variableA);

        var screenB = new ScreenSave { Name = "Screen2" };
        screenB.States.Add(new StateSave { Name = "Default", ParentContainer = screenB });
        var instanceB = new InstanceSave { Name = "InstanceB", BaseType = "Component1", ParentContainer = screenB };
        screenB.Instances.Add(instanceB);
        var variableB = new VariableSave { Name = "InstanceB.Variable1", Type = "float", Value = 2f };
        screenB.DefaultState.Variables.Add(variableB);

        _renameLogic.Setup(x => x.GetChangesForRenamedVariable(owner, variable.Name, variable.GetRootName()))
            .Returns(new VariableChangeResponse
            {
                VariableChanges =
                {
                    new VariableChange { Container = screenA, State = screenA.DefaultState, Variable = variableA },
                    new VariableChange { Container = screenB, State = screenB.DefaultState, Variable = variableB }
                }
            });

        _service.DeleteVariable(variable, owner);

        screenA.DefaultState.Variables.ShouldNotContain(variableA);
        screenB.DefaultState.Variables.ShouldNotContain(variableB);

        _undoManager.Verify(x => x.AttachCrossElementVariableRemovals(
            It.Is<System.Collections.Generic.IEnumerable<CrossElementVariableChange>>(removals =>
                System.Linq.Enumerable.Count(removals) == 2)),
            Times.Once);
    }
}
