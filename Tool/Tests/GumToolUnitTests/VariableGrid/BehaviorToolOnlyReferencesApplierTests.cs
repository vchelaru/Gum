using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using GumRuntime;
using Shouldly;

namespace GumToolUnitTests.VariableGrid;

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
    public void Apply_StateMissingIdentifier_FallsBackToBehaviorFormsPropertyDefault()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave
        {
            Type = "bool",
            Name = "IsEnabled",
            Value = false  // declared default, drives the wireframe to "Disabled" with no instance authoring
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

        defaultState.GetValue("ButtonCategoryState").ShouldBe("Disabled");
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
    public void Apply_CheckBoxStateEmpty_FallsBackToBehaviorIsCheckedFalseAndIsEnabledTrue()
    {
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

        defaultState.GetValue("CheckBoxCategoryState").ShouldBe("EnabledOff");
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
            Value = "off"
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
        behavior.FormsProperties.Add(new VariableSave { Type = "string", Name = "A", Value = "a0" });
        behavior.FormsProperties.Add(new VariableSave { Type = "string", Name = "B", Value = "b0" });
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
