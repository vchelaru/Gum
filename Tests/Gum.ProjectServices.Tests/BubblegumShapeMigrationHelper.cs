using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// One-off helper that migrates the bundled Bubblegum theme template off the retired
/// <c>ColoredRectangle</c> / <c>RoundedRectangle</c> / <c>ColoredCircle</c> standards onto the v3
/// <c>Rectangle</c> / <c>Circle</c> standards. Marked Skip by default — unskip, run once to rewrite
/// the on-disk component files in the worktree, then re-skip before committing. NOT a real test.
///
/// The retired shapes share a single flat Color gated by IsFilled; the v3 shapes split fill and
/// stroke into separate surfaces. The mapping per migrated instance:
///   - filled  (IsFilled true, the legacy default): Red/Green/Blue/Alpha -> Fill*, IsFilled=true,
///     StrokeWidth forced to 0 (the legacy filled shape drew no border; the v3 default StrokeWidth=2
///     would draw an unwanted one).
///   - outline (IsFilled false): Red/Green/Blue/Alpha -> Stroke*, IsFilled=false, StrokeWidth left as-is.
/// RoundedRectangle's CornerRadius / dropshadow / gradient carry across unchanged (Rectangle now
/// exposes CornerRadius). ColoredCircle sizes by Width/Height but a Circle renders by Radius, so
/// Radius = Width/2 is written for each state that sizes the circle.
/// VariableReferences targeting the Styles swatch colors (Styles.X.Red) are repointed to .FillX.
/// </summary>
public class BubblegumShapeMigrationHelper
{
    private static readonly Dictionary<string, string> RetiredToReplacement = new()
    {
        { "ColoredRectangle", "Rectangle" },
        { "RoundedRectangle", "Rectangle" },
        { "ColoredCircle", "Circle" },
    };

    private static readonly string[] ColorChannels = { "Red", "Green", "Blue", "Alpha" };

    // Styles.<Swatch>.<Channel> color references — all Styles swatches are filled, so they repoint
    // to the Fill surface. Mirrors the FormsTemplate v3 migration (PR #2971).
    private static readonly Regex StyleColorReference =
        new(@"(Styles\.[A-Za-z0-9_]+)\.(Red|Green|Blue|Alpha)\b", RegexOptions.Compiled);

    [Fact(Skip = "Manual migration helper. Set Skip = null to run once.")]
    public void Migrate()
    {
        StandardElementsManager.Self.Initialize();
        StandardElementsManager.Self.RegisterExtendedDefaultStates();

        string repoRoot = FindRepoRoot();
        string bubblegumDir = Path.Combine(repoRoot,
            "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", "Bubblegum");
        string gumxPath = Path.Combine(bubblegumDir, "GumProject.gumx");

        ProjectLoadResult result = new ProjectLoader().Load(gumxPath);
        result.Success.ShouldBeTrue();
        GumProjectSave project = result.Project!;

        HashSet<ElementSave> changedElements = new();

        foreach (ElementSave element in project.Components.Cast<ElementSave>().Concat(project.Screens))
        {
            bool changed = false;

            foreach (InstanceSave instance in element.Instances.ToList())
            {
                if (!RetiredToReplacement.TryGetValue(instance.BaseType, out string? newType))
                {
                    continue;
                }

                bool isFilled = ResolveIsFilled(element, instance.Name);
                MigrateInstance(element, instance.Name, newType, isFilled);
                instance.BaseType = newType;
                changed = true;
            }

            if (RepointStyleReferences(element))
            {
                changed = true;
            }

            if (changed)
            {
                foreach (StateSave state in element.AllStates)
                {
                    state.Variables.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
                }
                changedElements.Add(element);
            }
        }

        foreach (ElementSave element in changedElements)
        {
            element.Save(GetElementPath(bubblegumDir, element), useCompactFormat: true);
        }

        changedElements.ShouldNotBeEmpty("expected at least one migrated element");
    }

