using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.Content.AnimationChain;
using Gum.DataTypes;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Per-side border scale overrides reach the drawn MonoGame nine-slice (#6983). The same scenarios
/// run against the Skia nine-slice (SkiaGum.Tests) and the Raylib section layout (RaylibGum.Tests),
/// which must draw identically.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class NineSliceBorderScaleRenderTests : BaseTestClass
{
    // A 3x3 grid of distinct colors; the 1 texel frame is the outer ring.
    private static Color TexelColor(int x, int y) => new((byte)(40 + x * 80), (byte)(40 + y * 80), (byte)0, (byte)255);

    private static Texture2D CreateGridTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, 3, 3);
        Color[] data = new Color[9];
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                data[y * 3 + x] = TexelColor(x, y);
            }
        }
        texture.SetData(data);
        return texture;
    }

    private static Color[] Render(GraphicsDevice graphicsDevice, Texture2D texture, float width, float height,
        float borderScale, float? left, float? top, float? right, float? bottom, bool isTiling,
        params (int X, int Y)[] samples)
    {
        SystemManagers managers = SystemManagers.Default;

        AnimationChain chain = new() { Name = "c" };
        chain.Add(new AnimationFrame { FrameLength = 1f, Texture = texture, RightCoordinate = 1f, BottomCoordinate = 1f });
        AnimationChainList chains = new();
        chains.Add(chain);

        NineSliceRuntime nineSlice = new();
        nineSlice.X = 0;
        nineSlice.Y = 0;
        nineSlice.WidthUnits = DimensionUnitType.Absolute;
        nineSlice.HeightUnits = DimensionUnitType.Absolute;
        nineSlice.Width = width;
        nineSlice.Height = height;
        nineSlice.BorderScale = borderScale;
        nineSlice.CustomBorderScaleLeft = left;
        nineSlice.CustomBorderScaleTop = top;
        nineSlice.CustomBorderScaleRight = right;
        nineSlice.CustomBorderScaleBottom = bottom;
        nineSlice.IsTilingMiddleSections = isTiling;
        nineSlice.AnimationChains = chains;
        nineSlice.CurrentChainName = "c";

        ContainerRuntime root = new();
        root.AddChild(nineSlice);
        root.AddToManagers(managers, null);
        root.UpdateLayout();

        try
        {
            return RenderAndSample(graphicsDevice, managers.Renderer, managers, samples);
        }
        finally
        {
            root.RemoveFromManagers();
        }
    }

    private static void ShouldBe(Color actual, Color expected)
    {
        (actual.R, actual.G, actual.B).ShouldBe((expected.R, expected.G, expected.B));
    }

    [Fact]
    public void Render_NoOverrides_UsesBorderScaleOnEverySide()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateGridTexture(game.GraphicsDevice);

        Color[] pixels = Render(game.GraphicsDevice, texture, 60, 60, 10, null, null, null, null, false,
            (5, 5), (30, 30), (55, 55), (15, 5));

        ShouldBe(pixels[0], TexelColor(0, 0));
        ShouldBe(pixels[1], TexelColor(1, 1));
        ShouldBe(pixels[2], TexelColor(2, 2));
        ShouldBe(pixels[3], TexelColor(1, 0));
    }

    [Fact]
    public void Render_PerSideOverrides_DrawEachBorderAtItsOwnThickness()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateGridTexture(game.GraphicsDevice);

        // Left 20, top 5, right 30, bottom falls back to 10 (BorderScale).
        Color[] pixels = Render(game.GraphicsDevice, texture, 100, 100, 10, 20f, 5f, 30f, null, false,
            (10, 50), (30, 50), (60, 50), (80, 50), (50, 2), (50, 8), (50, 95), (50, 85));

        ShouldBe(pixels[0], TexelColor(0, 1));
        ShouldBe(pixels[1], TexelColor(1, 1));
        ShouldBe(pixels[2], TexelColor(1, 1));
        ShouldBe(pixels[3], TexelColor(2, 1));
        ShouldBe(pixels[4], TexelColor(1, 0));
        ShouldBe(pixels[5], TexelColor(1, 1));
        ShouldBe(pixels[6], TexelColor(1, 2));
        ShouldBe(pixels[7], TexelColor(1, 1));
    }

    [Fact]
    public void Render_ZeroScaleSide_DrawsNoBorderThereAndTheRestFillsTheSpace()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateGridTexture(game.GraphicsDevice);

        // Left 20, top 0, right 5, bottom 10 in a 60x60 nine-slice.
        Color[] pixels = Render(game.GraphicsDevice, texture, 60, 60, 10, 20f, 0f, 5f, null, false,
            (10, 25), (30, 25), (57, 25), (10, 55), (30, 55), (57, 55), (30, 2), (10, 2));

        ShouldBe(pixels[0], TexelColor(0, 1));
        ShouldBe(pixels[1], TexelColor(1, 1));
        ShouldBe(pixels[2], TexelColor(2, 1));
        ShouldBe(pixels[3], TexelColor(0, 2));
        ShouldBe(pixels[4], TexelColor(1, 2));
        ShouldBe(pixels[5], TexelColor(2, 2));
        ShouldBe(pixels[6], TexelColor(1, 1));
        ShouldBe(pixels[7], TexelColor(0, 1));
    }

    [Fact]
    public void Render_ZeroLeftWhileTiling_FillsFromTheLeftEdge()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Texture2D texture = CreateGridTexture(game.GraphicsDevice);

        Color[] pixels = Render(game.GraphicsDevice, texture, 60, 60, 10, 0f, null, null, null, true,
            (2, 30), (2, 2), (57, 30));

        ShouldBe(pixels[0], TexelColor(1, 1));
        ShouldBe(pixels[1], TexelColor(1, 0));
        ShouldBe(pixels[2], TexelColor(2, 1));
    }

    private static Color[] RenderAndSample(GraphicsDevice gd, Renderer renderer, SystemManagers managers,
        (int X, int Y)[] samples)
    {
        const int w = 128;
        const int h = 128;
        using RenderTarget2D capture = new(gd, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);

        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(Color.Black);
            renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        Color[] data = new Color[w * h];
        capture.GetData(data);
        return samples.Select(sample => data[(sample.Y * w) + sample.X]).ToArray();
    }

    private sealed class MinimalGame : SharedGameLease
    {
        public GumService GumService { get; } = new GumService();

        protected override void OnInitialize() => GumService.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);

        protected override void OnDispose()
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
        }
    }
}
