using System;
using System.Collections.Generic;
using System.IO;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using Shouldly;
using Xunit;
using V3Styling = Gum.Forms.DefaultVisuals.V3.Styling;
using V3MenuVisual = Gum.Forms.DefaultVisuals.V3.MenuVisual;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Each packaged theme's Menu draws its focus bar while focused and nothing there while not (#6706).
/// Set GUM_SCREENSHOT_DIR to also write &lt;theme&gt;-codeonly-&lt;unfocused|focused&gt;.png for visual review;
/// the size and background match the tool template renders so the two can be compared directly.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class ThemedMenuFocusRenderTests : BaseTestClass
{
    private const int CaptureWidth = 300;
    private const int CaptureHeight = 30;

    public static IEnumerable<object[]> Themes()
    {
        yield return new object[] { "bubblegum", (Action)Gum.Themes.Bubblegum.BubblegumTheme.Apply, new XnaColor(255, 240, 249) };
        yield return new object[] { "darkpro", (Action)Gum.Themes.DarkPro.DarkProTheme.Apply, new XnaColor(26, 27, 30) };
        yield return new object[] { "forestglade", (Action)Gum.Themes.ForestGlade.ForestGladeTheme.Apply, new XnaColor(10, 45, 38) };
        yield return new object[] { "hazard", (Action)Gum.Themes.Hazard.HazardTheme.Apply, new XnaColor(10, 10, 8) };
        yield return new object[] { "meadow", (Action)Gum.Themes.Meadow.MeadowTheme.Apply, new XnaColor(247, 237, 214) };
        yield return new object[] { "neon", (Action)Gum.Themes.Neon.NeonTheme.Apply, new XnaColor(6, 6, 18) };
        yield return new object[] { "retro95", (Action)Gum.Themes.Retro95.Retro95Theme.Apply, new XnaColor(192, 192, 192) };
        yield return new object[] { "template", (Action)Gum.Themes.Template.TemplateTheme.Apply, new XnaColor(26, 27, 30) };
    }

    [Theory]
    [MemberData(nameof(Themes))]
    public void Focused_DrawsTheThemesFocusBarUnderTheMenu(string themeName, Action applyTheme, XnaColor background)
    {
        Dictionary<Type, VisualTemplate> previousTemplates = new(FrameworkElement.DefaultFormsTemplates);
        V3Styling previousStyle = V3Styling.ActiveStyle;
        var previousFontCreator = global::Gum.Wireframe.CustomSetPropertyOnRenderable.InMemoryFontCreator;
        using ShapeGameLease game = new ShapeGameLease();
        try
        {
            applyTheme();

            Menu menu = new Menu();
            menu.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
            menu.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
            menu.X = 0;
            menu.Y = 0;
            menu.Width = CaptureWidth;
            menu.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            menu.AddToRoot();
            menu.Visual.UpdateLayout();

            GraphicalUiElement bar = ((V3MenuVisual)menu.Visual).FocusedIndicator;
            int sampleX = CaptureWidth / 2;
            // The hidden bar isn't laid out, so its top comes from the menu's bottom plus the bar's gap.
            int sampleY = (int)(menu.Visual.AbsoluteTop + menu.Visual.GetAbsoluteHeight() + bar.Y);
            sampleY.ShouldBeInRange(0, CaptureHeight - 1, themeName);

            XnaColor unfocused = Capture(game, background, $"{themeName}-codeonly-unfocused.png")[sampleY * CaptureWidth + sampleX];
            menu.IsFocused = true;
            XnaColor focused = Capture(game, background, $"{themeName}-codeonly-focused.png")[sampleY * CaptureWidth + sampleX];

            focused.ShouldNotBe(unfocused, $"{themeName} pixel at ({sampleX}, {sampleY})");
        }
        finally
        {
            FrameworkElement.DefaultFormsTemplates.Clear();
            foreach (KeyValuePair<Type, VisualTemplate> pair in previousTemplates)
            {
                FrameworkElement.DefaultFormsTemplates[pair.Key] = pair.Value;
            }
            V3Styling.ActiveStyle = previousStyle;
            global::Gum.Wireframe.CustomSetPropertyOnRenderable.InMemoryFontCreator = previousFontCreator;
        }
    }

    // Draws twice (the first draw settles lazy loads). Writes a PNG only when GUM_SCREENSHOT_DIR is set.
    private static XnaColor[] Capture(ShapeGameLease game, XnaColor background, string pngName)
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
}
