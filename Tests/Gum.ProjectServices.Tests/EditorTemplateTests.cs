using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ProjectServices;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Validates the bundled Editor theme template, the tool-content counterpart of
/// <c>Themes/Gum.Themes.Editor.*</c>. It starts as a copy of the Standard template (the Editor code
/// theme leaves most controls on the stock V3 look) with the Editor palette, plus the outlines and
/// swatch swaps the code theme applies to Button, CheckBox, ComboBox, TextBox, ListBox,
/// ListBoxItem, ScrollBar, ScrollViewer, Slider and Expander.
/// </summary>
public class EditorTemplateTests
{
    private static readonly string[] RestyledControls =
    {
        "Button", "CheckBox", "ComboBox", "TextBox", "ListBox", "ListBoxItem",
        "ScrollBar", "ScrollViewer", "Slider", "Expander"
    };

    private static GumProjectSave LoadEditor()
    {
        StandardElementsManager.Self.Initialize();

        string editorDir = Path.Combine(FindRepoRoot(),
            "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", "Editor");
        ProjectLoadResult result = new ProjectLoader().Load(Path.Combine(editorDir, "GumProject.gumx"));

        result.Success.ShouldBeTrue();
        result.LoadErrors.ShouldBeEmpty();
        return result.Project!;
    }

    private static ComponentSave GetControl(GumProjectSave project, string name) =>
        project.Components.Single(c => c.Name == "Editor/Controls/" + name);

    private static string? GetStyleReference(StateSave state, string instanceName, string lhs)
    {
        foreach (VariableListSave variableList in state.VariableLists)
        {
            if (variableList.GetRootName() != "VariableReferences" || variableList.SourceObject != instanceName)
            {
                continue;
            }

            foreach (string referenceString in variableList.ValueAsIList.Cast<string>())
            {
                if (referenceString.StartsWith(lhs + " =", StringComparison.Ordinal))
                {
                    return referenceString.Substring(referenceString.IndexOf('=') + 1).Trim();
                }
            }
        }
        return null;
    }

    private static bool? GetBool(StateSave state, string name) =>
        state.Variables.FirstOrDefault(v => v.Name == name && v.SetsValue)?.Value as bool?;

    private static IEnumerable<StateSave> CategoryStates(ComponentSave component, string categoryName) =>
        component.Categories.Single(c => c.Name == categoryName).States;

    [Fact]
    public void Load_EveryElementName_ShouldBeEditorPrefixed()
    {
        GumProjectSave project = LoadEditor();

        project.Components.Select(c => c.Name).Concat(project.Screens.Select(s => s.Name))
            .Where(name => !name.StartsWith("Editor/", StringComparison.Ordinal))
            .ShouldBeEmpty();
    }

    [Fact]
    public void Load_SharedBehaviorOverrides_ShouldPointAtExistingEditorComponents()
    {
        // A shared behavior's own DefaultImplementation names the Standard component; each theme
        // supplies its own through DefaultImplementationOverride.
        GumProjectSave project = LoadEditor();
        HashSet<string> componentNames = project.Components.Select(c => c.Name).ToHashSet();

        List<BehaviorReference> overrides = project.BehaviorReferences
            .Where(behavior => behavior.DefaultImplementationOverride != null)
            .ToList();
        overrides.ShouldNotBeEmpty();
        overrides
            .Where(behavior => !componentNames.Contains(behavior.DefaultImplementationOverride!))
            .Select(behavior => $"{behavior.Name} -> {behavior.DefaultImplementationOverride}")
            .ShouldBeEmpty();
    }

