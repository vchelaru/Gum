using System;
using System.Collections.Generic;
using System.Linq;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using Shouldly;
using Xunit;
using V3Styling = Gum.Forms.DefaultVisuals.V3.Styling;
using NineSlice = global::RenderingLibrary.Graphics.NineSlice;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// The Background nine-slice of a tool template control against the code-only visual for the same control:
/// same tint and same sprite-sheet cell means the same fill and the same border pixels (#6740).
/// </summary>
[Collection(SharedGame.CollectionName)]
public class ToolInputBackgroundTests : BaseTestClass
{
    private sealed record BackgroundLook(byte R, byte G, byte B, int TextureLeft, int TextureTop, int TextureWidth, int TextureHeight);

    public static IEnumerable<object[]> MatchingControls()
    {
        yield return new object[] { "FormsTemplate", "Controls/ScrollViewer", null!, (Func<FrameworkElement>)(() => new ScrollViewer()) };
        yield return new object[] { "FormsTemplate", "Controls/ItemsControl", null!, (Func<FrameworkElement>)(() => new ItemsControl()) };
        yield return new object[] { "FormsThemes/Editor", "Editor/Controls/ScrollViewer", (Action)Gum.Themes.Editor.EditorTheme.Apply, (Func<FrameworkElement>)(() => new ScrollViewer()) };
        yield return new object[] { "FormsThemes/Editor", "Editor/Controls/ItemsControl", (Action)Gum.Themes.Editor.EditorTheme.Apply, (Func<FrameworkElement>)(() => new ItemsControl()) };
        yield return new object[] { "FormsThemes/Editor", "Editor/Controls/ListBox", (Action)Gum.Themes.Editor.EditorTheme.Apply, (Func<FrameworkElement>)(() => new ListBox()) };
    }

    [Theory]
    [MemberData(nameof(MatchingControls))]
    public void ToolBackground_MatchesCodeOnly(string templateFolder, string componentName, Action? applyTheme, Func<FrameworkElement> createCodeOnly)
    {
        BackgroundLook codeOnly = MeasureCodeOnly(applyTheme, createCodeOnly);
        BackgroundLook tool = MeasureTool(templateFolder, componentName);

        tool.ShouldBe(codeOnly, componentName);
    }

    private static readonly string[] ListBoxStates = { "Enabled", "Disabled", "Focused", "DisabledFocused" };
    private static readonly string[] ComboBoxStates = { "Enabled", "Disabled", "DisabledFocused", "Focused", "Highlighted", "HighlightedFocused", "Pushed" };
    private static readonly string[] TextBoxStates = { "Enabled", "Disabled", "Highlighted", "Focused" };

    public static IEnumerable<object[]> MatchingControlStates()
    {
        foreach ((string folder, string prefix, Action? theme) in new (string, string, Action?)[]
        {
            ("FormsTemplate", "Controls/", null),
            ("FormsThemes/Editor", "Editor/Controls/", Gum.Themes.Editor.EditorTheme.Apply),
        })
        {
            yield return new object[] { folder, prefix + "ListBox", "ListBoxCategoryState", theme!, (Func<FrameworkElement>)(() => new ListBox()) };
            yield return new object[] { folder, prefix + "ComboBox", "ComboBoxCategoryState", theme!, (Func<FrameworkElement>)(() => new ComboBox()) };
            yield return new object[] { folder, prefix + "TextBox", "TextBoxCategoryState", theme!, (Func<FrameworkElement>)(() => new TextBox()) };
        }
    }

    private static string[] StatesOf(string categoryState) => categoryState switch
    {
        "ListBoxCategoryState" => ListBoxStates,
        "ComboBoxCategoryState" => ComboBoxStates,
        _ => TextBoxStates,
    };

