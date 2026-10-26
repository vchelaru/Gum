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


    #region Precedence matrix: view x selected state x IsEnabled x authored category state x default reference row

    public static IEnumerable<object[]> PrecedenceCells() =>
        AllPrecedenceCells().Where(cell => !IsKnownDefaultStateGap((string)cell[0], (string)cell[1], (string)cell[2], (string)cell[3]));

    /// <summary>The component's Default selection when the look should be Disabled: red, see the default-state branch.</summary>
    public static IEnumerable<object[]> DefaultStateGapCells() =>
        AllPrecedenceCells().Where(cell => IsKnownDefaultStateGap((string)cell[0], (string)cell[1], (string)cell[2], (string)cell[3]));

    private static IEnumerable<object[]> AllPrecedenceCells()
    {
        foreach (string view in new[] { "component", "screen", "toolbar" })
        {
            string[] states = view == "component"
                ? new[] { "Default", "Enabled", "Highlighted", "Pushed", "Focused", "Disabled" }
                : new[] { "Default", "Moved", "Other" };
            foreach (string state in states)
            {
                foreach (string isEnabled in new[] { "unset", "true", "false" })
                {
                    foreach (string authored in new[] { "absent", "Enabled", "Disabled" })
                    {
                        foreach (bool defaultReference in new[] { true, false })
                        {
                            yield return new object[] { view, state, isEnabled, authored, defaultReference };
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// Component view: the clicked categorized state wins; Default shows what the Forms variable derives,
    /// or the authored category state when IsEnabled does not make it Disabled. Instance view (screen or
    /// another component): the instance's IsEnabled decides the look and an authored category state that
    /// disagrees with it is replaced.
    /// </summary>
    private static string ExpectedLook(string view, string state, string isEnabled, string authored)
    {
        if (view == "component")
        {
            if (state != "Default")
            {
                return state;
            }
            return isEnabled == "false" ? "Disabled" : authored == "absent" ? "Enabled" : authored;
        }
        return isEnabled == "false" ? "Disabled" : "Enabled";
    }

    private static bool IsKnownDefaultStateGap(string view, string state, string isEnabled, string authored) =>
        view == "component" && state == "Default" && ExpectedLook(view, state, isEnabled, authored) == "Disabled";

    private static (int, int, int) ShowPrecedenceCell(string view, string state, string isEnabled, string authored, bool defaultReference)
    {
        bool? authoredIsEnabled = isEnabled == "unset" ? null : isEnabled == "true";
        string? authoredCategoryState = authored == "absent" ? null : authored;
        ComponentSave button = BuildFullButton(defaultReference, materialized: true);

        if (view == "component")
        {
            AuthorButton(button, "", authoredIsEnabled, authoredCategoryState);
            StateSave selected = state == "Default" ? button.DefaultState! : button.Categories[0].States.Single(item => item.Name == state);
            return ColorOfBackground(Show(button, selected));
        }

        ElementSave container;
        if (view == "screen")
        {
            ScreenSave screen = new ScreenSave { Name = "Menu" };
            screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
            ObjectFinder.Self.GumProjectSave!.Screens.Add(screen);
            container = screen;
        }
        else
        {
            ComponentSave toolbar = new ComponentSave { Name = "Toolbar", BaseType = "Container" };
            toolbar.States.Add(new StateSave { Name = "Default", ParentContainer = toolbar });
            ObjectFinder.Self.GumProjectSave!.Components.Add(toolbar);
            container = toolbar;
        }
        container.Instances.Add(new InstanceSave { Name = "Play", BaseType = "Controls/ButtonStandard", ParentContainer = container });
        AuthorButton(container, "Play.", authoredIsEnabled, authoredCategoryState);
        StateSave containerState = state == "Default"
            ? container.DefaultState!
            : AddCategoryState(container, "ModeCategory", state, s => SetNumber(s, "Play.X", 25f));
        return ButtonInstanceColor(Show(container, containerState));
    }

    private static void AuthorButton(ElementSave container, string prefix, bool? isEnabled, string? authoredCategoryState)
    {
        StateSave defaultState = container.DefaultState!;
        if (isEnabled != null)
        {
            defaultState.Variables.Add(new VariableSave { Name = prefix + "IsEnabled", Value = isEnabled.Value, Type = "bool", SetsValue = true });
        }
        if (authoredCategoryState != null)
        {
            defaultState.Variables.RemoveAll(v => v.Name == prefix + "ButtonCategoryState");
            defaultState.Variables.Add(new VariableSave { Name = prefix + "ButtonCategoryState", Value = authoredCategoryState, Type = "ButtonCategory", SetsValue = true });
        }
        BehaviorToolOnlyReferencesApplier.Apply(container, defaultState);
    }

    private static (int, int, int) ColorOfBackground(GraphicalUiElement root)
    {
        RecordingRenderable renderable = (RecordingRenderable)FindByName(root, "Background").RenderableComponent!;
        return (renderable.Red, renderable.Green, renderable.Blue);
    }

    [Theory]
    [MemberData(nameof(PrecedenceCells))]
    public void RefreshAll_PrecedenceCell_ShowsTheLookTheRuleSays(string view, string state, string isEnabled, string authored, bool defaultReference)
    {
        ShowPrecedenceCell(view, state, isEnabled, authored, defaultReference)
            .ShouldBe(ButtonColors[ExpectedLook(view, state, isEnabled, authored)]);
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
