using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms.Themed;

/// <summary>
/// In every packaged theme, the Highlighted, Pushed, Disabled and Focused states of an interactive control
/// change at least one value compared to the resting (Enabled) state, once variable references are resolved
/// the way GumService does before the first build. A state that sets only what the resting state already sets
/// looks dead in a running game. A real GumPreview render of Editor Button with and without --focus produced
/// byte-identical screenshots, while Meadow Button differed.
/// </summary>
public class ThemedControlStateDefinitionTests : BaseTestClass
{
    private static readonly string[] Controls =
        { "Button", "CheckBox", "RadioButton", "TextBox", "PasswordBox", "ComboBox", "Slider", "ListBoxItem", "MenuItem", "Expander" };
    private static readonly string[] States = { "Highlighted", "Pushed", "Disabled", "Focused" };

    public static IEnumerable<object[]> ThemeAndControl() =>
        ThemedControlHarness.ThemeNames.SelectMany(t => Controls.Select(c => new object[] { t, c }));

    [Theory]
    [MemberData(nameof(ThemeAndControl))]
    public void InteractiveStates_DifferFromTheRestingState(string theme, string controlName)
    {
        GumProjectSave project = ThemedControlHarness.LoadTheme(theme);
        ComponentSave? component = ThemedControlHarness.Find(project, controlName)
            ?? (controlName == "Button" ? ThemedControlHarness.Find(project, "ButtonStandard") : null)
            ?? (controlName == "Expander" ? ThemedControlHarness.Find(project, "ExpanderStandard") : null);
        if (component == null) { return; }
        HashSet<string> instances = component.Instances.Select(i => i.Name).ToHashSet();
        List<string> dead = new List<string>();

        foreach (StateSaveCategory category in component.Categories)
        {
            StateSave? resting = category.States.FirstOrDefault(s => s.Name == "Enabled");
            if (resting == null) { continue; }
            foreach (StateSave state in category.States.Where(s => States.Contains(s.Name)))
            {
                bool changesSomething = state.Variables.Any(v =>
                    v.SetsValue && v.Value != null && !v.Name.EndsWith("VariableReferences")
                    && (!v.Name.Contains('.') || instances.Contains(v.Name.Split('.')[0]))
                    && !Equals(resting.Variables.FirstOrDefault(r => r.Name == v.Name)?.Value
                        ?? component.DefaultState.Variables.FirstOrDefault(r => r.Name == v.Name)?.Value, v.Value));
                if (!changesSomething)
                {
                    dead.Add($"{category.Name}.{state.Name}");
                }
            }
        }

        dead.ShouldBeEmpty($"{theme} {controlName}: state(s) identical to resting: {string.Join(", ", dead)}");
    }
}
