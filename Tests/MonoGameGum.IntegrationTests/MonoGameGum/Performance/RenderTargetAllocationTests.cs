using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.GueDeriving;
using MonoGameGum.TestsCommon;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using ShadowDusk.Compiler;
using ShadowDusk.Compiler.Slang;
using ShadowDusk.Core;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Performance;

/// <summary>
/// A scene of static render-target containers must not allocate managed memory per frame (#7619).
/// Each frame still bakes and composites every container, so the cost scales with the container
/// count; the guard is bytes per container per frame.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class RenderTargetAllocationTests : BaseTestClass
{
    private const int ContainerCount = 20;

    private readonly ITestOutputHelper _output;

    public RenderTargetAllocationTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void StaticRenderTargetContainers_WithOutlineShader_AllocateNothingPerFrame()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        // The same shader and parameters the outline sample sets.
        using Effect outline = CompileOutline(game.GraphicsDevice);
        double bytesPerContainer = BuildAndMeasure(game.GraphicsDevice, outline);

        // Liveness: the effect-bound blit really ran, otherwise a low number proves nothing.
        SystemManagers.Default.Renderer.SpriteRenderer.LastFrameDrawStates
            .Any(state => ReferenceEquals(state.Effect, outline)).ShouldBeTrue();

        bytesPerContainer.ShouldBeLessThanOrEqualTo(MaxBytesPerContainerPerFrame,
            $"measured {bytesPerContainer:N1} bytes per container per frame");
    }

    [Fact]
    public void StaticRenderTargetContainers_WithoutShader_AllocateNothingPerFrame()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        double bytesPerContainer = BuildAndMeasure(game.GraphicsDevice, effect: null);

        SystemManagers.Default.Renderer.SpriteRenderer.LastFrameDrawStates.Any().ShouldBeTrue();

        bytesPerContainer.ShouldBeLessThanOrEqualTo(MaxBytesPerContainerPerFrame,
            $"measured {bytesPerContainer:N1} bytes per container per frame");
    }

    // Residual per container per frame (the per-frame fixed cost divided across the containers
    // included). Before #7619 this was about 800.
    private const double MaxBytesPerContainerPerFrame = 8;

    private double BuildAndMeasure(GraphicsDevice gd, Effect? effect)
    {
        List<ContainerRuntime> containers = BuildContainers(gd, effect);
        try
        {
            return MeasureBytesPerContainerPerFrame(gd);
        }
        finally
        {
            foreach (ContainerRuntime container in containers)
            {
                container.RemoveFromManagers();
            }
        }
    }

    private double MeasureBytesPerContainerPerFrame(GraphicsDevice gd)
    {
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        using RenderTarget2D scene = new(gd, 256, 256, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                gd.SetRenderTarget(scene);
                gd.Clear(Color.Black);
                renderer.Draw(managers);
                gd.SetRenderTarget(null);
            },
            attempts: 3,
            warmupIterations: 30,
            measuredIterations: 100);

        double perContainer = result.BytesPerIteration / ContainerCount;
        _output.WriteLine($"{ContainerCount} static render-target containers: {result.BytesPerIteration:N0} bytes/frame, " +
            $"{perContainer:N1} bytes per container per frame");
        return perContainer;
    }

    private static List<ContainerRuntime> BuildContainers(GraphicsDevice gd, Effect? effect)
    {
        List<ContainerRuntime> containers = new();
        Texture2D texture = new(gd, 16, 16);
        Color[] pixels = new Color[16 * 16];
        Array.Fill(pixels, Color.Red);
        texture.SetData(pixels);

        SystemManagers managers = SystemManagers.Default;
        for (int i = 0; i < ContainerCount; i++)
        {
            ContainerRuntime container = new();
            container.X = (i % 5) * 40;
            container.Y = (i / 5) * 40;
            container.Width = 32;
            container.Height = 32;
            container.IsRenderTarget = true;

            SpriteRuntime sprite = new();
            sprite.Texture = texture;
            sprite.WidthUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            sprite.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            sprite.Width = 24;
            sprite.Height = 24;
            sprite.X = 4;
            sprite.Y = 4;
            container.AddChild(sprite);

            if (effect != null)
            {
                container.RenderTargetEffect = effect;
                container.SetShaderParameter("Thickness", 2f);
                container.SetShaderParameter("OutlineRed", 255f);
                container.SetShaderParameter("OutlineGreen", 217f);
                container.SetShaderParameter("OutlineBlue", 26f);
            }

            container.AddToManagers(managers, null);
            container.UpdateLayout();
            containers.Add(container);
        }

        return containers;
    }

    private static Effect CompileOutline(GraphicsDevice gd)
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
        return new Effect(gd, compileResult.Value.Data);
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
