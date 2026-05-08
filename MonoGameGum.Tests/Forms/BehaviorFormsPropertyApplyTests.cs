using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

public class BehaviorFormsPropertyApplyTests : BaseTestClass
{
    public BehaviorFormsPropertyApplyTests()
    {
        ObjectFinder.Self.GumProjectSave = null;
    }

    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    [Fact]
    public void Apply_VisualWithBehaviorFormsPropertyDefault_AppliesValueToFormsControl()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "string", Name = "ToolTip" });

        ComponentSave component = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        defaultState.Variables.Add(new VariableSave
        {
            Type = "string",
            Name = "ToolTip",
            Value = "Click me",
            SetsValue = true
        });
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        InteractiveGue visual = new InteractiveGue();
        visual.ElementSave = component;

        Button button = new Button(visual);
        BehaviorFormsPropertyApplier.Apply(button, visual);

        button.ToolTip.ShouldBe("Click me");
    }

    [Fact]
    public void Apply_NeitherComponentNorParentDefinesValue_FallsBackToBehaviorFormsPropertyValue()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave
        {
            Type = "string",
            Name = "ToolTip",
            Value = "behavior default tooltip"
        });

        ComponentSave component = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        InteractiveGue visual = new InteractiveGue();
        visual.ElementSave = component;

        Button button = new Button(visual);
        BehaviorFormsPropertyApplier.Apply(button, visual);

        button.ToolTip.ShouldBe("behavior default tooltip");
    }

    [Fact]
    public void Apply_CheckBoxIsCheckedTrueFromState_CoercesBoolToNullableBoolOnCheckBox()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "CheckBoxBehavior" };
        behavior.FormsProperties.Add(new VariableSave
        {
            Type = "bool",
            Name = "IsChecked",
            Value = false
        });

        ComponentSave component = new ComponentSave { Name = "Controls/CheckBox", BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
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

        InteractiveGue visual = new InteractiveGue();
        visual.ElementSave = component;

        CheckBox checkBox = new CheckBox(visual);
        BehaviorFormsPropertyApplier.Apply(checkBox, visual);

        checkBox.IsChecked.ShouldBe(true);
    }

    [Fact]
    public void Apply_CheckBoxIsCheckedUnauthored_FallsBackToBehaviorDefaultFalse()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "CheckBoxBehavior" };
        behavior.FormsProperties.Add(new VariableSave
        {
            Type = "bool",
            Name = "IsChecked",
            Value = false
        });

        ComponentSave component = new ComponentSave { Name = "Controls/CheckBox", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "CheckBoxBehavior" });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        InteractiveGue visual = new InteractiveGue();
        visual.ElementSave = component;

        CheckBox checkBox = new CheckBox(visual);
        // Force a non-default starting value to verify Apply writes the declared default.
        checkBox.IsChecked = true;
        BehaviorFormsPropertyApplier.Apply(checkBox, visual);

        checkBox.IsChecked.ShouldBe(false);
    }

    [Fact]
    public void Apply_TextBoxAcceptsReturnTrue_AppliesToFormsControl()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "TextBoxBehavior" };
        behavior.FormsProperties.Add(new VariableSave
        {
            Type = "bool",
            Name = "AcceptsReturn",
            Value = false
        });

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

        // TextBox's parameterless ctor builds a default visual tree (TextInstance/CaretInstance
        // children) which TextBoxBase.ReactToVisualChanged requires. Attach the project component
        // to that visual so the applier can resolve the FormsProperty against state.
        TextBox textBox = new TextBox();
        textBox.Visual.ElementSave = component;
        BehaviorFormsPropertyApplier.Apply(textBox, textBox.Visual);

        textBox.AcceptsReturn.ShouldBe(true);
    }

    [Fact]
    public void Apply_ParentScreenInstanceOverride_OverridesComponentDefault()
    {
        BehaviorSave behavior = new BehaviorSave { Name = "ButtonBehavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "string", Name = "ToolTip" });

        // Component default ToolTip = "Default"
        ComponentSave component = new ComponentSave { Name = "Controls/ButtonStandard", BaseType = "Container" };
        StateSave componentDefault = new StateSave { Name = "Default", ParentContainer = component };
        componentDefault.Variables.Add(new VariableSave
        {
            Type = "string",
            Name = "ToolTip",
            Value = "Default",
            SetsValue = true
        });
        component.States.Add(componentDefault);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = "ButtonBehavior" });

        // Screen with instance override "ButtonInstance.ToolTip" = "Overridden"
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        StateSave screenDefault = new StateSave { Name = "Default", ParentContainer = screen };
        screenDefault.Variables.Add(new VariableSave
        {
            Type = "string",
            Name = "ButtonInstance.ToolTip",
            Value = "Overridden",
            SetsValue = true
        });
        screen.States.Add(screenDefault);

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Screens.Add(screen);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        // Live visual tree: screen GUE (ElementSave = screen), button GUE child whose
        // ElementSave = component, Name = "ButtonInstance", and ElementGueContainingThis
        // points to the screen GUE — same shape produced by ToGraphicalUiElement.
        InteractiveGue screenVisual = new InteractiveGue();
        screenVisual.ElementSave = screen;

        InteractiveGue buttonVisual = new InteractiveGue { Name = "ButtonInstance" };
        buttonVisual.ElementSave = component;
        buttonVisual.ElementGueContainingThis = screenVisual;

        Button button = new Button(buttonVisual);
        BehaviorFormsPropertyApplier.Apply(button, buttonVisual);

        button.ToolTip.ShouldBe("Overridden");
    }
}
