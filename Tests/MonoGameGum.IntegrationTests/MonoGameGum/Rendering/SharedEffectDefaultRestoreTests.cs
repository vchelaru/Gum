using System;
using System.Linq;
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
/// Containers that share one compiled effect (#7037): a container that does not set a parameter another
/// container set must read the value the effect had before Gum first wrote it (its compiled default), not 0.
/// The shader has non-zero defaults and no &lt;Name&gt;IsSet companion, and writes its parameters out as the
/// pixel color: red is Level, green and blue are the two components of Vec.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class SharedEffectDefaultRestoreTests : BaseTestClass
{
    private const string DefaultsSlang = @"
Texture2D SpriteTexture;
SamplerState SpriteTextureSampler;

float Level = 0.5;
float2 Vec = float2(0.25, 0.75);

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

[shader(""fragment"")]
float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float4 sprite = SpriteTexture.Sample(SpriteTextureSampler, input.TextureCoordinates);
    return float4(Level, Vec.x, Vec.y, 1.0) * sprite.a;
}
";

    private const int CaptureSize = 128;
    private const int ContainerSpacing = 40;

    // 0.5 / 0.25 / 0.75 as bytes, with room for the 8 bit rounding.
    private const int DefaultLevel = 128;
    private const int DefaultVecX = 64;
    private const int DefaultVecY = 191;

    [Fact]
    public void SharedEffect_AfterAContainerSetsAFloat_AnUnsetContainerReadsTheCompiledDefault()
    {
        Color[] data = Render(Containers(a => a.SetShaderParameter("Level", 1f), b => { }));

        Channel(data, 0, 'R').ShouldBe(255);
        Channel(data, 1, 'R').ShouldBe(DefaultLevel, 2);
    }

    [Fact]
    public void SharedEffect_AfterAContainerSetsAVector_AnUnsetContainerReadsTheCompiledDefault()
    {
        Color[] data = Render(Containers(a => a.SetShaderParameter("Vec", 1f, 0f), b => { }));

        Channel(data, 0, 'G').ShouldBe(255);
        Channel(data, 0, 'B').ShouldBe(0);
        Channel(data, 1, 'G').ShouldBe(DefaultVecX, 2);
        Channel(data, 1, 'B').ShouldBe(DefaultVecY, 2);
    }

    [Fact]
    public void SharedEffect_WhenTheFirstContainerSetsTheValueTwice_TheBaselineIsStillTheCompiledDefault()
    {
        Color[] data = Render(
            Containers(a => a.SetShaderParameter("Level", 1f), b => { }),
            changeAfterFirstFrame: a => a.SetShaderParameter("Level", 0.8f));

        Channel(data, 0, 'R').ShouldBe(204, 2);
        Channel(data, 1, 'R').ShouldBe(DefaultLevel, 2);
    }

    [Fact]
    public void SharedEffect_WithThreeContainers_AnUnsetOneReadsTheDefaultAfterTwoDifferentValues()
    {
        Color[] data = Render(Containers(
            a => a.SetShaderParameter("Level", 1f),
            b => b.SetShaderParameter("Level", 0.2f),
            c => { }));

        Channel(data, 0, 'R').ShouldBe(255);
        Channel(data, 1, 'R').ShouldBe(51, 2);
        Channel(data, 2, 'R').ShouldBe(DefaultLevel, 2);
    }

    [Fact]
    public void SharedEffect_AfterClearingTheParameters_TheContainerReadsTheCompiledDefault()
    {
        Color[] data = Render(
            Containers(a => a.SetShaderParameter("Level", 1f), b => { }),
            changeAfterFirstFrame: a => a.ClearShaderParameters());

        Channel(data, 0, 'R').ShouldBe(DefaultLevel, 2);
        Channel(data, 1, 'R').ShouldBe(DefaultLevel, 2);
    }

    [Fact]
    public void Effect_GumNeverWrote_KeepsAValueTheGameSetDirectly()
    {
        Color[] data = Render(Containers(a => { }, b => { }), gameSetsLevel: 0.9f);

        Channel(data, 0, 'R').ShouldBe(230, 2);
        Channel(data, 1, 'R').ShouldBe(230, 2);
    }

    private static int Channel(Color[] data, int containerIndex, char channel)
    {
        Color pixel = data[(20 * CaptureSize) + (containerIndex * ContainerSpacing) + 20];
        return channel switch { 'R' => pixel.R, 'G' => pixel.G, _ => pixel.B };
    }

    private static Action<ContainerRuntime>[] Containers(params Action<ContainerRuntime>[] setters) => setters;

    private Color[] Render(Action<ContainerRuntime>[] setters, Action<ContainerRuntime>? changeAfterFirstFrame = null, float? gameSetsLevel = null)
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        Result<SlangFxConversion, ShaderError[]> conversion = SlangFrontend.ConvertToFx(
            DefaultsSlang, new SlangConvertOptions { SourceName = "Defaults.slang" });
        conversion.IsSuccess.ShouldBeTrue(conversion.IsFailure ? string.Join("\n", conversion.Error.Select(e => e.Message)) : "");
        EffectCompiler compiler = new();
        Result<CompiledShader, ShaderError[]> compileResult =
            compiler.Compile(conversion.Value.FxText, new CompilerOptions { Target = PlatformTarget.OpenGL }, TestContext.Current.CancellationToken);
        compileResult.IsSuccess.ShouldBeTrue(compileResult.IsFailure ? string.Join("\n", compileResult.Error.Select(e => e.Message)) : "");
        using Effect effect = new(gd, compileResult.Value.Data);

        // The compiled defaults are what the whole fix rests on.
        effect.Parameters["Level"]!.GetValueSingle().ShouldBe(0.5f);
        if (gameSetsLevel != null)
        {
            effect.Parameters["Level"]!.SetValue(gameSetsLevel.Value);
        }

        ContainerRuntime[] containers = new ContainerRuntime[setters.Length];
        for (int i = 0; i < setters.Length; i++)
        {
            ContainerRuntime container = new() { X = i * ContainerSpacing, Y = 0, Width = 40, Height = 40, IsRenderTarget = true };
#pragma warning disable CS0618
            ColoredRectangleRuntime square = new();
#pragma warning restore CS0618
            square.X = 8;
            square.Y = 8;
            square.Width = 24;
            square.Height = 24;
            square.Color = Color.Red;
            container.AddChild(square);
            container.RenderTargetEffect = effect;
            setters[i](container);
            container.AddToManagers(managers, null);
            container.UpdateLayout();
            containers[i] = container;
        }

        Color[] data = RenderToCapture(gd, renderer, managers);
        if (changeAfterFirstFrame != null)
        {
            changeAfterFirstFrame(containers[0]);
            data = RenderToCapture(gd, renderer, managers);
        }
        return data;
    }

    private static Color[] RenderToCapture(GraphicsDevice gd, Renderer renderer, SystemManagers managers)
    {
        using RenderTarget2D capture = new(gd, CaptureSize, CaptureSize, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);

        for (int i = 0; i < 2; i++)
        {
            gd.SetRenderTarget(capture);
            gd.Clear(Color.Black);
            renderer.Draw(managers);
        }
        gd.SetRenderTarget(null);

        Color[] data = new Color[CaptureSize * CaptureSize];
        capture.GetData(data);
        return data;
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
