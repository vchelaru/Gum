using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Validates the bundled Neon theme template. Mirrors <see cref="DarkProTemplateTests"/> -
/// see that class for the reasoning behind each check.
/// </summary>
public class NeonTemplateTests
{
    private static readonly string[] RetiredStandards = { "ColoredRectangle", "RoundedRectangle", "ColoredCircle" };
    private static readonly string[] LegacyColorChannels = { "Red", "Green", "Blue", "Alpha" };

    private static GumProjectSave LoadNeon()
    {
        StandardElementsManager.Self.Initialize();

        string neonDir = Path.Combine(FindRepoRoot(),
            "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", "Neon");
        ProjectLoadResult result = new ProjectLoader().Load(Path.Combine(neonDir, "GumProject.gumx"));

        result.Success.ShouldBeTrue();
        result.LoadErrors.ShouldBeEmpty();
        return result.Project!;
    }

    private static GumProjectSave LoadFormsTemplate()
    {
        StandardElementsManager.Self.Initialize();

        string formsTemplateDir = Path.Combine(FindRepoRoot(),
            "Tools", "Gum.ProjectServices", "Templates", "FormsTemplate");
        ProjectLoadResult result = new ProjectLoader().Load(Path.Combine(formsTemplateDir, "GumProject.gumx"));

        result.Success.ShouldBeTrue();
        result.LoadErrors.ShouldBeEmpty();
        return result.Project!;
    }

    [Fact]
    public void Load_SharedBehaviorReferences_ShouldResolveWithNoSourceFileMissing()
    {
        // Both themes link the same ~24 behaviors from Templates/FormsBehaviors/ via
        // BehaviorReference.SourcePath (#4070) instead of each keeping its own physical copy.
        GumProjectSave neon = LoadNeon();
        GumProjectSave formsTemplate = LoadFormsTemplate();

        neon.Behaviors.ShouldNotBeEmpty();
        formsTemplate.Behaviors.ShouldNotBeEmpty();
        neon.Behaviors.ShouldAllBe(b => !b.IsSourceFileMissing, customMessage:
            "every Neon behavior reference (shared or theme-local) must resolve to a real file");
        formsTemplate.Behaviors.ShouldAllBe(b => !b.IsSourceFileMissing, customMessage:
            "every FormsTemplate behavior reference (shared or theme-local) must resolve to a real file");
    }

    [Fact]
    public void Load_SharedButtonBehavior_ShouldUsePerThemeDefaultImplementationOverride()
    {
        // ButtonBehavior's Categories/FormsProperties/RequiredVariables are shared, but each
        // theme needs its own default-visual path - that's what DefaultImplementationOverride
        // supplies per BehaviorReference without writing a theme-specific value into the shared file.
        GumProjectSave neon = LoadNeon();
        GumProjectSave formsTemplate = LoadFormsTemplate();

        neon.Behaviors.Single(b => b.Name == "ButtonBehavior").DefaultImplementation
            .ShouldBe("Neon/Controls/Button");
        formsTemplate.Behaviors.Single(b => b.Name == "ButtonBehavior").DefaultImplementation
            .ShouldBe("Controls/ButtonStandard");
    }

