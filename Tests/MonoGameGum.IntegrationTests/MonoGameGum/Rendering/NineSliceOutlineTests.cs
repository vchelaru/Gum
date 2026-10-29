using System;
using System.IO;
using System.Linq;
using CrossRuntimeTests;
using Gum.DataTypes;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using ShadowDusk.Compiler;
using ShadowDusk.Compiler.Slang;
using ShadowDusk.Core;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// The shipped Outline shader on a container holding a nine-slice with transparent corners (#6984).
/// Each case renders the nine-slice plain and through the shader and checks the second against the
/// first: the outline must follow the nine-slice's alpha with no seams between the slices, no
/// clipping at the container bounds (the container has exactly Thickness of transparent margin),
/// at whole and fractional sizes, tiled and stretched, at different zoom levels. The same cases run
/// against raylib (RaylibGum.Tests) and Skia (SkiaGum.Tests).
/// </summary>
[Collection(SharedGame.CollectionName)]
public class NineSliceOutlineTests : BaseTestClass
{
    private const int CaptureSize = 128;

    private static Effect CompileOutline(GraphicsDevice graphicsDevice)
    {
        Result<SlangFxConversion, ShaderError[]> conversion = SlangFrontend.ConvertToFx(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Outline.slang")), new SlangConvertOptions { SourceName = "Outline.slang" });
        conversion.IsSuccess.ShouldBeTrue(
            conversion.IsFailure ? string.Join("\n", conversion.Error.Select(e => e.Message)) : "");

        EffectCompiler compiler = new();
        Result<CompiledShader, ShaderError[]> compileResult =
            compiler.Compile(conversion.Value.FxText, new CompilerOptions { Target = PlatformTarget.OpenGL }, TestContext.Current.CancellationToken);
        compileResult.IsSuccess.ShouldBeTrue(
            compileResult.IsFailure ? string.Join("\n", compileResult.Error.Select(e => e.Message)) : "");
        return new Effect(graphicsDevice, compileResult.Value.Data);
    }

    private static Texture2D CreateFrameTexture(GraphicsDevice graphicsDevice)
    {
        Texture2D texture = new(graphicsDevice, NineSliceOutlineOracle.TextureSize, NineSliceOutlineOracle.TextureSize);
        Color[] texels = new Color[NineSliceOutlineOracle.TextureSize * NineSliceOutlineOracle.TextureSize];
        for (int y = 0; y < NineSliceOutlineOracle.TextureSize; y++)
        {
            for (int x = 0; x < NineSliceOutlineOracle.TextureSize; x++)
            {
                (byte r, byte g, byte b, byte a) = NineSliceOutlineOracle.Texel(x, y);
                texels[(y * NineSliceOutlineOracle.TextureSize) + x] = new Color(r, g, b, a);
            }
        }
        texture.SetData(texels);
        return texture;
    }

    // Draws a render-target container whose only child is the nine-slice, offset by margin on both axes,
    // optionally through the outline shader, and returns the capture.
    private static (byte R, byte G, byte B, byte A)[] Render(
        GraphicsDevice graphicsDevice, Texture2D texture, Effect? outline, float width, float height,
        bool isTiling, float borderScale, float thickness, float zoom)
    {
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        float margin = thickness;
        ContainerRuntime container = new();
        container.X = 0;
        container.Y = 0;
        container.WidthUnits = DimensionUnitType.Absolute;
        container.HeightUnits = DimensionUnitType.Absolute;
        container.Width = width + (2 * margin);
        container.Height = height + (2 * margin);
        container.IsRenderTarget = true;

        NineSliceRuntime nineSlice = new();
        nineSlice.Texture = texture;
        nineSlice.IsTilingMiddleSections = isTiling;
        nineSlice.BorderScale = borderScale;
        nineSlice.WidthUnits = DimensionUnitType.Absolute;
        nineSlice.HeightUnits = DimensionUnitType.Absolute;
        nineSlice.X = margin;
        nineSlice.Y = margin;
        nineSlice.Width = width;
        nineSlice.Height = height;
        container.AddChild(nineSlice);

        if (outline != null)
        {
            container.RenderTargetEffect = outline;
            container.SetShaderParameter("Thickness", thickness);
            container.SetShaderParameter("OutlineGreen", 128f);
            container.SetShaderParameter("OutlineBlue", 255f);
        }

        renderer.Camera.Zoom = zoom;
        container.AddToManagers(managers, null);
        container.UpdateLayout();

        try
        {
            using RenderTarget2D capture = new(graphicsDevice, CaptureSize, CaptureSize, false, SurfaceFormat.Color,
                DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            for (int i = 0; i < 2; i++)
            {
                graphicsDevice.SetRenderTarget(capture);
                graphicsDevice.Clear(Color.Transparent);
                renderer.Draw(managers);
            }
            graphicsDevice.SetRenderTarget(null);

            Color[] data = new Color[CaptureSize * CaptureSize];
            capture.GetData(data);
            return data.Select(color => (color.R, color.G, color.B, color.A)).ToArray();
        }
        finally
        {
            container.RemoveFromManagers();
            renderer.Camera.Zoom = 1f;
        }
    }

    [Theory]
    [InlineData(24f, 24f, false, 1f, 1f, 1f)]
    [InlineData(40f, 28f, false, 1f, 1f, 1f)]
    [InlineData(40f, 28f, false, 1f, 2f, 1f)]
    [InlineData(40f, 28f, true, 1f, 1f, 1f)]
    [InlineData(40f, 28f, true, 1f, 2f, 1f)]
    [InlineData(41f, 29f, true, 1f, 1f, 1f)]
    [InlineData(40f, 28f, false, 2f, 1f, 1f)]
    [InlineData(40f, 28f, true, 2f, 2f, 1f)]
    [InlineData(40.5f, 27.5f, false, 1f, 1f, 1f)]
    [InlineData(40.5f, 27.5f, true, 1f, 2f, 1f)]
    [InlineData(40f, 28f, false, 1f, 1f, 2f)]
    [InlineData(40f, 28f, true, 1f, 2f, 2f)]
    [InlineData(40.5f, 27.5f, true, 1f, 1f, 2f)]
    public void Outline_OfANineSliceWithTransparentCorners_FollowsItsAlpha(
        float width, float height, bool isTiling, float borderScale, float thickness, float zoom)
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        GraphicsDevice graphicsDevice = game.GraphicsDevice;
        using Texture2D texture = CreateFrameTexture(graphicsDevice);
        using Effect outline = CompileOutline(graphicsDevice);

        var plain = Render(graphicsDevice, texture, null, width, height, isTiling, borderScale, thickness, zoom);
        var outlined = Render(graphicsDevice, texture, outline, width, height, isTiling, borderScale, thickness, zoom);

        // The plain picture is a real nine-slice: a whole rectangle minus the transparent corner
        // texels (3 per corner, each borderScale * zoom pixels square).
        if (width % 1 == 0 && height % 1 == 0)
        {
            float cutTexel = borderScale * zoom;
            int expectedContent = (int)(width * zoom * height * zoom) - (int)(12 * cutTexel * cutTexel);
            NineSliceOutlineOracle.CountContent(plain).ShouldBe(expectedContent);
        }

        int reach = (int)(thickness * zoom);
        NineSliceOutlineOracle.Compare(plain, outlined, CaptureSize, CaptureSize, reach, (0, 128, 255)).ShouldBeEmpty();
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