    // Every state the control can reach. The V3 hover and disabled fills are a lighten/darken of the resting fill,
    // baked into *Light/*Dark swatches because static content can't compute them (#6746).
    [Theory]
    [MemberData(nameof(MatchingControlStates))]
    public void ToolBackgroundInState_MatchesCodeOnly(string templateFolder, string componentName, string categoryState,
        Action? applyTheme, Func<FrameworkElement> createCodeOnly)
    {
        string[] states = StatesOf(categoryState);
        // Coming from another state first: a state that leaves the fill unset would keep the previous state's fill.
        void EnterState(FrameworkElement control, string stateName)
        {
            control.Visual.SetProperty(categoryState, stateName == "Disabled" ? "Highlighted" : "Disabled");
            control.Visual.SetProperty(categoryState, stateName);
        }

        List<BackgroundLook> codeOnly = MeasureCodeOnly(applyTheme, createCodeOnly, EachState(states, EnterState, Measure));
        List<BackgroundLook> tool = MeasureTool(templateFolder, componentName, EachState(states, EnterState, Measure));

        for (int i = 0; i < states.Length; i++)
        {
            tool[i].ShouldBe(codeOnly[i], $"{componentName} {states[i]}");
        }
    }

    // One instance steps through every state, so each case loads its project and builds its control once.
    private static Func<FrameworkElement, List<T>> EachState<T>(IEnumerable<string> states, Action<FrameworkElement, string> enter,
        Func<FrameworkElement, T> measure) =>
        control => states.Select(state => { enter(control, state); return measure(control); }).ToList();

    // Visual review only: with GUM_SCREENSHOT_DIR set, writes 6746-<codeonly|tool>.png, one row per control
    // (ListBox, ComboBox, TextBox) and one column per state, the Standard tool template beside V3.
    [Fact]
    public void StandardInputStates_Screenshot()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GUM_SCREENSHOT_DIR")))
        {
            return;
        }

        (string Component, string CategoryState, string[] States, Func<FrameworkElement> Create)[] rows =
        {
            ("Controls/ListBox", "ListBoxCategoryState", ListBoxStates, () => new ListBox()),
            ("Controls/ComboBox", "ComboBoxCategoryState", ComboBoxStates, () => new ComboBox()),
            ("Controls/TextBox", "TextBoxCategoryState", TextBoxStates, () => new TextBox()),
        };

        void Render(Func<int, FrameworkElement> createRow, string pngName)
        {
            List<GraphicalUiElement> added = new();
            for (int row = 0; row < rows.Length; row++)
            {
                for (int column = 0; column < rows[row].States.Length; column++)
                {
                    FrameworkElement control = createRow(row);
                    control.Visual.SetProperty(rows[row].CategoryState, rows[row].States[column]);
                    control.Visual.X = 8 + column * 96;
                    control.Visual.Y = 8 + row * 72;
                    control.Visual.Width = 88;
                    control.Visual.Height = 64;
                    control.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
                    control.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
                    control.Visual.AddToManagers(SystemManagers.Default, null);
                    added.Add(control.Visual);
                }
            }
            MenuFocusRenderTests.Capture(SystemManagers.Default.Renderer.GraphicsDevice!, Microsoft.Xna.Framework.Color.Black, pngName, 688, 232);
            foreach (GraphicalUiElement visual in added)
            {
                visual.RemoveFromManagers();
            }
        }

        MenuFocusRenderTests.WithToolProject("FormsTemplate", mutateProject: null, () =>
            Render(row => (FrameworkElement)((InteractiveGue)ObjectFinder.Self.GetComponent(rows[row].Component)!
                .ToGraphicalUiElement(SystemManagers.Default, addToManagers: false)).FormsControlAsObject!, "6746-tool.png"));

