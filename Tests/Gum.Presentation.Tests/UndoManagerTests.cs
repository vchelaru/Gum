using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Presentation.Tests;
public class UndoManagerTests : BaseTestClass
{
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IUndoRenameLogic> _renameLogic;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IMessenger> _messenger;
    private readonly Mock<IUndoPluginNotifier> _pluginNotifier;
    private readonly UndoManager _undoManager;

    public UndoManagerTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _renameLogic = new Mock<IUndoRenameLogic>();
        _guiCommands = new Mock<IGuiCommands>();
        _fileCommands = new Mock<IFileCommands>();
        _messenger = new Mock<IMessenger>();
        _pluginNotifier = new Mock<IUndoPluginNotifier>();

        ComponentSave component = new();
        component.States.Add(new Gum.DataTypes.Variables.StateSave
        {
            Name="Default"
        });


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
            _messenger.Object,
            _pluginNotifier.Object
            );
    }

    [Fact]
    public void RequestLock_WhenDisposed_RemovesLockFromUndoLocks()
    {
        // Pins the UndoLock back-edge: RequestLock registers the lock, and disposing it
        // removes the same lock from UndoManager's UndoLocks collection. This behavior must
        // survive UndoLock moving headless (its Dispose now invokes an injected callback
        // instead of reaching back into UndoManager directly). See ADR-0005 Phase 3.
        var undoLock = _undoManager.RequestLock();
        _undoManager.UndoLocks.ShouldContain(undoLock);

        undoLock.Dispose();

        _undoManager.UndoLocks.ShouldNotContain(undoLock);
    }

    [Fact]
    public void PerformRedo_ShouldRestoreBehavior_AfterUndoingBehaviorAdd()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;
        var behaviorName = "MyBehavior";

        _undoManager.RecordState();

        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behaviorName });

        _undoManager.RecordUndo();
        _undoManager.PerformUndo();

        component.Behaviors.Count.ShouldBe(0);

        _undoManager.PerformRedo();

        component.Behaviors.Count.ShouldBe(1);
        component.Behaviors[0].BehaviorName.ShouldBe(behaviorName);
    }

    [Fact]
    public void PerformUndo_OnBehaviorAdd_ShouldRemoveBehaviorAndAssociatedCategory()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;
        var behaviorName = "MyBehavior";
        var categoryName = "MyBehaviorCategory";

        _undoManager.RecordState();

        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behaviorName });
        var category = new StateSaveCategory { Name = categoryName };
        category.States.Add(new StateSave { Name = "State1", ParentContainer = component });
        component.Categories.Add(category);

        _undoManager.RecordUndo();
        _undoManager.PerformUndo();

        component.Behaviors.Count.ShouldBe(0);
        component.Categories.Count.ShouldBe(0);
    }

    [Fact]
    public void PerformUndo_ShouldRestoreValue()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;

        component.DefaultState.SetValue("X", 10f);

        _undoManager.RecordState();

        component.DefaultState.SetValue("X", 11f);

        _undoManager.RecordUndo();

        component.DefaultState.GetValueOrDefault<float>("X").ShouldBe(11.0f);

        _undoManager.PerformUndo();

        component.DefaultState.GetValueOrDefault<float>("X").ShouldBe(10.0f);
    }

    [Fact]
    public void PerformUndo_ShouldAddRemovedInstance()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;

        var instance = new InstanceSave
        {
            Name = "Instance1",
            BaseType = "Sprite",
            ParentContainer = component
        };

        component.Instances.Add(instance);

        _undoManager.RecordState();

        component.Instances.Clear();

        _undoManager.RecordUndo();
        _undoManager.PerformUndo();

        component.Instances.Count.ShouldBe(1);
        component.Instances[0].Name.ShouldBe("Instance1");
        component.Instances[0].BaseType.ShouldBe("Sprite");
        component.Instances[0].ParentContainer.ShouldBe(component);
    }

    [Fact]
    public void PerformUndo_OnDeletedInstance_ShouldNotifyPluginOfInstanceAdd()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;

        var instance = new InstanceSave
        {
            Name = "Instance1",
            BaseType = "Sprite",
            ParentContainer = component
        };

        component.Instances.Add(instance);

        _undoManager.RecordState();

        component.Instances.Clear();

        _undoManager.RecordUndo();
        _undoManager.PerformUndo();

        _pluginNotifier.Verify(x => x.InstanceAdd(It.IsAny<ElementSave>(), It.IsAny<InstanceSave>()),
            Times.Once);
    }

    [Fact]
    public void PerformUndo_OnAddedInstance_ShouldNotifyPluginOfInstanceAdd()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;

        _undoManager.RecordState();

        var instance = new InstanceSave
        {
            Name = "Instance1",
            BaseType = "Sprite",
            ParentContainer = component
        };

        component.Instances.Add(instance);

        _undoManager.RecordUndo();
        _undoManager.PerformUndo();

        _pluginNotifier.Verify(
            x => x.InstancesDelete(It.IsAny<ElementSave>(), It.IsAny<InstanceSave[]>()),
            Times.Once);
    }

    [Fact]
    public void PerformUndo_OnBehaviorChange_ShouldNotifyPluginOfBehaviorSelected()
    {
        // Pins the third undo->plugin notification (alongside InstanceAdd / InstancesDelete above):
        // a behavior undo must still fire BehaviorSelected. This is the call that now travels through the
        // narrow IUndoPluginNotifier port rather than the concrete PluginManager (ADR-0005 Phase 3).
        var behavior = new BehaviorSave { Name = "MyBehavior" };
        var category = new StateSaveCategory { Name = "MyCategory" };
        behavior.Categories.Add(category);

        _selectedState
            .Setup(x => x.SelectedBehavior)
            .Returns(behavior);

        _undoManager.RecordBehaviorState();

        category.States.Add(new StateSave { Name = "State1" });

        _undoManager.RecordBehaviorUndo();
        _undoManager.PerformUndo();

        _pluginNotifier.Verify(x => x.BehaviorSelected(behavior), Times.Once);
    }


    [Fact]
    public void CurrentElementHistory_ShouldReportVariableChanges()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;

        component.DefaultState.SetValue("X", 10f);

        _undoManager.RecordState();

        component.DefaultState.SetValue("X", 11f);

        _undoManager.RecordUndo();

        var elementHistory = _undoManager.CurrentElementHistory;
        elementHistory.Actions.Count.ShouldBe(1);

        var comparisonInformation = UndoSnapshot.CompareAgainst(
            component,
            elementHistory.Actions[0].UndoState.Element);

        comparisonInformation.ToString().ShouldBe("Variables in Default: X=10");
    }

    [Fact]
    public void CurrentElementHistory_ShouldReportExposedVariables()
    {
        {
            ComponentSave component = _selectedState.Object.SelectedComponent!;

            component.DefaultState.SetValue("X", 10f);

            _undoManager.RecordState();

            var xVariable = component.DefaultState.GetVariableSave("X")!;
            xVariable.ExposedAsName = "ExposedX";

            _undoManager.RecordUndo();

            var elementHistory = _undoManager.CurrentElementHistory;
            elementHistory.Actions.Count.ShouldBe(1);

            var comparisonInformation = UndoSnapshot.CompareAgainst(
                component,
                elementHistory.Actions[0].UndoState.Element);

            comparisonInformation.ToString().ShouldBe("Un-exposed variables: X");
        }
    }

    [Fact]
    public void PerformUndo_ExposedVariableRename_ShouldDelegatePropagationToRenameLogic()
    {
        ComponentSave componentA = _selectedState.Object.SelectedComponent!;

        var exposedVar = new VariableSave
        {
            Name = "instanceX.Color",
            ExposedAsName = "ButtonColor"
        };
        componentA.DefaultState.Variables.Add(exposedVar);

        _undoManager.RecordState();

        exposedVar.ExposedAsName = "ButtonBgColor";

        _undoManager.RecordUndo();
        _undoManager.PerformUndo();

        _renameLogic.Verify(x => x.ApplyVariableRenameChanges(
            It.IsAny<VariableChangeResponse>(),
            "ButtonBgColor",
            "ButtonColor",
            It.IsAny<HashSet<ElementSave>>()),
            Times.Once);
    }

    [Fact]
    public void PerformUndo_CustomVariableRename_ShouldDelegatePropagationToRenameLogic()
    {
        ComponentSave componentA = _selectedState.Object.SelectedComponent!;

        var customVar = new VariableSave
        {
            Name = "OldName",
            Type = "float",
            IsCustomVariable = true,
            Value = 5f
        };
        componentA.DefaultState.Variables.Add(customVar);

        _undoManager.RecordState();

        customVar.Name = "NewName";

        _undoManager.RecordUndo();
        _undoManager.PerformUndo();

        _renameLogic.Verify(x => x.ApplyVariableRenameChanges(
            It.IsAny<VariableChangeResponse>(),
            "NewName",
            "OldName",
            It.IsAny<HashSet<ElementSave>>()),
            Times.Once);
    }

    [Fact]
    public void PerformUndo_OnElementRename_ShouldDelegateToRenameLogicHandleRename()
    {
        // Pins the third RenameLogic call routed through the narrow IUndoRenameLogic port
        // (alongside GetChangesForRenamedVariable / ApplyVariableRenameChanges, covered by the two
        // rename-delegation tests above): undoing an element whose Name changed must re-apply the
        // rename via HandleRename so references stay consistent. This is the call that now travels
        // through IUndoRenameLogic rather than the full IRenameLogic (ADR-0005 Phase 3, mirroring #3355).
        ComponentSave component = _selectedState.Object.SelectedComponent!;
        component.Name = "OriginalName";

        _undoManager.RecordState();

        component.Name = "RenamedName";

        _undoManager.RecordUndo();
        _undoManager.PerformUndo();

        _renameLogic.Verify(x => x.HandleRename(
            It.IsAny<IInstanceContainer>(),
            It.IsAny<InstanceSave>(),
            "RenamedName",
            NameChangeAction.Rename,
            false),
            Times.Once);
    }

    [Fact]
    public void RecordUndo_ShouldNotCrash_WithDifferentSelectedElement()
    {
        var component1 = new ComponentSave();
        component1.Name = "component1";
        component1.States.Add(new Gum.DataTypes.Variables.StateSave
        {
            Name = "Default",
            ParentContainer = component1
        });

        var component2 = new ComponentSave();
        component2.Name = "component2";
        component2.States.Add(new Gum.DataTypes.Variables.StateSave
        {
            Name = "Default",
            ParentContainer = component2
        });

        _selectedState
            .Setup(x => x.SelectedElement)
            .Returns(component1);

        _selectedState
            .Setup(x => x.SelectedStateSave)
            .Returns(component1.DefaultState);

        _undoManager.RecordState();

        _selectedState
            .Setup(x => x.SelectedElement)
            .Returns(component2);

        _selectedState
            .Setup(x => x.SelectedStateSave)
            .Returns(component2.DefaultState);

        _undoManager.RecordUndo();
    }

    [Fact]
    public void PerformRedo_ShouldRestoreState_AfterUndoingStateAddToBehavior()
    {
        var behavior = new BehaviorSave { Name = "MyBehavior" };
        var category = new StateSaveCategory { Name = "MyCategory" };
        behavior.Categories.Add(category);

        _selectedState
            .Setup(x => x.SelectedBehavior)
            .Returns(behavior);

        _undoManager.RecordBehaviorState();

        category.States.Add(new StateSave { Name = "State1" });

        _undoManager.RecordBehaviorUndo();
        _undoManager.PerformUndo();

        behavior.Categories[0].States.Count.ShouldBe(0);

        _undoManager.PerformRedo();

        behavior.Categories[0].States.Count.ShouldBe(1);
        behavior.Categories[0].States[0].Name.ShouldBe("State1");
    }

    [Fact]
    public void PerformUndo_ShouldRemoveInstance_WhenInstanceAddedToBehavior()
    {
        var behavior = new BehaviorSave { Name = "MyBehavior" };

        _selectedState
            .Setup(x => x.SelectedBehavior)
            .Returns(behavior);

        _undoManager.RecordBehaviorState();

        behavior.RequiredInstances.Add(new BehaviorInstanceSave { Name = "MyInstance", BaseType = "Sprite" });

        _undoManager.RecordBehaviorUndo();
        _undoManager.PerformUndo();

        behavior.RequiredInstances.Count.ShouldBe(0);
    }

    [Fact]
    public void PerformRedo_OnHideVariable_ShouldRestoreHiddenVariable()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;
        var variableName = "ButtonCategoryState";

        _undoManager.RecordState();

        component.VariablesHiddenFromInstances.Add(variableName);

        _undoManager.RecordUndo();
        _undoManager.PerformUndo();

        component.VariablesHiddenFromInstances.Count.ShouldBe(0);

        _undoManager.PerformRedo();

        component.VariablesHiddenFromInstances.Count.ShouldBe(1);
        component.VariablesHiddenFromInstances[0].ShouldBe(variableName);
    }

    [Fact]
    public void PerformUndo_OnHideVariable_ShouldRemoveVariableFromHiddenList()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;
        var variableName = "ButtonCategoryState";

        _undoManager.RecordState();

        component.VariablesHiddenFromInstances.Add(variableName);

        _undoManager.RecordUndo();
        _undoManager.PerformUndo();

        component.VariablesHiddenFromInstances.Count.ShouldBe(0);
    }

    [Fact]
    public void PerformUndo_OnShowVariable_ShouldRestoreVariableToHiddenList()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;
        var variableName = "ButtonCategoryState";
        component.VariablesHiddenFromInstances.Add(variableName);

        _undoManager.RecordState();

        component.VariablesHiddenFromInstances.Remove(variableName);

        _undoManager.RecordUndo();
        _undoManager.PerformUndo();

        component.VariablesHiddenFromInstances.Count.ShouldBe(1);
        component.VariablesHiddenFromInstances[0].ShouldBe(variableName);
    }

    [Fact]
    public void PerformUndo_ShouldRemoveState_WhenStateAddedToBehavior()
    {
        var behavior = new BehaviorSave { Name = "MyBehavior" };
        var category = new StateSaveCategory { Name = "MyCategory" };
        behavior.Categories.Add(category);

        _selectedState
            .Setup(x => x.SelectedBehavior)
            .Returns(behavior);

        _undoManager.RecordBehaviorState();

        category.States.Add(new StateSave { Name = "State1" });

        _undoManager.RecordBehaviorUndo();
        _undoManager.PerformUndo();

        behavior.Categories[0].States.Count.ShouldBe(0);
    }

    // ---------------------------------------------------------------------------------------------
    // New headless coverage (added when the suite was migrated out of GumToolUnitTests into
    // Gum.Presentation.Tests). These exercise core undo/redo control-flow branches that the ported
    // tests above did not reach — the empty-history early-outs, the RequestLock defer-until-disposed
    // contract, the redo-stack truncation on a divergent edit, ClearAll, and the post-undo autosave
    // port interaction — all against mocked ports with no UI present.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void CanRedo_ShouldReturnFalse_WhenNoChangesRecorded()
    {
        _undoManager.CanRedo().ShouldBeFalse();
    }

    [Fact]
    public void CanUndo_ShouldReturnFalse_WhenNoChangesRecorded()
    {
        _undoManager.CanUndo().ShouldBeFalse();
    }

    [Fact]
    public void ClearAll_ShouldDiscardUndoHistory()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;
        component.DefaultState.SetValue("X", 10f);

        _undoManager.RecordState();
        component.DefaultState.SetValue("X", 11f);
        _undoManager.RecordUndo();
        _undoManager.CanUndo().ShouldBeTrue();

        _undoManager.ClearAll();

        _undoManager.CanUndo().ShouldBeFalse();
    }

    [Fact]
    public void PerformRedo_AfterDivergentChange_ShouldNotBeAvailable()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;
        component.DefaultState.SetValue("X", 10f);

        _undoManager.RecordState();
        component.DefaultState.SetValue("X", 11f);
        _undoManager.RecordUndo();

        // Undo back to 10; a redo to 11 is now available.
        _undoManager.PerformUndo();
        _undoManager.CanRedo().ShouldBeTrue();

        // A new, divergent edit discards the redo branch (the truncation path in RecordUndo).
        component.DefaultState.SetValue("X", 20f);
        _undoManager.RecordUndo();

        _undoManager.CanRedo().ShouldBeFalse();
    }

    [Fact]
    public void PerformUndo_ShouldTriggerProjectAutoSave()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;
        component.DefaultState.SetValue("X", 10f);

        _undoManager.RecordState();
        component.DefaultState.SetValue("X", 11f);
        _undoManager.RecordUndo();

        _undoManager.PerformUndo();

        // TryAutoSaveProject has an optional parameter; supply it explicitly so the Moq
        // expression tree compiles (CS0854). UndoManager calls it with the default.
        _fileCommands.Verify(x => x.TryAutoSaveProject(It.IsAny<bool>()), Times.Once);
    }

    [Fact]
    public void PerformUndo_WhenNothingRecorded_ShouldLeaveValueUnchanged()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;
        component.DefaultState.SetValue("X", 7f);

        _undoManager.PerformUndo();

        component.DefaultState.GetValueOrDefault<float>("X").ShouldBe(7f);
    }

    [Fact]
    public void RequestLock_ShouldDeferRecording_UntilDisposed()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;
        component.DefaultState.SetValue("X", 10f);
        _undoManager.RecordState();

        UndoLock undoLock = _undoManager.RequestLock();
        component.DefaultState.SetValue("X", 11f);

        // While the lock is held, RecordUndo is suppressed, so nothing is recorded yet.
        _undoManager.CanUndo().ShouldBeFalse();

        undoLock.Dispose();

        // Disposing the last lock fires RecordUndo once, capturing the change as a single undo.
        _undoManager.CanUndo().ShouldBeTrue();
        _undoManager.CurrentElementHistory.Actions.Count.ShouldBe(1);
    }
}
