using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Wireframe;
using Shouldly;
using static Gum.Presentation.Tests.StateSelectionCanvasHarness;

namespace Gum.Presentation.Tests;

/// <summary>
/// What the canvas shows when the Forms values of a control (IsEnabled, IsChecked) and a visual state value
/// disagree (#6860). The rule: viewing the control's own component, the clicked state wins (Default shows the Default values with the
/// category state the Forms values choose on top, #6857); viewing an instance, an authored Forms value wins
/// over a category state a state of the container sets. Controls: Button (IsEnabled), CheckBox (IsEnabled,
/// IsChecked), the Forms properties that feed a *CategoryState through a behavior's tool-only reference.
/// </summary>
public partial class StateSelectionCanvasSplashTests
{
    #region Case model and oracle

    public sealed record FormsCase(
        string Control,
        string View,
        string Selected,
        bool? DefaultEnabled,
        bool? DefaultChecked,
        string? DefaultAuthoredCategory,
        bool? StateEnabled,
        bool? StateChecked,
        string? StateCategory)
    {
        public override string ToString() =>
            $"{Control}/{View}/{Selected} default(en={DefaultEnabled}, chk={DefaultChecked}, cat={DefaultAuthoredCategory}) state(en={StateEnabled}, chk={StateChecked}, cat={StateCategory})";
    }

    private static string[] FormsStatesOf(string control) => control == "Button"
        ? new[] { "Enabled", "Disabled" }
        : new[] { "EnabledOff", "EnabledOn", "DisabledOff", "DisabledOn" };

    private static string DerivedLook(string control, bool enabled, bool isChecked) => control == "Button"
        ? (enabled ? "Enabled" : "Disabled")
        : (enabled ? "Enabled" : "Disabled") + (isChecked ? "On" : "Off");

    /// <summary>The maintainer's two-case rule (2026-10-26), written independently of the canvas code.</summary>
    private static string ExpectedFormsLook(FormsCase c)
    {
        string resting = DerivedLook(c.Control, true, false);
        if (c.View == "component")
        {
            // Default selected is the base layer a running game never shows alone: the category state the
            // Forms values choose sits on top of it (#6857). An authored category state is ignored.
            return c.Selected == "Default"
                ? DerivedLook(c.Control, c.DefaultEnabled ?? true, c.DefaultChecked ?? false)
                : c.Selected;
        }

        bool authored = c.DefaultEnabled != null || c.DefaultChecked != null || c.StateEnabled != null || c.StateChecked != null;
        if (authored)
        {
            return DerivedLook(c.Control, c.StateEnabled ?? c.DefaultEnabled ?? true, c.StateChecked ?? c.DefaultChecked ?? false);
        }
        return c.StateCategory ?? resting;
    }

    #endregion

    #region Builders

    private static (int, int, int) ColorOfFormsState(string control, string state) =>
        (10 + 20 * Array.IndexOf(FormsStatesOf(control), state), 30, 50);

    /// <summary>
    /// A Forms control component: Background (NineSlice), the behavior with a tool-only reference, a default state
    /// holding a hard color equal to the resting state's, and one category state per look with its own color.
    /// </summary>
    private static ComponentSave BuildFormsControl(string control)
    {
        string categoryName = control + "Category";
        BehaviorSave behavior = new BehaviorSave { Name = control + "Behavior" };
        behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsEnabled", Value = true });
        if (control == "Button")
        {
            behavior.ToolOnlyVariableReferences.Add($"{categoryName}State = IsEnabled ? \"Enabled\" : \"Disabled\"");
        }
        else
        {
            behavior.FormsProperties.Add(new VariableSave { Type = "bool", Name = "IsChecked", Value = false });
            behavior.ToolOnlyVariableReferences.Add($"{categoryName}State = IsEnabled ? (IsChecked ? \"EnabledOn\" : \"EnabledOff\") : (IsChecked ? \"DisabledOn\" : \"DisabledOff\")");
        }

        ComponentSave component = new ComponentSave { Name = "Controls/" + control + "Standard", BaseType = "Container" };
        component.Behaviors.Add(new ElementBehaviorReference { BehaviorName = behavior.Name });
        component.Instances.Add(new InstanceSave { Name = "Background", BaseType = "NineSlice", ParentContainer = component });

        StateSave defaultState = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(defaultState);
        string resting = DerivedLook(control, true, false);
        SetFill(defaultState, "Background", ColorOfFormsState(control, resting), prefix: "");
        defaultState.Variables.Add(new VariableSave { Name = categoryName + "State", Type = categoryName, SetsValue = false });

        StateSaveCategory category = new StateSaveCategory { Name = categoryName };
        foreach (string stateName in FormsStatesOf(control))
        {
            StateSave state = new StateSave { Name = stateName, ParentContainer = component };
            SetFill(state, "Background", ColorOfFormsState(control, stateName), prefix: "");
            category.States.Add(state);
        }
        component.Categories.Add(category);

        GumProjectSave project = BuildProject(component);
        project.Behaviors.Add(behavior);
        return component;
    }

    private static void SetBool(StateSave state, string name, bool? value)
    {
        if (value != null)
        {
            state.Variables.Add(new VariableSave { Name = name, Value = value.Value, Type = "bool", SetsValue = true });
        }
    }

    private static void SetCategory(StateSave state, string name, string? value, string categoryName)
    {
        if (value != null)
        {
            state.Variables.RemoveAll(v => v.Name == name);
            state.Variables.Add(new VariableSave { Name = name, Value = value, Type = categoryName, SetsValue = true });
        }
    }