    [Fact]
    public void Load_CircleInstances_ShouldNotCarryRadiusVariable()
    {
        // Circle sizes via Width/Height since #2947 — "Radius" was dropped from the standard.
        GumProjectSave project = LoadNeon();

        List<string> offenders = new();
        foreach (ElementSave element in project.AllElements)
        {
            foreach (StateSave state in element.AllStates)
            {
                foreach (VariableSave variable in state.Variables)
                {
                    if (variable.Name != null && GetLastNameSegment(variable.Name) == "Radius")
                    {
                        offenders.Add($"{element.Name} / {state.Name}: {variable.Name}");
                    }
                }
            }
        }

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Load_ShouldNotReferenceRetiredStandardFiles()
    {
        string standardsDir = Path.Combine(FindRepoRoot(),
            "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", "Neon", "Standards");

        foreach (string retired in RetiredStandards)
        {
            File.Exists(Path.Combine(standardsDir, retired + ".gutx"))
                .ShouldBeFalse($"{retired}.gutx should have been removed");
        }
    }

    [Fact]
    public void Load_ShouldNotUseRetiredBaseTypes()
    {
        GumProjectSave project = LoadNeon();

        List<string> offenders = new();
        foreach (ElementSave element in project.AllElements)
        {
            foreach (InstanceSave instance in element.Instances)
            {
                if (RetiredStandards.Contains(instance.BaseType))
                {
                    offenders.Add($"{element.Name}.{instance.Name} -> {instance.BaseType}");
                }
            }
        }

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void Load_StylesColorReferences_ShouldTargetFillChannels()
    {
        GumProjectSave project = LoadNeon();

        List<string> dangling = new();
        foreach (ElementSave element in project.AllElements)
        {
            foreach (StateSave state in element.AllStates)
            {
                foreach (VariableListSave variableList in state.VariableLists)
                {
                    if (variableList.GetRootName() != "VariableReferences")
                    {
                        continue;
                    }

                    foreach (string referenceString in variableList.ValueAsIList.Cast<string>())
                    {
                        int equalsIndex = referenceString.IndexOf('=');
                        if (equalsIndex < 0)
                        {
                            continue;
                        }

                        string right = referenceString.Substring(equalsIndex + 1).Trim();
                        if (!right.Contains("Styles."))
                        {
                            continue;
                        }

                        string channel = right.Substring(right.LastIndexOf('.') + 1);
                        if (LegacyColorChannels.Contains(channel))
                        {
                            dangling.Add($"{element.Name} / {state.Name}: {referenceString}");
                        }
                    }
                }
            }
        }

        dangling.ShouldBeEmpty();
    }

    private static readonly string[] ColorChannelSuffixes =
    {
        "FillRed", "FillGreen", "FillBlue",
        "StrokeRed", "StrokeGreen", "StrokeBlue",
        "Red", "Green", "Blue"
    };

    [Fact]
    public void AllColorVariables_ShouldBeStylesWired()
    {
        // The whole point of Components/Neon/Styles.gucx is that every control routes its
        // colors through it via VariableReferences instead of hardcoding values - in every
        // state, not just Default. A control can look correctly wired at a glance (Default state
        // references Styles) while every one of its categorized states (Enabled/Disabled/
        // Highlighted/Pushed/...) still hardcodes the same colors directly.
        GumProjectSave project = LoadNeon();

        List<string> offenders = new();
        foreach (ComponentSave component in project.Components)
        {
            if (component.Name == "Neon/Styles")
            {
                continue;
            }

            // ColorPicker's chrome is intentionally theme-neutral (a gray outline plus a
            // black/white contrast pair that must stay legible against any user-picked hue),
            // matching the code-only ColorPickerVisual, which hardcodes the same colors rather
            // than reading them from a theme's Colors class.
            if (component.Name == "Neon/Controls/ColorPicker")
            {
                continue;
            }

            foreach (StateSave state in component.AllStates)
            {
                HashSet<string> wired = new();
                foreach (VariableListSave variableList in state.VariableLists)
                {
                    if (variableList.GetRootName() != "VariableReferences")
                    {
                        continue;
                    }

                    string? sourceObject = variableList.SourceObject;
                    foreach (string referenceString in variableList.ValueAsIList.Cast<string>())
                    {
                        int equalsIndex = referenceString.IndexOf('=');
                        if (equalsIndex < 0)
                        {
                            continue;
                        }

                        string left = referenceString.Substring(0, equalsIndex).Trim();

                        // A collapsed composite reference (e.g. "FillColor = Other.FillColor", #4153)
                        // wires all 3 of its channel scalars at apply time (GumRuntime.ElementSaveExtensions),
                        // not a literal "FillColor" scalar - so it must mark Red/Green/Blue as wired here too.
                        if (left.EndsWith("Color", StringComparison.Ordinal))
                        {
                            string colorPrefix = left.Substring(0, left.Length - "Color".Length);
                            foreach (string channelToken in new[] { "Red", "Green", "Blue" })
                            {
                                string channelLeft = colorPrefix + channelToken;
                                wired.Add(string.IsNullOrEmpty(sourceObject) ? channelLeft : $"{sourceObject}.{channelLeft}");
                            }
                        }
                        else
                        {
                            string effectiveLeft = string.IsNullOrEmpty(sourceObject) ? left : $"{sourceObject}.{left}";
                            wired.Add(effectiveLeft);
                        }
                    }
                }

                Dictionary<string, VariableSave> variablesByName = state.Variables
                    .Where(v => v.Name != null)
                    .GroupBy(v => v.Name!)
                    .ToDictionary(g => g.Key, g => g.First());

                foreach (VariableSave variable in state.Variables)
                {
                    if (variable.SetsValue != true || variable.Name == null)
                    {
                        continue;
                    }

                    string channel = GetLastNameSegment(variable.Name);
                    if (!ColorChannelSuffixes.Contains(channel))
                    {
                        continue;
                    }

                    if (wired.Contains(variable.Name))
                    {
                        continue;
                    }

                    // A fully transparent fill's RGB is never visible, so it doesn't need to
                    // route through Styles - it's not "touching color" in any observable way.
                    if (channel.StartsWith("Fill", StringComparison.Ordinal))
                    {
                        string prefix = variable.Name.Substring(0, variable.Name.Length - channel.Length - 1);
                        if (variablesByName.TryGetValue($"{prefix}.FillAlpha", out VariableSave? alphaVariable)
                            && alphaVariable.SetsValue == true
                            && alphaVariable.Value != null
                            && Convert.ToInt32(alphaVariable.Value) == 0)
                        {
                            continue;
                        }
                    }

                    offenders.Add($"{component.Name} [{state.Name}]: {variable.Name}");
                }
            }
        }

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void AllPhysicalComponentFiles_ShouldBeNeonNamespaced()
    {
        // FormsFileService.GetSourceDestinations copies every .gucx file it finds on disk
        // under the theme folder, regardless of whether that file is registered as a
        // ComponentReference in the theme's own GumProject.gumx. A file whose own <Name> tag
        // was left unrenamed during the Neon/ namespacing pass still gets imported under
        // its stale name, colliding with (or duplicating) content from every other theme's
        // import.
        string neonDir = Path.Combine(FindRepoRoot(),
            "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", "Neon");

        List<string> componentFiles = Directory
            .GetFiles(Path.Combine(neonDir, "Components"), "*.gucx", SearchOption.AllDirectories)
            .ToList();

        List<string> offenders = new();
        foreach (string file in componentFiles)
        {
            string? name = XDocument.Load(file).Root?.Element("Name")?.Value;
            if (name != null && !name.StartsWith("Neon/", StringComparison.Ordinal))
            {
                offenders.Add($"{Path.GetFileName(file)}: <Name>{name}</Name>");
            }
        }

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void AllBehaviorFiles_ShouldBeSharedAndUnnamespaced()
    {
        // Unlike Components (which define a theme's look), Behaviors declare the generic
        // category-state/FormsProperty contract every theme's visuals plug into - the same
        // content every project and theme needs. They live in the single shared
        // Templates/FormsBehaviors/ folder, linked into each theme's GumProject.gumx via
        // BehaviorReference.SourcePath (#4070) rather than duplicated per theme, so this
        // folder is normally empty. This test guards that a future Neon-only behavior
        // (if one is ever added here instead of shared) stays unnamespaced.
        string behaviorsDir = Path.Combine(FindRepoRoot(),
            "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", "Neon", "Behaviors");

        // Git doesn't track empty directories, so this folder may not physically exist at all
        // once every behavior it used to hold has been shared out to Templates/FormsBehaviors/.
        if (!Directory.Exists(behaviorsDir))
        {
            return;
        }

        List<string> topLevelBehaviorFiles = Directory
            .GetFiles(behaviorsDir, "*.behx", SearchOption.TopDirectoryOnly)
            .ToList();
        List<string> allBehaviorFiles = Directory
            .GetFiles(behaviorsDir, "*.behx", SearchOption.AllDirectories)
            .ToList();

        allBehaviorFiles.ShouldBe(topLevelBehaviorFiles, ignoreOrder: true,
            customMessage: "Behavior files must live directly under Behaviors/, not in a per-theme subfolder.");

        List<string> offenders = new();
        foreach (string file in topLevelBehaviorFiles)
        {
            string? name = XDocument.Load(file).Root?.Element("Name")?.Value;
            if (name != null && name.StartsWith("Neon/", StringComparison.Ordinal))
            {
                offenders.Add($"{Path.GetFileName(file)}: <Name>{name}</Name>");
            }
        }

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void StylesPalette_ShouldExactlyMatchCodeOnlyNeonColors()
    {
        // Components/Neon/Styles.gucx exists so tool content can reference one set of
        // color/text tokens instead of hardcoding values. Its swatch names must match
        // Themes/Gum.Themes.Neon.MonoGame/NeonStyling.cs's NeonColors/NeonText properties
        // exactly, and carry no swatch beyond what that class defines - otherwise the tool
        // content silently drifts from the theme it's supposed to mirror. Excluded by design
        // (computed/alias properties with no distinct settable value of their own):
        // TextPrimary/TextMuted/Primary (guardrail aliases), ScrollThumb/HoverRow/SelectedRow
        // (aliases onto Muted/Surface2/AccentDim).
        string[] expectedColorSwatches =
        {
            "Accent", "AccentDim", "Background", "Border", "BorderHover",
            "ButtonHoverFill", "ButtonPushedFill", "CheckedFill", "CheckedHoverFill", "CheckedPushedFill",
            "CloseFill", "Danger", "Disabled", "DisabledBorder", "Divider",
            "FocusRing", "Glow", "GlowMedium", "GlowStrong", "GlowSubtle",
            "Muted", "Placeholder", "ScrollThumbHover", "SliderThumbShadow", "Surface1",
            "Surface2", "Text", "White", "WindowShadow"
        };
        string[] expectedTextStyles = { "Normal", "Strong" };

        GumProjectSave project = LoadNeon();
        ComponentSave styles = project.Components.Single(c => c.Name == "Neon/Styles");

        List<string> actualColorSwatches = styles.Instances
            .Where(i => i.BaseType is "Rectangle")
            .Select(i => i.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
        List<string> actualTextStyles = styles.Instances
            .Where(i => i.BaseType is "Text")
            .Select(i => i.Name)
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();

        actualColorSwatches.ShouldBe(expectedColorSwatches.OrderBy(n => n, StringComparer.Ordinal).ToList());
        actualTextStyles.ShouldBe(expectedTextStyles.OrderBy(n => n, StringComparer.Ordinal).ToList());
    }

    private static string GetLastNameSegment(string variableName)
    {
        int lastDotIndex = variableName.LastIndexOf('.');
        return lastDotIndex < 0 ? variableName : variableName.Substring(lastDotIndex + 1);
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
