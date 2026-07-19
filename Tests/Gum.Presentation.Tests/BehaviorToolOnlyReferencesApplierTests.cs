using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using GumRuntime;
using Shouldly;

namespace Gum.Presentation.Tests;

public class BehaviorToolOnlyReferencesApplierTests : BaseTestClass
{
    public BehaviorToolOnlyReferencesApplierTests()
    {
        GumExpressionService.Initialize();
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.VariableChangedThroughReference = null;
        base.Dispose();
    }

    [Fact]
    public void Apply_ComponentDefaultStateWithIsEnabledFalse_WritesDisabledCategoryState()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled" });
        behavior.ToolOnlyVariableReferences.Add(
            "ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"");

        ComponentSave component = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave
        {
            Type = "bool",
            Name = "IsEnabled",
            Value = false,
            SetsValue = true
        });
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        BehaviorToolOnlyReferencesApplier.Apply(component, defaultState);

        defaultState.GetValue("ButtonCategoryState").ShouldBe("Disabled");
    }

    [Fact]
    public void Apply_ComponentDefaultStateWithIsEnabledTrue_WritesEnabledCategoryState()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled" });
        behavior.ToolOnlyVariableReferences.Add(
            "ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"");

        ComponentSave component = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave
        {
            Type = "bool",
            Name = "IsEnabled",
            Value = true,
            SetsValue = true
        });
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        BehaviorToolOnlyReferencesApplier.Apply(component, defaultState);

        defaultState.GetValue("ButtonCategoryState").ShouldBe("Enabled");
    }

    [Fact]
    public void Apply_EnumTypedTarget_CoercesToEnumNotRawString()
    {
        // Behavior-applier-specific: the tool-only applier evaluates the RHS directly rather
        // than through the state-level apply path, so it must perform the same left-type
        // coercion (EvaluatedSyntax.CastTo) that path does - otherwise an enum-typed target
        // like ChildrenLayout would store the raw string a ternary produces. The coercion
        // mechanism itself is covered by ApplyVariableReferencesElementSaveTests; this pins
        // that the behavior applier actually invokes it. Models StackPanelBehavior mapping
        // its Orientation FormsProperty onto the visual ChildrenLayout.
        BehaviorSave behavior = new BehaviorSave { Name = "StackPanelBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "Orientation", Name = "Orientation", Value = "Vertical" });
        behavior.ToolOnlyVariableReferences.Add(
            "ChildrenLayout = Orientation == \"Horizontal\" ? \"LeftToRightStack\" : \"TopToBottomStack\"");

        ComponentSave component = new ComponentSave { Name = "Controls/StackPanel", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave
        {
            Type = "Orientation",
            Name = "Orientation",
            Value = "Horizontal",
            SetsValue = true
        });
        // The component authors ChildrenLayout (as the real Controls/StackPanel does), which
        // is also how the applier resolves the left-hand side's declared enum type.
        defaultState.Variables.Add(new VariableSave
        {
            Type = "ChildrenLayout",
            Name = "ChildrenLayout",
            Value = ChildrenLayout.TopToBottomStack,
            SetsValue = true
        });
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "StackPanelBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        BehaviorToolOnlyReferencesApplier.Apply(component, defaultState);

        defaultState.GetValue("ChildrenLayout").ShouldBe(ChildrenLayout.LeftToRightStack);
    }

    [Fact]
    public void Apply_ScreenWithStackPanelInstanceOrientationHorizontal_WritesEnumChildrenLayout()
    {
        // Repro of the editor crash (string "LeftToRightStack" reaching the typed ChildrenLayout
        // setter): a StackPanel instance on a screen with Orientation = Horizontal. The instance
        // path resolves the left-side enum type via ObjectFinder.GetRootVariable (the value is not
        // authored on the screen) - distinct from the component-default path, which reads the
        // authored variable. The materialized value must still be the boxed ChildrenLayout enum.
        BehaviorSave behavior = new BehaviorSave { Name = "StackPanelBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "Orientation", Name = "Orientation", Value = "Vertical" });
        behavior.ToolOnlyVariableReferences.Add(
            "ChildrenLayout = Orientation == \"Horizontal\" ? \"LeftToRightStack\" : \"TopToBottomStack\"");

        ComponentSave component = new ComponentSave { Name = "Controls/StackPanel", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "StackPanelBehavior" });

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);

        InstanceSave stackInstance = new InstanceSave
        {
            Name = "StackInstance",
            BaseType = "Controls/StackPanel",
            ParentContainer = screen
        };
        screen.Instances.Add(stackInstance);

        screenDefault.Variables.Add(new VariableSave
        {
            Type = "Orientation",
            Name = "StackInstance.Orientation",
            Value = "Horizontal",
            SetsValue = true
        });

        // The Container standard declares ChildrenLayout so the applier can resolve the instance's
        // left-side type via GetRootVariable (the value itself is not authored on the screen).
        StandardElementSave containerStandard = new StandardElementSave { Name = "Container" };
        StateSave containerDefault = new StateSave { Name = "Default", ParentContainer = containerStandard };
        containerDefault.Variables.Add(new VariableSave
        {
            Type = "ChildrenLayout",
            Name = "ChildrenLayout",
            Value = ChildrenLayout.TopToBottomStack,
            SetsValue = true
        });
        containerStandard.States.Add(containerDefault);

        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(containerStandard);
        project.Components.Add(component);
        project.Screens.Add(screen);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        BehaviorToolOnlyReferencesApplier.Apply(screen, screenDefault);

        screenDefault.GetValue("StackInstance.ChildrenLayout").ShouldBe(ChildrenLayout.LeftToRightStack);
    }

    [Fact]
    public void Apply_ScreenWithButtonInstanceIsEnabledFalse_WritesQualifiedDisabledCategoryState()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled" });
        behavior.ToolOnlyVariableReferences.Add(
            "ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"");

        ComponentSave component = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);

        InstanceSave buttonInstance = new InstanceSave
        {
            Name = "ButtonInstance",
            BaseType = "Controls/ButtonStandard",
            ParentContainer = screen
        };
        screen.Instances.Add(buttonInstance);

        screenDefault.Variables.Add(new VariableSave
        {
            Type = "bool",
            Name = "ButtonInstance.IsEnabled",
            Value = false,
            SetsValue = true
        });

        StandardElementSave containerStandard = new StandardElementSave { Name = "Container" };
        containerStandard.States.Add(new StateSave { Name = "Default", ParentContainer = containerStandard });

        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(containerStandard);
        project.Components.Add(component);
        project.Screens.Add(screen);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        BehaviorToolOnlyReferencesApplier.Apply(screen, screenDefault);

        screenDefault.GetValue("ButtonInstance.ButtonCategoryState").ShouldBe("Disabled");
    }

    [Fact]
    public void Apply_ScreenWithUnauthoredButtonInstance_DoesNotMaterializeSelector()
    {
        // Repro #3080: a screen/component holding many Forms-control instances (buttons,
        // checkboxes, combos...) got every instance's category selector spammed into its
        // state - MapCreateButton.ButtonCategoryState, IsStartingMapCheckbox.CheckBoxCategoryState,
        // etc. - even when the instance authored nothing. The selector value derived purely
        // from the behavior's FormsProperty default (IsEnabled = true -> "Enabled"), so it
        // carries no information beyond the resting wireframe and must NOT be written.
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        behavior.ToolOnlyVariableReferences.Add(
            "ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"");

        ComponentSave component = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);

        InstanceSave buttonInstance = new InstanceSave
        {
            Name = "ButtonInstance",
            BaseType = "Controls/ButtonStandard",
            ParentContainer = screen
        };
        screen.Instances.Add(buttonInstance);

        StandardElementSave containerStandard = new StandardElementSave { Name = "Container" };
        containerStandard.States.Add(new StateSave { Name = "Default", ParentContainer = containerStandard });

        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(containerStandard);
        project.Components.Add(component);
        project.Screens.Add(screen);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        BehaviorToolOnlyReferencesApplier.Apply(screen, screenDefault);

        screenDefault.Variables.ShouldNotContain(v => v.Name == "ButtonInstance.ButtonCategoryState",
            "an instance that authors no driving FormsProperty must not have its selector materialized into the parent state");
    }

    [Fact]
    public void Apply_StateMissingIdentifier_DoesNotMaterializeFormsPropertyDefault()
    {
        // When nothing is authored, the reference resolves purely from the behavior's
        // FormsProperty default. That value equals the resting wireframe, so materializing
        // it into the state adds no information and only spams it (issue #3080). The default
        // is virtual — "state-empty stays state-empty" — so the selector must not be written.
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave
        {
            Type = "bool",
            Name = "IsEnabled",
            Value = false
        });
        behavior.ToolOnlyVariableReferences.Add(
            "ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"");

        ComponentSave component = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        BehaviorToolOnlyReferencesApplier.Apply(component, defaultState);

        defaultState.Variables.ShouldNotContain(v => v.Name == "ButtonCategoryState",
            "a value derived purely from the FormsProperty default equals the resting wireframe and must not be materialized");
    }

    [Fact]
    public void Apply_StateAuthoredValue_OverridesBehaviorFormsPropertyDefault()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave
        {
            Type = "bool",
            Name = "IsEnabled",
            Value = true
        });
        behavior.ToolOnlyVariableReferences.Add(
            "ButtonCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"");

        ComponentSave component = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave
        {
            Type = "bool",
            Name = "IsEnabled",
            Value = false,
            SetsValue = true
        });
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        BehaviorToolOnlyReferencesApplier.Apply(component, defaultState);

        defaultState.GetValue("ButtonCategoryState").ShouldBe("Disabled");
    }

    [Fact]
    public void Apply_CheckBoxIsCheckedTrueAndIsEnabledTrue_WritesEnabledOnCategoryState()
    {
        BehaviorSave behavior = BuildCheckBoxBehavior();

        ComponentSave component = new ComponentSave { Name = "Controls/CheckBox", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave
        {
            Type = "bool",
            Name = "IsEnabled",
            Value = true,
            SetsValue = true
        });
        defaultState.Variables.Add(new VariableSave
        {
            Type = "bool",
            Name = "IsChecked",
            Value = true,
            SetsValue = true
        });
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "CheckBoxBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        BehaviorToolOnlyReferencesApplier.Apply(component, defaultState);

        defaultState.GetValue("CheckBoxCategoryState").ShouldBe("EnabledOn");
    }

    [Fact]
    public void Apply_CheckBoxIsCheckedFalseAndIsEnabledFalse_WritesDisabledOffCategoryState()
    {
        BehaviorSave behavior = BuildCheckBoxBehavior();

        ComponentSave component = new ComponentSave { Name = "Controls/CheckBox", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave
        {
            Type = "bool",
            Name = "IsEnabled",
            Value = false,
            SetsValue = true
        });
        defaultState.Variables.Add(new VariableSave
        {
            Type = "bool",
            Name = "IsChecked",
            Value = false,
            SetsValue = true
        });
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "CheckBoxBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        BehaviorToolOnlyReferencesApplier.Apply(component, defaultState);

        defaultState.GetValue("CheckBoxCategoryState").ShouldBe("DisabledOff");
    }

    [Fact]
    public void Apply_CheckBoxIsCheckedTrueAndIsEnabledFalse_WritesDisabledOnCategoryState()
    {
        BehaviorSave behavior = BuildCheckBoxBehavior();

        ComponentSave component = new ComponentSave { Name = "Controls/CheckBox", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave
        {
            Type = "bool",
            Name = "IsEnabled",
            Value = false,
            SetsValue = true
        });
        defaultState.Variables.Add(new VariableSave
        {
            Type = "bool",
            Name = "IsChecked",
            Value = true,
            SetsValue = true
        });
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "CheckBoxBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        BehaviorToolOnlyReferencesApplier.Apply(component, defaultState);

        defaultState.GetValue("CheckBoxCategoryState").ShouldBe("DisabledOn");
    }

    [Fact]
    public void Apply_CheckBoxStateEmpty_DoesNotMaterializeFormsPropertyDefault()
    {
        // With nothing authored, every identifier resolves from the behavior's FormsProperty
        // defaults (IsEnabled = true, IsChecked = false -> "EnabledOff"). That is the resting
        // wireframe value, so it must not be materialized into the state (issue #3080).
        BehaviorSave behavior = BuildCheckBoxBehavior();

        ComponentSave component = new ComponentSave { Name = "Controls/CheckBox", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "CheckBoxBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        BehaviorToolOnlyReferencesApplier.Apply(component, defaultState);

        defaultState.Variables.ShouldNotContain(v => v.Name == "CheckBoxCategoryState",
            "a value derived purely from the FormsProperty defaults equals the resting wireframe and must not be materialized");
    }

    [Fact(Timeout = 5000)]
    public void Apply_SelfReferentialReferenceWritingItsOwnRhs_TerminatesInOnePass()
    {
        // A reference whose LHS is the same variable read on the RHS would be a recursion
        // hazard if Apply re-fired itself when intermediate writes hit the state. It does
        // not — Apply walks references once per call. This test pins that contract by
        // running Apply with a tight self-loop and asserting it terminates (Timeout will
        // trip if it ever regresses to a re-entrant design).
        BehaviorSave behavior = new BehaviorSave { Name = "SelfLoopBehavior" };
        behavior.FormsProperties.Add(new VariableSave
        {
            Type = "string",
            Name = "Toggle",
            // The FormsProperty default differs from the authored value below so the
            // authored result ("on") is not suppressed as equal to the resting value
            // (the resting eval reads Toggle = "on" -> "off"). Keeps this a write-and-
            // terminate test, not a resting-skip test (issue #3080).
            Value = "on"
        });
        // RHS reads Toggle, LHS writes Toggle.
        behavior.ToolOnlyVariableReferences.Add(
            "Toggle = Toggle == \"off\" ? \"on\" : \"off\"");

        ComponentSave component = new ComponentSave { Name = "Controls/SelfLoop", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave
        {
            Type = "string",
            Name = "Toggle",
            Value = "off",
            SetsValue = true
        });
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "SelfLoopBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        Should.NotThrow(() => BehaviorToolOnlyReferencesApplier.Apply(component, defaultState));
        // One pass: off -> on. If Apply re-fired on its own write, the value would either
        // oscillate or run away.
        defaultState.GetValue("Toggle").ShouldBe("on");
    }

    [Fact(Timeout = 5000)]
    public void Apply_TwoMutuallyReferentialReferences_TerminatesInOnePass()
    {
        // Two ToolOnly references where each LHS appears on the other's RHS. Apply walks
        // them in source order, in a single pass — A is evaluated and written first, then
        // B is evaluated against the *already-updated* A. Outcome is deterministic and
        // bounded; no recursion.
        BehaviorSave behavior = new BehaviorSave { Name = "MutualBehavior" };
        // FormsProperty defaults differ from the authored a0/b0 below so the authored
        // results are not suppressed as equal to the resting value (issue #3080); this
        // stays a write-and-terminate test.
        behavior.FormsProperties.Add(new VariableSave { Type = "string", Name = "A", Value = "dA" });
        behavior.FormsProperties.Add(new VariableSave { Type = "string", Name = "B", Value = "dB" });
        behavior.ToolOnlyVariableReferences.Add("A = B + \"->A\"");
        behavior.ToolOnlyVariableReferences.Add("B = A + \"->B\"");

        ComponentSave component = new ComponentSave { Name = "Controls/Mutual", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave { Type = "string", Name = "A", Value = "a0", SetsValue = true });
        defaultState.Variables.Add(new VariableSave { Type = "string", Name = "B", Value = "b0", SetsValue = true });
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "MutualBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        Should.NotThrow(() => BehaviorToolOnlyReferencesApplier.Apply(component, defaultState));
        // Pass order: A = B + "->A" -> "b0->A"; B = A + "->B" -> "b0->A->B".
        // Pinning these confirms single-pass evaluation in source order.
        defaultState.GetValue("A").ShouldBe("b0->A");
        defaultState.GetValue("B").ShouldBe("b0->A->B");
    }

    [Fact]
    public void Apply_NonDefaultCategoryState_DoesNotMaterializeSelectorIntoIt()
    {
        // Repro #3055: the applier is invoked with whatever state is currently selected.
        // When that state is a category state (e.g. TextBoxCategory/Focused), materializing
        // TextBoxCategoryState into it bakes the category's own selector into one of its
        // states - a self-referential/circular state that re-drives the category back to
        // "Enabled" on preview, clobbering the state's authored values. Tool-only references
        // exist only to drive the resting default-state preview, so a non-default state must
        // be left untouched.
        BehaviorSave behavior = new BehaviorSave { Name = "TextBoxBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        behavior.ToolOnlyVariableReferences.Add(
            "TextBoxCategoryState = IsEnabled ? \"Enabled\" : \"Disabled\"");

        ComponentSave component = new ComponentSave { Name = "Controls/TextBox", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "TextBoxBehavior" });

        StateSave focusedState = new StateSave { Name = "Focused", ParentContainer = component };
        focusedState.Variables.Add(new VariableSave
        {
            Type = "ColorCategory",
            Name = "Border.ColorCategoryState",
            Value = "Primary",
            SetsValue = true
        });
        component.Categories.Add(new StateSaveCategory
        {
            Name = "TextBoxCategory",
            States = new List<StateSave> { focusedState }
        });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        BehaviorToolOnlyReferencesApplier.Apply(component, focusedState);

        focusedState.Variables.ShouldNotContain(v => v.Name == "TextBoxCategoryState",
            "a category state must not have its own category's selector materialized into it");
    }

    [Fact]
    public void Apply_CheckBoxInstanceAuthoringOnlyIsChecked_StillMaterializesEnabledOn()
    {
        // Guard for the resting-comparison fix (#3080): an instance that authors only one of
        // several driving FormsProperties (IsChecked = true, IsEnabled left at its default) must
        // still materialize, because the authored result ("EnabledOn") differs from the resting
        // wireframe ("EnabledOff"). A naive "skip unless every input is authored" gate would
        // wrongly drop this common case (a checkbox that starts checked).
        BehaviorSave behavior = BuildCheckBoxBehavior();

        ComponentSave component = new ComponentSave { Name = "Controls/CheckBox", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "CheckBoxBehavior" });

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);

        InstanceSave checkBoxInstance = new InstanceSave
        {
            Name = "CheckBoxInstance",
            BaseType = "Controls/CheckBox",
            ParentContainer = screen
        };
        screen.Instances.Add(checkBoxInstance);

        screenDefault.Variables.Add(new VariableSave
        {
            Type = "bool",
            Name = "CheckBoxInstance.IsChecked",
            Value = true,
            SetsValue = true
        });

        StandardElementSave containerStandard = new StandardElementSave { Name = "Container" };
        containerStandard.States.Add(new StateSave { Name = "Default", ParentContainer = containerStandard });

        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(containerStandard);
        project.Components.Add(component);
        project.Screens.Add(screen);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        BehaviorToolOnlyReferencesApplier.Apply(screen, screenDefault);

        screenDefault.GetValue("CheckBoxInstance.CheckBoxCategoryState").ShouldBe("EnabledOn");
    }

    [Fact]
    public void Apply_BehaviorWithoutToolOnlyReferences_DoesNothing()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "string", Name = "ToolTip" });

        ComponentSave component = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        Should.NotThrow(() => BehaviorToolOnlyReferencesApplier.Apply(component, defaultState));
        defaultState.Variables.ShouldBeEmpty();
    }

    [Fact]
    public void Apply_InstanceDriverReturnsToDefault_RemovesStaleMaterializedValue()
    {
        // Repro: a StackPanel instance had Orientation = Horizontal, which materialized
        // ChildrenLayout = LeftToRightStack. Setting Orientation back to Vertical (its default)
        // makes the reference resolve to the resting value (TopToBottomStack), so ChildrenLayout
        // belongs at its default. The stale LeftToRightStack must be removed - otherwise the
        // resting-skip (issue #3080) leaves it behind and the instance keeps rendering horizontally.
        BehaviorSave behavior = new BehaviorSave { Name = "StackPanelBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "Orientation", Name = "Orientation", Value = "Vertical" });
        behavior.ToolOnlyVariableReferences.Add(
            "ChildrenLayout = Orientation == \"Horizontal\" ? \"LeftToRightStack\" : \"TopToBottomStack\"");

        ComponentSave component = new ComponentSave { Name = "Controls/StackPanel", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "StackPanelBehavior" });

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);

        InstanceSave stackInstance = new InstanceSave
        {
            Name = "StackInstance",
            BaseType = "Controls/StackPanel",
            ParentContainer = screen
        };
        screen.Instances.Add(stackInstance);

        // Orientation is back at its default (Vertical); a stale ChildrenLayout from a prior
        // Orientation = Horizontal still sits on the instance.
        screenDefault.Variables.Add(new VariableSave
        {
            Type = "Orientation",
            Name = "StackInstance.Orientation",
            Value = "Vertical",
            SetsValue = true
        });
        screenDefault.Variables.Add(new VariableSave
        {
            Type = "ChildrenLayout",
            Name = "StackInstance.ChildrenLayout",
            Value = ChildrenLayout.LeftToRightStack,
            SetsValue = true
        });

        StandardElementSave containerStandard = new StandardElementSave { Name = "Container" };
        containerStandard.States.Add(new StateSave { Name = "Default", ParentContainer = containerStandard });

        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(containerStandard);
        project.Components.Add(component);
        project.Screens.Add(screen);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        BehaviorToolOnlyReferencesApplier.Apply(screen, screenDefault);

        screenDefault.Variables.ShouldNotContain(v => v.Name == "StackInstance.ChildrenLayout",
            "a value materialized while the driver was non-default must be removed when the driver returns to its default, so the variable resets to its default rather than rendering stale");
    }

    [Fact]
    public void Apply_ComponentOwnDefaultAtResting_KeepsAuthoredBaseline()
    {
        // The element's own default state holds the authored baseline - Controls/StackPanel sets
        // ChildrenLayout = TopToBottomStack so it stacks. The resting-skip cleanup must only reset
        // instance-level materialized values; it must NOT strip the component's own authored value
        // (instance == null), or the StackPanel loses its stacking layout entirely.
        BehaviorSave behavior = new BehaviorSave { Name = "StackPanelBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "Orientation", Name = "Orientation", Value = "Vertical" });
        behavior.ToolOnlyVariableReferences.Add(
            "ChildrenLayout = Orientation == \"Horizontal\" ? \"LeftToRightStack\" : \"TopToBottomStack\"");

        ComponentSave component = new ComponentSave { Name = "Controls/StackPanel", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave
        {
            Type = "ChildrenLayout",
            Name = "ChildrenLayout",
            Value = ChildrenLayout.TopToBottomStack,
            SetsValue = true
        });
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "StackPanelBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        BehaviorToolOnlyReferencesApplier.Apply(component, defaultState);

        defaultState.GetValue("ChildrenLayout").ShouldBe(ChildrenLayout.TopToBottomStack);
    }

    [Fact]
    public void GetUnderlyingMembersDrivenBy_ComponentLevelSpacing_ReturnsStackSpacing()
    {
        // Component-level (instance == null) resolution: the component itself carries the behavior.
        // Mirrors editing Spacing on the Controls/StackPanel component directly.
        BehaviorSave behavior = BuildStackPanelBehavior();

        ComponentSave component = new ComponentSave { Name = "Controls/StackPanel", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "StackPanelBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        IReadOnlyList<string> result = BehaviorToolOnlyReferencesApplier.GetUnderlyingMembersDrivenBy(component, instance: null, "Spacing");

        result.ShouldContain("StackSpacing");
    }

    [Fact]
    public void GetUnderlyingMembersDrivenBy_OrientationOnInstance_ReturnsChildrenLayoutOnly()
    {
        // Orientation drives ChildrenLayout (via the ternary reference) but not StackSpacing.
        (ScreenSave screen, InstanceSave stackInstance) = BuildScreenWithStackPanelInstance();

        IReadOnlyList<string> result = BehaviorToolOnlyReferencesApplier.GetUnderlyingMembersDrivenBy(screen, stackInstance, "Orientation");

        result.ShouldContain("ChildrenLayout");
        result.ShouldNotContain("StackSpacing");
    }

    [Fact]
    public void GetUnderlyingMembersDrivenBy_SpacingOnInstance_ReturnsStackSpacingOnly()
    {
        // Spacing drives StackSpacing (via "StackSpacing = Spacing") but not ChildrenLayout - so a
        // Spacing scrub must push only StackSpacing to the live element, not re-stack it (issue #3191).
        (ScreenSave screen, InstanceSave stackInstance) = BuildScreenWithStackPanelInstance();

        IReadOnlyList<string> result = BehaviorToolOnlyReferencesApplier.GetUnderlyingMembersDrivenBy(screen, stackInstance, "Spacing");

        result.ShouldContain("StackSpacing");
        result.ShouldNotContain("ChildrenLayout");
    }

    [Fact]
    public void GetUnderlyingMembersDrivenBy_UnrelatedMember_ReturnsEmpty()
    {
        // A normal variable (X) is referenced by no behavior tool-only reference, so it drives no
        // underlying member and the wireframe should fall back to its usual incremental handling.
        (ScreenSave screen, InstanceSave stackInstance) = BuildScreenWithStackPanelInstance();

        IReadOnlyList<string> result = BehaviorToolOnlyReferencesApplier.GetUnderlyingMembersDrivenBy(screen, stackInstance, "X");

        result.ShouldBeEmpty();
    }

    [Fact]
    public void Apply_TextBoxTextWrappingWrap_WritesMultiLineModeCategoryState()
    {
        // Models the TextBoxBehavior line-mode reference: authoring TextWrapping = Wrap must
        // drive the visual LineModeCategoryState to "Multi" at design time, mirroring the
        // runtime's UpdateStateForSingleOrMultiLine. Without it the wireframe never previews
        // multi-line and (in older projects) the caret stays single-line.
        BehaviorSave behavior = BuildTextBoxLineModeBehavior();

        ComponentSave component = new ComponentSave { Name = "Controls/TextBox", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave
        {
            Type = "TextWrapping",
            Name = "TextWrapping",
            Value = "Wrap",
            SetsValue = true
        });
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "TextBoxBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        BehaviorToolOnlyReferencesApplier.Apply(component, defaultState);

        defaultState.GetValue("LineModeCategoryState").ShouldBe("Multi");
    }

    [Fact]
    public void Apply_TextBoxAcceptsReturnTrue_WritesMultiLineModeCategoryState()
    {
        // AcceptsReturn = true with TextWrapping left at its NoWrap default still produces a
        // vertically-growing text box, so the reference collapses to "Multi" (the TextBox
        // component has no MultiNoWrap state, matching the runtime fallback).
        BehaviorSave behavior = BuildTextBoxLineModeBehavior();

        ComponentSave component = new ComponentSave { Name = "Controls/TextBox", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave
        {
            Type = "bool",
            Name = "AcceptsReturn",
            Value = true,
            SetsValue = true
        });
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "TextBoxBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        BehaviorToolOnlyReferencesApplier.Apply(component, defaultState);

        defaultState.GetValue("LineModeCategoryState").ShouldBe("Multi");
    }

    [Fact]
    public void Apply_TextBoxLineModeAtDefaults_DoesNotMaterializeLineModeCategoryState()
    {
        // NoWrap + AcceptsReturn=false resolve to "Single", which equals the resting wireframe;
        // per #3080 the resting value must not be materialized into the state.
        BehaviorSave behavior = BuildTextBoxLineModeBehavior();

        ComponentSave component = new ComponentSave { Name = "Controls/TextBox", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "TextBoxBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        BehaviorToolOnlyReferencesApplier.Apply(component, defaultState);

        defaultState.Variables.ShouldNotContain(v => v.Name == "LineModeCategoryState",
            "the default line mode resolves to the resting \"Single\" value and must not be materialized");
    }

    private static BehaviorSave BuildTextBoxLineModeBehavior()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "TextBoxBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "TextWrapping", Name = "TextWrapping", Value = "NoWrap" });
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "AcceptsReturn", Value = false });
        behavior.ToolOnlyVariableReferences.Add(
            "LineModeCategoryState = TextWrapping == \"Wrap\" ? \"Multi\" : (AcceptsReturn ? \"Multi\" : \"Single\")");
        return behavior;
    }

    private static BehaviorSave BuildStackPanelBehavior()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "StackPanelBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "Orientation", Name = "Orientation", Value = "Vertical" });
        behavior.FormsProperties.Add(new VariableSave { Type = "float", Name = "Spacing", Value = 0f });
        behavior.ToolOnlyVariableReferences.Add(
            "ChildrenLayout = Orientation == \"Horizontal\" ? \"LeftToRightStack\" : \"TopToBottomStack\"");
        behavior.ToolOnlyVariableReferences.Add("StackSpacing = Spacing");
        return behavior;
    }

    private static (ScreenSave screen, InstanceSave stackInstance) BuildScreenWithStackPanelInstance()
    {
        BehaviorSave behavior = BuildStackPanelBehavior();

        ComponentSave component = new ComponentSave { Name = "Controls/StackPanel", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "StackPanelBehavior" });

        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(screenDefault);

        InstanceSave stackInstance = new InstanceSave
        {
            Name = "StackInstance",
            BaseType = "Controls/StackPanel",
            ParentContainer = screen
        };
        screen.Instances.Add(stackInstance);

        StandardElementSave containerStandard = new StandardElementSave { Name = "Container" };
        containerStandard.States.Add(new StateSave { Name = "Default", ParentContainer = containerStandard });

        GumProjectSave project = new GumProjectSave();
        project.StandardElements.Add(containerStandard);
        project.Components.Add(component);
        project.Screens.Add(screen);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        return (screen, stackInstance);
    }

    private static BehaviorSave BuildCheckBoxBehavior()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "CheckBoxBehavior" };
        behavior.FormsProperties.Add(new VariableSave
        {
            Type = "bool",
            Name = "IsEnabled",
            Value = true
        });
        behavior.FormsProperties.Add(new VariableSave
        {
            Type = "bool",
            Name = "IsChecked",
            Value = false
        });
        behavior.ToolOnlyVariableReferences.Add(
            "CheckBoxCategoryState = IsEnabled ? (IsChecked ? \"EnabledOn\" : \"EnabledOff\") : (IsChecked ? \"DisabledOn\" : \"DisabledOff\")");
        return behavior;
    }
}
