using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework.Graphics;
using Moq;
using RenderingLibrary;
using Shouldly;
using Xunit;
using Keys = Gum.Forms.Input.Keys;
using V3Styling = Gum.Forms.DefaultVisuals.V3.Styling;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Like WPF, a focused Menu shows focus only through its highlighted top-level item (#6706): focusing a
/// Menu changes no pixel outside its first item, for the V3 visual, every code-only theme, and every
/// tool template. Set GUM_SCREENSHOT_DIR to also write
/// &lt;theme&gt;-&lt;codeonly|tool&gt;-&lt;unfocused|focused|open&gt;.png for visual review; both halves of a
/// theme render at the same size on the same background.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class MenuFocusRenderTests : BaseTestClass
{
    private const int CaptureWidth = 320;
    private const int CaptureHeight = 160;

    public static IEnumerable<object[]> CodeOnlyThemes()
    {
        yield return new object[] { "standard", null!, new XnaColor(32, 32, 32) };
        yield return new object[] { "bubblegum", (Action)Gum.Themes.Bubblegum.BubblegumTheme.Apply, new XnaColor(255, 240, 249) };
        yield return new object[] { "darkpro", (Action)Gum.Themes.DarkPro.DarkProTheme.Apply, new XnaColor(26, 27, 30) };
        yield return new object[] { "editor", (Action)Gum.Themes.Editor.EditorTheme.Apply, new XnaColor(27, 27, 27) };
        yield return new object[] { "forestglade", (Action)Gum.Themes.ForestGlade.ForestGladeTheme.Apply, new XnaColor(10, 45, 38) };
        yield return new object[] { "hazard", (Action)Gum.Themes.Hazard.HazardTheme.Apply, new XnaColor(10, 10, 8) };
        yield return new object[] { "meadow", (Action)Gum.Themes.Meadow.MeadowTheme.Apply, new XnaColor(247, 237, 214) };
        yield return new object[] { "neon", (Action)Gum.Themes.Neon.NeonTheme.Apply, new XnaColor(6, 6, 18) };
        yield return new object[] { "retro95", (Action)Gum.Themes.Retro95.Retro95Theme.Apply, new XnaColor(192, 192, 192) };
        yield return new object[] { "template", (Action)Gum.Themes.Template.TemplateTheme.Apply, new XnaColor(26, 27, 30) };
    }

    public static IEnumerable<object[]> ToolTemplates()
    {
        yield return new object[] { "standard", "FormsTemplate", "Controls/", new XnaColor(32, 32, 32) };
        yield return new object[] { "bubblegum", "FormsThemes/Bubblegum", "Bubblegum/Controls/", new XnaColor(255, 240, 249) };
        yield return new object[] { "darkpro", "FormsThemes/DarkPro", "DarkPro/Controls/", new XnaColor(26, 27, 30) };
        yield return new object[] { "editor", "FormsThemes/Editor", "Editor/Controls/", new XnaColor(27, 27, 27) };
        yield return new object[] { "forestglade", "FormsThemes/ForestGlade", "ForestGlade/Controls/", new XnaColor(10, 45, 38) };
        yield return new object[] { "hazard", "FormsThemes/Hazard", "Hazard/Controls/", new XnaColor(10, 10, 8) };
        yield return new object[] { "meadow", "FormsThemes/Meadow", "Meadow/Controls/", new XnaColor(247, 237, 214) };
        yield return new object[] { "neon", "FormsThemes/Neon", "Neon/Controls/", new XnaColor(6, 6, 18) };
        yield return new object[] { "retro95", "FormsThemes/Retro95", "Retro95/Controls/", new XnaColor(192, 192, 192) };
    }

    public static IEnumerable<object[]> Pairs()
    {
        yield return new object[] { "standard", null!, "FormsTemplate", "Controls/", new XnaColor(32, 32, 32) };
        yield return new object[] { "bubblegum", (Action)Gum.Themes.Bubblegum.BubblegumTheme.Apply, "FormsThemes/Bubblegum", "Bubblegum/Controls/", new XnaColor(255, 240, 249) };
        yield return new object[] { "darkpro", (Action)Gum.Themes.DarkPro.DarkProTheme.Apply, "FormsThemes/DarkPro", "DarkPro/Controls/", new XnaColor(26, 27, 30) };
        yield return new object[] { "editor", (Action)Gum.Themes.Editor.EditorTheme.Apply, "FormsThemes/Editor", "Editor/Controls/", new XnaColor(27, 27, 27) };
        yield return new object[] { "forestglade", (Action)Gum.Themes.ForestGlade.ForestGladeTheme.Apply, "FormsThemes/ForestGlade", "ForestGlade/Controls/", new XnaColor(10, 45, 38) };
        yield return new object[] { "hazard", (Action)Gum.Themes.Hazard.HazardTheme.Apply, "FormsThemes/Hazard", "Hazard/Controls/", new XnaColor(10, 10, 8) };
        yield return new object[] { "meadow", (Action)Gum.Themes.Meadow.MeadowTheme.Apply, "FormsThemes/Meadow", "Meadow/Controls/", new XnaColor(247, 237, 214) };
        yield return new object[] { "neon", (Action)Gum.Themes.Neon.NeonTheme.Apply, "FormsThemes/Neon", "Neon/Controls/", new XnaColor(6, 6, 18) };
        yield return new object[] { "retro95", (Action)Gum.Themes.Retro95.Retro95Theme.Apply, "FormsThemes/Retro95", "Retro95/Controls/", new XnaColor(192, 192, 192) };
    }

    // The keyboard highlight is the Menu's only focus indicator, so the tool's MenuItem must match the
    // code-only one: the same space before the header and the same highlight fill (#6706).
    [Theory]
    [MemberData(nameof(Pairs))]
    public void Tool_HighlightedMenuItem_MatchesCodeOnly(string themeName, Action? applyTheme, string templateFolder, string componentPrefix, XnaColor background)
    {
        MenuMeasure codeOnly = RunScenario(themeName + "-codeonly", gumxPath: null, applyTheme, background, componentPrefix: null);
        MenuMeasure tool = RunTool(themeName, templateFolder, componentPrefix, background);

        tool.HeaderInset.ShouldBe(codeOnly.HeaderInset, 1f, $"{themeName}: space before the header");
        ShouldBeClose(tool.HighlightFill, codeOnly.HighlightFill, $"{themeName}: highlighted item fill");
        ShouldBeClose(tool.HighlightedHeaderColor, codeOnly.HighlightedHeaderColor, $"{themeName}: highlighted item header color");
        ShouldBeClose(tool.HighlightEdge, codeOnly.HighlightEdge, $"{themeName}: highlighted item edge");
        ShouldBeClose(tool.HighlightCorner, codeOnly.HighlightCorner, $"{themeName}: highlighted item corner");
    }

    // With its submenu open, the top-level item shows the open (Selected) fill, the same in both (#6716).
    [Theory]
    [MemberData(nameof(Pairs))]
    public void Tool_OpenMenuItem_MatchesCodeOnly(string themeName, Action? applyTheme, string templateFolder, string componentPrefix, XnaColor background)
    {
        MenuMeasure codeOnly = RunScenario(themeName + "-codeonly", gumxPath: null, applyTheme, background, componentPrefix: null);
        MenuMeasure tool = RunTool(themeName, templateFolder, componentPrefix, background);

        tool.ShouldSatisfyAllConditions(
            () => ShouldBeClose(tool.OpenFill, codeOnly.OpenFill, $"{themeName}: open item fill"),
            () => ShouldBeClose(tool.OpenHeaderColor, codeOnly.OpenHeaderColor, $"{themeName}: open item header color"),
            () =>
            {
                if (tool.OpenIndicatorColor is XnaColor toolIndicator)
                {
                    ShouldBeClose(toolIndicator, codeOnly.OpenIndicatorColor!.Value, $"{themeName}: open item submenu indicator color");
                }
            },
            () => ShouldBeClose(tool.OpenCorner, codeOnly.OpenCorner, $"{themeName}: open item corner"));
    }

    public static IEnumerable<object[]> StandardAndEditorPairs()
    {
        HashSet<string> themes = new() { "standard", "editor" };
        foreach (object[] pair in Pairs())
        {
            if (themes.Contains((string)pair[0]))
            {
                yield return pair;
            }
        }
    }

    // The menu bar draws the same fill at the same height in both (#6732).
    [Theory]
    [MemberData(nameof(StandardAndEditorPairs))]
    public void Tool_MenuBar_MatchesCodeOnly(string themeName, Action? applyTheme, string templateFolder, string componentPrefix, XnaColor background)
    {
        MenuMeasure codeOnly = RunScenario(themeName + "-codeonly", gumxPath: null, applyTheme, background, componentPrefix: null);
        MenuMeasure tool = RunTool(themeName, templateFolder, componentPrefix, background);

        tool.ShouldSatisfyAllConditions(
            () => ShouldBeClose(tool.BarFill, codeOnly.BarFill, $"{themeName}: menu bar fill", tolerance: 2),
            () => tool.BarHeight.ShouldBe(codeOnly.BarHeight, 1f, $"{themeName}: menu bar height"));
    }

    // The bar reads its fill from the Styles InputBackground swatch, so recoloring the swatch recolors the bar (#6732).
    [Theory]
    [InlineData("FormsTemplate", "Controls/", "Styles")]
    [InlineData("FormsThemes/Editor", "Editor/Controls/", "Editor/Styles")]
    public void Tool_MenuBar_FollowsInputBackgroundSwatch(string templateFolder, string componentPrefix, string stylesName)
    {
        WithToolProject(templateFolder, mutateProject: null, () =>
        {
            GumProjectSave project = ObjectFinder.Self.GumProjectSave.ShouldNotBeNull();
            StateSave styles = project.Components.Single(c => c.Name == stylesName).DefaultState;
            styles.SetValue("InputBackground.FillRed", 200, "int");
            styles.SetValue("InputBackground.FillGreen", 10, "int");
            styles.SetValue("InputBackground.FillBlue", 20, "int");
            project.ApplyAllVariableReferences();

            Menu menu = FromComponent<Menu>(componentPrefix + "Menu");

            var background = (menu.Visual.GetGraphicalUiElementByName("Background")?.RenderableComponent as global::RenderingLibrary.Graphics.NineSlice).ShouldNotBeNull();
            (background.Color.R, background.Color.G, background.Color.B).ShouldBe(((byte)200, (byte)10, (byte)20));
        });
    }

    // A top-level item that is not highlighted or open paints the same pixels in both: enabled, disabled, or
    // focused itself (MenuItem.UpdateState applies Focused when IsFocused is set) (#6731).
    [Theory]
    [MemberData(nameof(StandardAndEditorPairs))]
    public void Tool_RestingMenuItemRow_MatchesCodeOnly(string themeName, Action? applyTheme, string templateFolder, string componentPrefix, XnaColor background)
    {
        MenuMeasure codeOnly = RunScenario(themeName + "-codeonly", gumxPath: null, applyTheme, background, componentPrefix: null);
        MenuMeasure tool = RunTool(themeName, templateFolder, componentPrefix, background);

        tool.ShouldSatisfyAllConditions(
            () => ShouldBeClose(tool.RestingRowFill, codeOnly.RestingRowFill, $"{themeName}: enabled item fill", tolerance: 2),
            () => ShouldBeClose(tool.DisabledRowFill, codeOnly.DisabledRowFill, $"{themeName}: disabled item fill", tolerance: 2),
            () => ShouldBeClose(tool.FocusedRowFill, codeOnly.FocusedRowFill, $"{themeName}: focused item fill", tolerance: 2));
    }

    // The open submenu popup draws the theme's surface and border, and its rows' text, the same in both
    // (#6720). Standard's popup is its ScrollViewer component (#6740).
    [Theory]
    [MemberData(nameof(Pairs))]
    public void Tool_SubmenuPopup_MatchesCodeOnly(string themeName, Action? applyTheme, string templateFolder, string componentPrefix, XnaColor background)
    {
        MenuMeasure codeOnly = RunScenario(themeName + "-codeonly", gumxPath: null, applyTheme, background, componentPrefix: null);
        MenuMeasure tool = RunTool(themeName, templateFolder, componentPrefix, background);

        tool.ShouldSatisfyAllConditions(
            () => ShouldBeClose(tool.PopupFill, codeOnly.PopupFill, $"{themeName}: submenu popup fill", tolerance: 2),
            () => ShouldBeClose(tool.PopupBorder, codeOnly.PopupBorder, $"{themeName}: submenu popup border", tolerance: 2),
            () => ShouldBeClose(tool.PopupRowTextColor, codeOnly.PopupRowTextColor, $"{themeName}: non-highlighted submenu row text"));
    }

    // In a tool project the submenu popup is the project's MenuItemSubmenuPopupBehavior component when it has
    // one (Retro95, Editor), else its ScrollViewer component (the ScrollViewerBehavior's DefaultImplementation).
    // Either way it sits under its item and sizes itself to its rows (#6720).
    [Theory]
    [MemberData(nameof(ToolTemplates))]
    public void Tool_SubmenuPopup_IsProjectComponent_SizedToRows(string themeName, string templateFolder, string componentPrefix, XnaColor background)
    {
        HashSet<string> withSubmenuPopupComponent = new() { "editor", "retro95" };
        string expectedComponent = componentPrefix + (withSubmenuPopupComponent.Contains(themeName) ? "MenuItemSubmenuPopup" : "ScrollViewer");

        MenuMeasure tool = RunTool(themeName, templateFolder, componentPrefix, background);

        tool.PopupElementName.ShouldBe(expectedComponent, themeName);
        tool.PopupGrowthPerRow.ShouldBe(tool.AddedRowHeight, 1f, $"{themeName}: popup height change when a row is added");
    }

    // Like the Tooltip, a ScrollViewer template that user code registered wins over the project's components,
    // the ScrollViewer one (Bubblegum) and the MenuItemSubmenuPopupBehavior one (Retro95) (#6720).
    [Theory]
    [InlineData("bubblegum", "FormsThemes/Bubblegum", "Bubblegum/Controls/", 255, 240, 249)]
    [InlineData("retro95", "FormsThemes/Retro95", "Retro95/Controls/", 192, 192, 192)]
    public void Tool_SubmenuPopup_UserScrollViewerTemplate_WinsOverProjectComponent(string themeName, string templateFolder, string componentPrefix, int red, int green, int blue)
    {
        const string userVisualName = "UserScrollViewer";
        void RegisterUserTemplate() =>
            FrameworkElement.DefaultFormsTemplates[typeof(ScrollViewer)] = new VisualTemplate((_, createForms) =>
                new global::Gum.Forms.DefaultVisuals.V3.ScrollViewerVisual(tryCreateFormsObject: createForms) { Name = userVisualName });

        MenuMeasure tool = RunTool(themeName, templateFolder, componentPrefix, new XnaColor(red, green, blue), RegisterUserTemplate);

        tool.PopupVisualName.ShouldBe(userVisualName, themeName);
        tool.PopupGrowthPerRow.ShouldBe(tool.AddedRowHeight, 1f, "popup height change when a row is added");
    }

    // A MenuItem builds no popup visual until its submenu opens (#6720).
    [Fact]
    public void Tool_SubmenuPopup_NotCreatedUntilOpened()
    {
        WithToolProject("FormsThemes/Retro95", mutateProject: null, () =>
        {
            MenuItem Item(string header)
            {
                MenuItem item = FromComponent<MenuItem>("Retro95/Controls/MenuItem");
                item.Header = header;
                return item;
            }
            Menu menu = FromComponent<Menu>("Retro95/Controls/Menu");
            MenuItem file = Item("File");
            file.Items!.Add(Item("New"));
            menu.Items!.Add(file);
            menu.AddToRoot();
            menu.Visual.UpdateLayout();

            file.itemsPopup.ShouldBeNull("before opening");
            file.TryShowPopup();
            file.itemsPopup.ShouldNotBeNull("after opening").Visual.ElementSave.ShouldNotBeNull().Name.ShouldBe("Retro95/Controls/MenuItemSubmenuPopup");
        });
    }

    // A submenu opened from a submenu row uses the MenuItemSubmenuPopupBehavior component too, and grows by
    // one row (#6720).
    [Fact]
    public void Tool_NestedSubmenuPopup_IsSubmenuPopupComponent_SizedToRows()
    {
        WithToolProject("FormsThemes/Retro95", mutateProject: null, () =>
        {
            (MenuItem _, MenuItem recent) = OpenNestedMenu("Retro95/Controls/");
            ScrollViewer popup = recent.itemsPopup.ShouldNotBeNull();
            popup.Visual.ElementSave.ShouldNotBeNull().Name.ShouldBe("Retro95/Controls/MenuItemSubmenuPopup");

            float heightBefore = popup.Visual.GetAbsoluteHeight();
            MenuItem added = FromComponent<MenuItem>("Retro95/Controls/MenuItem");
            added.Header = "Third";
            recent.Items!.Add(added);
            (popup.Visual.GetAbsoluteHeight() - heightBefore).ShouldBe(added.Visual.GetAbsoluteHeight(), 1f);
        });
    }

    // The submenu-popup component is resolved by name when a submenu opens, so one removed from the project
    // after load (a hot reload) falls back to the project's ScrollViewer component (#6720).
    [Fact]
    public void Tool_SubmenuPopup_ComponentRemovedAfterLoad_FallsBackToScrollViewerComponent()
    {
        WithToolProject("FormsThemes/Retro95", mutateProject: null, () =>
        {
            GumProjectSave project = ObjectFinder.Self.GumProjectSave.ShouldNotBeNull();
            ComponentSave popupComponent = project.Components.Single(item => item.Name == "Retro95/Controls/MenuItemSubmenuPopup");
            project.Components.Remove(popupComponent);
            ObjectFinder.Self.GetComponent("Retro95/Controls/MenuItemSubmenuPopup").ShouldBeNull("removed component still found");

            (MenuItem file, MenuItem _) = OpenNestedMenu("Retro95/Controls/");

            file.itemsPopup.ShouldNotBeNull().Visual.ElementSave.ShouldNotBeNull().Name.ShouldBe("Retro95/Controls/ScrollViewer");
        });
    }

    // A project whose gumx references MenuItemSubmenuPopupBehavior while its .behx file is missing still
    // loads, and its submenu still uses the component that has the behavior (#6720).
    [Fact]
    public void Tool_SubmenuPopup_BehaviorFileMissing_LoadsAndUsesComponent()
    {
        static void DeleteBehaviorFile(string projectDir)
        {
            string behavior = Path.Combine(projectDir, "..", "..", "FormsBehaviors", "MenuItemSubmenuPopupBehavior.behx");
            File.Exists(behavior).ShouldBeTrue(behavior);
            File.Delete(behavior);
        }

        WithToolProject("FormsThemes/Retro95", DeleteBehaviorFile, () =>
        {
            (MenuItem file, MenuItem _) = OpenNestedMenu("Retro95/Controls/");

            file.itemsPopup.ShouldNotBeNull().Visual.ElementSave.ShouldNotBeNull().Name.ShouldBe("Retro95/Controls/MenuItemSubmenuPopup");
        });
    }

    // A submenu opened from a submenu row is the project's ScrollViewer too, and grows by one row (#6720).
    [Fact]
    public void Tool_NestedSubmenuPopup_IsProjectScrollViewer_SizedToRows()
    {
        WithToolProject("FormsThemes/Bubblegum", mutateProject: null, () =>
        {
            (MenuItem _, MenuItem recent) = OpenNestedMenu("Bubblegum/Controls/");
            ScrollViewer popup = recent.itemsPopup.ShouldNotBeNull();
            popup.Visual.ElementSave.ShouldNotBeNull().Name.ShouldBe("Bubblegum/Controls/ScrollViewer");

            float heightBefore = popup.Visual.GetAbsoluteHeight();
            MenuItem added = FromComponent<MenuItem>("Bubblegum/Controls/MenuItem");
            added.Header = "Third";
            recent.Items!.Add(added);
            (popup.Visual.GetAbsoluteHeight() - heightBefore).ShouldBe(added.Visual.GetAbsoluteHeight(), 1f);
        });
    }

    // A tool project with no ScrollViewer component keeps the code-built V3 popup, still sized to its rows.
    [Fact]
    public void Tool_SubmenuPopup_NoScrollViewerComponent_FallsBackToV3_SizedToRows()
    {
        static void RemoveScrollViewerBehavior(string projectDir)
        {
            string path = Path.Combine(projectDir, "Components", "Bubblegum", "Controls", "ScrollViewer.gucx");
            string text = File.ReadAllText(path);
            string withoutBehavior = System.Text.RegularExpressions.Regex.Replace(text,
                @"<ElementBehaviorReference>\s*<BehaviorName>ScrollViewerBehavior</BehaviorName>\s*</ElementBehaviorReference>", "");
            withoutBehavior.ShouldNotBe(text);
            File.WriteAllText(path, withoutBehavior);
        }

        WithToolProject("FormsThemes/Bubblegum", RemoveScrollViewerBehavior, () =>
        {
            (MenuItem file, MenuItem _) = OpenNestedMenu("Bubblegum/Controls/");
            ScrollViewer popup = file.itemsPopup.ShouldNotBeNull();
            popup.Visual.ShouldBeOfType<global::Gum.Forms.DefaultVisuals.V3.ScrollViewerVisual>();

            float heightBefore = popup.Visual.GetAbsoluteHeight();
            MenuItem added = FromComponent<MenuItem>("Bubblegum/Controls/MenuItem");
            added.Header = "Close";
            file.Items!.Add(added);
            (popup.Visual.GetAbsoluteHeight() - heightBefore).ShouldBe(added.Visual.GetAbsoluteHeight(), 1f);
        });
    }

    // File > Recent > {First, Second}, with File's and Recent's popups open.
    private static (MenuItem File, MenuItem Recent) OpenNestedMenu(string componentPrefix)
    {
        MenuItem Item(string header)
        {
            MenuItem item = FromComponent<MenuItem>(componentPrefix + "MenuItem");
            item.Header = header;
            return item;
        }
        Menu menu = FromComponent<Menu>(componentPrefix + "Menu");
        MenuItem file = Item("File");
        MenuItem recent = Item("Recent");
        recent.Items!.Add(Item("First"));
        recent.Items!.Add(Item("Second"));
        file.Items!.Add(Item("New"));
        file.Items!.Add(recent);
        menu.Items!.Add(file);
        menu.AddToRoot();
        menu.Visual.UpdateLayout();
        file.TryShowPopup();
        recent.TryShowPopup();
        recent.itemsPopup.ShouldNotBeNull("Recent's submenu opened");
        return (file, recent);
    }

    internal static void WithToolProject(string templateFolder, Action<string>? mutateProject, Action body)
    {
        string root = Path.Combine(Path.GetTempPath(), "GumMenuFocus_" + Guid.NewGuid().ToString("N"));
        Dictionary<Type, VisualTemplate> previousTemplates = new(FrameworkElement.DefaultFormsTemplates);
        V3Styling previousStyle = V3Styling.ActiveStyle;
        var previousFontCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        try
        {
            string templates = FindTemplatesDirectory();
            CopyDirectory(Path.Combine(templates, "FormsBehaviors"), Path.Combine(root, "FormsBehaviors"));
            string projectDir = Path.Combine(root, templateFolder.Replace('/', Path.DirectorySeparatorChar));
            CopyDirectory(Path.Combine(templates, templateFolder.Replace('/', Path.DirectorySeparatorChar)), projectDir);
            mutateProject?.Invoke(projectDir);
            using MenuLease game = new MenuLease(Path.Combine(projectDir, "GumProject.gumx"));
            body();
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
            try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
        }
    }

    // Code-only themes keep their own popup visual, under the item and sized to its rows.
    [Theory]
    [MemberData(nameof(CodeOnlyThemes))]
    public void CodeOnly_SubmenuPopup_SizedToRows(string themeName, Action? applyTheme, XnaColor background)
    {
        MenuMeasure codeOnly = RunScenario(themeName + "-codeonly", gumxPath: null, applyTheme, background, componentPrefix: null);

        codeOnly.PopupElementName.ShouldBeNull(themeName);
        codeOnly.PopupGrowthPerRow.ShouldBe(codeOnly.AddedRowHeight, 1f, $"{themeName}: popup height change when a row is added");
    }

    // In these themes the highlight fill alone is close to the menu bar color, so the highlight also
    // draws an edge (#6715).
    public static IEnumerable<object[]> OutlinedHighlightPairs()
    {
        HashSet<string> outlined = new() { "bubblegum", "darkpro", "hazard", "meadow", "neon" };
        foreach (object[] pair in Pairs())
        {
            if (outlined.Contains((string)pair[0]))
            {
                yield return pair;
            }
        }
    }

    [Theory]
    [MemberData(nameof(OutlinedHighlightPairs))]
    public void HighlightedMenuItem_HasEdgeDistinctFromFill(string themeName, Action? applyTheme, string templateFolder, string componentPrefix, XnaColor background)
    {
        MenuMeasure codeOnly = RunScenario(themeName + "-codeonly", gumxPath: null, applyTheme, background, componentPrefix: null);
        MenuMeasure tool = RunTool(themeName, templateFolder, componentPrefix, background);

        ColorDistance(codeOnly.HighlightEdge, codeOnly.HighlightFill).ShouldBeGreaterThan(60, $"{themeName} code-only: edge {codeOnly.HighlightEdge} vs fill {codeOnly.HighlightFill}");
        ColorDistance(tool.HighlightEdge, tool.HighlightFill).ShouldBeGreaterThan(60, $"{themeName} tool: edge {tool.HighlightEdge} vs fill {tool.HighlightFill}");
    }

    private static int ColorDistance(XnaColor a, XnaColor b) =>
        Math.Abs(a.R - b.R) + Math.Abs(a.G - b.G) + Math.Abs(a.B - b.B);

    private static void ShouldBeClose(XnaColor actual, XnaColor expected, string because, int tolerance = 12)
    {
        ColorDistance(actual, expected).ShouldBeLessThanOrEqualTo(tolerance, $"{because}: tool {actual} vs code-only {expected}");
    }

    // A tool project renders its text from the fonts in its FontCache, so a template that ships without
    // them falls back to the default font (#6722). Same font and size means the same line height and the
    // same width for each header, on a top-level row and on a submenu row.
    [Theory]
    [MemberData(nameof(Pairs))]
    public void Tool_MenuItemFont_MatchesCodeOnly(string themeName, Action? applyTheme, string templateFolder, string componentPrefix, XnaColor background)
    {
        _ = background;
        MenuFontMeasure codeOnly = MeasureMenuFont(gumxPath: null, applyTheme, componentPrefix: null);

        string root = Path.Combine(Path.GetTempPath(), "GumMenuFont_" + Guid.NewGuid().ToString("N"));
        MenuFontMeasure tool;
        try
        {
            string templates = FindTemplatesDirectory();
            CopyDirectory(Path.Combine(templates, "FormsBehaviors"), Path.Combine(root, "FormsBehaviors"));
            string projectDir = Path.Combine(root, templateFolder.Replace('/', Path.DirectorySeparatorChar));
            CopyDirectory(Path.Combine(templates, templateFolder.Replace('/', Path.DirectorySeparatorChar)), projectDir);
            tool = MeasureMenuFont(Path.Combine(projectDir, "GumProject.gumx"), applyTheme: null, componentPrefix);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
        }

        tool.TopLevelLineHeight.ShouldBe(codeOnly.TopLevelLineHeight, $"{themeName}: top-level row line height");
        ((float)tool.TopLevelWidth).ShouldBe(codeOnly.TopLevelWidth, 1f, $"{themeName}: top-level row header width");
        tool.SubmenuLineHeight.ShouldBe(codeOnly.SubmenuLineHeight, $"{themeName}: submenu row line height");
        ((float)tool.SubmenuWidth).ShouldBe(codeOnly.SubmenuWidth, 1f, $"{themeName}: submenu row header width");
    }

    private sealed record MenuFontMeasure(int TopLevelLineHeight, int TopLevelWidth, int SubmenuLineHeight, int SubmenuWidth);

    private static MenuFontMeasure MeasureMenuFont(string? gumxPath, Action? applyTheme, string? componentPrefix)
    {
        Dictionary<Type, VisualTemplate> previousTemplates = new(FrameworkElement.DefaultFormsTemplates);
        V3Styling previousStyle = V3Styling.ActiveStyle;
        var previousFontCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        using MenuLease game = new MenuLease(gumxPath);
        try
        {
            applyTheme?.Invoke();
            MenuItem Item(string header)
            {
                MenuItem item = componentPrefix == null ? new MenuItem() : FromComponent<MenuItem>(componentPrefix + "MenuItem");
                item.Header = header;
                return item;
            }
            Menu menu = componentPrefix == null ? new Menu() : FromComponent<Menu>(componentPrefix + "Menu");
            MenuItem file = Item("File");
            MenuItem newItem = Item("New");
            file.Items!.Add(newItem);
            menu.Items!.Add(file);

            global::RenderingLibrary.Graphics.Text TextOf(MenuItem item) =>
                (item.Visual.GetGraphicalUiElementByName("TextInstance")?.RenderableComponent as global::RenderingLibrary.Graphics.Text).ShouldNotBeNull();
            var fileFont = TextOf(file).BitmapFont.ShouldNotBeNull();
            var newFont = TextOf(newItem).BitmapFont.ShouldNotBeNull();
            return new MenuFontMeasure(fileFont.LineHeightInPixels, fileFont.MeasureString("File"), newFont.LineHeightInPixels, newFont.MeasureString("New"));
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

    // HighlightCorner is the highlighted item's pixel one in from its top-left corner, which a rounded
    // highlight leaves at the menu bar color.
    private sealed record MenuMeasure(float HeaderInset, XnaColor HighlightFill, XnaColor HighlightedHeaderColor, XnaColor HighlightEdge, XnaColor HighlightCorner, XnaColor OpenFill,
        XnaColor OpenHeaderColor, XnaColor? OpenIndicatorColor, XnaColor OpenCorner,
        XnaColor PopupFill, XnaColor PopupBorder, XnaColor PopupRowTextColor, string? PopupElementName, string? PopupVisualName, float PopupGrowthPerRow, float AddedRowHeight,
        XnaColor[] BarColumn, XnaColor[] FocusedItemColumn, int MenuBottom,
        XnaColor BarFill, float BarHeight, XnaColor RestingRowFill, XnaColor DisabledRowFill, XnaColor FocusedRowFill);

    [Theory]
    [MemberData(nameof(CodeOnlyThemes))]
    public void CodeOnly_Focus_ChangesOnlyTheFirstItem(string themeName, Action? applyTheme, XnaColor background)
    {
        RunScenario(themeName + "-codeonly", gumxPath: null, applyTheme, background, componentPrefix: null);
    }

    [Theory]
    [MemberData(nameof(ToolTemplates))]
    public void Tool_Focus_ChangesOnlyTheFirstItem(string themeName, string templateFolder, string componentPrefix, XnaColor background)
    {
        RunTool(themeName, templateFolder, componentPrefix, background);
    }

    private static MenuMeasure RunTool(string themeName, string templateFolder, string componentPrefix, XnaColor background, Action? afterInitialize = null)
    {
        string root = Path.Combine(Path.GetTempPath(), "GumMenuFocus_" + Guid.NewGuid().ToString("N"));
        try
        {
            string templates = FindTemplatesDirectory();
            CopyDirectory(Path.Combine(templates, "FormsBehaviors"), Path.Combine(root, "FormsBehaviors"));
            string projectDir = Path.Combine(root, templateFolder.Replace('/', Path.DirectorySeparatorChar));
            CopyDirectory(Path.Combine(templates, templateFolder.Replace('/', Path.DirectorySeparatorChar)), projectDir);

            return RunScenario(themeName + "-tool", Path.Combine(projectDir, "GumProject.gumx"), afterInitialize, background, componentPrefix);
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
        }
    }
    // The same path as a Menu placed in a screen in the tool: the template's own component, instantiated.
    private static T FromComponent<T>(string componentName) where T : FrameworkElement
    {
        ComponentSave component = ObjectFinder.Self.GetComponent(componentName).ShouldNotBeNull(componentName);
        GraphicalUiElement visual = component.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        return (visual as InteractiveGue)?.FormsControlAsObject.ShouldBeOfType<T>(componentName)!;
    }
    private static MenuMeasure RunScenario(string pngPrefix, string? gumxPath, Action? applyTheme, XnaColor background, string? componentPrefix)
    {
        Dictionary<Type, VisualTemplate> previousTemplates = new(FrameworkElement.DefaultFormsTemplates);
        V3Styling previousStyle = V3Styling.ActiveStyle;
        var previousFontCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        using MenuLease game = new MenuLease(gumxPath);
        try
        {
            applyTheme?.Invoke();
            Mock<ICursor> cursor = new();
            cursor.SetupProperty(c => c.VisualOver);
            cursor.SetupProperty(c => c.WindowPushed);
            cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
            cursor.Setup(c => c.X).Returns(-1000);
            cursor.Setup(c => c.Y).Returns(-1000);
            cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(-1000);
            cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(-1000);
            FormsUtilities.SetCursor(cursor.Object);

            Menu menu = componentPrefix == null ? new Menu() : FromComponent<Menu>(componentPrefix + "Menu");
            MenuItem Item(string header)
            {
                MenuItem item = componentPrefix == null ? new MenuItem() : FromComponent<MenuItem>(componentPrefix + "MenuItem");
                item.Header = header;
                return item;
            }
            menu.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
            menu.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            menu.Visual.YUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
            menu.Visual.YOrigin = global::RenderingLibrary.Graphics.VerticalAlignment.Top;
            menu.X = 0;
            menu.Y = 0;
            menu.Width = CaptureWidth;
            menu.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            MenuItem file = Item("File");
            MenuItem newItem = Item("New");
            MenuItem saveItem = Item("Save");
            file.Items!.Add(newItem);
            file.Items!.Add(Item("Open"));
            file.Items!.Add(saveItem);
            menu.Items!.Add(file);
            MenuItem editItem = Item("Edit");
            menu.Items!.Add(editItem);
            menu.Items!.Add(Item("View"));
            MenuItem helpItem = Item("Help");
            helpItem.IsEnabled = false;
            menu.Items!.Add(helpItem);
            menu.AddToRoot();
            menu.Visual.UpdateLayout();

            XnaColor[] unfocused = Capture(game, background, $"{pngPrefix}-unfocused.png");
            // The bar right of its last item, and a resting item inside its padding, clear of the header.
            float barHeight = menu.Visual.GetAbsoluteHeight();
            XnaColor barFill = unfocused[(int)(barHeight / 2) * CaptureWidth + CaptureWidth - 4];
            XnaColor restingRowFill = unfocused[RowSampleIndex(editItem)];
            XnaColor disabledRowFill = unfocused[RowSampleIndex(helpItem)];

            // The menu sizes to its items, so its own fill sits behind them.
            menu.Visual.GetAbsoluteHeight().ShouldBeGreaterThanOrEqualTo(file.Visual.GetAbsoluteHeight(), pngPrefix);

            menu.IsFocused = true;
            file.IsHighlighted.ShouldBeTrue(pngPrefix);
            XnaColor[] focused = Capture(game, background, $"{pngPrefix}-focused.png");
            var headerText = (file.Visual.GetGraphicalUiElementByName("TextInstance")?.RenderableComponent as global::RenderingLibrary.Graphics.Text).ShouldNotBeNull(pngPrefix);
            XnaColor highlightedHeaderColor = new XnaColor(headerText.Red, headerText.Green, headerText.Blue);

            int left = (int)file.Visual.AbsoluteLeft - 2;
            int top = (int)file.Visual.AbsoluteTop - 2;
            int right = (int)(file.Visual.AbsoluteLeft + file.Visual.GetAbsoluteWidth()) + 2;
            int bottom = (int)(file.Visual.AbsoluteTop + file.Visual.GetAbsoluteHeight()) + 2;
            List<string> changedOutsideFile = new();
            for (int y = 0; y < CaptureHeight; y++)
            {
                for (int x = 0; x < CaptureWidth; x++)
                {
                    bool insideFile = x >= left && x <= right && y >= top && y <= bottom;
                    if (!insideFile && unfocused[y * CaptureWidth + x] != focused[y * CaptureWidth + x] && changedOutsideFile.Count < 5)
                    {
                        changedOutsideFile.Add($"({x}, {y})");
                    }
                }
            }
            changedOutsideFile.ShouldBeEmpty($"{pngPrefix}: focusing the menu changed pixels outside its first item");

            // A menu bar's bottom separator stays visible under the highlighted item (#6715).
            GraphicalUiElement? separator = null;
            foreach (GraphicalUiElement child in menu.Visual.Children)
            {
                if (child.Name != null && child.Name.Contains("Separator") && child.Visible)
                {
                    separator = child;
                }
            }
            if (separator != null)
            {
                int separatorTop = (int)separator.AbsoluteTop;
                int separatorBottom = (int)(separator.AbsoluteTop + separator.GetAbsoluteHeight());
                List<string> changedSeparator = new();
                for (int y = separatorTop; y < separatorBottom; y++)
                {
                    for (int x = (int)file.Visual.AbsoluteLeft; x < (int)(file.Visual.AbsoluteLeft + file.Visual.GetAbsoluteWidth()); x++)
                    {
                        if (unfocused[y * CaptureWidth + x] != focused[y * CaptureWidth + x] && changedSeparator.Count < 5)
                        {
                            changedSeparator.Add($"({x}, {y})");
                        }
                    }
                }
                changedSeparator.ShouldBeEmpty($"{pngPrefix}: the highlighted item covers the menu's bottom separator");
            }

            Press(Keys.Down);
            file.IsPopupVisible.ShouldBeTrue(pngPrefix);
            newItem.IsHighlighted.ShouldBeTrue(pngPrefix);
            XnaColor[] open = Capture(game, background, $"{pngPrefix}-open.png");
            XnaColor openHeaderColor = new XnaColor(headerText.Red, headerText.Green, headerText.Blue);
            XnaColor? openIndicatorColor = IndicatorColor(file.Visual);

            GraphicalUiElement header = file.Visual.GetGraphicalUiElementByName("TextInstance").ShouldNotBeNull(pngPrefix);
            // Inside the padding at mid-height, clear of the header and of any rounded corner; with no padding,
            // the top row, above the header.
            float inset = header.AbsoluteLeft - file.Visual.AbsoluteLeft;
            int sampleX = (int)(file.Visual.AbsoluteLeft + (inset >= 4 ? inset / 2 : 1));
            int sampleY = inset >= 4 ? (int)(file.Visual.AbsoluteTop + file.Visual.GetAbsoluteHeight() / 2) : (int)file.Visual.AbsoluteTop + 1;
            int edgeY = (int)(file.Visual.AbsoluteTop + file.Visual.GetAbsoluteHeight() / 2);
            XnaColor edge = focused[edgeY * CaptureWidth + (int)file.Visual.AbsoluteLeft];
            XnaColor corner = focused[((int)file.Visual.AbsoluteTop + 1) * CaptureWidth + (int)file.Visual.AbsoluteLeft + 1];
            XnaColor openCorner = open[((int)file.Visual.AbsoluteTop + 1) * CaptureWidth + (int)file.Visual.AbsoluteLeft + 1];

            // The popup: its fill inside the last row's padding (the row draws nothing while not highlighted),
            // its border as the left-edge pixel least like the fill (a dashed border has gaps), and that row's text color.
            GraphicalUiElement popup = file.itemsPopup.ShouldNotBeNull(pngPrefix).Visual;
            popup.AbsoluteTop.ShouldBeGreaterThanOrEqualTo(file.Visual.AbsoluteTop + file.Visual.GetAbsoluteHeight() - 1, $"{pngPrefix}: popup sits under its item");
            popup.AbsoluteLeft.ShouldBe(file.Visual.AbsoluteLeft, 2f, $"{pngPrefix}: popup lines up with its item");
            GraphicalUiElement saveText = saveItem.Visual.GetGraphicalUiElementByName("TextInstance").ShouldNotBeNull(pngPrefix);
            int fillX = (int)((saveItem.Visual.AbsoluteLeft + saveText.AbsoluteLeft) / 2);
            int fillY = (int)(saveItem.Visual.AbsoluteTop + saveItem.Visual.GetAbsoluteHeight() / 2);
            XnaColor popupFill = open[fillY * CaptureWidth + fillX];
            XnaColor popupBorder = popupFill;
            int borderBottom = Math.Min((int)(popup.AbsoluteTop + popup.GetAbsoluteHeight()) - 10, CaptureHeight - 1);
            for (int y = (int)popup.AbsoluteTop + 10; y <= borderBottom; y++)
            {
                XnaColor candidate = open[y * CaptureWidth + (int)popup.AbsoluteLeft];
                if (ColorDistance(candidate, popupFill) > ColorDistance(popupBorder, popupFill))
                {
                    popupBorder = candidate;
                }
            }
            var saveRenderable = (saveText.RenderableComponent as global::RenderingLibrary.Graphics.Text).ShouldNotBeNull(pngPrefix);
            XnaColor rowTextColor = new XnaColor(saveRenderable.Red, saveRenderable.Green, saveRenderable.Blue);
            string? popupElementName = popup.ElementSave?.Name;

            float heightBefore = popup.GetAbsoluteHeight();
            MenuItem closeItem = Item("Close");
            file.Items!.Add(closeItem);
            float growth = popup.GetAbsoluteHeight() - heightBefore;
            float addedRowHeight = closeItem.Visual.GetAbsoluteHeight();

            // Last, since focusing an item takes focus from the menu.
            editItem.IsFocused = true;
            editItem.IsFocused.ShouldBeTrue(pngPrefix);
            menu.Visual.UpdateLayout();
            XnaColor[] itemFocused = Capture(game, background, $"{pngPrefix}-itemfocused.png");
            XnaColor focusedRowFill = itemFocused[RowSampleIndex(editItem)];

            return new MenuMeasure(inset, focused[sampleY * CaptureWidth + sampleX], highlightedHeaderColor, edge, corner,
                open[sampleY * CaptureWidth + sampleX], openHeaderColor, openIndicatorColor, openCorner,
                popupFill, popupBorder, rowTextColor, popupElementName, popup.Name, growth, addedRowHeight,
                Column(unfocused, CaptureWidth - 8), Column(focused, (int)(file.Visual.AbsoluteLeft + file.Visual.GetAbsoluteWidth() / 2)),
                (int)(menu.Visual.AbsoluteTop + menu.Visual.GetAbsoluteHeight()),
                barFill, barHeight, restingRowFill, disabledRowFill, focusedRowFill);
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

    private const int SeparatorBandHeight = 48;

    // The top SeparatorBandHeight pixels of one column of a capture.
    private static XnaColor[] Column(XnaColor[] pixels, int x)
    {
        XnaColor[] column = new XnaColor[SeparatorBandHeight];
        for (int y = 0; y < SeparatorBandHeight; y++)
        {
            column[y] = pixels[y * CaptureWidth + x];
        }
        return column;
    }

    // Compares the rows from a few above each menu's own bottom edge to a few below it, so a difference
    // in item height (font, padding) does not count, only the separator's thickness, color and gap.
    private static void BottomEdgesShouldMatch(XnaColor[] tool, int toolBottom, XnaColor[] codeOnly, int codeOnlyBottom, string because)
    {
        List<string> differing = new();
        for (int offset = -4; offset < 3; offset++)
        {
            XnaColor toolPixel = tool[toolBottom + offset];
            XnaColor codeOnlyPixel = codeOnly[codeOnlyBottom + offset];
            if (ColorDistance(toolPixel, codeOnlyPixel) > 12)
            {
                differing.Add($"{offset} from the menu bottom: tool {toolPixel} vs code-only {codeOnlyPixel}");
            }
        }
        differing.ShouldBeEmpty(because);
    }

    // Left out: Standard's and Editor's menus draw no separator in either visual; their bars are pinned by
    // Tool_MenuBar_MatchesCodeOnly (#6732).
    public static IEnumerable<object[]> SeparatorPairs()
    {
        HashSet<string> noSeparator = new() { "standard", "editor" };
        foreach (object[] pair in Pairs())
        {
            if (!noSeparator.Contains((string)pair[0]))
            {
                yield return pair;
            }
        }
    }

    // The menu bar's bottom separator has the same thickness and color, and sits the same distance under
    // the bar fill, beside the items (#6713).
    [Theory]
    [MemberData(nameof(SeparatorPairs))]
    public void Tool_MenuBottomSeparator_MatchesCodeOnly(string themeName, Action? applyTheme, string templateFolder, string componentPrefix, XnaColor background)
    {
        MenuMeasure codeOnly = RunScenario(themeName + "-codeonly", gumxPath: null, applyTheme, background, componentPrefix: null);
        MenuMeasure tool = RunTool(themeName, templateFolder, componentPrefix, background);

        BottomEdgesShouldMatch(tool.BarColumn, tool.MenuBottom, codeOnly.BarColumn, codeOnly.MenuBottom,
            $"{themeName}: menu bar column right of the items, unfocused");
    }

    // And under the highlighted first item of a focused menu (#6713).
    [Theory]
    [MemberData(nameof(SeparatorPairs))]
    public void Tool_MenuBottomSeparator_UnderHighlightedItem_MatchesCodeOnly(string themeName, Action? applyTheme, string templateFolder, string componentPrefix, XnaColor background)
    {
        MenuMeasure codeOnly = RunScenario(themeName + "-codeonly", gumxPath: null, applyTheme, background, componentPrefix: null);
        MenuMeasure tool = RunTool(themeName, templateFolder, componentPrefix, background);

        BottomEdgesShouldMatch(tool.FocusedItemColumn, tool.MenuBottom, codeOnly.FocusedItemColumn, codeOnly.MenuBottom,
            $"{themeName}: column under the highlighted item's center, focused");
    }

    // A pixel inside the item's padding at mid-height, or its top row when it has no padding.
    private static int RowSampleIndex(MenuItem item)
    {
        GraphicalUiElement header = item.Visual.GetGraphicalUiElementByName("TextInstance").ShouldNotBeNull();
        float inset = header.AbsoluteLeft - item.Visual.AbsoluteLeft;
        int x = (int)(item.Visual.AbsoluteLeft + (inset >= 4 ? inset / 2 : 1));
        int y = inset >= 4 ? (int)(item.Visual.AbsoluteTop + item.Visual.GetAbsoluteHeight() / 2) : (int)item.Visual.AbsoluteTop + 1;
        return y * CaptureWidth + x;
    }

    // The V3-based code-only visuals tint a sprite; the themed tool templates use a Text named
    // SubmenuIndicatorInstance. Null for a tool template whose indicator is an Icon component (Standard, Editor).
    private static XnaColor? IndicatorColor(GraphicalUiElement visual)
    {
        if (visual is global::Gum.Forms.DefaultVisuals.V3.MenuItemVisual v3)
        {
            return v3.SubmenuIndicatorIcon.Color;
        }
        if (visual.GetGraphicalUiElementByName("SubmenuIndicatorInstance")?.RenderableComponent is global::RenderingLibrary.Graphics.Text text)
        {
            return new XnaColor(text.Red, text.Green, text.Blue);
        }
        return null;
    }

    // Runs one frame of the real focus dispatch with only this key down (pushed, typed and held).
    private static void Press(Keys key)
    {
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new();
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(key)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(key)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyTyped(key)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<Keys> { key });
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);
        try
        {
            global::Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());
        }
        finally
        {
            FrameworkElement.KeyboardsForUiControl.Remove(keyboard.Object);
        }
    }

    // Draws twice (the first draw settles lazy loads). Writes a PNG only when GUM_SCREENSHOT_DIR is set.
    private static XnaColor[] Capture(MenuLease game, XnaColor background, string pngName) =>
        Capture(game.GraphicsDevice, background, pngName, CaptureWidth, CaptureHeight);

    internal static XnaColor[] Capture(GraphicsDevice gd, XnaColor background, string pngName, int width, int height)
    {
        using RenderTarget2D capture = new RenderTarget2D(gd, width, height, false,
            SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        SystemManagers managers = SystemManagers.Default;
        for (int pass = 0; pass < 2; pass++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(background);
            managers.Renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[width * height];
        capture.GetData(pixels);

        string? directory = Environment.GetEnvironmentVariable("GUM_SCREENSHOT_DIR");
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
            using FileStream stream = File.Create(Path.Combine(directory, pngName));
            capture.SaveAsPng(stream, width, height);
        }
        return pixels;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.GetFiles(source))
        {
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), overwrite: true);
        }
        foreach (string directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
        }
    }

    private static string FindTemplatesDirectory()
    {
        string? overrideDirectory = Environment.GetEnvironmentVariable("GUM_TEMPLATES_DIR");
        if (!string.IsNullOrEmpty(overrideDirectory))
        {
            return overrideDirectory;
        }
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string candidate = Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            current = Path.GetDirectoryName(current)!;
        }
        throw new InvalidOperationException("could not locate Tools/Gum.ProjectServices/Templates from " + AppContext.BaseDirectory);
    }

    // Gum on the V3 defaults, or on a tool project when a .gumx is given, plus the shape renderer.
    internal sealed class MenuLease : SharedGameLease
    {
        public MenuLease(string? gumxPath)
        {
            if (gumxPath == null)
            {
                global::Gum.GumService.Default.Initialize(Game, DefaultVisualsVersion.V3);
            }
            else
            {
                global::Gum.GumService.Default.Initialize(Game, gumxPath);
            }
            Gum.Renderables.ShapeRenderer.Self.Initialize(GraphicsDevice, Content);
        }

        protected override void OnDispose()
        {
            if (global::Gum.GumService.Default.IsInitialized)
            {
                global::Gum.GumService.Default.Uninitialize();
            }
        }
    }
}
