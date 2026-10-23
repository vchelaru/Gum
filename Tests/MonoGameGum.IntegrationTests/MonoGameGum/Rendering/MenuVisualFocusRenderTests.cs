using System;
using System.IO;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// The default V3 Menu shows a focus bar under itself while focused (#6696).
/// </summary>
[Collection(SharedGame.CollectionName)]
public class MenuVisualFocusRenderTests : BaseTestClass
{
    private const int CaptureWidth = 400;
    private const int CaptureHeight = 120;

    [Fact]
    public void Focused_DrawsFocusBarUnderTheMenu()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        Menu menu = new Menu();
        menu.Width = 300;
        menu.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        menu.X = 20;
        menu.Visual.XUnits = global::Gum.Converters.GeneralUnitType.PixelsFromSmall;
        menu.Visual.XOrigin = global::RenderingLibrary.Graphics.HorizontalAlignment.Left;
        menu.Y = 20;
        menu.Items.Add(new MenuItem { Header = "File" });
        menu.Items.Add(new MenuItem { Header = "Edit" });
        menu.AddToRoot();
        menu.Visual.UpdateLayout();

        int sampleX = (int)(menu.Visual.AbsoluteLeft + menu.Visual.GetAbsoluteWidth() / 2);
        int sampleY = (int)(menu.Visual.AbsoluteTop + menu.Visual.GetAbsoluteHeight() + 3);
        XnaColor warning = ((MenuVisual)menu.Visual).FocusedIndicatorColor;

        XnaColor unfocused = Capture(game, "6696-menu-unfocused.png")[sampleY * CaptureWidth + sampleX];

        menu.IsFocused = true;
        XnaColor focused = Capture(game, "6696-menu-focused.png")[sampleY * CaptureWidth + sampleX];

        unfocused.ShouldNotBe(warning);
        focused.ShouldBe(warning, $"pixel at ({sampleX}, {sampleY})");
    }

    // Draws twice (the first draw settles lazy loads) and writes the capture to the temp folder.
    private static XnaColor[] Capture(MinimalGame game, string pngName)
    {
        GraphicsDevice gd = game.GraphicsDevice;
        using RenderTarget2D capture = new RenderTarget2D(gd, CaptureWidth, CaptureHeight, false,
            SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
        SystemManagers managers = game.GumService.SystemManagers;
        for (int pass = 0; pass < 2; pass++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(XnaColor.Black);
            managers.Renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        XnaColor[] pixels = new XnaColor[CaptureWidth * CaptureHeight];
        capture.GetData(pixels);

        string directory = Path.Combine(Path.GetTempPath(), "GumMenuVisualFocus");
        Directory.CreateDirectory(directory);
        using (FileStream stream = File.Create(Path.Combine(directory, pngName)))
        {
            capture.SaveAsPng(stream, CaptureWidth, CaptureHeight);
        }
        return pixels;
    }

    private sealed class MinimalGame : SharedGameLease
    {
        public global::Gum.GumService GumService { get; } = new global::Gum.GumService();

        protected override void OnInitialize() => GumService.Initialize(Game, global::Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
        }
    }
}