    [Fact]
    public void StylesPalette_ShouldMatchCodeOnlyEditorColors()
    {
        // Mirrors Themes/Gum.Themes.Editor.MonoGame/EditorStyling.cs's EditorColors by name and value.
        // PrimaryDark/PrimaryLight are the V3 shading of Primary (Adjust -15 / +15), baked in because
        // static content can't compute them. The Standard swatches the code theme leaves on their V3
        // defaults (Black ... Danger) are unchanged.
        Dictionary<string, (int R, int G, int B)> expected = new()
        {
            ["TextPrimary"] = (180, 180, 180),
            ["TextMuted"] = (88, 88, 88),
            ["Primary"] = (60, 60, 60),
            ["PrimaryDark"] = (51, 51, 51),
            ["PrimaryLight"] = (89, 89, 89),
            ["Accent"] = (192, 222, 255),
            ["BorderHover"] = (150, 150, 150),
            ["BorderPushed"] = (255, 255, 255),
            ["Selection"] = (0, 92, 128),
            ["PanelBackground"] = (27, 27, 27),
            ["RecessedBackground"] = (10, 10, 10),
        };

        GumProjectSave project = LoadEditor();
        StateSave styles = project.Components.Single(c => c.Name == "Editor/Styles").DefaultState!;

        List<string> swatches = project.Components.Single(c => c.Name == "Editor/Styles").Instances
            .Where(i => i.BaseType == "Rectangle")
            .Select(i => i.Name)
            .ToList();
        swatches.ShouldBe(new[]
        {
            "Black", "DarkGray", "Gray", "LightGray", "White", "PrimaryDark", "Primary", "PrimaryLight",
            "Success", "Warning", "Danger", "Accent", "TextPrimary", "TextMuted", "BorderHover",
            "BorderPushed", "Selection", "PanelBackground", "RecessedBackground"
        }, ignoreOrder: true);

        foreach ((string name, (int r, int g, int b)) in expected)
        {
            int Channel(string channel) => Convert.ToInt32(styles.Variables.Single(v => v.Name == $"{name}.{channel}").Value);
            (Channel("FillRed"), Channel("FillGreen"), Channel("FillBlue")).ShouldBe((r, g, b), name);
        }
    }

