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
