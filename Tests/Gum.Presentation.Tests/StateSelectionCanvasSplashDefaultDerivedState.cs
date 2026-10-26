using Gum.DataTypes;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Shouldly;
using static Gum.Presentation.Tests.StateSelectionCanvasHarness;

namespace Gum.Presentation.Tests;

public partial class StateSelectionCanvasSplashTests
{
    /// <summary>Discriminator for the authored-false default case: no default color row, so nothing re-applies Enabled.</summary>
    [Fact]
    public void RefreshAll_DefaultSelectedOnButtonWithAuthoredIsEnabledFalseAndNoDefaultReference_ShowsDisabled()
    {
        ComponentSave button = BuildFullButton(defaultReference: false, materialized: true);
        button.DefaultState!.SetValue("IsEnabled", false, "bool");
        BehaviorToolOnlyReferencesApplier.Apply(button, button.DefaultState);

        ShowSelectedAndReadBackground(button, button.DefaultState).ShouldBe(ButtonColors["Disabled"]);
    }

    /// <summary>With authored IsEnabled=false and a default color reference, the default selection shows Disabled.</summary>
    [Fact]
    public void RefreshAll_DefaultSelectedOnButtonWithAuthoredIsEnabledFalse_ShowsDisabled()
    {
        ComponentSave button = BuildFullButton(defaultReference: true, materialized: true);
        button.DefaultState!.SetValue("IsEnabled", false, "bool");
        BehaviorToolOnlyReferencesApplier.Apply(button, button.DefaultState);

        ShowSelectedAndReadBackground(button, button.DefaultState).ShouldBe(ButtonColors["Disabled"]);
    }
}
