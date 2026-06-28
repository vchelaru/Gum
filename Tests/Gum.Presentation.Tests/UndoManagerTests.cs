using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.StateAnimation.SaveClasses;
using Gum.ToolStates;
using Gum.Undo;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace Gum.Presentation.Tests;
public class UndoManagerTests : BaseTestClass
{
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IUndoRenameLogic> _renameLogic;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IMessenger> _messenger;
    private readonly Mock<IUndoPluginNotifier> _pluginNotifier;
    private readonly FakeAnimationUndoProvider _animationUndoProvider;
    private readonly UndoManager _undoManager;

    /// <summary>
    /// Stands in for the animation plugin's <see cref="IAnimationUndoProvider"/>. Backs each element's
    /// animations with an in-memory store (the headless analogue of the .ganx). GetCurrentAnimations
    /// returns a clone (so the strategy's captured baseline doesn't alias the store, mirroring how the
    /// real provider returns a fresh ToSave()/deserialize) and normalizes an empty save to null, just
    /// like the real provider does for an element with no animations. ApplyAnimations writes a clone back.
    /// </summary>
    private sealed class FakeAnimationUndoProvider : IAnimationUndoProvider
    {
        public Dictionary<ElementSave, ElementAnimationsSave> Store { get; } = new();

        public ElementAnimationsSave? GetCurrentAnimations(ElementSave element)
        {
            if (!Store.TryGetValue(element, out var save) || save == null || save.Animations.Count == 0)
            {
                return null;
            }
            return FileManager.CloneSaveObject(save);
        }

        public void ApplyAnimations(ElementSave element, ElementAnimationsSave animations)
        {
            Store[element] = FileManager.CloneSaveObject(animations);
        }
    }

    public UndoManagerTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _renameLogic = new Mock<IUndoRenameLogic>();
        _guiCommands = new Mock<IGuiCommands>();
        _fileCommands = new Mock<IFileCommands>();
        _messenger = new Mock<IMessenger>();
        _pluginNotifier = new Mock<IUndoPluginNotifier>();
        _animationUndoProvider = new FakeAnimationUndoProvider();

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
            _pluginNotifier.Object,
            _animationUndoProvider
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

    // ---------------------------------------------------------------------------------------------
    // Backfill for the strategy-extraction refactor (#3403). These pin the element/behavior tricky
    // paths the existing suite left uncovered before UndoManager is split into per-domain strategies:
    // the element REDO paths (variable value, instance add, category add), selection restore after
    // undo, and the behavior required-variables track. They are green against the current
    // (pre-refactor) UndoManager and must stay green after the split.
    // ---------------------------------------------------------------------------------------------

    [Fact]
    public void GetBehaviorActionDescription_ShouldDescribeAddedState()
    {
        // Pins the History-tab wording that moved from UndosViewModel into BehaviorUndoStrategy (#3403):
        // diffing a before (UndoState) without the state against an after (RedoState) with it yields
        // the "Add states" line. Keeps the description output byte-identical across the move.
        var before = new BehaviorSave { Name = "MyBehavior" };
        before.Categories.Add(new StateSaveCategory { Name = "MyCategory" });

        var after = new BehaviorSave { Name = "MyBehavior" };
        var afterCategory = new StateSaveCategory { Name = "MyCategory" };
        afterCategory.States.Add(new StateSave { Name = "State1" });
        after.Categories.Add(afterCategory);

        var action = new BehaviorHistoryAction
        {
            UndoState = new BehaviorSnapshot { Behavior = before },
            RedoState = new BehaviorSnapshot { Behavior = after }
        };

        BehaviorUndoStrategy.GetBehaviorActionDescription(action).ShouldBe("Add states: State1");
    }

    [Fact]
    public void PerformRedo_ShouldReAddCategory_AfterUndoingCategoryAdd()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;
        var categoryName = "MyCategory";

        _undoManager.RecordState();

        component.Categories.Add(new StateSaveCategory { Name = categoryName });

        _undoManager.RecordUndo();
        _undoManager.PerformUndo();

        component.Categories.Count.ShouldBe(0);

        _undoManager.PerformRedo();

