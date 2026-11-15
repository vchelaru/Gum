using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Within one tool theme, every text-input surface (TextBox, PasswordBox, ListBox, ScrollViewer,
/// ItemsControl, ComboBox, TreeView) is one family of fields: in a given state they reference the
/// same color, so two inputs never differ by a near-miss tint.
/// </summary>
public class ThemeInputFillsMatchTests
{
    private static readonly string[] Controls =
        { "TextBox", "PasswordBox", "ListBox", "ScrollViewer", "ItemsControl", "ComboBox", "TreeView" };

    // The instance that paints each control's field fill, by theme.
    private static readonly string[] FillInstances = { "Background", "BevelInstance", "FieldBevelInstance", "Fill" };

    public static IEnumerable<object[]> Themes()
    {
        string templates = Path.Combine(FindRepoRoot(), "Tools", "Gum.ProjectServices", "Templates");
        yield return new object[] { "Standard", Path.Combine(templates, "FormsTemplate", "Components") };
        foreach (string folder in Directory.GetDirectories(Path.Combine(templates, "FormsThemes")))
        {
            yield return new object[] { Path.GetFileName(folder), Path.Combine(folder, "Components") };
        }
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void InputControls_SameStateResolvesToSameFillColor(string theme, string componentsFolder)
    {
        Dictionary<string, string> swatches = LoadSwatches(componentsFolder);
        // state -> control -> "R,G,B"
        Dictionary<string, Dictionary<string, string>> byState = new();

        foreach (string control in Controls)
        {
            string? file = Directory.GetFiles(componentsFolder, control + ".gucx", SearchOption.AllDirectories).FirstOrDefault();
            if (file == null)
            {
                continue;
            }

            foreach (XElement state in XDocument.Load(file).Descendants("State"))
            {
                string stateName = state.Element("Name")?.Value ?? "";
                // A control with no Enabled state paints its Default state when enabled.
                stateName = stateName == "Default" ? "Enabled" : stateName;
                foreach (XElement list in state.Elements("VariableList"))
                {
                    string listName = list.Element("Name")?.Value ?? "";
                    string instance = listName.EndsWith(".VariableReferences", StringComparison.Ordinal)
                        ? listName[..^".VariableReferences".Length] : "";
                    if (!FillInstances.Contains(instance))
                    {
                        continue;
                    }
                    foreach (string reference in list.Descendants("string").Select(s => s.Value))
                    {
                        Match match = Regex.Match(reference, @"^(?:Fill)?Color = .*Styles\.(\w+)\.FillColor$");
                        if (match.Success && swatches.TryGetValue(match.Groups[1].Value, out string? rgb))
                        {
                            // Default state is Enabled; an explicit Enabled state wins by being written last.
                            if (!byState.TryGetValue(stateName, out Dictionary<string, string>? controls))
                            {
                                byState[stateName] = controls = new Dictionary<string, string>();
                            }
                            if (!(controls.ContainsKey(control) && (string?)state.Element("Name") == "Default"))
                            {
                                controls[control] = rgb;
                            }
                        }
                    }
                }
            }
        }

        List<string> problems = byState
            .Where(s => s.Value.Values.Distinct().Count() > 1)
            .Select(s => $"{s.Key}: " + string.Join(", ", s.Value.Select(c => $"{c.Key}={c.Value}")))
            .ToList();
        problems.ShouldBeEmpty($"{theme} input fills differ between controls");
    }

    private static Dictionary<string, string> LoadSwatches(string componentsFolder)
    {
        string styles = Directory.GetFiles(componentsFolder, "Styles.gucx", SearchOption.AllDirectories).Single();
        Dictionary<string, string> values = XDocument.Load(styles)
            .Descendants("Variable")
            .Select(v => (Name: (string?)v.Attribute("Name") ?? "", Value: v.Element("Value")?.Value ?? ""))
            .Where(x => x.Name.Length > 0)
            .GroupBy(x => x.Name)
            .ToDictionary(g => g.Key, g => g.First().Value);

        Dictionary<string, string> swatches = new();
        foreach (string name in values.Keys.Where(k => k.EndsWith(".FillRed", StringComparison.Ordinal)))
        {
            string swatch = name[..^".FillRed".Length];
            if (values.TryGetValue(swatch + ".FillGreen", out string? g) && values.TryGetValue(swatch + ".FillBlue", out string? b))
            {
                swatches[swatch] = $"{values[name]},{g},{b}";
            }
        }
        return swatches;
    }

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 12; i++)
        {
            if (Directory.Exists(Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates")))
            {
                return current;
            }
            current = Path.GetDirectoryName(current)!;
        }
        throw new InvalidOperationException("Repo root not found");
    }
}
