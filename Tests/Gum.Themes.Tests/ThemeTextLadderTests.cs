using System.Xml.Linq;
using Gum.DataTypes.Variables;
using Gum.Themes.Bubblegum;
using Gum.Themes.DarkPro;
using Gum.Themes.Editor;
using Gum.Themes.ForestGlade;
using Gum.Themes.Hazard;
using Gum.Themes.Meadow;
using Gum.Themes.Neon;
using Gum.Themes.Retro95;
using Shouldly;
using V3Styling = Gum.Forms.DefaultVisuals.V3.Styling;
using V3Text = Gum.Forms.DefaultVisuals.V3.Text;

namespace Gum.Themes.Tests;

/// <summary>
/// A code-only theme fills V3 Styling.Text with the same text ladder its Forms template's Styles
/// component carries (#8509): same rungs, sizes, bold and italic, and the same font family.
/// </summary>
public class ThemeTextLadderTests
{
    private static readonly string[] Ladder = { "Tiny", "Small", "Normal", "Emphasis", "Strong", "H3", "H2", "H1", "Title" };

    public static IEnumerable<object[]> ThemeNames => new[]
    {
        "Bubblegum", "DarkPro", "Editor", "ForestGlade", "Hazard", "Meadow", "Neon", "Retro95"
    }.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(ThemeNames))]
    public void ConfigureStyling_ShouldMatchTheTemplatesTextLadder(string themeName)
    {
        ConfigureStyling(themeName);
        Dictionary<string, string> template = LoadTemplateStylesVariables(themeName);
        V3Text text = V3Styling.ActiveStyle.Text;

        foreach (string rung in Ladder)
        {
            string label = $"{themeName} {rung}";
            System.Reflection.FieldInfo? field = typeof(V3Text).GetField(rung);
            field.ShouldNotBeNull($"V3 Styling.Text has no {rung} style");
            StateSave state = (StateSave)field.GetValue(text)!;
            template.ShouldContainKey(rung + ".FontSize", $"{themeName}'s template Styles has no {rung} style");

            state.GetValue("FontSize").ShouldBe(int.Parse(template[rung + ".FontSize"]), label);
            state.GetValue("IsBold").ShouldBe(template.GetValueOrDefault(rung + ".IsBold") == "true", label);
            state.GetValue("IsItalic").ShouldBe(template.GetValueOrDefault(rung + ".IsItalic") == "true", label);

            // The template names a bundled file ("Fonts/SairaCondensed-SemiBold.ttf"), the code theme the
            // family it registered that file under ("Saira Condensed").
            if (template.TryGetValue(rung + ".Font", out string? templateFont))
            {
                string templateFamily = Path.GetFileNameWithoutExtension(templateFont).Split('-')[0];
                ((string)state.GetValue("Font")!).Replace(" ", "").ShouldBe(templateFamily, label);
            }
        }
    }

    private static void ConfigureStyling(string themeName)
    {
        switch (themeName)
        {
            case "Bubblegum": BubblegumTheme.ConfigureStyling(); break;
            case "DarkPro": DarkProTheme.ConfigureStyling(); break;
            case "Editor": EditorTheme.ConfigureStyling(); break;
            case "ForestGlade": ForestGladeTheme.ConfigureStyling(); break;
            case "Hazard": HazardTheme.ConfigureStyling(); break;
            case "Meadow": MeadowTheme.ConfigureStyling(); break;
            case "Neon": NeonTheme.ConfigureStyling(); break;
            case "Retro95": Retro95Theme.ConfigureStyling(); break;
            default: throw new ArgumentException(themeName);
        }
    }

    private static Dictionary<string, string> LoadTemplateStylesVariables(string themeName)
    {
        string path = Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsThemes",
            themeName, "Components", themeName, "Styles.gucx");
        XElement defaultState = XDocument.Load(path).Root!.Element("State")!;
        return defaultState.Elements("Variable")
            .Where(v => v.Element("Value") != null)
            .ToDictionary(v => (string)v.Attribute("Name")!, v => v.Element("Value")!.Value);
    }

    private static string FindRepoRoot()
    {
        string? current = AppContext.BaseDirectory;
        while (current != null)
        {
            if (Directory.Exists(Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates")))
            {
                return current;
            }
            current = Path.GetDirectoryName(current);
        }
        throw new InvalidOperationException("could not locate repo root from " + AppContext.BaseDirectory);
    }
}