    private static bool ResolveIsFilled(ElementSave element, string instanceName)
    {
        // ColoredRectangle has no IsFilled (always a solid fill); RoundedRectangle / ColoredCircle
        // default IsFilled to true. So an absent IsFilled means filled.
        VariableSave? variable = element.DefaultState?.Variables
            .FirstOrDefault(v => v.Name == instanceName + ".IsFilled");
        if (variable?.Value is bool b)
        {
            return b;
        }
        return true;
    }

    private static void MigrateInstance(ElementSave element, string instanceName, string newType, bool isFilled)
    {
        string prefix = instanceName + ".";
        string colorTarget = isFilled ? "Fill" : "Stroke";

        foreach (StateSave state in element.AllStates)
        {
            // Rename the shared flat color to the chosen fill/stroke surface.
            foreach (VariableSave variable in state.Variables)
            {
                if (variable.SourceObject != instanceName)
                {
                    continue;
                }
                string rootName = variable.GetRootName();
                if (ColorChannels.Contains(rootName))
                {
                    variable.Name = prefix + colorTarget + rootName;
                }
            }

            // Drop dash variables that don't exist on the v3 Rectangle / Circle surface.
            state.Variables.RemoveAll(v => v.Name == prefix + "StrokeDashLength"
                || v.Name == prefix + "StrokeGapLength");

            // ColoredCircle sized by Width; a Circle renders by Radius. Write Radius for each state
            // that sizes the circle so the visual diameter is preserved.
            if (newType == "Circle")
            {
                VariableSave? widthVariable = state.Variables.FirstOrDefault(v => v.Name == prefix + "Width");
                if (widthVariable?.Value != null)
                {
                    float width = Convert.ToSingle(widthVariable.Value, CultureInfo.InvariantCulture);
                    SetVariable(state, prefix + "Radius", "float", width / 2f);
                }
            }
        }

        StateSave defaultState = element.DefaultState!;
        SetVariable(defaultState, prefix + "IsFilled", "bool", isFilled);
        if (isFilled)
        {
            // Legacy filled shapes drew no border; suppress the v3 default StrokeWidth = 2.
            SetVariable(defaultState, prefix + "StrokeWidth", "float", 0f);
        }
    }

    private static void SetVariable(StateSave state, string name, string type, object value)
    {
        VariableSave? existing = state.Variables.FirstOrDefault(v => v.Name == name);
        if (existing != null)
        {
            existing.Value = value;
            existing.SetsValue = true;
            existing.Type = type;
        }
        else
        {
            state.Variables.Add(new VariableSave
            {
                SetsValue = true,
                Type = type,
                Value = value,
                Name = name,
            });
        }
    }

    private static bool RepointStyleReferences(ElementSave element)
    {
        bool changed = false;
        foreach (StateSave state in element.AllStates)
        {
            foreach (VariableListSave list in state.VariableLists)
            {
                if (list.GetRootName() != "VariableReferences")
                {
                    continue;
                }
                if (list.ValueAsIList is not { } items)
                {
                    continue;
                }
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i] is string entry)
                    {
                        string updated = StyleColorReference.Replace(entry,
                            m => $"{m.Groups[1].Value}.Fill{m.Groups[2].Value}");
                        if (updated != entry)
                        {
                            items[i] = updated;
                            changed = true;
                        }
                    }
                }
            }
        }
        return changed;
    }

    private static string GetElementPath(string bubblegumDir, ElementSave element)
    {
        string subDirectory;
        string extension;
        if (element is ScreenSave)
        {
            subDirectory = "Screens";
            extension = GumProjectSave.ScreenExtension;
        }
        else
        {
            subDirectory = "Components";
            extension = GumProjectSave.ComponentExtension;
        }
        string relative = element.Name.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(bubblegumDir, subDirectory, relative + "." + extension);
    }

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string templatesDir = Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates");
            if (Directory.Exists(templatesDir))
            {
                return current;
            }
            string? parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent) || parent == current)
            {
                break;
            }
            current = parent;
        }
        throw new InvalidOperationException("could not locate repo root from " + AppContext.BaseDirectory);
    }
}
