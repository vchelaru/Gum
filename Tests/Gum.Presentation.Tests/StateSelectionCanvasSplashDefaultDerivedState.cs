using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.Managers;
using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Wireframe;
using Shouldly;
using static Gum.Presentation.Tests.StateSelectionCanvasHarness;

namespace Gum.Presentation.Tests;

/// <summary>
/// A component viewed itself with Default selected shows what a running game shows: the Default values
/// with the Enabled or Disabled category state (chosen by IsEnabled, true when unset) on top (#6857).
/// </summary>
public partial class StateSelectionCanvasSplashTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RefreshAll_DefaultSelectedOnButtonWithAuthoredIsEnabledFalse_ShowsDisabled(bool defaultReference)
    {
        ComponentSave button = BuildFullButton(defaultReference, materialized: true);
        button.DefaultState!.SetValue("IsEnabled", false, "bool");
        BehaviorToolOnlyReferencesApplier.Apply(button, button.DefaultState);

        ShowSelectedAndReadBackground(button, button.DefaultState).ShouldBe(ButtonColors["Disabled"]);
    }

    /// <summary>The Enabled state sets the color, so a color typed on Default does not show, as in a running game.</summary>
    [Fact]
    public void RefreshAll_DefaultSelectedAndDefaultColorEditedWhileEnabled_ShowsTheEnabledStateColor()
    {
        ComponentSave button = BuildFullButton(defaultReference: false, materialized: true);
        SetFill(button.DefaultState!, "Background", (222, 11, 33), prefix: "");

        ShowSelectedAndReadBackground(button, button.DefaultState!).ShouldBe(ButtonColors["Enabled"]);
    }

    /// <summary>The Disabled state does not set Width, so the Default value shows.</summary>
    [Fact]
    public void RefreshAll_DefaultSelectedAndDisabledWhileDefaultSetsWidth_ShowsDefaultWidth()
    {
        ComponentSave button = BuildFullButton(defaultReference: false, materialized: true);
        button.DefaultState!.SetValue("IsEnabled", false, "bool");
        SetNumber(button.DefaultState, "Background.Width", 123f);
        BehaviorToolOnlyReferencesApplier.Apply(button, button.DefaultState);

        ShowSelectedAndReadBackgroundWidth(button, button.DefaultState).ShouldBe(123f);
    }

    /// <summary>A clicked non-default state still wins over what IsEnabled derives.</summary>
    [Fact]
    public void RefreshAll_HighlightedSelectedWhileIsEnabledFalse_ShowsHighlighted()
    {
        ComponentSave button = BuildFullButton(defaultReference: true, materialized: true);
        button.DefaultState!.SetValue("IsEnabled", false, "bool");
        BehaviorToolOnlyReferencesApplier.Apply(button, button.DefaultState);
        StateSave highlighted = button.Categories[0].States.Single(item => item.Name == "Highlighted");

        ShowSelectedAndReadBackground(button, highlighted).ShouldBe(ButtonColors["Highlighted"]);
    }

    /// <summary>A value the Forms control assigns (here a proxy feeding Background.Width) belongs to Default and always shows.</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void RefreshAll_DefaultSelectedAndCategoryStateSetsAFormsAssignedValue_ShowsTheFormsValue(bool isEnabled)
    {
        ComponentSave button = BuildFullButton(defaultReference: false, materialized: true);
        BehaviorSave behavior = ObjectFinder.Self.GumProjectSave!.Behaviors.Single();
        behavior.FormsProperties.Add(new VariableSave { Type = "float", Name = "BackgroundWidth", Value = 10f });
        behavior.ToolOnlyVariableReferences.Add("Background.Width = BackgroundWidth");
        button.DefaultState!.SetValue("BackgroundWidth", 77f, "float");
        button.DefaultState.SetValue("IsEnabled", isEnabled, "bool");
        string active = isEnabled ? "Enabled" : "Disabled";
        button.Categories[0].States.Single(item => item.Name == active).SetValue("Background.Width", 5f, "float");
        BehaviorToolOnlyReferencesApplier.Apply(button, button.DefaultState);

        ShowSelectedAndReadBackgroundWidth(button, button.DefaultState).ShouldBe(77f);
    }
}
