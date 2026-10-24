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

    // Kept as they are: these Standard controls tint with DarkGray where V3 uses InputBackground, and their
    // states derive lighter/darker fills the templates don't carry yet.
    [Theory]
    [InlineData("Controls/ListBox")]
    [InlineData("Controls/ComboBox")]
    [InlineData("Controls/TextBox")]
    public void StandardToolBackground_StaysDarkGray(string componentName)
    {
        BackgroundLook tool = MeasureTool("FormsTemplate", componentName);

        (tool.R, tool.G, tool.B).ShouldBe(((byte)70, (byte)70, (byte)80), componentName);
    }

    // The fill comes from the Styles InputBackground swatch, so recoloring the swatch recolors the control.
    [Theory]
    [InlineData("FormsTemplate", "Controls/ScrollViewer", "Styles")]
    [InlineData("FormsTemplate", "Controls/ItemsControl", "Styles")]
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

    private static BackgroundLook MeasureTool(string templateFolder, string componentName, Action? beforeCreate = null)
    {
        BackgroundLook? look = null;
        MenuFocusRenderTests.WithToolProject(templateFolder, mutateProject: null, () =>
        {
            beforeCreate?.Invoke();
            ComponentSave component = ObjectFinder.Self.GetComponent(componentName).ShouldNotBeNull(componentName);
            GraphicalUiElement visual = component.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
            look = Measure((visual as InteractiveGue)?.FormsControlAsObject.ShouldBeAssignableTo<FrameworkElement>(componentName)!);
        });
        return look.ShouldNotBeNull();
    }

    private static BackgroundLook MeasureCodeOnly(Action? applyTheme, Func<FrameworkElement> create)
    {
        Dictionary<Type, VisualTemplate> previousTemplates = new(FrameworkElement.DefaultFormsTemplates);
        V3Styling previousStyle = V3Styling.ActiveStyle;
        var previousFontCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            using MenuFocusRenderTests.MenuLease game = new(gumxPath: null);
            applyTheme?.Invoke();
            return Measure(create());
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
