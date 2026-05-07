using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.PropertyGridHelpers;
using Gum.ToolStates;
using Moq;
using Moq.AutoMock;
using Shouldly;
using WpfDataUi.DataTypes;

namespace GumToolUnitTests.PropertyGridHelpers;

public class ElementSaveDisplayerFormsPropertiesTests : BaseTestClass
{
    private readonly AutoMocker _mocker = new();
    private readonly ElementSaveDisplayer _displayer;
    private readonly GumProjectSave _project;
    private readonly BehaviorSave _buttonBehavior;
    private readonly ComponentSave _buttonComponent;
    private readonly ScreenSave _screen;
    private readonly InstanceSave _buttonInstance;
    private readonly StateSave _screenDefaultState;

    public ElementSaveDisplayerFormsPropertiesTests()
    {
        StandardElementsManager.Self.Initialize();
        Gum.Reflection.TypeManager.Self.Initialize();

        _buttonBehavior = new BehaviorSave { Name = "ButtonBehavior" };
        _buttonBehavior.FormsProperties.Add(new VariableSave
        {
            Type = "string",
            Name = "ToolTip"
        });

        _buttonComponent = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        _buttonComponent.States.Add(new StateSave { Name = "Default", ParentContainer = _buttonComponent });
        _buttonComponent.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });

        _screen = new ScreenSave { Name = "TestScreen" };
        _screenDefaultState = new StateSave { Name = "Default", ParentContainer = _screen };
        _screen.States.Add(_screenDefaultState);

        _buttonInstance = new InstanceSave
        {
            Name = "ButtonInstance",
            BaseType = "Controls/ButtonStandard",
            ParentContainer = _screen
        };
        _screen.Instances.Add(_buttonInstance);

        _project = new GumProjectSave();
        _project.Components.Add(_buttonComponent);
        _project.Screens.Add(_screen);
        _project.Behaviors.Add(_buttonBehavior);
        ObjectFinder.Self.GumProjectSave = _project;

        _mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedStateSave)
            .Returns(_screenDefaultState);
        _mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedElement)
            .Returns(_screen);

        _mocker.Use(Gum.Reflection.TypeManager.Self);

        _displayer = _mocker.CreateInstance<ElementSaveDisplayer>();
    }

    [Fact]
    public void AddBehaviorFormsPropertyMembers_InstanceContext_AddsQualifiedToolTipMember()
    {
        List<MemberCategory> categories = new List<MemberCategory>();

        _displayer.AddBehaviorFormsPropertyMembers(
            elementWithBehaviors: _buttonComponent,
            instanceOwner: _screen,
            instance: _buttonInstance,
            stateSave: _screenDefaultState,
            stateSaveCategory: null,
            categories: categories);

        var behaviorCategory = categories.FirstOrDefault(c => c.Name == "Behavior");
        behaviorCategory.ShouldNotBeNull("a Behavior category should be added when the instance's BaseType has a behavior with FormsProperties");
        behaviorCategory.Members.Any(m => m.Name == "ButtonInstance.ToolTip")
            .ShouldBeTrue("ToolTip should appear qualified with the instance name");
    }

    [Fact]
    public void AddBehaviorFormsPropertyMembers_ComponentContext_AddsUnqualifiedToolTipMember()
    {
        List<MemberCategory> categories = new List<MemberCategory>();

        _displayer.AddBehaviorFormsPropertyMembers(
            elementWithBehaviors: _buttonComponent,
            instanceOwner: _buttonComponent,
            instance: null,
            stateSave: _buttonComponent.DefaultState,
            stateSaveCategory: null,
            categories: categories);

        var behaviorCategory = categories.FirstOrDefault(c => c.Name == "Behavior");
        behaviorCategory.ShouldNotBeNull();
        behaviorCategory.Members.Any(m => m.Name == "ToolTip").ShouldBeTrue();
    }

    [Fact]
    public void AddBehaviorFormsPropertyMembers_VariableAlreadyInGeneralCategory_MovesToBehaviorCategory()
    {
        // Simulates the case where the component has set a default value for the
        // FormsProperty: the standard properties path adds the variable under
        // "General" first; the helper must reclaim it into "Behavior".
        var generalCategory = new MemberCategory("General");
        var existingMember = new WpfDataUi.DataTypes.InstanceMember
        {
            Name = "ButtonInstance.ToolTip"
        };
        generalCategory.Members.Add(existingMember);
        List<MemberCategory> categories = new List<MemberCategory> { generalCategory };

        _displayer.AddBehaviorFormsPropertyMembers(
            elementWithBehaviors: _buttonComponent,
            instanceOwner: _screen,
            instance: _buttonInstance,
            stateSave: _screenDefaultState,
            stateSaveCategory: null,
            categories: categories);

        generalCategory.Members.ShouldNotContain(existingMember,
            "the existing entry should be moved out of General");

        var behaviorCategory = categories.FirstOrDefault(c => c.Name == "Behavior");
        behaviorCategory.ShouldNotBeNull();
        behaviorCategory.Members.ShouldContain(existingMember,
            "the existing entry should be reused (preserves its DetailText, displayer hints, etc.) rather than rebuilt");
    }
}
