using System;
using System.Collections.Generic;
using System.IO;
using Gum.DataTypes;
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
    }

    private static void ShouldBeClose(XnaColor actual, XnaColor expected, string because)
    {
        int distance = Math.Abs(actual.R - expected.R) + Math.Abs(actual.G - expected.G) + Math.Abs(actual.B - expected.B);
        distance.ShouldBeLessThanOrEqualTo(12, $"{because}: tool {actual} vs code-only {expected}");
    }

    private sealed record MenuMeasure(float HeaderInset, XnaColor HighlightFill, XnaColor HighlightedHeaderColor);
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

    private static MenuMeasure RunTool(string themeName, string templateFolder, string componentPrefix, XnaColor background)
    {
        string root = Path.Combine(Path.GetTempPath(), "GumMenuFocus_" + Guid.NewGuid().ToString("N"));
        try
        {
            string templates = FindTemplatesDirectory();
            CopyDirectory(Path.Combine(templates, "FormsBehaviors"), Path.Combine(root, "FormsBehaviors"));
            string projectDir = Path.Combine(root, templateFolder.Replace('/', Path.DirectorySeparatorChar));
            CopyDirectory(Path.Combine(templates, templateFolder.Replace('/', Path.DirectorySeparatorChar)), projectDir);

            return RunScenario(themeName + "-tool", Path.Combine(projectDir, "GumProject.gumx"), applyTheme: null, background, componentPrefix);
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
            file.Items!.Add(newItem);
            file.Items!.Add(Item("Open"));
            file.Items!.Add(Item("Save"));
            menu.Items!.Add(file);
            menu.Items!.Add(Item("Edit"));
            menu.Items!.Add(Item("View"));
            menu.AddToRoot();
            menu.Visual.UpdateLayout();

            XnaColor[] unfocused = Capture(game, background, $"{pngPrefix}-unfocused.png");

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

            Press(Keys.Down);
            file.IsPopupVisible.ShouldBeTrue(pngPrefix);
            newItem.IsHighlighted.ShouldBeTrue(pngPrefix);
            Capture(game, background, $"{pngPrefix}-open.png");

            GraphicalUiElement header = file.Visual.GetGraphicalUiElementByName("TextInstance").ShouldNotBeNull(pngPrefix);
            // Inside the padding at mid-height, clear of the header and of any rounded corner; with no padding,
            // the top row, above the header.
            float inset = header.AbsoluteLeft - file.Visual.AbsoluteLeft;
            int sampleX = (int)(file.Visual.AbsoluteLeft + (inset >= 4 ? inset / 2 : 1));
            int sampleY = inset >= 4 ? (int)(file.Visual.AbsoluteTop + file.Visual.GetAbsoluteHeight() / 2) : (int)file.Visual.AbsoluteTop + 1;
            return new MenuMeasure(inset, focused[sampleY * CaptureWidth + sampleX], highlightedHeaderColor);
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
    private static XnaColor[] Capture(MenuLease game, XnaColor background, string pngName)
    {
        GraphicsDevice gd = game.GraphicsDevice;
        using RenderTarget2D capture = new RenderTarget2D(gd, CaptureWidth, CaptureHeight, false,
            SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        SystemManagers managers = SystemManagers.Default;
        for (int pass = 0; pass < 2; pass++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(background);
            managers.Renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[CaptureWidth * CaptureHeight];
        capture.GetData(pixels);

        string? directory = Environment.GetEnvironmentVariable("GUM_SCREENSHOT_DIR");
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
            using FileStream stream = File.Create(Path.Combine(directory, pngName));
            capture.SaveAsPng(stream, CaptureWidth, CaptureHeight);
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
    private sealed class MenuLease : SharedGameLease
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
