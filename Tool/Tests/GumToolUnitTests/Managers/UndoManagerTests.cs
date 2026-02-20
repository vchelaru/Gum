using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Plugins;
using Gum.Plugins.BaseClasses;
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
public class UndoManagerTests : BaseTestClass
{
    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IRenameLogic> _renameLogic;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IMessenger> _messenger;
    private readonly Mock<PluginManager> _pluginManager;
    private readonly UndoManager _undoManager;

    public UndoManagerTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _renameLogic = new Mock<IRenameLogic>();
        _guiCommands = new Mock<IGuiCommands>();
        _fileCommands = new Mock<IFileCommands>();
        _messenger = new Mock<IMessenger>();
        _pluginManager = new Mock<PluginManager>();
        _pluginManager.Object.Plugins = new List<PluginBase>();

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
            _pluginManager.Object
            );
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

        _pluginManager.Verify(x => x.InstanceAdd(It.IsAny<ElementSave>(), It.IsAny<InstanceSave>()),
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

        _pluginManager.Verify(
            x => x.InstancesDelete(It.IsAny<ElementSave>(), It.IsAny<InstanceSave[]>()),
            Times.Once);
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

        _renameLogic.Verify(x => x.PropagateVariableRename(
            componentA,
            "instanceX.Color",
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

        _renameLogic.Verify(x => x.PropagateVariableRename(
            componentA,
            "NewName",
            "NewName",
            "OldName",
            It.IsAny<HashSet<ElementSave>>()),
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

}
