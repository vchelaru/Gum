using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using KernSmith.Gum;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using RenderingLibrary.Graphics.Fonts;
using Shouldly;
using System.Collections.Generic;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Issue #6814: oversampled text must land each atlas pixel on exactly one screen pixel, so a glyph
/// on screen is a copy of its atlas bitmap. Any other scale resamples the glyph and doubles or drops
/// pixel rows and columns. The atlas itself is the expected image, so no stored baseline is needed.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class OversampledTextPixelScaleTests : BaseTestClass
{
    private const int CaptureWidth = 160;
    private const int CaptureHeight = 120;

    // Linear filtering turns any scale other than 1:1 into blended pixel values, so a resampled glyph
    // misses by far more than this. The allowance covers the glyph tie-break bias (1/64 of a screen
    // pixel), which under linear filtering moves each value by up to about 4.
    private const int MaxChannelDifference = 4;

    private static readonly string TestFontPath = System.IO.Path.GetFullPath(
        System.IO.Path.Combine(System.AppContext.BaseDirectory, "..", "..", "..", "..", "SkiaGum.Tests", "Assets", "Fonts", "TestFont.ttf"));

    [Theory]
    [InlineData('2', 1.3953f)]
    [InlineData('K', 1.3953f)]
    [InlineData('9', 1.75f)]
    public void OversampledGlyph_DrawsAsAnExactCopyOfItsAtlasBitmap(char character, float zoom)
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        TextRuntime text = new();
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(game.GraphicsDevice);
            TextRuntime.UseFontOversampling = true;
            Renderer.TextureFilter = TextureFilter.Linear;
            managers.Renderer.Camera.Zoom = zoom;

            text.UseCustomFont = true;
            text.CustomFontFile = TestFontPath;
            text.FontSize = 26;
            text.WidthUnits = DimensionUnitType.RelativeToChildren;
            text.HeightUnits = DimensionUnitType.RelativeToChildren;
            text.Text = character.ToString();
            text.X = 10.3f;
            text.Y = 10.6f;
            text.AddToManagers(managers, null);

            XnaColor[] screen = Render(game.GraphicsDevice, managers, CaptureWidth, CaptureHeight);

            BitmapFont displayFont = ((Text)text.RenderableComponent).BitmapFont!;
            byte[,] atlasGlyph = ReadAtlasGlyph(displayFont, character);
            byte[,] screenGlyph = Crop(CaptureWidth, CaptureHeight, (x, y) => screen[(y * CaptureWidth) + x].R);

            screenGlyph.GetLength(0).ShouldBe(atlasGlyph.GetLength(0), "because the drawn glyph must be exactly as wide as its atlas bitmap");
            screenGlyph.GetLength(1).ShouldBe(atlasGlyph.GetLength(1), "because the drawn glyph must be exactly as tall as its atlas bitmap");
            List<string> differences = new();
            for (int x = 0; x < atlasGlyph.GetLength(0); x++)
            {
                for (int y = 0; y < atlasGlyph.GetLength(1); y++)
                {
                    if (System.Math.Abs(screenGlyph[x, y] - atlasGlyph[x, y]) > MaxChannelDifference)
                    {
                        differences.Add($"({x},{y}) screen {screenGlyph[x, y]} atlas {atlasGlyph[x, y]}");
                    }
                }
            }
            differences.ShouldBeEmpty();
        }
        finally
        {
            text.RemoveFromManagers();
            managers.Renderer.Camera.Zoom = 1f;
            Renderer.TextureFilter = TextureFilter.Point;
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // Issue #6814: the display font's line height, drawn at exactly 1/ratio, differs from the native
    // line height by rounding, and that difference would add up line after line. Lines step by the
    // pinned native line height instead, so a block of lines stays as tall as its measured box.
    [Fact]
    public void OversampledMultiLineText_StepsLinesByTheNativeLineHeight()
    {
        // At this size and zoom the oversampled line height is 2.6 screen pixels short of the native one.
        const float zoom = 2.85f;
        const int lineCount = 6;
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        IInMemoryFontCreator? savedCreator = CustomSetPropertyOnRenderable.InMemoryFontCreator;
        bool savedUseFontOversampling = TextRuntime.UseFontOversampling;
        TextRuntime text = new();
        try
        {
            CustomSetPropertyOnRenderable.InMemoryFontCreator = new KernSmithFontCreator(game.GraphicsDevice);
            TextRuntime.UseFontOversampling = true;
            managers.Renderer.Camera.Zoom = zoom;

            text.UseCustomFont = true;
            text.CustomFontFile = TestFontPath;
            text.FontSize = 12;
            text.WidthUnits = DimensionUnitType.RelativeToChildren;
            text.HeightUnits = DimensionUnitType.RelativeToChildren;
            text.Text = string.Join("\n", System.Linq.Enumerable.Repeat("HH", lineCount));
            text.X = 4;
            text.Y = 4;
            text.AddToManagers(managers, null);
            int nativeLineHeight = ((Text)text.RenderableComponent).BitmapFont!.LineHeightInPixels;

            XnaColor[] screen = Render(game.GraphicsDevice, managers, width: 120, height: 400);

            List<int> lineTops = FindLitRowRunTops(screen, width: 120, height: 400);
            lineTops.Count.ShouldBe(lineCount);
            float expectedDistance = (lineCount - 1) * nativeLineHeight * zoom;
            ((float)(lineTops[lineCount - 1] - lineTops[0])).ShouldBe(expectedDistance, tolerance: 1f,
                "because lines must step by the native line height, not drift by the oversampled font's rounding");
        }
        finally
        {
            text.RemoveFromManagers();
            managers.Renderer.Camera.Zoom = 1f;
            TextRuntime.UseFontOversampling = savedUseFontOversampling;
            CustomSetPropertyOnRenderable.InMemoryFontCreator = savedCreator;
        }
    }

    // The top row of each run of rows that contain a lit pixel.
    private static List<int> FindLitRowRunTops(XnaColor[] pixels, int width, int height)
    {
        List<int> tops = new();
        bool previousLit = false;
        for (int y = 0; y < height; y++)
        {
            bool lit = false;
            for (int x = 0; x < width && !lit; x++)
            {
                lit = pixels[(y * width) + x].R > 0;
            }
            if (lit && !previousLit)
            {
                tops.Add(y);
            }
            previousLit = lit;
        }
        return tops;
    }

    private static byte[,] ReadAtlasGlyph(BitmapFont font, char character)
    {
        BitmapCharacterInfo info = font.GetCharacterInfo(character)!;
        Texture2D page = font.Texture!;
        int left = info.GetPixelLeft(page);
        int top = info.GetPixelTop(page);
        int width = info.GetPixelRight(page) - left;
        int height = info.GetPixelBottom(page) - top;
        XnaColor[] pixels = new XnaColor[width * height];
        page.GetData(0, new Microsoft.Xna.Framework.Rectangle(left, top, width, height), pixels, 0, pixels.Length);
        return Crop(width, height, (x, y) => pixels[(y * width) + x].A);
    }

    // Trims the image to the bounding box of its non-zero values.
    private static byte[,] Crop(int width, int height, System.Func<int, int, byte> valueAt)
    {
        int minX = width, minY = height, maxX = -1, maxY = -1;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (valueAt(x, y) > 0)
                {
                    minX = System.Math.Min(minX, x);
                    minY = System.Math.Min(minY, y);
                    maxX = System.Math.Max(maxX, x);
                    maxY = System.Math.Max(maxY, y);
                }
            }
        }

        if (maxX < 0)
        {
            return new byte[0, 0];
        }

        byte[,] result = new byte[maxX - minX + 1, maxY - minY + 1];
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                result[x - minX, y - minY] = valueAt(x, y);
            }
        }
        return result;
    }

    private static XnaColor[] Render(GraphicsDevice graphicsDevice, SystemManagers managers, int width, int height)
    {
        using RenderTarget2D capture = new(graphicsDevice, width, height, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        // The first draw's pre-render pass generates the oversampled font for the camera zoom.
        for (int i = 0; i < 2; i++)
        {
            graphicsDevice.SetRenderTarget(capture);
            graphicsDevice.Clear(XnaColor.Black);
            managers.Renderer.Draw(managers);
        }
        graphicsDevice.SetRenderTarget(null);

        XnaColor[] data = new XnaColor[width * height];
        capture.GetData(data);
        return data;
    }

    private sealed class MinimalGame : SharedGameLease
    {
        protected override void OnInitialize() => Gum.GumService.Default.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (Gum.GumService.Default.IsInitialized)
            {
                Gum.GumService.Default.Uninitialize();
            }
        }
    }
}