        component.Categories.Count.ShouldBe(1);
        component.Categories[0].Name.ShouldBe(categoryName);
    }

    [Fact]
    public void PerformRedo_ShouldReAddInstance_AfterUndoingInstanceAdd()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;

        _undoManager.RecordState();

        component.Instances.Add(new InstanceSave
        {
            Name = "Instance1",
            BaseType = "Sprite",
            ParentContainer = component
        });

        _undoManager.RecordUndo();
        _undoManager.PerformUndo();

        component.Instances.Count.ShouldBe(0);

        _undoManager.PerformRedo();

        component.Instances.Count.ShouldBe(1);
        component.Instances[0].Name.ShouldBe("Instance1");
    }

    [Fact]
    public void PerformRedo_ShouldRestoreNewValue_AfterUndoingVariableChange()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;

        component.DefaultState.SetValue("X", 10f);
        _undoManager.RecordState();
        component.DefaultState.SetValue("X", 11f);
        _undoManager.RecordUndo();

        _undoManager.PerformUndo();
        component.DefaultState.GetValueOrDefault<float>("X").ShouldBe(10f);

        _undoManager.PerformRedo();
        component.DefaultState.GetValueOrDefault<float>("X").ShouldBe(11f);
    }

    [Fact]
    public void PerformRedo_ShouldRestoreRequiredVariable_AfterUndoingBehaviorRequiredVariableAdd()
    {
        var behavior = new BehaviorSave { Name = "MyBehavior" };
        var requiredVariableName = "MyRequiredVariable";

        _selectedState
            .Setup(x => x.SelectedBehavior)
            .Returns(behavior);

        _undoManager.RecordBehaviorState();

        behavior.RequiredVariables.Variables.Add(new VariableSave { Name = requiredVariableName, Type = "float" });

        _undoManager.RecordBehaviorUndo();
        _undoManager.PerformUndo();

        behavior.RequiredVariables.Variables.Count.ShouldBe(0);

        _undoManager.PerformRedo();

        behavior.RequiredVariables.Variables.Count.ShouldBe(1);
        behavior.RequiredVariables.Variables[0].Name.ShouldBe(requiredVariableName);
    }

    [Fact]
    public void PerformUndo_ShouldRestoreSelectedState()
    {
        // Pins selection restore: after an undo, UndoManager re-selects the state matching the
        // snapshot's StateName (here the "Default" state set up in the test fixture).
        ComponentSave component = _selectedState.Object.SelectedComponent!;
        component.DefaultState.SetValue("X", 10f);

        _undoManager.RecordState();
        component.DefaultState.SetValue("X", 11f);
        _undoManager.RecordUndo();

        _undoManager.PerformUndo();

        _selectedState.VerifySet(
            x => x.SelectedStateSave = It.Is<StateSave>(state => state.Name == "Default"),
            Times.AtLeastOnce);
    }

    // ---------------------------------------------------------------------------------------------
    // Animation undo (#3406). Animations are folded into the element's UndoSnapshot via the injected
    // IAnimationUndoProvider (here the in-memory FakeAnimationUndoProvider), so an animation edit and
    // the element edit it was made next to undo/redo as one atomic action. The headline guard is the
    // combined state-rename + keyframe-reference change: both must revert from a single undo, or the
    // keyframe desyncs from the state it names.
    // ---------------------------------------------------------------------------------------------

    private static ElementAnimationsSave AnimationsWithKeyframes(string animationName, params string[] keyframeStateNames)
    {
        var save = new ElementAnimationsSave();
        var animation = new AnimationSave { Name = animationName };
        float time = 0;
        foreach (var stateName in keyframeStateNames)
        {
            animation.States.Add(new AnimatedStateSave { StateName = stateName, Time = time++ });
        }
        save.Animations.Add(animation);
        return save;
    }

    [Fact]
    public void PerformRedo_OnAnimationKeyframeDelete_ShouldReapplyTheDeletion()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;
        _animationUndoProvider.Store[component] = AnimationsWithKeyframes("Anim", "Default", "Highlighted");

        _undoManager.RecordState();

        // Simulate deleting the second keyframe in the live Animations tab.
        _animationUndoProvider.Store[component] = AnimationsWithKeyframes("Anim", "Default");

        _undoManager.RecordUndo();
        _undoManager.PerformUndo();
        _animationUndoProvider.Store[component].Animations[0].States.Count.ShouldBe(2);

        _undoManager.PerformRedo();

        _animationUndoProvider.Store[component].Animations[0].States.Count.ShouldBe(1);
        _animationUndoProvider.Store[component].Animations[0].States[0].StateName.ShouldBe("Default");
    }

    [Fact]
    public void PerformUndo_OnAnimationKeyframeDelete_ShouldRestoreTheKeyframe()
    {
        ComponentSave component = _selectedState.Object.SelectedComponent!;
        _animationUndoProvider.Store[component] = AnimationsWithKeyframes("Anim", "Default", "Highlighted");

        _undoManager.RecordState();

        // Simulate deleting the second keyframe in the live Animations tab.
        _animationUndoProvider.Store[component] = AnimationsWithKeyframes("Anim", "Default");

        _undoManager.RecordUndo();
        _undoManager.PerformUndo();

        _animationUndoProvider.Store[component].Animations[0].States.Count.ShouldBe(2);
        _animationUndoProvider.Store[component].Animations[0].States[1].StateName.ShouldBe("Highlighted");
    }

    [Fact]
    public void PerformUndo_OnCombinedStateRenameAndKeyframeEdit_ShouldUndoBothAtomically()
    {
        // The desync guard. A keyframe references an element state by name ("Category/State"), so a
        // state rename is one logical edit that changes BOTH the element data and the keyframe. It
        // must record as a single snapshot and undo both halves together; otherwise Ctrl+Z reverts
        // the rename while the keyframe still points at the new name (a dangling reference).
        ComponentSave component = _selectedState.Object.SelectedComponent!;
        var category = new StateSaveCategory { Name = "Category" };
        category.States.Add(new StateSave { Name = "Old", ParentContainer = component });
        component.Categories.Add(category);

        _animationUndoProvider.Store[component] = AnimationsWithKeyframes("Anim", "Category/Old");

        _undoManager.RecordState();

        // One logical edit: rename the state on the element and rewrite the keyframe that names it.
        component.Categories[0].States[0].Name = "New";
        _animationUndoProvider.Store[component] = AnimationsWithKeyframes("Anim", "Category/New");

        _undoManager.RecordUndo();

        // A single atomic action captures the coupled change.
        _undoManager.CurrentElementHistory.Actions.Count.ShouldBe(1);

        _undoManager.PerformUndo();

        // Both halves revert from the one undo, keeping the keyframe and the state name in sync.
        component.Categories[0].States[0].Name.ShouldBe("Old");
        _animationUndoProvider.Store[component].Animations[0].States[0].StateName.ShouldBe("Category/Old");
    }

    [Fact]
    public void RecordUndo_OnElementOnlyChange_ShouldLeaveSnapshotAnimationsNull()
    {
        // An element edit with no animation change must leave the snapshot's Animations null (the
        // null-when-unchanged convention), so applying the undo touches no .ganx.
        ComponentSave component = _selectedState.Object.SelectedComponent!;
        _animationUndoProvider.Store[component] = AnimationsWithKeyframes("Anim", "Default");

        component.DefaultState.SetValue("X", 10f);
        _undoManager.RecordState();
        component.DefaultState.SetValue("X", 11f);
        _undoManager.RecordUndo();

        var action = _undoManager.CurrentElementHistory.Actions.Single();
        action.UndoState.Animations.ShouldBeNull();
        action.RedoState!.Animations.ShouldBeNull();
    }

    [Fact]
    public void RecordUndo_WithNoAnimationsAndNoEdit_ShouldRecordNothing()
    {
        // An element with no animations (provider returns null) and no edit must produce no undo: a
        // null-vs-null animation diff has to compare equal, or every selection would record spuriously.
        _undoManager.RecordState();
        _undoManager.RecordUndo();

        _undoManager.CanUndo().ShouldBeFalse();
    }
}
