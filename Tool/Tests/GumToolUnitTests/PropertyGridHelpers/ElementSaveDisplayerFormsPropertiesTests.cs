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
    public void AddBehaviorFormsPropertyMembers_StateMissingValue_MemberValueFallsBackToBehaviorFormsPropertyDefault()
    {
        // Behavior declares IsEnabled with default true; component's default state authors nothing.
        BehaviorSave behaviorWithDefault = new BehaviorSave { Name = "WithDefault" };
        behaviorWithDefault.FormsProperties.Add(new VariableSave
        {
            Type = "bool",
            Name = "IsEnabled",
            Value = true
        });

        ComponentSave component = new ComponentSave { Name = "Controls/PlainButton", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "WithDefault" });

        _project.Behaviors.Add(behaviorWithDefault);
        _project.Components.Add(component);

        List<MemberCategory> categories = new List<MemberCategory>();

        _displayer.AddBehaviorFormsPropertyMembers(
            elementWithBehaviors: component,
            instanceOwner: component,
            instance: null,
            stateSave: component.DefaultState,
            stateSaveCategory: null,
            categories: categories);

        MemberCategory? behaviorCategory = categories.FirstOrDefault(c => c.Name == "Behavior");
        behaviorCategory.ShouldNotBeNull();
        InstanceMember? isEnabledMember = behaviorCategory.Members.FirstOrDefault(m => m.Name == "IsEnabled");
        isEnabledMember.ShouldNotBeNull();
        isEnabledMember.Value.ShouldBe(true,
            "with no value authored on state, the variable grid should display the behavior's declared FormsProperty default");
    }

    [Fact]
    public void AddBehaviorFormsPropertyMembers_FormsPropertyWithNullName_SilentlySkippedNoCrash()
    {
        // Reproduces the v1 (legacy) gumx scenario where the standard XmlSerializer
        // deserializes a compact-form <FormsProperty Type=".." Name=".." /> into a
        // VariableSave whose Type and Name are both null.
        BehaviorSave bogusBehavior = new BehaviorSave { Name = "BogusBehavior" };
        bogusBehavior.FormsProperties.Add(new VariableSave
        {
            Type = null,
            Name = null
        });

        ComponentSave bogusComponent = new ComponentSave { Name = "Controls/BogusComponent", BaseType = "Container" };
        bogusComponent.States.Add(new StateSave { Name = "Default", ParentContainer = bogusComponent });
        bogusComponent.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "BogusBehavior" });
        _project.Components.Add(bogusComponent);
        _project.Behaviors.Add(bogusBehavior);

        List<MemberCategory> categories = new List<MemberCategory>();

        Should.NotThrow(() =>
            _displayer.AddBehaviorFormsPropertyMembers(
                elementWithBehaviors: bogusComponent,
                instanceOwner: bogusComponent,
                instance: null,
                stateSave: bogusComponent.DefaultState,
                stateSaveCategory: null,
                categories: categories));

        // No member added for the malformed entry; the well-formed ToolTip from
        // ButtonBehavior on the other component is still on a separate path.
        var behaviorCategory = categories.FirstOrDefault(c => c.Name == "Behavior");
        if (behaviorCategory != null)
        {
            behaviorCategory.Members.Count.ShouldBe(0);
        }
    }

    [Fact]
    public void AddBehaviorFormsPropertyMembers_FormsPropertyWithDescription_SeedsSrimDetailText()
    {
        BehaviorSave describedBehavior = new BehaviorSave { Name = "DescribedBehavior" };
        describedBehavior.FormsProperties.Add(new VariableSave
        {
            Type = "bool",
            Name = "AcceptsReturn",
            Value = false,
            Description = "If true, pressing Enter inserts a newline."
        });

        ComponentSave component = new ComponentSave { Name = "Controls/Described", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "DescribedBehavior" });
        _project.Behaviors.Add(describedBehavior);
        _project.Components.Add(component);

        List<MemberCategory> categories = new List<MemberCategory>();

        _displayer.AddBehaviorFormsPropertyMembers(
            elementWithBehaviors: component,
            instanceOwner: component,
            instance: null,
            stateSave: component.DefaultState,
            stateSaveCategory: null,
            categories: categories);

        MemberCategory? behaviorCategory = categories.FirstOrDefault(c => c.Name == "Behavior");
        behaviorCategory.ShouldNotBeNull();
        InstanceMember? acceptsReturnMember = behaviorCategory.Members.FirstOrDefault(m => m.Name == "AcceptsReturn");
        acceptsReturnMember.ShouldNotBeNull();
        acceptsReturnMember.DetailText.ShouldBe("If true, pressing Enter inserts a newline.");
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

    [Fact]
    public void AddBehaviorFormsPropertyMembers_EnumTypedFormsProperty_ResolvesEnumComponentType()
    {
        // The variable grid renders an enum picker only when the SRIM's PropertyType
        // (sourced from TypeManager.GetTypeFromString on the FormsProperty's Type
        // string) is the actual enum. Regression for the v4 enum-typed FormsProperty
        // path: TypeManager must find Gum.Forms.Controls.ScrollBarVisibility (now
        // owned by GumCommon, where TypeManager scans).
        BehaviorSave scrollViewerBehavior = new BehaviorSave { Name = "ScrollViewerBehavior" };
        scrollViewerBehavior.FormsProperties.Add(new VariableSave
        {
            Type = "ScrollBarVisibility",
            Name = "VerticalScrollBarVisibility",
            Value = "Auto"
        });

        ComponentSave scrollViewerComponent = new ComponentSave
        {
            Name = "Controls/ScrollViewer",
            BaseType = "Container"
        };
        scrollViewerComponent.States.Add(new StateSave { Name = "Default", ParentContainer = scrollViewerComponent });
        scrollViewerComponent.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ScrollViewerBehavior" });
        _project.Behaviors.Add(scrollViewerBehavior);
        _project.Components.Add(scrollViewerComponent);

        List<MemberCategory> categories = new List<MemberCategory>();

        _displayer.AddBehaviorFormsPropertyMembers(
            elementWithBehaviors: scrollViewerComponent,
            instanceOwner: scrollViewerComponent,
            instance: null,
            stateSave: scrollViewerComponent.DefaultState,
            stateSaveCategory: null,
            categories: categories);

        MemberCategory? behaviorCategory = categories.FirstOrDefault(c => c.Name == "Behavior");
        behaviorCategory.ShouldNotBeNull();
        InstanceMember? member = behaviorCategory.Members.FirstOrDefault(m => m.Name == "VerticalScrollBarVisibility");
        member.ShouldNotBeNull();
        member.PropertyType.ShouldBe(typeof(Gum.Forms.Controls.ScrollBarVisibility));
    }
}
