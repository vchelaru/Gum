using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Plugins;
using Gum.PropertyGridHelpers;
using Gum.PropertyGridHelpers.Converters;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Wireframe;
using Moq;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

public class ElementCommandsTests : BaseTestClass
{
    private readonly ElementCommands _sut;

    private readonly Mock<ISelectedState> _selectedState;
    private readonly Mock<IGuiCommands> _guiCommands;
    private readonly Mock<IFileCommands> _fileCommands;
    private readonly Mock<IVariableInCategoryPropagationLogic> _variableInCategoryPropagationLogic;
    private readonly Mock<IWireframeObjectManager> _wireframeObjectManager;
    private readonly Mock<IPluginManager> _pluginManager;
    private readonly Mock<IProjectManager> _projectManager;
    private readonly Mock<IProjectState> _projectState;

    private ObjectFinder ObjectFinder => ObjectFinder.Self;

    public ElementCommandsTests()
    {
        _selectedState = new Mock<ISelectedState>();
        _guiCommands = new Mock<IGuiCommands>();
        _fileCommands = new Mock<IFileCommands>();
        _variableInCategoryPropagationLogic = new Mock<IVariableInCategoryPropagationLogic>();
        _wireframeObjectManager = new Mock<IWireframeObjectManager>();
        _pluginManager = new Mock<IPluginManager>();
        _projectManager = new Mock<IProjectManager>();
        _projectState = new Mock<IProjectState>();

        _sut = new ElementCommands(
            _selectedState.Object,
            _guiCommands.Object,
            _fileCommands.Object,
            _variableInCategoryPropagationLogic.Object,
            _wireframeObjectManager.Object,
            _pluginManager.Object,
            _projectManager.Object,
            _projectState.Object);
    }

