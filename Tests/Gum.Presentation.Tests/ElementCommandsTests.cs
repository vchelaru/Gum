using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.PropertyGridHelpers.Converters;
using Gum.Services;
using Gum.Services.Dialogs;
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

    [Fact]
    public void ModifyVariable_DragThenReleaseOnRectScreenScenario_TracksCorrectlyThroughoutWithNoReversion()
    {
        // Reproduces the real RectScreen.gusx repro reported by the user: RectangleInstance1 has
        // "X = RectangleInstance.AbsoluteRight". Drags RectangleInstance across several ticks (like
        // real mouse-move deltas), then replicates EditorContext.DoEndOfSettingValuesLogic's release
        // commit loop (iterate stateSave.Variables in file order, SetVariableLogic.PropertyValueChanged
        // -> VariableReferenceLogic.DoVariableReferenceReaction for each variable that changed since
        // the drag started). Asserts every step tracks correctly and release never reverts the value -
        // pins the ElementCommands.ModifyVariable fix at the data/logic layer. (A live-app flicker the
        // user still saw after this fix did not reproduce here, so it lives in WPF-canvas refresh
        // timing outside what this headless test can drive - see PrintOutput diagnostics in
        // ElementCommands.ModifyVariable / VariableReferenceLogic.DoVariableReferenceReaction.)
        GumExpressionService.Initialize();

        ScreenSave screen = new ScreenSave { Name = "RectScreen" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(defaultState);

        InstanceSave rectInstance = new InstanceSave { Name = "RectangleInstance", BaseType = "Rectangle", ParentContainer = screen };
        InstanceSave rectInstance1 = new InstanceSave { Name = "RectangleInstance1", BaseType = "Rectangle", ParentContainer = screen };
        screen.Instances.Add(rectInstance);
        screen.Instances.Add(rectInstance1);

        StandardElementSave rectangleStandard = new StandardElementSave { Name = "Rectangle" };
        rectangleStandard.States.Add(new StateSave { Name = "Default", ParentContainer = rectangleStandard });

        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(rectangleStandard);
        project.Screens.Add(screen);
        ObjectFinder.GumProjectSave = project;

        // Matches RectScreen.gusx exactly (file order: RectangleInstance's vars, then RectangleInstance1's).
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.Height", Value = 154f, Type = "float", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.Width", Value = 225f, Type = "float", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.X", Value = 394f, Type = "float", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance.Y", Value = 139f, Type = "float", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance1.Height", Value = 154f, Type = "float", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance1.Width", Value = 225f, Type = "float", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance1.X", Value = 619f, Type = "float", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Name = "RectangleInstance1.Y", Value = 314f, Type = "float", SetsValue = true });

        VariableListSave<string> refs = new VariableListSave<string> { Type = "string", Name = "RectangleInstance1.VariableReferences" };
        refs.Value.Add("X=RectangleInstance.AbsoluteRight");
        defaultState.VariableLists.Add(refs);

        GraphicalUiElement rootGue = new GraphicalUiElement(new InvisibleRenderable());
        GraphicalUiElement rectGue = new GraphicalUiElement(new InvisibleRenderable()) { Name = "RectangleInstance", X = 394f, Width = 225f, Tag = rectInstance };
        GraphicalUiElement rectGue1 = new GraphicalUiElement(new InvisibleRenderable()) { Name = "RectangleInstance1", X = 619f, Width = 225f, Tag = rectInstance1 };
        rectGue.Parent = rootGue;
        rectGue1.Parent = rootGue;

        _selectedState.SetupGet(x => x.SelectedElement).Returns(screen);
        _selectedState.SetupGet(x => x.SelectedStateSave).Returns(defaultState);
        _selectedState.SetupGet(x => x.CustomCurrentStateSave).Returns((StateSave)null);

        _wireframeObjectManager.Setup(x => x.GetRepresentation(rectInstance, null)).Returns(rectGue);
        _wireframeObjectManager.Setup(x => x.GetRepresentation(screen)).Returns(rootGue);
        _wireframeObjectManager.SetupGet(x => x.RootGue).Returns(rootGue);

        defaultState.GetValue("RectangleInstance1.X").ShouldBe(619f, "before drag");

        // Pre-drag snapshot, matching what EditorContext.GrabbedState.StateSave would hold.
        float preDragRectX = 394f;
        float preDragRect1X = 619f;

        // Simulate 3 mouse-move ticks of +10px each (real drags fire ModifyVariable every tick).
        // AbsoluteRight = X + Width(225), so it must track 619 -> 629 -> 639 -> 649 exactly.
        float[] expectedAfterEachTick = { 629f, 639f, 649f };
        for (int tick = 1; tick <= 3; tick++)
        {
            _sut.ModifyVariable("X", 10f, rectInstance);

            defaultState.GetValue("RectangleInstance1.X").ShouldBe(expectedAfterEachTick[tick - 1], $"state after drag tick {tick}");
            rectGue1.X.ShouldBe(expectedAfterEachTick[tick - 1], $"live GUE after drag tick {tick}");
        }

        // Simulate EditorContext.DoEndOfSettingValuesLogic's release commit loop: iterate
        // stateSave.Variables in order, and for each one that changed since the drag started,
        // fire the same reaction SetVariableLogic.PropertyValueChanged would (DoVariableReferenceReaction).
        var wireframeCommandsMock = new Mock<IWireframeCommands>();
        var dialogServiceMock = new Mock<IDialogService>();
        var fileCommandsMock = new Mock<IFileCommands>();
        var dispatcherMock = new Mock<IDispatcher>();
        var registryMock = new Mock<ICompositeMemberRegistry>();
        registryMock.Setup(x => x.Descriptors).Returns(new List<CompositeMemberDescriptor>());
        var variableReferenceLogic = new VariableReferenceLogic(
            _guiCommands.Object, wireframeCommandsMock.Object, dialogServiceMock.Object,
            fileCommandsMock.Object, registryMock.Object, dispatcherMock.Object, _wireframeObjectManager.Object);

        foreach (var possiblyChangedVariable in defaultState.Variables.ToList())
        {
            object oldValue = possiblyChangedVariable.Name switch
            {
                "RectangleInstance.X" => preDragRectX,
                "RectangleInstance1.X" => preDragRect1X,
                _ => defaultState.GetValue(possiblyChangedVariable.Name)
            };
            var newValue = defaultState.GetValue(possiblyChangedVariable.Name);
            if (Equals(oldValue, newValue)) continue;

            var instance = screen.GetInstance(possiblyChangedVariable.SourceObject);
            variableReferenceLogic.DoVariableReferenceReaction(screen, instance,
                possiblyChangedVariable.GetRootName(), defaultState, possiblyChangedVariable.Name, trySave: false);

            // Release must never revert the drag's resolved value.
            defaultState.GetValue("RectangleInstance1.X").ShouldBe(649f, $"after release commit of {possiblyChangedVariable.Name}");
        }
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