    private static (int, int, int) ShowFormsCase(FormsCase c)
    {
        ComponentSave control = BuildFormsControl(c.Control);
        string categoryVariable = c.Control + "CategoryState";
        string categoryName = c.Control + "Category";

        if (c.View == "component")
        {
            SetBool(control.DefaultState!, "IsEnabled", c.DefaultEnabled);
            SetBool(control.DefaultState!, "IsChecked", c.DefaultChecked);
            SetCategory(control.DefaultState!, categoryVariable, c.DefaultAuthoredCategory, categoryName);
            BehaviorToolOnlyReferencesApplier.Apply(control, control.DefaultState!);
            StateSave selected = c.Selected == "Default" ? control.DefaultState! : control.Categories[0].States.Single(item => item.Name == c.Selected);
            return ColorOfBackground(Show(control, selected));
        }

        ElementSave container;
        if (c.View == "screen")
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
        container.Instances.Add(new InstanceSave { Name = "Play", BaseType = control.Name, ParentContainer = container });
        SetBool(container.DefaultState!, "Play.IsEnabled", c.DefaultEnabled);
        SetBool(container.DefaultState!, "Play.IsChecked", c.DefaultChecked);
        SetCategory(container.DefaultState!, "Play." + categoryVariable, c.DefaultAuthoredCategory, categoryName);
        BehaviorToolOnlyReferencesApplier.Apply(container, container.DefaultState!);

        StateSave containerState = c.Selected == "Default"
            ? container.DefaultState!
            : AddCategoryState(container, "ModeCategory", c.Selected, s =>
            {
                SetNumber(s, "Play.X", 25f);
                SetBool(s, "Play.IsEnabled", c.StateEnabled);
                SetBool(s, "Play.IsChecked", c.StateChecked);
                SetCategory(s, "Play." + categoryVariable, c.StateCategory, categoryName);
            });
        return ButtonInstanceColor(Show(container, containerState));
    }

    #endregion

    #region Matrix

    private static readonly bool?[] FormsValues = { null, true, false };

    public static IEnumerable<object[]> FormsValueCells()
    {
        foreach (string control in new[] { "Button", "CheckBox" })
        {
            string[] categories = new string?[] { null }.Concat(FormsStatesOf(control)).Cast<string>().ToArray();
            foreach (string view in new[] { "screen", "toolbar" })
            {
                foreach (string selected in new[] { "Default", "Moved" })
                {
                    foreach (bool? defaultEnabled in FormsValues)
                    foreach (bool? defaultChecked in control == "Button" ? new bool?[] { null } : FormsValues)
                    foreach (string? authored in categories)
                    foreach (bool? stateEnabled in selected == "Default" ? new bool?[] { null } : FormsValues)
                    foreach (bool? stateChecked in selected == "Default" || control == "Button" ? new bool?[] { null } : FormsValues)
                    foreach (string? stateCategory in selected == "Default" ? new string?[] { null } : categories)
                    {
                        yield return new object[] { new FormsCase(control, view, selected, defaultEnabled, defaultChecked, authored, stateEnabled, stateChecked, stateCategory) };
                    }
                }
            }

            foreach (string selected in new[] { "Default" }.Concat(FormsStatesOf(control)))
            {
                foreach (bool? defaultEnabled in FormsValues)
                foreach (bool? defaultChecked in control == "Button" ? new bool?[] { null } : FormsValues)
                foreach (string? authored in categories)
                {
                    yield return new object[] { new FormsCase(control, "component", selected, defaultEnabled, defaultChecked, authored, null, null, null) };
                }
            }
        }
    }

    [Theory]
    [MemberData(nameof(FormsValueCells))]
    public void RefreshAll_FormsValuesAndVisualStateDisagree_CanvasShowsTheLookTheRuleSays(FormsCase formsCase)
    {
        ShowFormsCase(formsCase).ShouldBe(ColorOfFormsState(formsCase.Control, ExpectedFormsLook(formsCase)), formsCase.ToString());
    }

    #endregion

    #region Seeded sweep

    [Fact]
    public void Sweep_RefreshAllWithRandomFormsValuesAndStates_CanvasShowsTheLookTheRuleSays()
    {
        int cases = MonoGameGum.Tests.SweepScale.Cases(300);
        Random random = new Random(68600);
        List<string> failures = new List<string>();
        for (int i = 0; i < cases; i++)
        {
            string control = random.Next(2) == 0 ? "Button" : "CheckBox";
            string view = new[] { "component", "screen", "toolbar" }[random.Next(3)];
            string[] states = FormsStatesOf(control);
            string selected = view == "component"
                ? (random.Next(3) == 0 ? "Default" : states[random.Next(states.Length)])
                : (random.Next(2) == 0 ? "Default" : "Moved");
            bool? Pick() => FormsValues[random.Next(3)];
            string? PickState() => random.Next(3) == 0 ? null : states[random.Next(states.Length)];
            bool inState = view != "component" && selected != "Default";
            FormsCase formsCase = new FormsCase(
                control, view, selected,
                Pick(), control == "Button" ? null : Pick(), PickState(),
                inState ? Pick() : null, inState && control != "Button" ? Pick() : null, inState ? PickState() : null);

            (int, int, int) shown = ShowFormsCase(formsCase);
            string expected = ExpectedFormsLook(formsCase);
            if (shown != ColorOfFormsState(control, expected))
            {
                failures.Add($"{formsCase} expected {expected}");
            }
        }

        failures.ShouldBeEmpty(string.Join(Environment.NewLine, failures.Take(10)));
    }

    #endregion
}
