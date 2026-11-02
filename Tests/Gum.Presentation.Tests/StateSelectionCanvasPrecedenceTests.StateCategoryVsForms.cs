using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using Shouldly;
using static Gum.Presentation.Tests.StateSelectionCanvasHarness;

namespace Gum.Presentation.Tests;

public partial class StateSelectionCanvasPrecedenceTests
{
    /// <summary>A screen state that sets the instance's category state while IsEnabled is false: the Forms variable wins.</summary>
    [Fact]
    public void RefreshAll_ScreenStateSetsInstanceCategoryStateEnabledWhileIsEnabledFalse_FormsVariableWins()
    {
        ScreenSave screen = BuildScreenWithButtonInstance(isEnabled: false, authoredCategoryState: null);
        StateSave forcedEnabled = AddCategoryState(screen, "ModeCategory", "Forced",
            s => s.Variables.Add(new VariableSave { Name = "Play.ButtonCategoryState", Value = "Enabled", Type = "ButtonCategory", SetsValue = true }));

        ButtonInstanceColor(Show(screen, forcedEnabled)).ShouldBe(ButtonColors["Disabled"]);
    }

    /// <summary>A state clicked on the Toolbar does not change its Button instance's Forms-derived look.</summary>
    [Theory]
    [InlineData(false, "Disabled")]
    [InlineData(true, "Enabled")]
    public void RefreshAll_ToolbarStateSelectedWithButtonInstanceIsEnabled_InstanceKeepsFormsDerivedLook(bool isEnabled, string expectedLook)
    {
        ComponentSave toolbar = BuildToolbarWithButtonInstance(isEnabled);
        StateSave wide = AddCategoryState(toolbar, "SizeCategory", "Wide", s => SetNumber(s, "Play.Width", 300f));

        GraphicalUiElement root = Show(toolbar, wide);

        ButtonInstanceColor(root).ShouldBe(ButtonColors[expectedLook]);
        FindByName(root, "Play").Width.ShouldBe(300f);
    }

    [Fact]
    public void RefreshAll_ToolbarStateSetsInstanceCategoryStateEnabledWhileIsEnabledFalse_FormsVariableWins()
    {
        ComponentSave toolbar = BuildToolbarWithButtonInstance(isEnabled: false);
        StateSave forcedEnabled = AddCategoryState(toolbar, "ModeCategory", "Forced",
            s => s.Variables.Add(new VariableSave { Name = "Play.ButtonCategoryState", Value = "Enabled", Type = "ButtonCategory", SetsValue = true }));

        ButtonInstanceColor(Show(toolbar, forcedEnabled)).ShouldBe(ButtonColors["Disabled"]);
    }
}
