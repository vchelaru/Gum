using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Moq.AutoMock;
using Shouldly;
using System.Reflection;

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
    private readonly IServiceProvider _testServiceProvider;
    private readonly Mock<IProjectManager> _projectManagerMock;

    public ElementSaveDisplayerFormsPropertiesTests()
    {
        StandardElementsManager.Self.Initialize();
        var typeManager = new Gum.Reflection.TypeManager();
        typeManager.Initialize();

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

        _projectManagerMock = new Mock<IProjectManager>();
        _projectManagerMock.SetupGet(x => x.GumProjectSave).Returns(_project);
        var services = new ServiceCollection();
        services.AddSingleton(_projectManagerMock.Object);
        // Static Locator path (e.g. GetVariableFromThisOrBase) resolves ISelectedState; register the
        // same mock the displayer is constructor-injected with so both paths agree.
        services.AddSingleton(_mocker.GetMock<ISelectedState>().Object);
        // The static Locator path (extension methods like GetIsEnumeration/GetRuntimeType)
        // resolves ITypeManager; register the same real instance the displayer is
        // constructor-injected with so both paths agree.
        services.AddSingleton<Gum.Reflection.ITypeManager>(typeManager);
        _testServiceProvider = services.BuildServiceProvider();
        Locator.Register(_testServiceProvider);

        _mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedStateSave)
            .Returns(_screenDefaultState);
        _mocker.GetMock<ISelectedState>()
            .Setup(x => x.SelectedElement)
            .Returns(_screen);

        _mocker.Use(typeManager);

        _mocker.GetMock<IVariableSaveLogic>()
            .Setup(x => x.GetIfVariableIsActive(It.IsAny<VariableSave>(), It.IsAny<ElementSave>(), It.IsAny<InstanceSave>()))
            .Returns(true);

        _mocker.GetMock<IPluginManager>()
            .Setup(x => x.GetAttributesFor(It.IsAny<VariableSave>()))
            .Returns(new List<Attribute>());

        // ElementSaveDisplayer constructor-injects IProjectState directly (AvailableBaseTypeConverter's
        // dependency, once resolved via Locator<IProjectManager>) - keep it in sync with the same
        // project the Locator-registered IProjectManager mock above returns.
        _mocker.GetMock<IProjectState>()
            .Setup(x => x.GumProjectSave)
            .Returns(_project);

        _displayer = _mocker.CreateInstance<ElementSaveDisplayer>();
    }

    public override void Dispose()
    {
        var prop = typeof(Locator).GetProperty(
            "ServiceProviders", BindingFlags.NonPublic | BindingFlags.Static)!;
        var providers = (List<IServiceProvider>)prop.GetValue(null)!;
        providers.Remove(_testServiceProvider);

        base.Dispose();
    }

    /// <summary>
    /// Builds a standalone <see cref="VariableGridEntry"/> with the same mocked dependency closure
    /// the displayer itself is constructed with, for tests that need to simulate a pre-existing row
    /// (e.g. one already placed in a category before <c>AddBehaviorFormsPropertyMembers</c> runs).
    /// </summary>
    private VariableGridEntry CreateBareEntry(string variableName, InstanceSave? instanceSave, IStateContainer stateListCategoryContainer)
    {
        return new VariableGridEntry(
            attributes: null,
            converter: null,
            componentType: typeof(string),
            isReadOnly: false,
            isAssignedByReference: false,
            isVariable: true,
            stateSave: _screenDefaultState,
            stateSaveCategory: null,
            variableName: variableName,
            instanceSave: instanceSave,
            stateListCategoryContainer: stateListCategoryContainer,
            selectedState: _mocker.GetMock<ISelectedState>().Object,
            undoManager: _mocker.GetMock<IUndoManager>().Object,
            guiCommands: _mocker.GetMock<IGuiCommands>().Object,
            fileCommands: _mocker.GetMock<IFileCommands>().Object,
            setVariableLogic: _mocker.GetMock<ISetVariableLogic>().Object,
            wireframeObjectManager: _mocker.GetMock<IWireframeObjectManager>().Object,
            pluginManager: _mocker.GetMock<IPluginManager>().Object,
            hotkeyManager: _mocker.GetMock<IHotkeyManager>().Object,
            deleteVariableService: _mocker.GetMock<IDeleteVariableService>().Object,
            exposeVariableService: _mocker.GetMock<IExposeVariableService>().Object,
            editVariableService: _mocker.GetMock<IEditVariableService>().Object,
            typeManager: _mocker.Get<Gum.Reflection.TypeManager>(),
            clipboardService: _mocker.GetMock<IClipboardService>().Object);
    }

    [Fact]
    public void AddBehaviorFormsPropertyMembers_InstanceContext_AddsQualifiedToolTipMember()
    {
        List<VariableCategoryDescriptor> categories = new List<VariableCategoryDescriptor>();

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
        List<VariableCategoryDescriptor> categories = new List<VariableCategoryDescriptor>();

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

        List<VariableCategoryDescriptor> categories = new List<VariableCategoryDescriptor>();

        _displayer.AddBehaviorFormsPropertyMembers(
            elementWithBehaviors: component,
            instanceOwner: component,
            instance: null,
            stateSave: component.DefaultState,
            stateSaveCategory: null,
            categories: categories);

        VariableCategoryDescriptor? behaviorCategory = categories.FirstOrDefault(c => c.Name == "Behavior");
        behaviorCategory.ShouldNotBeNull();
        VariableGridEntry? isEnabledMember = behaviorCategory.Members.FirstOrDefault(m => m.Name == "IsEnabled");
        isEnabledMember.ShouldNotBeNull();
        isEnabledMember.GetValue(isEnabledMember.Instance!).ShouldBe(true,
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

        List<VariableCategoryDescriptor> categories = new List<VariableCategoryDescriptor>();

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

        List<VariableCategoryDescriptor> categories = new List<VariableCategoryDescriptor>();

        _displayer.AddBehaviorFormsPropertyMembers(
            elementWithBehaviors: component,
            instanceOwner: component,
            instance: null,
            stateSave: component.DefaultState,
            stateSaveCategory: null,
            categories: categories);

        VariableCategoryDescriptor? behaviorCategory = categories.FirstOrDefault(c => c.Name == "Behavior");
        behaviorCategory.ShouldNotBeNull();
        VariableGridEntry? acceptsReturnMember = behaviorCategory.Members.FirstOrDefault(m => m.Name == "AcceptsReturn");
        acceptsReturnMember.ShouldNotBeNull();
        acceptsReturnMember.DetailText.ShouldBe("If true, pressing Enter inserts a newline.");
    }

    [Fact]
    public void AddBehaviorFormsPropertyMembers_VariableAlreadyInGeneralCategory_MovesToBehaviorCategory()
    {
        // Simulates the case where the component has set a default value for the
        // FormsProperty: the standard properties path adds the variable under
        // "General" first; the helper must reclaim it into "Behavior".
        var generalCategory = new VariableCategoryDescriptor("General");
        var existingMember = CreateBareEntry("ButtonInstance.ToolTip", _buttonInstance, _screen);
        generalCategory.Members.Add(existingMember);
        List<VariableCategoryDescriptor> categories = new List<VariableCategoryDescriptor> { generalCategory };

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
        // The variable grid renders an enum picker only when the entry's value type
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

        List<VariableCategoryDescriptor> categories = new List<VariableCategoryDescriptor>();

        _displayer.AddBehaviorFormsPropertyMembers(
            elementWithBehaviors: scrollViewerComponent,
            instanceOwner: scrollViewerComponent,
            instance: null,
            stateSave: scrollViewerComponent.DefaultState,
            stateSaveCategory: null,
            categories: categories);

        VariableCategoryDescriptor? behaviorCategory = categories.FirstOrDefault(c => c.Name == "Behavior");
        behaviorCategory.ShouldNotBeNull();
        VariableGridEntry? member = behaviorCategory.Members.FirstOrDefault(m => m.Name == "VerticalScrollBarVisibility");
        member.ShouldNotBeNull();
        member.GetValueType(member.Instance!).ShouldBe(typeof(Gum.Forms.Controls.ScrollBarVisibility));
    }

    [Fact]
    public void GetCategories_InstanceOfComponentWithDefaultChildContainer_DoesNotLeakDefaultChildContainerVariable()
    {
        // Repros issue #2672: DefaultChildContainer ("Default Slot") on a component
        // bled through to instances of that component because the variable grid
        // displayer removed it from addedNames instead of guarding against re-add.
        _buttonComponent.DefaultState.Variables.Add(new VariableSave
        {
            Type = "string",
            Name = "DefaultChildContainer",
            Value = "InnerContainer"
        });

        var categories = _displayer.GetCategories(
            instanceOwner: _screen,
            instance: _buttonInstance,
            stateSave: _screenDefaultState,
            stateSaveCategory: null);

        categories.SelectMany(c => c.Members)
            .ShouldNotContain(m => m.Name.EndsWith("DefaultChildContainer"),
                "DefaultChildContainer should only appear on the component itself, not on instances of it");
    }

    [Fact]
    public void GetCategories_InstanceCategoryStateMaterializedByBehaviorToolOnlyReference_StaysHiddenFromInstance()
    {
        // Repro #3028: ButtonCategoryState is hidden-from-instances, but the ButtonBehavior's
        // tool-only reference (ButtonCategoryState = IsEnabled ? "Enabled" : "Disabled") materializes
        // a value into the instance's state. The grid's "explicitly set" escape hatch then re-surfaces
        // it as an editable combo that fights IsEnabled. A reference-materialized value is not
        // user-authored, so it must remain hidden.

        // Component declares the category state variable, its category, and hides it from instances.
        _buttonComponent.DefaultState.Variables.Add(new VariableSave
        {
            Type = "ButtonCategory",
            Name = "ButtonCategoryState",
            SetsValue = false
        });
        _buttonComponent.Categories.Add(new StateSaveCategory
        {
            Name = "ButtonCategory",
            States = new List<StateSave>
            {
                new StateSave { Name = "Enabled" },
                new StateSave { Name = "Disabled" }
            }
        });
        _buttonComponent.VariablesHiddenFromInstances.Add("ButtonCategoryState");

        // The behavior drives the category state from IsEnabled via a tool-only reference.
        _buttonBehavior.ToolOnlyVariableReferences.Add(
            "ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"");

        // The materialized value sits in the screen's state, as it would after the applier runs.
        _screenDefaultState.Variables.Add(new VariableSave
        {
            Type = "string",
            Name = "ButtonInstance.ButtonCategoryState",
            Value = "Enabled",
            SetsValue = true
        });

        var categories = _displayer.GetCategories(
            instanceOwner: _screen,
            instance: _buttonInstance,
            stateSave: _screenDefaultState,
            stateSaveCategory: null);

        categories.SelectMany(c => c.Members)
            .ShouldNotContain(m => m.Name.EndsWith("ButtonCategoryState"),
                "a category state materialized by a behavior tool-only reference is not user-authored and must remain hidden from instances");
    }

    [Fact]
    public void GetCategories_InstanceStandardEnumVariableMaterializedByBehaviorToolOnlyReference_StaysHiddenFromInstance()
    {
        // The #3028 fix must generalize from category-state variables to ordinary enum variables.
        // StackPanelBehavior drives the visual ChildrenLayout enum from its Orientation FormsProperty
        // (ChildrenLayout = Orientation == "Horizontal" ? "LeftToRightStack" : "TopToBottomStack").
        // When the component hides ChildrenLayout from instances, the materialized value must stay
        // hidden - it must not re-surface via the "explicitly set" escape hatch the way it would if
        // the behavior-driven detection only recognized category-state (<X>State) variables.
        _buttonComponent.DefaultState.Variables.Add(new VariableSave
        {
            Type = "ChildrenLayout",
            Name = "ChildrenLayout",
            Value = ChildrenLayout.TopToBottomStack,
            SetsValue = true
        });
        _buttonComponent.VariablesHiddenFromInstances.Add("ChildrenLayout");

        _buttonBehavior.ToolOnlyVariableReferences.Add(
            "ChildrenLayout = Orientation == \"Horizontal\" ? \"LeftToRightStack\" : \"TopToBottomStack\"");

        // The materialized value sits in the screen's state, as it would after the applier runs.
        _screenDefaultState.Variables.Add(new VariableSave
        {
            Type = "ChildrenLayout",
            Name = "ButtonInstance.ChildrenLayout",
            Value = ChildrenLayout.LeftToRightStack,
            SetsValue = true
        });

        var categories = _displayer.GetCategories(
            instanceOwner: _screen,
            instance: _buttonInstance,
            stateSave: _screenDefaultState,
            stateSaveCategory: null);

        categories.SelectMany(c => c.Members)
            .ShouldNotContain(m => m.Name.EndsWith("ChildrenLayout"),
                "a standard enum variable materialized by a behavior tool-only reference must remain hidden from instances");
    }

    [Fact]
    public void GetCategories_InstanceOfDerivedTypeWithInheritedBehaviorToolOnlyReference_StaysHiddenFromInstance()
    {
        // Same as the above, but the instance's type is a *derived* component whose BaseType is the
        // button. The ButtonBehavior (and its tool-only reference) lives on the base, not on the
        // derived type directly, so the reference-driven detection must walk the inheritance chain -
        // mirroring IsVariableHiddenRecursively, which already walks BaseType to hide the variable.
        _buttonComponent.DefaultState.Variables.Add(new VariableSave
        {
            Type = "ButtonCategory",
            Name = "ButtonCategoryState",
            SetsValue = false
        });
        _buttonComponent.Categories.Add(new StateSaveCategory
        {
            Name = "ButtonCategory",
            States = new List<StateSave>
            {
                new StateSave { Name = "Enabled" },
                new StateSave { Name = "Disabled" }
            }
        });
        _buttonComponent.VariablesHiddenFromInstances.Add("ButtonCategoryState");
        _buttonBehavior.ToolOnlyVariableReferences.Add(
            "ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"");

        // Derived component inherits from the button but does NOT re-declare the behavior.
        ComponentSave derivedComponent = new ComponentSave
        {
            Name = "Controls/FancyButton",
            BaseType = "Controls/ButtonStandard"
        };
        derivedComponent.States.Add(new StateSave { Name = "Default", ParentContainer = derivedComponent });
        _project.Components.Add(derivedComponent);

        InstanceSave fancyInstance = new InstanceSave
        {
            Name = "FancyInstance",
            BaseType = "Controls/FancyButton",
            ParentContainer = _screen
        };
        _screen.Instances.Add(fancyInstance);

        _screenDefaultState.Variables.Add(new VariableSave
        {
            Type = "string",
            Name = "FancyInstance.ButtonCategoryState",
            Value = "Enabled",
            SetsValue = true
        });

        var categories = _displayer.GetCategories(
            instanceOwner: _screen,
            instance: fancyInstance,
            stateSave: _screenDefaultState,
            stateSaveCategory: null);

        categories.SelectMany(c => c.Members)
            .ShouldNotContain(m => m.Name.EndsWith("ButtonCategoryState"),
                "a category state driven by an inherited behavior tool-only reference must remain hidden from instances of derived types");
    }

    [Fact]
    public void AddBehaviorFormsPropertyMembers_NullableEnumTypedFormsProperty_ResolvesNullableEnumComponentType()
    {
        // FormsProperty Type="Foo?" must resolve to typeof(Foo?) so the variable grid
        // renders an enum picker (with a "None" option) instead of falling back to a
        // string textbox. Without nullable-enum support in TypeManager.GetTypeFromString,
        // the entry's value type is null and the grid uses a generic editor — that's
        // the bug for Splitter.ResizeBehavior? and ItemsControl/ListBox.Orientation?.
        BehaviorSave splitterBehavior = new BehaviorSave { Name = "SplitterBehavior" };
        splitterBehavior.FormsProperties.Add(new VariableSave
        {
            Type = "ResizeBehavior?",
            Name = "ResizeBehavior"
        });

        ComponentSave splitterComponent = new ComponentSave
        {
            Name = "Controls/Splitter",
            BaseType = "Container"
        };
        splitterComponent.States.Add(new StateSave { Name = "Default", ParentContainer = splitterComponent });
        splitterComponent.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "SplitterBehavior" });
        _project.Behaviors.Add(splitterBehavior);
        _project.Components.Add(splitterComponent);

        List<VariableCategoryDescriptor> categories = new List<VariableCategoryDescriptor>();

        _displayer.AddBehaviorFormsPropertyMembers(
            elementWithBehaviors: splitterComponent,
            instanceOwner: splitterComponent,
            instance: null,
            stateSave: splitterComponent.DefaultState,
            stateSaveCategory: null,
            categories: categories);

        VariableCategoryDescriptor? behaviorCategory = categories.FirstOrDefault(c => c.Name == "Behavior");
        behaviorCategory.ShouldNotBeNull();
        VariableGridEntry? member = behaviorCategory.Members.FirstOrDefault(m => m.Name == "ResizeBehavior");
        member.ShouldNotBeNull();
        member.GetValueType(member.Instance!).ShouldBe(typeof(Gum.Forms.Controls.ResizeBehavior?));
    }
}