    [Fact]
    public void Load_TextInstances_ShouldNotReferenceTheWhiteSwatch()
    {
        // The code theme sets Colors.TextPrimary, which every stock V3 visual's text reads; the
        // Standard template spells that as Styles.White.
        GumProjectSave project = LoadEditor();

        List<string> offenders = new();
        foreach (ElementSave element in project.Components.Cast<ElementSave>().Concat(project.Screens))
        {
            Dictionary<string, string> types = element.Instances.ToDictionary(i => i.Name, i => i.BaseType);
            foreach (StateSave state in element.AllStates)
            {
                foreach (VariableListSave variableList in state.VariableLists.Where(l => l.GetRootName() == "VariableReferences"))
                {
                    string? source = variableList.SourceObject;
                    bool isText = string.IsNullOrEmpty(source) ? element.BaseType == "Text" : types.GetValueOrDefault(source) == "Text";
                    if (isText && variableList.ValueAsIList.Cast<string>().Any(s => s.StartsWith("Color =") && s.EndsWith("Styles.White.FillColor")))
                    {
                        offenders.Add($"{element.Name} [{state.Name}]: {source}");
                    }
                }
            }
        }

        offenders.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("Button", "ButtonCategory")]
    [InlineData("CheckBox", "CheckBoxCategory")]
    [InlineData("ComboBox", "ComboBoxCategory")]
    public void Outline_ShouldShowOnlyWhileHighlightedOrPushed(string control, string category)
    {
        // ButtonVisual / CheckBoxVisual / ComboBoxVisual add a rectangle outline: BorderHover while
        // highlighted, BorderPushed while pushed, hidden otherwise.
        GumProjectSave project = LoadEditor();
        ComponentSave component = GetControl(project, control);

        List<string> offenders = new();
        foreach (StateSave state in CategoryStates(component, category))
        {
            string? expectedStroke = state.Name.Contains("Pushed") ? "BorderPushed"
                : state.Name.Contains("Highlighted") ? "BorderHover"
                : null;
            bool? visible = GetBool(state, "Outline.Visible");
            string? stroke = GetStyleReference(state, "Outline", "StrokeColor");

            if (visible != (expectedStroke != null)
                || (expectedStroke != null && stroke != $"Components/Editor/Styles.{expectedStroke}.FillColor"))
            {
                offenders.Add($"{control} [{state.Name}]: visible={visible}, stroke={stroke}");
            }
        }

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void CheckBoxOutline_ShouldSitInsideTheBox()
    {
        GumProjectSave project = LoadEditor();
        ComponentSave checkBox = GetControl(project, "CheckBox");

        checkBox.DefaultState!.Variables.Single(v => v.Name == "Outline.Parent").Value.ShouldBe("CheckBoxBackground");
    }

    [Fact]
    public void TextBoxOutline_ShouldBeAccentWhileFocusedAndBorderHoverWhileHighlighted()
    {
        GumProjectSave project = LoadEditor();
        ComponentSave textBox = GetControl(project, "TextBox");
        Dictionary<string, StateSave> states = CategoryStates(textBox, "TextBoxCategory").ToDictionary(s => s.Name);

        GetBool(states["Enabled"], "Outline.Visible").ShouldBe(false);
        GetBool(states["Disabled"], "Outline.Visible").ShouldBe(false);
        GetBool(states["Highlighted"], "Outline.Visible").ShouldBe(true);
        GetStyleReference(states["Highlighted"], "Outline", "StrokeColor").ShouldBe("Components/Editor/Styles.BorderHover.FillColor");
        GetBool(states["Focused"], "Outline.Visible").ShouldBe(true);
        GetStyleReference(states["Focused"], "Outline", "StrokeColor").ShouldBe("Components/Editor/Styles.Accent.FillColor");
    }

    [Theory]
    [InlineData("Button")]
    [InlineData("ComboBox")]
    [InlineData("TextBox")]
    public void FocusedIndicator_ShouldNeverShow_OnControlsTheEditorOutlinesInstead(string control)
    {
        // The code theme detaches the stock focus indicator on these three.
        GumProjectSave project = LoadEditor();
        ComponentSave component = GetControl(project, control);

        List<string> offenders = new();
        foreach (StateSave state in component.AllStates)
        {
            if (GetBool(state, "FocusedIndicator.Visible") == true)
            {
                offenders.Add($"{control} [{state.Name}]");
            }
        }

        offenders.ShouldBeEmpty();
    }

    [Fact]
    public void TextBox_ShouldUseRecessedBackgroundMutedPlaceholderAndAdditiveSelection()
    {
        GumProjectSave project = LoadEditor();
        ComponentSave textBox = GetControl(project, "TextBox");

        foreach (StateSave state in CategoryStates(textBox, "TextBoxCategory"))
        {
            GetStyleReference(state, "Background", "Color").ShouldBe("Components/Editor/Styles.RecessedBackground.FillColor", state.Name);
            GetStyleReference(state, "PlaceholderTextInstance", "Color").ShouldBe("Components/Editor/Styles.TextMuted.FillColor", state.Name);
        }

        StateSave defaultState = textBox.DefaultState!;
        GetStyleReference(defaultState, "SelectionInstance", "Color").ShouldBe("Components/Editor/Styles.Selection.FillColor");
        GetStyleReference(defaultState, "CaretInstance", "Color").ShouldBe("Components/Editor/Styles.Accent.FillColor");
        defaultState.Variables.Single(v => v.Name == "SelectionInstance.Blend").Value.ShouldBe(Gum.RenderingLibrary.Blend.Additive);
        // Drawn after its siblings in the clip container so the selection tints the text it covers.
        List<string> order = textBox.Instances.Select(i => i.Name).ToList();
        foreach (string sibling in new[] { "TextInstance", "PlaceholderTextInstance", "CaretInstance" })
        {
            order.IndexOf("SelectionInstance").ShouldBeGreaterThan(order.IndexOf(sibling), sibling);
        }
    }

    [Fact]
    public void ListBox_ShouldUsePanelBackgroundWithAPrimaryBorder()
    {
        GumProjectSave project = LoadEditor();
        ComponentSave listBox = GetControl(project, "ListBox");

        GetStyleReference(listBox.DefaultState!, "Background", "Color").ShouldBe("Components/Editor/Styles.PanelBackground.FillColor");
        GetStyleReference(listBox.DefaultState!, "Outline", "StrokeColor").ShouldBe("Components/Editor/Styles.Primary.FillColor");
        listBox.DefaultState!.Variables.Single(v => v.Name == "Outline.Parent").Value.ShouldBe("Background");
        GetBool(listBox.DefaultState!, "Outline.Visible").ShouldBe(true);
    }

    [Fact]
    public void ListBoxItem_ShouldHighlightGrayAndSelectWithSelectionColor()
    {
        GumProjectSave project = LoadEditor();
        ComponentSave item = GetControl(project, "ListBoxItem");
        Dictionary<string, StateSave> states = CategoryStates(item, "ListBoxItemCategory").ToDictionary(s => s.Name);

        GetStyleReference(states["Selected"], "Background", "Color").ShouldBe("Components/Editor/Styles.Selection.FillColor");
        GetStyleReference(states["Highlighted"], "Background", "Color").ShouldBeNull();
        foreach (string channel in new[] { "Red", "Green", "Blue" })
        {
            states["Highlighted"].Variables.Single(v => v.Name == $"Background.{channel}").Value.ShouldBe(70);
        }
        GetBool(states["Highlighted"], "Outline.Visible").ShouldBe(true);
        GetBool(states["Enabled"], "Outline.Visible").ShouldBe(false);
        GetBool(states["Selected"], "Outline.Visible").ShouldBe(false);
    }

    [Fact]
    public void ScrollBarAndScrollViewer_ShouldUseRecessedAndPanelBackgrounds()
    {
        GumProjectSave project = LoadEditor();

        GetStyleReference(GetControl(project, "ScrollBar").DefaultState!, "TrackBackground", "Color")
            .ShouldBe("Components/Editor/Styles.RecessedBackground.FillColor");
        GetStyleReference(GetControl(project, "ScrollViewer").DefaultState!, "Background", "Color")
            .ShouldBe("Components/Editor/Styles.PanelBackground.FillColor");
    }

    [Fact]
    public void Slider_ShouldUseTwelvePixelThumb()
    {
        GumProjectSave project = LoadEditor();
        StateSave slider = GetControl(project, "Slider").DefaultState!;

        slider.Variables.Single(v => v.Name == "ThumbInstance.Width").Value.ShouldBe(12f);
        slider.Variables.Single(v => v.Name == "TrackInstance.Width").Value.ShouldBe(-12f);
        slider.Variables.Single(v => v.Name == "FocusedIndicator.Width").Value.ShouldBe(-12f);
    }

    [Fact]
    public void RestyledControls_ShouldWireEveryColorToStyles_ExceptTheTwoSingleUseLiterals()
    {
        // Same check the other themes run over the whole theme: a control can look wired (Default state
        // references Styles) while its categorized states still hardcode colors. Scoped to the
        // controls the Editor code theme restyles; the rest keep the Standard template's own literals.
        // The code theme keeps two colors as single-use literals rather than palette entries (the
        // ListBoxItem highlight and the Expander header), so the content does too.
        GumProjectSave project = LoadEditor();
        string[] channelSuffixes = { "FillRed", "FillGreen", "FillBlue", "StrokeRed", "StrokeGreen", "StrokeBlue", "Red", "Green", "Blue" };

        List<string> offenders = new();
        foreach (string control in RestyledControls)
        {
            ComponentSave component = GetControl(project, control);
            foreach (StateSave state in component.AllStates)
            {
                HashSet<string> wired = new();
                foreach (VariableListSave variableList in state.VariableLists.Where(l => l.GetRootName() == "VariableReferences"))
                {
                    string prefix = string.IsNullOrEmpty(variableList.SourceObject) ? "" : variableList.SourceObject + ".";
                    foreach (string reference in variableList.ValueAsIList.Cast<string>())
                    {
                        string left = reference.Substring(0, reference.IndexOf('=')).Trim();
                        if (left.EndsWith("Color", StringComparison.Ordinal))
                        {
                            string colorPrefix = left.Substring(0, left.Length - "Color".Length);
                            foreach (string channel in new[] { "Red", "Green", "Blue" })
                            {
                                wired.Add(prefix + colorPrefix + channel);
                            }
                        }
                        else
                        {
                            wired.Add(prefix + left);
                        }
                    }
                }

                foreach (VariableSave variable in state.Variables.Where(v => v.SetsValue && v.Name != null))
                {
                    string channel = variable.Name!.Substring(variable.Name.LastIndexOf('.') + 1);
                    if (!channelSuffixes.Contains(channel) || wired.Contains(variable.Name))
                    {
                        continue;
                    }

                    bool isListBoxItemHighlight = control == "ListBoxItem" && state.Name == "Highlighted" && variable.Name.StartsWith("Background.");
                    bool isExpanderHeader = control == "Expander" && variable.Name.StartsWith("HeaderBackground.Fill");
                    if (!isListBoxItemHighlight && !isExpanderHeader)
                    {
                        offenders.Add($"{control} [{state.Name}]: {variable.Name}");
                    }
                }
            }
        }

        offenders.ShouldBeEmpty();
    }

    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates")))
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