        MeasureCodeOnly(applyTheme: () => Render(row => rows[row].Create(), "6746-codeonly.png"), create: () => new ListBox());
    }

    public static IEnumerable<object[]> IndicatorControlStates()
    {
        foreach ((string folder, string prefix, Action? theme) in new (string, string, Action?)[]
        {
            ("FormsTemplate", "Controls/", null),
            ("FormsThemes/Editor", "Editor/Controls/", Gum.Themes.Editor.EditorTheme.Apply),
        })
        {
            yield return new object[] { folder, prefix + "ComboBox", "ComboBoxCategoryState", theme!, (Func<FrameworkElement>)(() => new ComboBox()) };
            yield return new object[] { folder, prefix + "TextBox", "TextBoxCategoryState", theme!, (Func<FrameworkElement>)(() => new TextBox()) };
        }
    }

    // The ComboBox dropdown arrow and focus underline, and the TextBox placeholder, per state against V3 (#6754).
    [Theory]
    [MemberData(nameof(IndicatorControlStates))]
    public void ToolIndicatorsInState_MatchCodeOnly(string templateFolder, string componentName, string categoryState,
        Action? applyTheme, Func<FrameworkElement> createCodeOnly)
    {
        string[] states = StatesOf(categoryState);
        // Coming from another state first: a state that leaves a value unset would keep the previous state's value.
        void EnterState(FrameworkElement control, string stateName)
        {
            string previousState = stateName == "Pushed" ? "Disabled" : "Pushed";
            if (categoryState == "TextBoxCategoryState")
            {
                previousState = stateName == "Disabled" ? "Highlighted" : "Disabled";
            }
            control.Visual.SetProperty(categoryState, previousState);
            control.Visual.SetProperty(categoryState, stateName);
        }

        List<IndicatorLook> codeOnly = MeasureCodeOnly(applyTheme, createCodeOnly, EachState(states, EnterState, MeasureIndicators));
        List<IndicatorLook> tool = MeasureTool(templateFolder, componentName, EachState(states, EnterState, MeasureIndicators));

        for (int i = 0; i < states.Length; i++)
        {
            tool[i].ShouldBe(codeOnly[i], $"{componentName} {states[i]}");
        }
    }

    public static IEnumerable<object[]> ComboBoxArrowCases()
    {
        (string Folder, string Prefix, Action? Theme)[] themes =
        {
            ("FormsTemplate", "Controls/", null),
            ("FormsThemes/Editor", "Editor/Controls/", Gum.Themes.Editor.EditorTheme.Apply),
        };
        // Narrow, V3 default, wide, tall.
        const string sizes = "64x24,256x24,480x24,200x64";
        foreach ((string folder, string prefix, Action? theme) in themes)
        {
            yield return new object[] { folder, prefix + "ComboBox", theme!, sizes };
        }
        // The other themes draw a text glyph; checked at the V3 default size only, to keep the run short.
        foreach ((string name, Action theme) in new (string, Action)[]
        {
            ("Bubblegum", Gum.Themes.Bubblegum.BubblegumTheme.Apply),
            ("DarkPro", Gum.Themes.DarkPro.DarkProTheme.Apply),
            ("ForestGlade", Gum.Themes.ForestGlade.ForestGladeTheme.Apply),
            ("Hazard", Gum.Themes.Hazard.HazardTheme.Apply),
            ("Meadow", Gum.Themes.Meadow.MeadowTheme.Apply),
            ("Neon", Gum.Themes.Neon.NeonTheme.Apply),
            ("Retro95", Gum.Themes.Retro95.Retro95Theme.Apply),
        })
        {
            yield return new object[] { "FormsThemes/" + name, name + "/Controls/ComboBox", theme, "256x24" };
        }
    }

    // The dropdown arrow's drawn rectangle (position, size and rotation) matches V3 at several sizes, in every state (#6763).
    [Theory]
    [MemberData(nameof(ComboBoxArrowCases))]
    public void ToolComboBoxArrowRect_MatchesCodeOnly(string templateFolder, string componentName, Action? applyTheme, string sizes)
    {
        // Each case is "<width>x<height>|<state>"; one instance is resized and stepped through every state.
        List<string> cases = sizes.Split(',').SelectMany(size => ComboBoxStates.Select(state => size + "|" + state)).ToList();
        void Arrange(FrameworkElement control, string testCase)
        {
            string[] parts = testCase.Split('|');
            string[] size = parts[0].Split('x');
            control.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            control.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            control.Visual.Width = float.Parse(size[0], System.Globalization.CultureInfo.InvariantCulture);
            control.Visual.Height = float.Parse(size[1], System.Globalization.CultureInfo.InvariantCulture);
            control.Visual.SetProperty("ComboBoxCategoryState", parts[1] == "Pushed" ? "Disabled" : "Pushed");
            control.Visual.SetProperty("ComboBoxCategoryState", parts[1]);
            control.Visual.UpdateLayout();
        }

        List<string> codeOnly = MeasureCodeOnly(applyTheme, () => new ComboBox(), EachState(cases, Arrange, MeasureArrowRect));
        List<string> tool = MeasureTool(templateFolder, componentName, EachState(cases, Arrange, MeasureArrowRect));

        for (int i = 0; i < cases.Count; i++)
        {
            tool[i].ShouldBe(codeOnly[i], $"{componentName} {cases[i]}");
        }
    }

    // Visual review only: with GUM_SCREENSHOT_DIR set, writes 6763-<standard|editor>-<tool|codeonly>.png, one ComboBox
    // per arrow test size, the tool template beside its code-only visual.
    [Fact]
    public void ComboBoxArrowSizes_Screenshot()
    {
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("GUM_SCREENSHOT_DIR")))
        {
            return;
        }

        (float Width, float Height)[] sizes = { (64, 24), (256, 24), (480, 24), (200, 64) };

        void Render(Func<FrameworkElement> create, string pngName)
        {
            List<GraphicalUiElement> added = new();
            float y = 8;
            foreach ((float width, float height) in sizes)
            {
                FrameworkElement control = create();
                control.Visual.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
                control.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
                control.Visual.Width = width;
                control.Visual.Height = height;
                control.Visual.X = 8;
                control.Visual.Y = y;
                control.Visual.AddToManagers(SystemManagers.Default, null);
                added.Add(control.Visual);
                y += height + 12;
            }
            MenuFocusRenderTests.Capture(SystemManagers.Default.Renderer.GraphicsDevice!, Microsoft.Xna.Framework.Color.Black, pngName, 504, (int)y);
            foreach (GraphicalUiElement visual in added)
            {
                visual.RemoveFromManagers();
            }
        }

        foreach ((string folder, string component, Action? theme, string name) in new (string, string, Action?, string)[]
        {
            ("FormsTemplate", "Controls/ComboBox", null, "standard"),
            ("FormsThemes/Editor", "Editor/Controls/ComboBox", Gum.Themes.Editor.EditorTheme.Apply, "editor"),
        })
        {
            MenuFocusRenderTests.WithToolProject(folder, mutateProject: null, () =>
                Render(() => (FrameworkElement)((InteractiveGue)ObjectFinder.Self.GetComponent(component)!
                    .ToGraphicalUiElement(SystemManagers.Default, addToManagers: false)).FormsControlAsObject!, $"6763-{name}-tool.png"));

            MeasureCodeOnly(applyTheme: () => { theme?.Invoke(); Render(() => new ComboBox(), $"6763-{name}-codeonly.png"); },
                create: () => new ComboBox());
        }
    }

    // The code-only themes name their dropdown text glyph "<Theme>...Glyph".
    private static GraphicalUiElement? FindGlyph(GraphicalUiElement parent)
    {
        foreach (GraphicalUiElement child in parent.Children?.OfType<GraphicalUiElement>() ?? Enumerable.Empty<GraphicalUiElement>())
        {
            if (child.Name?.EndsWith("Glyph") == true)
            {
                return child;
            }
            GraphicalUiElement? nested = FindGlyph(child);
            if (nested != null)
            {
                return nested;
            }
        }
        return null;
    }

    // The arrow's drawn quad as its axis-aligned bounds relative to the control, plus its rotation, rounded to 0.1 px.
    // The arrow is the V3 DropdownIndicator, a theme's *Glyph text, the Standard/Editor IconInstance sprite, or a
    // themed template's IconInstance text.
    private static string MeasureArrowRect(FrameworkElement control)
    {
        GraphicalUiElement visual = control.Visual;
        GraphicalUiElement? iconInstance = visual.GetGraphicalUiElementByName("IconInstance");
        GraphicalUiElement arrow = (visual.GetGraphicalUiElementByName("DropdownIndicator")
            ?? iconInstance?.GetGraphicalUiElementByName("IconSprite")
            ?? iconInstance
            ?? FindGlyph(visual)).ShouldNotBeNull();
        global::RenderingLibrary.Graphics.IRenderableIpso renderable = (arrow.RenderableComponent as global::RenderingLibrary.Graphics.IRenderableIpso).ShouldNotBeNull();
        Microsoft.Xna.Framework.Matrix matrix = renderable.GetAbsoluteRotationMatrix();
        Microsoft.Xna.Framework.Vector2 topLeft = new(renderable.GetAbsoluteX() - visual.GetAbsoluteX(), renderable.GetAbsoluteY() - visual.GetAbsoluteY());
        Microsoft.Xna.Framework.Vector2[] corners =
        {
            topLeft,
            topLeft + Microsoft.Xna.Framework.Vector2.Transform(new Microsoft.Xna.Framework.Vector2(renderable.Width, 0), matrix),
            topLeft + Microsoft.Xna.Framework.Vector2.Transform(new Microsoft.Xna.Framework.Vector2(0, renderable.Height), matrix),
            topLeft + Microsoft.Xna.Framework.Vector2.Transform(new Microsoft.Xna.Framework.Vector2(renderable.Width, renderable.Height), matrix),
        };
        static string R(float v) => MathF.Round(v, 1).ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        float rotation = ((renderable.GetAbsoluteRotation() % 360) + 360) % 360;
        return $"x {R(corners.Min(c => c.X))}..{R(corners.Max(c => c.X))} y {R(corners.Min(c => c.Y))}..{R(corners.Max(c => c.Y))} " +
            $"rot {R(rotation)} visible {arrow.Visible}";
    }

    // Adding Icon ColorCategory states for the ComboBox arrow leaves every other Icon user unchanged: each sprite's
    // color and visibility in every state matches IconUserColors.txt, recorded from the templates before #6754.
    // Set GUM_RECORD_ICON_USERS=1 to rewrite the reference.
    [Theory]
    [InlineData("FormsTemplate", "Controls/", "CheckBox,RadioButton,ButtonClose,MenuItem,ExpanderStandard,ButtonIcon")]
    [InlineData("FormsThemes/Editor", "Editor/Controls/", "CheckBox,RadioButton,ButtonClose,MenuItem,Expander,ButtonIcon")]
    public void IconUsers_EveryState_MatchRecordedColors(string templateFolder, string prefix, string components)
    {
        List<string> lines = new();
        MenuFocusRenderTests.WithToolProject(templateFolder, mutateProject: null, () =>
        {
            foreach (string name in components.Split(','))
            {
                ComponentSave component = ObjectFinder.Self.GetComponent(prefix + name).ShouldNotBeNull(prefix + name);
                GraphicalUiElement visual = component.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
                foreach (StateSaveCategory category in component.Categories)
                {
                    foreach (StateSave state in category.States)
                    {
                        visual.ApplyState(state);
                        foreach (string sprite in DescribeSprites(visual, "", visual.Visible))
                        {
                            lines.Add($"{prefix}{name} {category.Name}.{state.Name} {sprite}");
                        }
                    }
                }
            }
        });

        string referencePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(SourceFile())!, "IconUserColors.txt");
        string section = $"[{templateFolder}]";
        List<string> reference = System.IO.File.Exists(referencePath) ? System.IO.File.ReadAllLines(referencePath).ToList() : new();
        if (Environment.GetEnvironmentVariable("GUM_RECORD_ICON_USERS") == "1")
        {
            int at = reference.IndexOf(section);
            if (at >= 0)
            {
                int next = reference.FindIndex(at + 1, l => l.StartsWith("["));
                reference.RemoveRange(at, (next < 0 ? reference.Count : next) - at);
            }
            reference.Add(section);
            reference.AddRange(lines);
            System.IO.File.WriteAllLines(referencePath, reference);
            return;
        }

        int sectionStart = reference.IndexOf(section);
        sectionStart.ShouldBeGreaterThanOrEqualTo(0, referencePath);
        int sectionEnd = reference.FindIndex(sectionStart + 1, l => l.StartsWith("["));
        List<string> expected = reference.Skip(sectionStart + 1).Take((sectionEnd < 0 ? reference.Count : sectionEnd) - sectionStart - 1).ToList();
        lines.Count.ShouldBeGreaterThan(0);
        lines.ShouldBe(expected);
    }

    private static string SourceFile([System.Runtime.CompilerServices.CallerFilePath] string path = "") => path;

    private static IEnumerable<string> DescribeSprites(GraphicalUiElement parent, string path, bool parentVisible)
    {
        foreach (GraphicalUiElement child in parent.Children?.OfType<GraphicalUiElement>() ?? Enumerable.Empty<GraphicalUiElement>())
        {
            string childPath = path + "/" + child.Name;
            bool visible = parentVisible && child.Visible;
            if (child.RenderableComponent is global::RenderingLibrary.Graphics.Sprite sprite)
            {
                System.Drawing.Color c = sprite.Color;
                yield return $"{childPath} {c.R},{c.G},{c.B},{c.A} visible={visible}";
            }
            foreach (string nested in DescribeSprites(child, childPath, visible))
            {
                yield return nested;
            }
        }
    }

    // The fill comes from the Styles InputBackground swatch, so recoloring the swatch recolors the control.
    [Theory]
    [InlineData("FormsTemplate", "Controls/ScrollViewer", "Styles")]
    [InlineData("FormsTemplate", "Controls/ItemsControl", "Styles")]
    [InlineData("FormsTemplate", "Controls/ListBox", "Styles")]
    [InlineData("FormsTemplate", "Controls/ComboBox", "Styles")]
    [InlineData("FormsTemplate", "Controls/TextBox", "Styles")]
    [InlineData("FormsThemes/Editor", "Editor/Controls/ItemsControl", "Editor/Styles")]
    public void ToolBackground_FollowsInputBackgroundSwatch(string templateFolder, string componentName, string stylesName)
    {
        BackgroundLook tool = MeasureTool(templateFolder, componentName, beforeCreate: () =>
        {
            GumProjectSave project = ObjectFinder.Self.GumProjectSave.ShouldNotBeNull();
            StateSave styles = project.Components.Single(c => c.Name == stylesName).DefaultState!;
            styles.SetValue("InputBackground.FillRed", 200, "int");
            styles.SetValue("InputBackground.FillGreen", 10, "int");
            styles.SetValue("InputBackground.FillBlue", 20, "int");
            project.ApplyAllVariableReferences();
        });

        (tool.R, tool.G, tool.B).ShouldBe(((byte)200, (byte)10, (byte)20), componentName);
    }

    // Each arrow swatch recolors the ComboBox arrow in exactly the states that read it (#6754).
    [Theory]
    [InlineData("FormsTemplate", "Controls/ComboBox", "Styles", "PrimaryDisabled", "Disabled,DisabledFocused")]
    [InlineData("FormsTemplate", "Controls/ComboBox", "Styles", "PrimaryHighlighted", "Highlighted,HighlightedFocused")]
    [InlineData("FormsTemplate", "Controls/ComboBox", "Styles", "PrimaryPushed", "Pushed")]
    [InlineData("FormsThemes/Editor", "Editor/Controls/ComboBox", "Editor/Styles", "TextPrimaryDisabled", "Disabled,DisabledFocused")]
    [InlineData("FormsThemes/Editor", "Editor/Controls/ComboBox", "Editor/Styles", "TextPrimaryHighlighted", "Highlighted,HighlightedFocused")]
    [InlineData("FormsThemes/Editor", "Editor/Controls/ComboBox", "Editor/Styles", "TextPrimaryPushed", "Pushed")]
    public void ToolComboBoxArrow_StateSwatch_RecolorsOnlyItsStates(string templateFolder, string componentName, string stylesName,
        string swatch, string recoloredStates)
    {
        HashSet<string> expectedRecolored = new(recoloredStates.Split(','));

        List<IndicatorLook> looks = MeasureTool(templateFolder, componentName,
            EachState(ComboBoxStates, (control, state) => control.Visual.SetProperty("ComboBoxCategoryState", state), MeasureIndicators),
            beforeCreate: () => RecolorSwatch(stylesName, swatch));

        for (int i = 0; i < ComboBoxStates.Length; i++)
        {
            (looks[i].Arrow == "200,10,20").ShouldBe(expectedRecolored.Contains(ComboBoxStates[i]),
                $"{componentName} {ComboBoxStates[i]} with {swatch} recolored");
        }
    }

    // Each per-state swatch recolors exactly the states that read it, and no others (#6746).
    [Theory]
    [InlineData("FormsTemplate", "Controls/ListBox", "Styles", "InputBackgroundDark", "ListBoxCategoryState", "Disabled,DisabledFocused")]
    [InlineData("FormsTemplate", "Controls/TextBox", "Styles", "InputBackgroundDark", "TextBoxCategoryState", "Disabled")]
    [InlineData("FormsTemplate", "Controls/TextBox", "Styles", "InputBackgroundLight", "TextBoxCategoryState", "Highlighted")]
    [InlineData("FormsThemes/Editor", "Editor/Controls/ListBox", "Editor/Styles", "PanelBackgroundDark", "ListBoxCategoryState", "Disabled,DisabledFocused")]
    [InlineData("FormsThemes/Editor", "Editor/Controls/TextBox", "Editor/Styles", "RecessedBackgroundDark", "TextBoxCategoryState", "Disabled")]
    [InlineData("FormsThemes/Editor", "Editor/Controls/TextBox", "Editor/Styles", "RecessedBackgroundLight", "TextBoxCategoryState", "Highlighted")]
    public void ToolBackground_StateSwatch_RecolorsOnlyItsStates(string templateFolder, string componentName, string stylesName,
        string swatch, string categoryState, string recoloredStates)
    {
        HashSet<string> expectedRecolored = new(recoloredStates.Split(','));
        string[] states = categoryState == "ListBoxCategoryState" ? ListBoxStates : TextBoxStates;

        List<BackgroundLook> looks = MeasureTool(templateFolder, componentName,
            EachState(states, (control, state) => control.Visual.SetProperty(categoryState, state), Measure),
            beforeCreate: () => RecolorSwatch(stylesName, swatch));

        for (int i = 0; i < states.Length; i++)
        {
            bool isRecolored = (looks[i].R, looks[i].G, looks[i].B) == ((byte)200, (byte)10, (byte)20);
            isRecolored.ShouldBe(expectedRecolored.Contains(states[i]), $"{componentName} {states[i]} with {swatch} recolored");
        }
    }

    // Sets the swatch in the loaded project's Styles to 200,10,20 and pushes it through the variable references.
    private static void RecolorSwatch(string stylesName, string swatch)
    {
        GumProjectSave project = ObjectFinder.Self.GumProjectSave.ShouldNotBeNull();
        StateSave styles = project.Components.Single(c => c.Name == stylesName).DefaultState!;
        styles.SetValue(swatch + ".FillRed", 200, "int");
        styles.SetValue(swatch + ".FillGreen", 10, "int");
        styles.SetValue(swatch + ".FillBlue", 20, "int");
        project.ApplyAllVariableReferences();
    }

    // Re-importing only the new ListBox into an older project keeps the project's own Styles (import is file-level
    // Skip/Overwrite), which has no InputBackgroundDark swatch. The state keeps its saved fill and nothing reports
    // the missing swatch; only recoloring that state waits until the swatch exists (#6746).
    [Fact]
    public void ToolListBox_StylesWithoutDarkSwatch_KeepsSavedDisabledFill()
    {
        BackgroundLook? look = null;
        List<string> errors = new();
        void OnError(string message) => errors.Add(message);
        CustomSetPropertyOnRenderable.PropertyAssignmentError += OnError;
        try
        {
            MenuFocusRenderTests.WithToolProject("FormsTemplate", mutateProject: RemoveSwatch, () =>
            {
                GumProjectSave project = ObjectFinder.Self.GumProjectSave.ShouldNotBeNull();
                project.Components.Single(c => c.Name == "Styles").Instances.ShouldNotContain(i => i.Name == "InputBackgroundDark");
                project.ApplyAllVariableReferences();
                ComponentSave component = ObjectFinder.Self.GetComponent("Controls/ListBox").ShouldNotBeNull();
                GraphicalUiElement visual = component.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
                FrameworkElement control = (FrameworkElement)((InteractiveGue)visual).FormsControlAsObject!;
                control.Visual.SetProperty("ListBoxCategoryState", "Disabled");
                look = Measure(control);
            });
        }
        finally
        {
            CustomSetPropertyOnRenderable.PropertyAssignmentError -= OnError;
        }

        BackgroundLook result = look.ShouldNotBeNull();
        (result.R, result.G, result.B).ShouldBe(((byte)48, (byte)48, (byte)48));
        errors.ShouldNotContain(e => e.Contains("InputBackgroundDark"));

        static void RemoveSwatch(string projectDirectory)
        {
            string stylesPath = System.IO.Path.Combine(projectDirectory, "Components", "Styles.gucx");
            System.Xml.Linq.XDocument document = System.Xml.Linq.XDocument.Load(stylesPath);
            foreach (System.Xml.Linq.XElement element in document.Descendants()
                .Where(e => ((string?)e.Attribute("Name"))?.StartsWith("InputBackgroundDark") == true).ToList())
            {
                element.Remove();
            }
            document.Save(stylesPath);
        }
    }

    private static BackgroundLook MeasureTool(string templateFolder, string componentName, Action? beforeCreate = null,
        Action<FrameworkElement>? afterCreate = null) =>
        MeasureTool(templateFolder, componentName, Measure, beforeCreate, afterCreate);

    private static T MeasureTool<T>(string templateFolder, string componentName, Func<FrameworkElement, T> measure,
        Action? beforeCreate = null, Action<FrameworkElement>? afterCreate = null) where T : class
    {
        T? look = null;
        MenuFocusRenderTests.WithToolProject(templateFolder, mutateProject: null, () =>
        {
            beforeCreate?.Invoke();
            ComponentSave component = ObjectFinder.Self.GetComponent(componentName).ShouldNotBeNull(componentName);
            GraphicalUiElement visual = component.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
            FrameworkElement control = (visual as InteractiveGue)?.FormsControlAsObject.ShouldBeAssignableTo<FrameworkElement>(componentName)!;
            afterCreate?.Invoke(control);
            look = measure(control);
        });
        return look.ShouldNotBeNull();
    }

    private static BackgroundLook MeasureCodeOnly(Action? applyTheme, Func<FrameworkElement> create,
        Action<FrameworkElement>? afterCreate = null) =>
        MeasureCodeOnly(applyTheme, create, Measure, afterCreate);

    private static T MeasureCodeOnly<T>(Action? applyTheme, Func<FrameworkElement> create, Func<FrameworkElement, T> measure,
        Action<FrameworkElement>? afterCreate = null)
    {
        Dictionary<Type, VisualTemplate> previousTemplates = new(FrameworkElement.DefaultFormsTemplates);
        V3Styling previousStyle = V3Styling.ActiveStyle;
        var previousFontCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            using MenuFocusRenderTests.MenuLease game = new(gumxPath: null);
            applyTheme?.Invoke();
            FrameworkElement control = create();
            afterCreate?.Invoke(control);
            return measure(control);
        }
        finally
        {
            FrameworkElement.DefaultFormsTemplates.Clear();
            foreach (KeyValuePair<Type, VisualTemplate> pair in previousTemplates)
            {
                FrameworkElement.DefaultFormsTemplates[pair.Key] = pair.Value;
            }
            V3Styling.ActiveStyle = previousStyle;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = previousFontCreator;
        }
    }

    // Arrow is the V3 DropdownIndicator sprite or the tool's IconInstance sprite; the underline color counts only
    // while it is visible.
    private sealed record IndicatorLook(string? Arrow, bool UnderlineVisible, string? Underline, string? Placeholder);

    private static IndicatorLook MeasureIndicators(FrameworkElement control)
    {
        static string Rgb(System.Drawing.Color c) => $"{c.R},{c.G},{c.B}";

        GraphicalUiElement visual = control.Visual;
        GraphicalUiElement? arrow = visual.GetGraphicalUiElementByName("DropdownIndicator")
            ?? visual.GetGraphicalUiElementByName("IconInstance")?.GetGraphicalUiElementByName("IconSprite");
        GraphicalUiElement? underline = visual.GetGraphicalUiElementByName("FocusedIndicator");
        GraphicalUiElement? placeholder = visual.GetGraphicalUiElementByName("PlaceholderTextInstance");

        bool underlineVisible = underline?.Visible == true;
        return new IndicatorLook(
            arrow == null ? null : Rgb(arrow.RenderableComponent.ShouldBeOfType<global::RenderingLibrary.Graphics.Sprite>().Color),
            underlineVisible,
            underlineVisible ? Rgb(underline!.RenderableComponent.ShouldBeOfType<NineSlice>().Color) : null,
            placeholder == null ? null : Rgb(placeholder.RenderableComponent.ShouldBeOfType<global::RenderingLibrary.Graphics.Text>().Color));
    }

    private static BackgroundLook Measure(FrameworkElement control)
    {
        GraphicalUiElement background = control.Visual.GetGraphicalUiElementByName("Background").ShouldNotBeNull();
        NineSlice nineSlice = (background.RenderableComponent as NineSlice).ShouldNotBeNull();
        return new BackgroundLook(nineSlice.Color.R, nineSlice.Color.G, nineSlice.Color.B,
            (int)background.TextureLeft, (int)background.TextureTop, (int)background.TextureWidth, (int)background.TextureHeight);
    }
}
