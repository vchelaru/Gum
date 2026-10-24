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
            foreach (string state in ListBoxStates)
            {
                yield return new object[] { folder, prefix + "ListBox", "ListBoxCategoryState", state, theme!, (Func<FrameworkElement>)(() => new ListBox()) };
            }
            foreach (string state in ComboBoxStates)
            {
                yield return new object[] { folder, prefix + "ComboBox", "ComboBoxCategoryState", state, theme!, (Func<FrameworkElement>)(() => new ComboBox()) };
            }
            foreach (string state in TextBoxStates)
            {
                yield return new object[] { folder, prefix + "TextBox", "TextBoxCategoryState", state, theme!, (Func<FrameworkElement>)(() => new TextBox()) };
            }
        }
    }

    // Every state the control can reach. The V3 hover and disabled fills are a lighten/darken of the resting fill,
    // baked into *Light/*Dark swatches because static content can't compute them (#6746).
    [Theory]
    [MemberData(nameof(MatchingControlStates))]
    public void ToolBackgroundInState_MatchesCodeOnly(string templateFolder, string componentName, string categoryState, string stateName,
        Action? applyTheme, Func<FrameworkElement> createCodeOnly)
    {
        // Coming from another state first: a state that leaves the fill unset would keep the previous state's fill.
        string previousState = stateName == "Disabled" ? "Highlighted" : "Disabled";
        void EnterState(FrameworkElement control)
        {
            control.Visual.SetProperty(categoryState, previousState);
            control.Visual.SetProperty(categoryState, stateName);
        }

        BackgroundLook codeOnly = MeasureCodeOnly(applyTheme, createCodeOnly, EnterState);
        BackgroundLook tool = MeasureTool(templateFolder, componentName, afterCreate: EnterState);

        tool.ShouldBe(codeOnly, $"{componentName} {stateName}");
    }

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
            MenuFocusRenderTests.Capture(SystemManagers.Default.Renderer.GraphicsDevice, Microsoft.Xna.Framework.Color.Black, pngName, 688, 232);
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
            StateSave styles = project.Components.Single(c => c.Name == stylesName).DefaultState;
            styles.SetValue("InputBackground.FillRed", 200, "int");
            styles.SetValue("InputBackground.FillGreen", 10, "int");
            styles.SetValue("InputBackground.FillBlue", 20, "int");
            project.ApplyAllVariableReferences();
        });

        (tool.R, tool.G, tool.B).ShouldBe(((byte)200, (byte)10, (byte)20), componentName);
    }

    private static BackgroundLook MeasureTool(string templateFolder, string componentName, Action? beforeCreate = null,
        Action<FrameworkElement>? afterCreate = null)
    {
        BackgroundLook? look = null;
        MenuFocusRenderTests.WithToolProject(templateFolder, mutateProject: null, () =>
        {
            beforeCreate?.Invoke();
            ComponentSave component = ObjectFinder.Self.GetComponent(componentName).ShouldNotBeNull(componentName);
            GraphicalUiElement visual = component.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
            FrameworkElement control = (visual as InteractiveGue)?.FormsControlAsObject.ShouldBeAssignableTo<FrameworkElement>(componentName)!;
            afterCreate?.Invoke(control);
            look = Measure(control);
        });
        return look.ShouldNotBeNull();
    }

    private static BackgroundLook MeasureCodeOnly(Action? applyTheme, Func<FrameworkElement> create,
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
            return Measure(control);
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

    private static BackgroundLook Measure(FrameworkElement control)
    {
        GraphicalUiElement background = control.Visual.GetGraphicalUiElementByName("Background").ShouldNotBeNull();
        NineSlice nineSlice = (background.RenderableComponent as NineSlice).ShouldNotBeNull();
        return new BackgroundLook(nineSlice.Color.R, nineSlice.Color.G, nineSlice.Color.B,
            (int)background.TextureLeft, (int)background.TextureTop, (int)background.TextureWidth, (int)background.TextureHeight);
    }
}
