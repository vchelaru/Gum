using System.Reflection;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.DataTypes.Variables;
using Gum.Wireframe;
using NineSliceRuntime = MonoGameGum.GueDeriving.NineSliceRuntime;
using RectangleRuntime = Gum.GueDeriving.RectangleRuntime;
using Shouldly;
using Bubblegum = Gum.Themes.Bubblegum;
using DarkPro = Gum.Themes.DarkPro;
using Editor = Gum.Themes.Editor;
using ForestGlade = Gum.Themes.ForestGlade;
using Hazard = Gum.Themes.Hazard;
using Meadow = Gum.Themes.Meadow;
using Neon = Gum.Themes.Neon;
using Retro95 = Gum.Themes.Retro95;
using Template = Gum.Themes.Template;

namespace Gum.Themes.Tests;

/// <summary>
/// Every text-input surface of a theme (TextBox, PasswordBox, ListBox, ScrollViewer, ItemsControl,
/// ComboBox) is one family of fields, so in a given state they must paint the exact same fill.
/// Near-miss colors between sibling inputs read as a mistake, not a design.
/// </summary>
public class InputFillConsistencyTests
{
    private static readonly string[] StateNames = { "Enabled", "Disabled", "Highlighted", "Focused" };

    public static IEnumerable<object[]> Themes()
    {
        yield return new object[] { "Bubblegum", (Action)Bubblegum.BubblegumTheme.ConfigureStyling, (Action)Bubblegum.BubblegumTheme.RegisterVisuals };
        yield return new object[] { "DarkPro", (Action)DarkPro.DarkProTheme.ConfigureStyling, (Action)DarkPro.DarkProTheme.RegisterVisuals };
        yield return new object[] { "Editor", (Action)Editor.EditorTheme.ConfigureStyling, (Action)Editor.EditorTheme.RegisterVisuals };
        yield return new object[] { "ForestGlade", (Action)ForestGlade.ForestGladeTheme.ConfigureStyling, (Action)ForestGlade.ForestGladeTheme.RegisterVisuals };
        yield return new object[] { "Hazard", (Action)Hazard.HazardTheme.ConfigureStyling, (Action)Hazard.HazardTheme.RegisterVisuals };
        yield return new object[] { "Meadow", (Action)Meadow.MeadowTheme.ConfigureStyling, (Action)Meadow.MeadowTheme.RegisterVisuals };
        yield return new object[] { "Neon", (Action)Neon.NeonTheme.ConfigureStyling, (Action)Neon.NeonTheme.RegisterVisuals };
        yield return new object[] { "Retro95", (Action)Retro95.Retro95Theme.ConfigureStyling, (Action)Retro95.Retro95Theme.RegisterVisuals };
        yield return new object[] { "Template", (Action)Template.TemplateTheme.ConfigureStyling, (Action)Template.TemplateTheme.RegisterVisuals };
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void InputControls_SameStateHasSameFill(string theme, Action configureStyling, Action registerVisuals)
    {
        Dictionary<Type, VisualTemplate> previousTemplates = new(FrameworkElement.DefaultFormsTemplates);
        try
        {
            configureStyling();
            registerVisuals();

            (string Name, FrameworkElement Element)[] controls =
            {
                ("TextBox", new TextBox()),
                ("PasswordBox", new PasswordBox()),
                ("ListBox", new ListBox()),
                ("ScrollViewer", new ScrollViewer()),
                ("ItemsControl", new ItemsControl()),
                ("ComboBox", new ComboBox()),
            };

            List<string> problems = new();
            foreach (string state in StateNames)
            {
                Dictionary<string, string> fills = new();
                foreach ((string name, FrameworkElement element) in controls)
                {
                    // The Editor combo box is a flat well that signals hover and disabled with its outline and
                    // text; the stock V3 combo box background has no per-state tint to align.
                    if (theme == "Editor" && name == "ComboBox" && state is "Disabled" or "Highlighted")
                    {
                        continue;
                    }
                    GraphicalUiElement visual = (GraphicalUiElement)element.Visual;
                    if (!ApplyState(visual, state))
                    {
                        continue;
                    }
                    string? fill = FindFill(visual);
                    if (fill != null)
                    {
                        fills[name] = fill;
                    }
                }

                if (fills.Values.Distinct().Count() > 1)
                {
                    problems.Add($"{state}: " + string.Join(", ", fills.Select(f => $"{f.Key}={f.Value}")));
                }
            }

            problems.ShouldBeEmpty($"{theme} input fills differ between controls");
        }
        finally
        {
            FrameworkElement.DefaultFormsTemplates.Clear();
            foreach (KeyValuePair<Type, VisualTemplate> pair in previousTemplates)
            {
                FrameworkElement.DefaultFormsTemplates[pair.Key] = pair.Value;
            }
        }
    }

    /// <summary>Applies the named state if the visual declares it. The default state counts as Enabled for a control with no states.</summary>
    private static bool ApplyState(GraphicalUiElement visual, string state)
    {
        // The typed category states are a public field named States that hides GraphicalUiElement.States.
        object? states = visual.GetType().GetField("States", BindingFlags.Public | BindingFlags.Instance)?.GetValue(visual);
        if (states == null)
        {
            return state == "Enabled";
        }
        StateSave? save = states.GetType().GetProperty(state)?.GetValue(states) as StateSave;
        if (save == null)
        {
            return state == "Enabled";
        }
        save.Apply?.Invoke();
        return save.Apply != null;
    }

    /// <summary>The shallowest filled rectangle named like a fill or background, as an RGBA string.</summary>
    private static string? FindFill(GraphicalUiElement root)
    {
        Queue<GraphicalUiElement> queue = new();
        queue.Enqueue(root);
        while (queue.Count > 0)
        {
            GraphicalUiElement current = queue.Dequeue();
            string name = current.Name ?? "";
            bool looksLikeFill = name.EndsWith("Fill", StringComparison.Ordinal) || name == "Background";
            if (looksLikeFill && current is RectangleRuntime rectangle && rectangle.IsFilled)
            {
                // A gradient field (ForestGlade) darkens toward its top; the far end is the field color.
                Microsoft.Xna.Framework.Color color = rectangle.UseGradient ? rectangle.Color2 : rectangle.FillColor;
                return $"{color.R},{color.G},{color.B},{color.A}";
            }
            if (looksLikeFill && current is NineSliceRuntime nineSlice)
            {
                return $"{nineSlice.Color.R},{nineSlice.Color.G},{nineSlice.Color.B},{nineSlice.Color.A}";
            }
            foreach (GraphicalUiElement child in current.Children.OfType<GraphicalUiElement>())
            {
                queue.Enqueue(child);
            }
        }
        return null;
    }
}
