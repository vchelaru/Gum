using System;
using System.IO;
using System.Linq;
using Gum;
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

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering.ManualRenderTarget;

/// <summary>
/// Shared setup for the render-target caching tests (#7624): a GumService on the shared device, a capture
/// target, and helpers that build containers and draw whole frames the way a game does.
/// </summary>
internal sealed class ManualRenderTargetScene : SharedGameLease
{
    public const int Size = 160;

    private static Effect? _outline;

    private RenderTarget2D? _capture;
    private readonly Color[] _pixels = new Color[Size * Size];

    public ManualRenderTargetScene()
    {
        Gum.GumService.Default.Initialize(Game, Gum.Forms.DefaultVisualsVersion.V3);
        RunOneFrame();
    }

    protected override Color ClearColor => Color.Black;

    public SystemManagers Managers => SystemManagers.Default;

    public Renderer Renderer => Managers.Renderer;

    public int BakeCount => Renderer.RenderTargetBakeCount;

    protected override void OnDispose()
    {
        _capture?.Dispose();
        Renderer.Camera.Zoom = 1;
        if (Gum.GumService.Default.IsInitialized)
        {
            Gum.GumService.Default.Uninitialize();
        }
    }

    /// <summary>Draws one full frame (sweep included) into the capture target and returns nothing.</summary>
    public void DrawFrame()
    {
        _capture ??= new RenderTarget2D(GraphicsDevice, Size, Size, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        GraphicsDevice.SetRenderTarget(_capture);
        GraphicsDevice.Clear(Color.Black);
        Renderer.Draw(Managers);
        GraphicsDevice.SetRenderTarget(null);
    }

    public void DrawFrames(int count)
    {
        for (int i = 0; i < count; i++)
        {
            DrawFrame();
        }
    }

    /// <summary>Draws a frame and returns a copy of the capture target's pixels.</summary>
    public Color[] CaptureFrame()
    {
        DrawFrame();
        _capture!.GetData(_pixels);
        return (Color[])_pixels.Clone();
    }

    public Color PixelAt(Color[] pixels, int x, int y) => pixels[(y * Size) + x];

    public Effect GetOutline()
    {
        if (_outline == null || _outline.IsDisposed || _outline.GraphicsDevice != GraphicsDevice)
        {
            _outline = CompileOutline(GraphicsDevice);
        }

        return _outline;
    }

    public ContainerRuntime Add(ContainerRuntime container, ContainerRuntime? parent = null)
    {
        if (parent != null)
        {
            parent.AddChild(container);
        }
        else
        {
            container.AddToManagers(Managers, null);
        }

        container.UpdateLayout();
        return container;
    }

    public Texture2D SolidTexture(Color color)
    {
        Texture2D texture = new(GraphicsDevice, 16, 16);
        Color[] pixels = new Color[16 * 16];
        Array.Fill(pixels, color);
        texture.SetData(pixels);
        return texture;
    }

    /// <summary>
    /// A render-target container holding a 24x24 sprite inset by 4 and a smaller second sprite, so the content
    /// has opaque edges, transparent margins and two colors.
    /// </summary>
    public ContainerRuntime CreateContainer(
        float x, float y, Color color, bool manual, bool withEffect, float width = 40, float height = 40)
    {
        ContainerRuntime container = new();
        container.X = x;
        container.Y = y;
        container.Width = width;
        container.Height = height;
        container.IsRenderTarget = true;
        container.RenderTargetRefreshMode = manual ? RenderTargetRefreshMode.Manual : RenderTargetRefreshMode.EveryFrame;

        container.AddChild(CreateSprite(color, 4, 4, 24, 24));
        container.AddChild(CreateSprite(Color.Blue, 20, 20, 12, 12));

        if (withEffect)
        {
            ApplyOutline(container);
        }

        return container;
    }

    public void ApplyOutline(ContainerRuntime container)
    {
        container.RenderTargetEffect = GetOutline();
        container.SetShaderParameter("Thickness", 2f);
        container.SetShaderParameter("OutlineRed", 255f);
        container.SetShaderParameter("OutlineGreen", 217f);
        container.SetShaderParameter("OutlineBlue", 26f);
    }

    public SpriteRuntime CreateSprite(Color color, float x, float y, float width, float height)
    {
        SpriteRuntime sprite = new();
        sprite.Texture = SolidTexture(color);
        sprite.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        sprite.X = x;
        sprite.Y = y;
        sprite.Width = width;
        sprite.Height = height;
        return sprite;
    }

    public bool HasCached(ContainerRuntime container) =>
        Renderer.HasCachedRenderTarget((IRenderableIpso)container.RenderableComponent);

    public static void ShouldBeIdentical(Color[] expected, Color[] actual, string because)
    {
        expected.Length.ShouldBe(actual.Length);
        int different = 0;
        int firstIndex = -1;
        for (int i = 0; i < expected.Length; i++)
        {
            if (expected[i] != actual[i])
            {
                different++;
                if (firstIndex < 0)
                {
                    firstIndex = i;
                }
            }
        }

        different.ShouldBe(0,
            $"{because}: {different} pixels differ, first at ({firstIndex % Size},{firstIndex / Size}): " +
            $"expected {(firstIndex >= 0 ? expected[firstIndex].ToString() : "")} got {(firstIndex >= 0 ? actual[firstIndex].ToString() : "")}");
    }

    public static int CountNonBlack(Color[] pixels) =>
        pixels.Count(pixel => pixel.R != 0 || pixel.G != 0 || pixel.B != 0);

    private static Effect CompileOutline(GraphicsDevice gd)
    {
        Result<SlangFxConversion, ShaderError[]> conversion = SlangFrontend.ConvertToFx(
            File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Outline.slang")),
            new SlangConvertOptions { SourceName = "Outline.slang" });
        conversion.IsSuccess.ShouldBeTrue(
            conversion.IsFailure ? string.Join("\n", conversion.Error.Select(e => e.Message)) : "");

        EffectCompiler compiler = new();
        Result<CompiledShader, ShaderError[]> compileResult = compiler.Compile(
            conversion.Value.FxText, new CompilerOptions { Target = PlatformTarget.OpenGL }, TestContext.Current.CancellationToken);
        compileResult.IsSuccess.ShouldBeTrue(
            compileResult.IsFailure ? string.Join("\n", compileResult.Error.Select(e => e.Message)) : "");
        return new Effect(gd, compileResult.Value.Data);
    }
}
