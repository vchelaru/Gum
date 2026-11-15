using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Plugins;
using Gum.PropertyGridHelpers;
using Gum.Services.Dialogs;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// A state added to, or moved into, a category receives the union of the variables its sibling
/// states set (#7654), not just the first sibling's.
/// </summary>
public class NewStateInCategoryVariableTests : BaseTestClass
{
    private readonly ElementCommands _elementCommands;
    private readonly EditCommands _editCommands;
    private readonly Mock<ISelectedState> _selectedState = new();
    private readonly Mock<IUndoManager> _undoManager = new();

    public NewStateInCategoryVariableTests()
    {
        VariableInCategoryPropagationLogic propagation = new(
            _undoManager.Object,
            new Mock<IGuiCommands>().Object,
            new Mock<IFileCommands>().Object,
            new Mock<IDialogService>().Object,
            new Mock<IPluginManager>().Object);

        _elementCommands = new ElementCommands(
            _selectedState.Object,
            new Mock<IGuiCommands>().Object,
            new Mock<IFileCommands>().Object,
            propagation,
            new Mock<IWireframeObjectManager>().Object,
            new Mock<IPluginManager>().Object,
            new Mock<IProjectManager>().Object,
            new Mock<IProjectState>().Object);

        Mock<IDeleteLogic> deleteLogic = new();
        deleteLogic.Setup(x => x.GetBehaviorsNeedingCategory(It.IsAny<StateSaveCategory>(), It.IsAny<ComponentSave?>()))
            .Returns(new List<BehaviorSave>());

        _editCommands = new EditCommands(
            _selectedState.Object,
            new Mock<INameVerifier>().Object,
            new Mock<IRenameLogic>().Object,
            _undoManager.Object,
            new Mock<IDialogService>().Object,
            new Mock<IFileCommands>().Object,
            null!,
            new Mock<IGuiCommands>().Object,
            propagation,
            new Mock<IPluginManager>().Object,
            deleteLogic.Object,
            new Mock<IProjectManager>().Object,
            new Mock<IProjectState>().Object,
            new Mock<IStandardElementsManagerGumTool>().Object,
            new Mock<IReferenceFinder>().Object,
            new Mock<Gum.Managers.IProjectRestrictions>().Object,
            new Mock<Gum.Logic.IElementDuplicateRestrictions>().Object);

        ObjectFinder.Self.GumProjectSave = new GumProjectSave();
        StandardElementSave sprite = new() { Name = "Sprite" };
        StateSave spriteDefault = new() { Name = "Default", ParentContainer = sprite };
        spriteDefault.Variables.Add(new VariableSave { Name = "X", Type = "float", Value = 0f });
        sprite.States.Add(spriteDefault);
        ObjectFinder.Self.GumProjectSave.StandardElements.Add(sprite);
    }

    private static ComponentSave CreateComponent(out StateSaveCategory category)
    {
        ComponentSave component = new() { Name = "Comp" };
        StateSave defaultState = new() { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave { Name = "A", Type = "float", Value = 1f });
        defaultState.Variables.Add(new VariableSave { Name = "B", Type = "float", Value = 2f });
        defaultState.Variables.Add(new VariableSave { Name = "Instance1.X", Type = "float", Value = 3f });
        defaultState.VariableLists.Add(new VariableListSave<string>
        {
            Name = "Items",
            Type = "string",
            Value = new List<string> { "one" }
        });
        component.States.Add(defaultState);
        component.Instances.Add(new InstanceSave { Name = "Instance1", BaseType = "Sprite" });

        category = new StateSaveCategory { Name = "Cat" };
        component.Categories.Add(category);
        return component;
    }

    private static void AddFloat(StateSave state, string name, float value) =>
        state.Variables.Add(new VariableSave { Name = name, Type = "float", Value = value, SetsValue = true });

    [Fact]
    public void AddState_SiblingsSetDifferentVariables_NewStateGetsTheUnion()
    {
        ComponentSave component = CreateComponent(out StateSaveCategory category);
        StateSave first = new() { Name = "First", ParentContainer = component };
        AddFloat(first, "A", 10f);
        StateSave second = new() { Name = "Second", ParentContainer = component };
        AddFloat(second, "B", 20f);
        category.States.Add(first);
        category.States.Add(second);

        StateSave added = _elementCommands.AddState(component, category, "Added");

        added.GetVariableSave("A").ShouldNotBeNull();
        added.GetVariableSave("B").ShouldNotBeNull();
        added.Variables.Count.ShouldBe(2);
    }

    [Fact]
    public void AddState_ExtraVariable_TakesTheDefaultStatesValue()
    {
        ComponentSave component = CreateComponent(out StateSaveCategory category);
        StateSave first = new() { Name = "First", ParentContainer = component };
        AddFloat(first, "A", 10f);
        StateSave second = new() { Name = "Second", ParentContainer = component };
        AddFloat(second, "B", 20f);
        category.States.Add(first);
        category.States.Add(second);

        StateSave added = _elementCommands.AddState(component, category, "Added");

        added.GetVariableSave("B")!.Value.ShouldBe(2f);
        added.GetVariableSave("B")!.SetsValue.ShouldBeTrue();
    }

