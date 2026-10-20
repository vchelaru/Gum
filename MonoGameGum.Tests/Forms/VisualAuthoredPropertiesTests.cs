using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System.Collections.Generic;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Pins that values authored on a Visual (the way the Gum tool saves them) are read by the Forms
/// control on Visual assignment, survive RefreshStyles, and are not overridden by a Visual swap.
/// Each test passes on the commit before the 2026-10 Visual-assignment / RefreshStyles fixes
/// (#6200, #6203, #6204, #6214, #6221), except four that pin those fixes: the ScrollBar
/// IsEnabled-forwarding test, the TextBox swap line-mode test, and the two "set in code" text
/// preservation tests.
/// </summary>
public class VisualAuthoredPropertiesTests : BaseTestClass
{
    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    #region IsEnabled

    [Fact]
    public void ButtonAssign_VisualAuthoredDisabled_VisualStaysDisabled()
    {
        ButtonVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        visual.IsEnabled = false;

        Button button = new(visual);

        visual.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void ScrollBarAssign_AuthoredDisabledUpDownAndThumb_StayDisabled()
    {
        ScrollBarVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        visual.UpButtonInstance!.IsEnabled = false;
        visual.DownButtonInstance!.IsEnabled = false;
        visual.ThumbInstance.IsEnabled = false;

        ScrollBar scrollBar = new(visual);

        scrollBar.UpButton!.Visual.IsEnabled.ShouldBeFalse();
        scrollBar.DownButton!.Visual.IsEnabled.ShouldBeFalse();
        visual.ThumbInstance.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void ScrollBarAssign_AuthoredDisabledButtonsWithFormsControls_StayDisabled()
    {
        // The tool-authored path creates the Forms Buttons (and applies their IsEnabled) before the ScrollBar.
        ScrollBarVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        Button upButton = (Button)visual.UpButtonInstance!.FormsControlAsObject!;
        Button downButton = (Button)visual.DownButtonInstance!.FormsControlAsObject!;
        Button thumb = (Button)visual.ThumbInstance.FormsControlAsObject!;
        upButton.IsEnabled = false;
        downButton.IsEnabled = false;
        thumb.IsEnabled = false;

        ScrollBar scrollBar = new(visual);

        scrollBar.IsEnabled.ShouldBeTrue();
        upButton.IsEnabled.ShouldBeFalse();
        downButton.IsEnabled.ShouldBeFalse();
        thumb.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void ScrollBarSwap_NewVisualWithAuthoredDisabledButtons_StayDisabled()
    {
        ScrollBar scrollBar = new();
        ScrollBarVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        Button upButton = (Button)newVisual.UpButtonInstance!.FormsControlAsObject!;
        Button thumb = (Button)newVisual.ThumbInstance.FormsControlAsObject!;
        upButton.IsEnabled = false;
        thumb.IsEnabled = false;

        scrollBar.Visual = newVisual;

        upButton.IsEnabled.ShouldBeFalse();
        thumb.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void ScrollBarFormsPropertyIsEnabledFalse_FromFileComponent_DisablesControl()
    {
        ScrollBarVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        ScrollBar scrollBar = new(visual);
        AuthorFormsProperties(visual, "Controls/ScrollBar", StandardFormsBehaviorNames.ScrollBarBehaviorName,
            new List<(string, string, object)> { ("IsEnabled", "bool", false) });

        BehaviorFormsPropertyApplier.Apply(scrollBar, visual);

        scrollBar.IsEnabled.ShouldBeFalse();
        scrollBar.UpButton!.Visual.IsEnabled.ShouldBeFalse();
    }

    #endregion

    #region TextBox line mode and placeholder

    [Fact]
    public void TextBoxAssign_VisualAuthoredMultiLineMode_StaysMulti()
    {
        TextBoxVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        visual.SetProperty("LineModeCategoryState", "Multi");

        TextBox textBox = new(visual);

        // The Multi state top-aligns the text; the Single state would put it back to center.
        visual.TextInstance.VerticalAlignment.ShouldBe(VerticalAlignment.Top);
        textBox.TextWrapping.ShouldBe(TextWrapping.NoWrap);
        textBox.AcceptsReturn.ShouldBeFalse();
    }

    [Fact]
    public void TextBoxSwap_NewVisualAuthoredMultiLineMode_DefaultControlKeepsMulti()
    {
        TextBox textBox = new();
        TextBoxVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        newVisual.SetProperty("LineModeCategoryState", "Multi");

        textBox.Visual = newVisual;

        newVisual.TextInstance.VerticalAlignment.ShouldBe(VerticalAlignment.Top);
    }

    [Fact]
    public void TextBoxSwap_ControlWithAcceptsReturn_NewVisualGetsMultiLineMode()
    {
        TextBox textBox = new();
        textBox.AcceptsReturn = true;
        TextBoxVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        newVisual.SetProperty("LineModeCategoryState", "Single");

        textBox.Visual = newVisual;

        newVisual.TextInstance.VerticalAlignment.ShouldBe(VerticalAlignment.Top);
        textBox.AcceptsReturn.ShouldBeTrue();
    }

    [Fact]
    public void TextBoxAssign_AuthoredTextAndPlaceholderText_AreReadByControl()
    {
        TextBoxVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        visual.TextInstance.Text = "Authored text";
        visual.PlaceholderTextInstance.Text = "Authored placeholder";

        TextBox textBox = new(visual);

        textBox.Text.ShouldBe("Authored text");
        textBox.Placeholder.ShouldBe("Authored placeholder");
    }

    [Fact]
    public void TextBoxSwap_NewVisualAuthoredTextAndPlaceholderText_AreNotOverwrittenByControl()
    {
        TextBox textBox = new();
        TextBoxVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        newVisual.TextInstance.Text = "Authored text";
        newVisual.PlaceholderTextInstance.Text = "Authored placeholder";

        textBox.Visual = newVisual;

        newVisual.TextInstance.Text.ShouldBe("Authored text");
        newVisual.PlaceholderTextInstance.Text.ShouldBe("Authored placeholder");
    }

    [Fact]
    public void TextBoxFormsProperties_AuthoredPlaceholderIsEnabledAndLineMode_AppliedAndSurviveRefreshStyles()
    {
        TextBoxVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        TextBox textBox = new(visual);
        AuthorFormsProperties(visual, "Controls/TextBox", StandardFormsBehaviorNames.TextBoxBehaviorName,
            new List<(string, string, object)>
            {
                ("Placeholder", "string", "Authored hint"),
                ("AcceptsReturn", "bool", true),
                ("IsEnabled", "bool", false)
            });

        BehaviorFormsPropertyApplier.Apply(textBox, visual);

        textBox.Placeholder.ShouldBe("Authored hint");
        textBox.AcceptsReturn.ShouldBeTrue();
        textBox.IsEnabled.ShouldBeFalse();

        visual.RefreshStyles();

        textBox.Placeholder.ShouldBe("Authored hint");
        textBox.AcceptsReturn.ShouldBeTrue();
        textBox.IsEnabled.ShouldBeFalse();
    }

    [Fact]
    public void TextBoxRefreshStyles_AuthoredTextAndPlaceholder_Unchanged()
    {
        TextBoxVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        visual.TextInstance.Text = "Authored text";
        visual.PlaceholderTextInstance.Text = "Authored placeholder";
        TextBox textBox = new(visual);

        visual.RefreshStyles();

        textBox.Text.ShouldBe("Authored text");
        textBox.Placeholder.ShouldBe("Authored placeholder");
    }

    #endregion

    #region Text preserved across RefreshStyles is the value on the Visual

    [Fact]
    public void ToggleButtonRefreshStyles_AuthoredTextNotSetInCode_KeepsAuthoredText()
    {
        ToggleButtonVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        visual.TextInstance.Text = "Authored toggle";
        ToggleButton toggleButton = new(visual);

        visual.RefreshStyles();

        toggleButton.Text.ShouldBe("Authored toggle");
    }

    [Fact]
    public void MenuItemRefreshStyles_AuthoredHeaderNotSetInCode_KeepsAuthoredHeader()
    {
        MenuItemVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        visual.TextInstance.Text = "Authored header";
        MenuItem menuItem = new(visual);

        visual.RefreshStyles();

        menuItem.Header.ShouldBe("Authored header");
    }

    [Fact]
    public void ExpanderRefreshStyles_AuthoredHeaderNotSetInCode_KeepsAuthoredHeader()
    {
        InteractiveGue visual = new(new InvisibleRenderable());
        TextRuntime textInstance = new() { Name = "TextInstance" };
        textInstance.Text = "Authored header";
        visual.Children!.Add(textInstance);
        Expander expander = new(visual);

        visual.RefreshStyles();

        expander.Header.ShouldBe("Authored header");
    }

    [Fact]
    public void ToggleButtonRefreshStyles_AuthoredTextChanged_ShowsNewAuthoredText()
    {
        ToggleButtonVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        visual.TextInstance.Text = "Old authored";
        ToggleButton toggleButton = new(visual);
        ContainerRuntime screen = AttachToScreenAuthoring(visual, "TextInstance.Text", "New authored");

        screen.RefreshStyles();

        toggleButton.Text.ShouldBe("New authored");
    }

    [Fact]
    public void ToggleButtonRefreshStyles_TextSetInCodeAndAuthoredChanged_KeepsCodeText()
    {
        ToggleButtonVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        ToggleButton toggleButton = new(visual);
        toggleButton.Text = "From code";
        ContainerRuntime screen = AttachToScreenAuthoring(visual, "TextInstance.Text", "New authored");

        screen.RefreshStyles();

        toggleButton.Text.ShouldBe("From code");
    }

    [Fact]
    public void MenuItemRefreshStyles_AuthoredHeaderChanged_ShowsNewAuthoredHeader()
    {
        MenuItemVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        visual.TextInstance.Text = "Old authored";
        MenuItem menuItem = new(visual);
        ContainerRuntime screen = AttachToScreenAuthoring(visual, "TextInstance.Text", "New authored");

        screen.RefreshStyles();

        menuItem.Header.ShouldBe("New authored");
    }

    [Fact]
    public void ExpanderRefreshStyles_AuthoredHeaderChanged_ShowsNewAuthoredHeader()
    {
        InteractiveGue visual = new(new InvisibleRenderable());
        TextRuntime textInstance = new() { Name = "TextInstance" };
        textInstance.Text = "Old authored";
        visual.Children!.Add(textInstance);
        Expander expander = new(visual);
        ContainerRuntime screen = AttachToScreenAuthoring(visual, "TextInstance.Text", "New authored");

        screen.RefreshStyles();

        expander.Header.ShouldBe("New authored");
    }

    [Fact]
    public void TextBoxRefreshStyles_AuthoredPlaceholderChanged_ShowsNewAuthoredPlaceholder()
    {
        TextBoxVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        visual.PlaceholderTextInstance.Text = "Old authored";
        TextBox textBox = new(visual);
        ContainerRuntime screen = AttachToScreenAuthoring(visual, "PlaceholderTextInstance.Text", "New authored");

        screen.RefreshStyles();

        textBox.Placeholder.ShouldBe("New authored");
    }

    [Fact]
    public void TextBoxRefreshStyles_PlaceholderSetInCodeAndAuthoredChanged_KeepsCodePlaceholder()
    {
        TextBoxVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        TextBox textBox = new(visual);
        textBox.Placeholder = "From code";
        ContainerRuntime screen = AttachToScreenAuthoring(visual, "PlaceholderTextInstance.Text", "New authored");

        screen.RefreshStyles();

        textBox.Placeholder.ShouldBe("From code");
    }

    [Fact]
    public void ButtonAssign_AuthoredTextOnVisual_IsReadByControl()
    {
        ButtonVisual visual = new(fullInstantiation: true, tryCreateFormsObject: false);
        visual.TextInstance.Text = "Authored button";

        Button button = new(visual);

        button.Text.ShouldBe("Authored button");
    }

    [Fact]
    public void ToggleButtonSwap_NewVisualAuthoredText_IsNotOverwritten()
    {
        ToggleButton toggleButton = new();
        toggleButton.Text = "Old";
        ToggleButtonVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        newVisual.TextInstance.Text = "Authored on new visual";

        toggleButton.Visual = newVisual;

        toggleButton.Text.ShouldBe("Authored on new visual");
    }

    [Fact]
    public void MenuItemSwap_NewVisualAuthoredHeader_IsNotOverwritten()
    {
        MenuItem menuItem = new();
        menuItem.Header = "Old";
        MenuItemVisual newVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        newVisual.TextInstance.Text = "Authored on new visual";

        menuItem.Visual = newVisual;

        menuItem.Header.ShouldBe("Authored on new visual");
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Parents the visual to a screen and gives it a component whose Default state authors the
    /// given variable, like a Gum project does. Refreshing the returned screen re-applies it.
    /// </summary>
    private static ContainerRuntime AttachToScreenAuthoring(
        InteractiveGue visual, string variableName, string authoredValue)
    {
        ContainerRuntime screenVisual = new();
        visual.Name = "ControlInstance";
        visual.Parent = screenVisual;

        ComponentSave component = new() { Name = "Controls/Control" };
        StateSave componentDefault = new() { Name = "Default", ParentContainer = component };
        componentDefault.Variables.Add(new VariableSave
        {
            Name = variableName,
            Value = authoredValue,
            SetsValue = true
        });
        component.States.Add(componentDefault);
        visual.AddStates(new List<StateSave> { componentDefault });
        visual.ElementSave = component;

        ScreenSave screen = new() { Name = "TestScreen" };
        StateSave screenDefault = new() { Name = "Default" };
        screen.States.Add(screenDefault);
        screenVisual.AddStates(new List<StateSave> { screenDefault });
        screenVisual.ElementSave = screen;

        return screenVisual;
    }

    /// <summary>
    /// Gives the visual a component whose default state authors the given Forms properties and
    /// registers a behavior declaring them, the way a Gum project does.
    /// </summary>
    private static void AuthorFormsProperties(
        InteractiveGue visual,
        string componentName,
        string behaviorName,
        List<(string Name, string Type, object Value)> properties)
    {
        BehaviorSave behavior = new BehaviorSave { Name = behaviorName };
        ComponentSave component = new ComponentSave { Name = componentName, BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        foreach ((string name, string type, object value) in properties)
        {
            behavior.FormsProperties.Add(new VariableSave { Type = type, Name = name });
            defaultState.Variables.Add(new VariableSave
            {
                Type = type,
                Name = name,
                Value = value,
                SetsValue = true
            });
        }
        component.States.Add(defaultState);
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behaviorName });

        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        project.Behaviors.Add(behavior);
        ObjectFinder.Self.GumProjectSave = project;

        visual.AddStates(new List<StateSave> { defaultState });
        visual.ElementSave = component;
    }

    #endregion
}