    [Fact]
    public void AddCategory_ShouldAddCategoryStateVariable_WithAvailableStatesConverter()
    {
        ComponentSave component = new();
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });

        StateSaveCategory category = _sut.AddCategory(component, "MyCategory");

        VariableSave? stateVariable = component.DefaultState.Variables
            .FirstOrDefault(item => item.Name == "MyCategoryState");

        stateVariable.ShouldNotBeNull();
        stateVariable.CustomTypeConverter.ShouldBeOfType<AvailableStatesConverter>();
        ((AvailableStatesConverter)stateVariable.CustomTypeConverter).CategoryName.ShouldBe("MyCategory");
    }

    [Fact]
    public void AddInstance_AddsInstanceToProject()
    {
        ComponentSave component = new();

        _sut.AddInstance(component, "NewInstanceName", "Sprite");

        component.Instances.Count.ShouldBe(1);
        component.Instances[0].Name.ShouldBe("NewInstanceName");
        component.Instances[0].BaseType.ShouldBe("Sprite");
    }

    [Fact]
    public void AddInstance_ShouldNotifyPlugins()
    {
        ComponentSave component = new();
        _sut.AddInstance(component, "NewInstanceName", "Sprite");
        _pluginManager.Verify(
            x => x.InstanceAdd(
                It.IsAny<ElementSave>(),
                It.Is<InstanceSave>(i => i.Name == "NewInstanceName" && i.BaseType == "Sprite")),
            Times.Once);
    }

    [Fact]
    public void GetUniqueNameForNewInstance_ShouldReturnDefaultName()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.GumProjectSave = project;

        StandardElementSave text = new()
        {
            Name = "Text"
        };
        ObjectFinder.GumProjectSave.StandardElements.Add(text);

        ComponentSave component = new();

        // act
        string name = _sut.GetUniqueNameForNewInstance(text, component);

        // assert
        name.ShouldBe("TextInstance");
    }

    [Fact]
    public void GetUniqueNameForNewInstance_ShouldIncrement_OnMatchingName()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.GumProjectSave = project;

        StandardElementSave text = new()
        {
            Name = "Text"
        };
        ObjectFinder.GumProjectSave.StandardElements.Add(text);

        ComponentSave component = new();
        component.Instances.Add(new InstanceSave
        {
            Name = "TextInstance"
        });

        // act
        string name = _sut.GetUniqueNameForNewInstance(text, component);

        // assert
        name.ShouldBe("TextInstance1");
    }

    #region ModifyVariable

    [Fact]
    public void ModifyVariable_ReferenceReadsAbsoluteRightOfDraggedInstance_MaterializesLiveResolvedValueIntoStateSave()
    {
        // Repro for a live-drag bug: while dragging Source, a sibling's "X = Source.AbsoluteRight"
        // reference must track Source's live position - not the position from before this drag
        // tick started. ModifyVariable is called on every drag tick (ElementCommands.MoveSelectedObjectsBy),
        // so its ElementSave-overload ApplyVariableReferences call must resolve against a live root
        // that already reflects Source's just-applied position, or the materialized value goes stale
        // and later gets overwritten back to the pre-drag value when the wireframe next refreshes
        // from the (never-updated) StateSave scalar.
        GumExpressionService.Initialize();

        ComponentSave component = new ComponentSave { Name = "TestComponent" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);

        InstanceSave sourceInstance = new InstanceSave { Name = "Source", BaseType = "Container", ParentContainer = component };
        InstanceSave targetInstance = new InstanceSave { Name = "Target", BaseType = "Container", ParentContainer = component };
        component.Instances.Add(sourceInstance);
        component.Instances.Add(targetInstance);

        StandardElementSave containerStandard = new StandardElementSave { Name = "Container" };
        containerStandard.States.Add(new StateSave { Name = "Default", ParentContainer = containerStandard });

        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(containerStandard);
        project.Components.Add(component);
        ObjectFinder.GumProjectSave = project;

        defaultState.Variables.Add(new VariableSave { Name = "Source.X", Value = 0f, Type = "float", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "Source.XUnits", Value = PositionUnitType.PixelsFromLeft, Type = "PositionUnitType", SetsValue = true });
        // The existing scalar the reference will overwrite - also what gives ApplyVariableReferencesOnSpecificOwner
        // the left-side type ("float") it needs to cast the resolved AbsoluteRight value to.
        defaultState.Variables.Add(new VariableSave { Name = "Target.X", Value = 0f, Type = "float", SetsValue = true });

        VariableListSave<string> targetRefs = new VariableListSave<string> { Type = "string", Name = "Target.VariableReferences" };
        targetRefs.Value.Add("X = Source.AbsoluteRight");
        defaultState.VariableLists.Add(targetRefs);

        GraphicalUiElement rootGue = new GraphicalUiElement(new InvisibleRenderable());
        GraphicalUiElement sourceGue = new GraphicalUiElement(new InvisibleRenderable()) { Name = "Source", Width = 40f };
        sourceGue.Parent = rootGue;

        _selectedState.SetupGet(x => x.SelectedElement).Returns(component);
        _selectedState.SetupGet(x => x.SelectedStateSave).Returns(defaultState);
        _selectedState.SetupGet(x => x.CustomCurrentStateSave).Returns((StateSave)null);

        _wireframeObjectManager.Setup(x => x.GetRepresentation(sourceInstance, null)).Returns(sourceGue);
        _wireframeObjectManager.SetupGet(x => x.RootGue).Returns(rootGue);

        // Drag Source 100px to the right.
        _sut.ModifyVariable("X", 100f, sourceInstance);

        // AbsoluteRight = AbsoluteX (100, the just-applied drag position) + Width (40) = 140.
        // A stale/unresolved reference would leave Target.X unset (null) or resolved against
        // Source's pre-drag X (0 + 40 = 40).
        defaultState.GetValue("Target.X").ShouldBe(140f);
    }

    #endregion

    [Fact]
    public void GetUniqueNameForNewInstance_WithBehaviorSave_ShouldReturnDefaultName()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.GumProjectSave = project;

        StandardElementSave text = new()
        {
            Name = "Text"
        };
        ObjectFinder.GumProjectSave.StandardElements.Add(text);

        BehaviorSave behavior = new();

        // act
        string name = _sut.GetUniqueNameForNewInstance(text, behavior);

        // assert
        name.ShouldBe("TextInstance");
    }

    [Fact]
    public void GetUniqueNameForNewInstance_WithBehaviorSave_ShouldIncrement_OnMatchingName()
    {
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.GumProjectSave = project;

        StandardElementSave text = new()
        {
            Name = "Text"
        };
        ObjectFinder.GumProjectSave.StandardElements.Add(text);

        BehaviorSave behavior = new();
        behavior.RequiredInstances.Add(new BehaviorInstanceSave
        {
            Name = "TextInstance"
        });

        // act
        string name = _sut.GetUniqueNameForNewInstance(text, behavior);

        // assert
        name.ShouldBe("TextInstance1");
    }
}
