using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using static Gum.Presentation.Tests.StateSelectionCanvasHarness;

namespace Gum.Presentation.Tests;

/// <summary>
/// Precedence when a Button is drawn as an instance. Viewing the Button component itself, the clicked
/// state wins over what the Forms variable derives. Viewing an instance of it (inside a screen or another
/// component), the instance's Forms variables (IsEnabled) win over visual state values. The tool
/// materializes a Forms variable into the instance's category state with
/// <see cref="BehaviorToolOnlyReferencesApplier"/> after every edit, so these tests run it the same way.
/// </summary>
public partial class StateSelectionCanvasSplashTests
{
    #region Instance of a Button inside a screen

    [Theory]
    [InlineData(false, "Disabled")]
    [InlineData(true, "Enabled")]
    public void RefreshAll_ScreenWithButtonInstanceAuthoredIsEnabled_ShowsTheFormsDerivedLook(bool isEnabled, string expectedLook)
    {
        ScreenSave screen = BuildScreenWithButtonInstance(isEnabled, authoredCategoryState: null);

        ButtonInstanceColor(Show(screen, screen.DefaultState!)).ShouldBe(ButtonColors[expectedLook]);
    }

    [Theory]
    [InlineData("Disabled", true, "Enabled")]
    [InlineData("Enabled", false, "Disabled")]
    [InlineData("Disabled", false, "Disabled")]
    [InlineData("Enabled", true, "Enabled")]
    public void RefreshAll_ScreenWithButtonInstanceCategoryStateConflictsWithIsEnabled_FormsVariableWins(string authoredCategoryState, bool isEnabled, string expectedLook)
    {
        ScreenSave screen = BuildScreenWithButtonInstance(isEnabled, authoredCategoryState);

        ButtonInstanceColor(Show(screen, screen.DefaultState!)).ShouldBe(ButtonColors[expectedLook]);
    }

    /// <summary>A screen state that does not touch the button leaves the instance's Forms-derived look.</summary>
    [Theory]
    [InlineData(false, "Disabled")]
    [InlineData(true, "Enabled")]
    public void RefreshAll_ScreenStateSelectedWhileInstanceHasAuthoredIsEnabled_InstanceKeepsFormsDerivedLook(bool isEnabled, string expectedLook)
    {
        ScreenSave screen = BuildScreenWithButtonInstance(isEnabled, authoredCategoryState: null);
        StateSave moved = AddCategoryState(screen, "ModeCategory", "Moved", s => SetNumber(s, "Play.X", 40f));

        GraphicalUiElement root = Show(screen, moved);

        ButtonInstanceColor(root).ShouldBe(ButtonColors[expectedLook]);
        FindByName(root, "Play").X.ShouldBe(40f);
    }

    /// <summary>A screen state that sets the instance's category state while IsEnabled is false: the Forms variable wins.</summary>
    [Fact]
    public void RefreshAll_ScreenStateSetsInstanceCategoryStateEnabledWhileIsEnabledFalse_FormsVariableWins()
    {
        ScreenSave screen = BuildScreenWithButtonInstance(isEnabled: false, authoredCategoryState: null);
        StateSave forcedEnabled = AddCategoryState(screen, "ModeCategory", "Forced",
            s => s.Variables.Add(new VariableSave { Name = "Play.ButtonCategoryState", Value = "Enabled", Type = "ButtonCategory", SetsValue = true }));

        ButtonInstanceColor(Show(screen, forcedEnabled)).ShouldBe(ButtonColors["Disabled"]);
    }

    #endregion

    #region Instance of a Button inside a component viewed through that component

    [Theory]
    [InlineData(false, "Disabled")]
    [InlineData(true, "Enabled")]
    public void RefreshAll_ToolbarDefaultSelectedWithButtonInstanceIsEnabled_InstanceShowsFormsDerivedLook(bool isEnabled, string expectedLook)
    {
        ComponentSave toolbar = BuildToolbarWithButtonInstance(isEnabled);

        ButtonInstanceColor(Show(toolbar, toolbar.DefaultState!)).ShouldBe(ButtonColors[expectedLook]);
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

    /// <summary>
    /// What the merger can touch when an instance is selected: the root is the containing element, so only
    /// that element's reference rows move. A row on the instance (<c>Play.Width = ...</c>) loses to the
    /// containing element's selected state setting that same variable, and nothing else changes.
    /// </summary>
    [Fact]
    public void RefreshAll_ToolbarStateSetsInstanceWidthAndDefaultRowWritesIt_StatesWidthWinsAndIsEnabledLookIsKept()
    {
        ComponentSave toolbar = BuildToolbarWithButtonInstance(isEnabled: false);
        toolbar.Instances.Add(new InstanceSave { Name = "Width Source", BaseType = "Rectangle", ParentContainer = toolbar });
        SetNumber(toolbar.DefaultState!, "Width Source.Width", 90f);
        AddReference(toolbar.DefaultState!, "Play", "Width = Width Source.Width");
        StateSave wide = AddCategoryState(toolbar, "SizeCategory", "Wide", s => SetNumber(s, "Play.Width", 300f));

        GraphicalUiElement root = Show(toolbar, wide);

        FindByName(root, "Play").Width.ShouldBe(300f);
        ButtonInstanceColor(root).ShouldBe(ButtonColors["Disabled"]);
    }

    #endregion

    #region Instance setup

    private static (int, int, int) ButtonInstanceColor(GraphicalUiElement root)
    {
        GraphicalUiElement background = FindByName(FindByName(root, "Play"), "Background");
        RecordingRenderable renderable = (RecordingRenderable)background.RenderableComponent!;
        return (renderable.Red, renderable.Green, renderable.Blue);
    }

    private static void AuthorButtonInstance(ElementSave container, bool isEnabled, string? authoredCategoryState)
    {
        StateSave defaultState = container.DefaultState!;
        defaultState.Variables.Add(new VariableSave { Name = "Play.IsEnabled", Value = isEnabled, Type = "bool", SetsValue = true });
        if (authoredCategoryState != null)
        {
            defaultState.Variables.Add(new VariableSave { Name = "Play.ButtonCategoryState", Value = authoredCategoryState, Type = "ButtonCategory", SetsValue = true });
        }
        // The tool runs this after every edit; it is what carries the Forms variable into the category state.
        BehaviorToolOnlyReferencesApplier.Apply(container, defaultState);
    }

    private static ScreenSave BuildScreenWithButtonInstance(bool isEnabled, string? authoredCategoryState)
    {
        BuildFullButton(defaultReference: true, materialized: true);
        ScreenSave screen = new ScreenSave { Name = "Menu" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        screen.Instances.Add(new InstanceSave { Name = "Play", BaseType = "Controls/ButtonStandard", ParentContainer = screen });
        ObjectFinder.Self.GumProjectSave!.Screens.Add(screen);
        AuthorButtonInstance(screen, isEnabled, authoredCategoryState);
        return screen;
    }

    private static ComponentSave BuildToolbarWithButtonInstance(bool isEnabled)
    {
        BuildFullButton(defaultReference: true, materialized: true);
        ComponentSave toolbar = new ComponentSave { Name = "Toolbar", BaseType = "Container" };
        toolbar.States.Add(new StateSave { Name = "Default", ParentContainer = toolbar });
        toolbar.Instances.Add(new InstanceSave { Name = "Play", BaseType = "Controls/ButtonStandard", ParentContainer = toolbar });
        ObjectFinder.Self.GumProjectSave!.Components.Add(toolbar);
        AuthorButtonInstance(toolbar, isEnabled, authoredCategoryState: null);
        return toolbar;
    }

    #endregion
}