    [Fact]
    public void AddState_VariableSetInSeveralStates_AppearsOnce()
    {
        ComponentSave component = CreateComponent(out StateSaveCategory category);
        StateSave first = new() { Name = "First", ParentContainer = component };
        AddFloat(first, "A", 10f);
        StateSave second = new() { Name = "Second", ParentContainer = component };
        AddFloat(second, "A", 11f);
        AddFloat(second, "B", 20f);
        category.States.Add(first);
        category.States.Add(second);

        StateSave added = _elementCommands.AddState(component, category, "Added");

        added.Variables.Count(v => v.Name == "A").ShouldBe(1);
        added.Variables.Count(v => v.Name == "B").ShouldBe(1);
    }

    [Fact]
    public void AddState_LaterStateSetsVariableList_NewStateGetsTheList()
    {
        ComponentSave component = CreateComponent(out StateSaveCategory category);
        StateSave first = new() { Name = "First", ParentContainer = component };
        AddFloat(first, "A", 10f);
        StateSave second = new() { Name = "Second", ParentContainer = component };
        second.VariableLists.Add(new VariableListSave<string>
        {
            Name = "Items",
            Type = "string",
            Value = new List<string> { "two" }
        });
        category.States.Add(first);
        category.States.Add(second);

        StateSave added = _elementCommands.AddState(component, category, "Added");

        added.GetVariableListSave("Items").ShouldNotBeNull();
        added.VariableLists.Count.ShouldBe(1);
    }

    [Fact]
    public void AddState_LaterStateSetsInstanceVariable_NewStateGetsIt()
    {
        ComponentSave component = CreateComponent(out StateSaveCategory category);
        StateSave first = new() { Name = "First", ParentContainer = component };
        AddFloat(first, "A", 10f);
        StateSave second = new() { Name = "Second", ParentContainer = component };
        AddFloat(second, "Instance1.X", 50f);
        category.States.Add(first);
        category.States.Add(second);

        StateSave added = _elementCommands.AddState(component, category, "Added");

        added.GetVariableSave("Instance1.X").ShouldNotBeNull();
    }

    [Fact]
    public void AddState_NewStateAlreadySetsVariable_KeepsItsValue()
    {
        ComponentSave component = CreateComponent(out StateSaveCategory category);
        StateSave first = new() { Name = "First", ParentContainer = component };
        AddFloat(first, "A", 10f);
        category.States.Add(first);
        StateSave incoming = new() { Name = "Incoming" };
        AddFloat(incoming, "A", 99f);

        _elementCommands.AddState(component, category, incoming);

        incoming.GetVariableSave("A")!.Value.ShouldBe(99f);
        incoming.Variables.Count(v => v.Name == "A").ShouldBe(1);
    }

    [Fact]
    public void AddState_EmptyCategory_AddsNoVariables()
    {
        ComponentSave component = CreateComponent(out StateSaveCategory category);

        StateSave added = _elementCommands.AddState(component, category, "Added");

        added.Variables.ShouldBeEmpty();
        added.VariableLists.ShouldBeEmpty();
    }

    [Fact]
    public void AddState_FirstStateSetsEverything_NewStateGetsIt()
    {
        ComponentSave component = CreateComponent(out StateSaveCategory category);
        StateSave first = new() { Name = "First", ParentContainer = component };
        AddFloat(first, "A", 10f);
        AddFloat(first, "B", 20f);
        category.States.Add(first);

        StateSave added = _elementCommands.AddState(component, category, "Added");

        added.GetVariableSave("A").ShouldNotBeNull();
        added.GetVariableSave("B").ShouldNotBeNull();
    }

    [Fact]
    public void MoveToCategory_SiblingsSetDifferentVariables_MovedStateGetsTheUnion()
    {
        ComponentSave component = CreateComponent(out StateSaveCategory category);
        StateSaveCategory oldCategory = new() { Name = "Old" };
        component.Categories.Add(oldCategory);
        StateSave moving = new() { Name = "Moving", ParentContainer = component };
        oldCategory.States.Add(moving);
        StateSave first = new() { Name = "First", ParentContainer = component };
        AddFloat(first, "A", 10f);
        StateSave second = new() { Name = "Second", ParentContainer = component };
        AddFloat(second, "B", 20f);
        category.States.Add(first);
        category.States.Add(second);
        // The selected category differs from the target; propagation must go to the target.
        StateSaveCategory otherCategory = new() { Name = "Other" };
        StateSave bystander = new() { Name = "Bystander", ParentContainer = component };
        otherCategory.States.Add(bystander);
        component.Categories.Add(otherCategory);
        _selectedState.SetupGet(x => x.SelectedStateCategorySave).Returns(otherCategory);

        _editCommands.MoveToCategory("Cat", moving, component);

        moving.GetVariableSave("A").ShouldNotBeNull();
        moving.GetVariableSave("B").ShouldNotBeNull();
        bystander.Variables.ShouldBeEmpty();
        _undoManager.Verify(x => x.RequestLock(), Times.Once);
    }
}
