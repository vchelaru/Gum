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

float2 Vec = float2(0.25, 0.75);
float Level = 0.5;

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

    // Same, with a <Name>IsSet companion (like Outline's Thickness): red is Level, green is LevelIsSet.
    private const string CompanionSlang = @"
Texture2D SpriteTexture;
SamplerState SpriteTextureSampler;

float Level = 0.5;
float LevelIsSet = 0.0;

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
    return float4(Level, LevelIsSet, 0.0, 1.0) * sprite.a;
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
        ShouldBeNear(Channel(data, 1, 'R'), DefaultLevel);
    }

    [Fact]
    public void SharedEffect_AfterAContainerSetsAVector_AnUnsetContainerReadsTheCompiledDefault()
    {
        Color[] data = Render(Containers(a => a.SetShaderParameter("Vec", 1f, 0f), b => { }));

        Channel(data, 0, 'G').ShouldBe(255);
        Channel(data, 0, 'B').ShouldBe(0);
        ShouldBeNear(Channel(data, 1, 'G'), DefaultVecX);
        ShouldBeNear(Channel(data, 1, 'B'), DefaultVecY);
    }

    [Fact]
    public void SharedEffect_WhenTheFirstContainerSetsTheValueTwice_TheBaselineIsStillTheCompiledDefault()
    {
        Color[] data = Render(
            Containers(a => a.SetShaderParameter("Level", 1f), b => { }),
            changeAfterFirstFrame: a => a.SetShaderParameter("Level", 0.8f));

        ShouldBeNear(Channel(data, 0, 'R'), 204);
        ShouldBeNear(Channel(data, 1, 'R'), DefaultLevel);
    }

    [Fact]
    public void SharedEffect_WithThreeContainers_AnUnsetOneReadsTheDefaultAfterTwoDifferentValues()
    {
        Color[] data = Render(Containers(
            a => a.SetShaderParameter("Level", 1f),
            b => b.SetShaderParameter("Level", 0.2f),
            c => { }));

        Channel(data, 0, 'R').ShouldBe(255);
        ShouldBeNear(Channel(data, 1, 'R'), 51);
        ShouldBeNear(Channel(data, 2, 'R'), DefaultLevel);
    }

    [Fact]
    public void SharedEffect_AfterClearingTheParameters_TheContainerReadsTheCompiledDefault()
    {
        Color[] data = Render(
            Containers(a => a.SetShaderParameter("Level", 1f), b => { }),
            changeAfterFirstFrame: a => a.ClearShaderParameters());

        ShouldBeNear(Channel(data, 0, 'R'), DefaultLevel);
        ShouldBeNear(Channel(data, 1, 'R'), DefaultLevel);
    }

    [Fact]
    public void Effect_GumNeverWrote_KeepsAValueTheGameSetDirectly()
    {
        Color[] data = Render(Containers(a => { }, b => { }), gameSetsLevel: 0.9f);

        ShouldBeNear(Channel(data, 0, 'R'), 230);
        ShouldBeNear(Channel(data, 1, 'R'), 230);
    }

    // The baseline is "what the application left on the effect", not only the compiled default (#7047): a game
    // that changes a parameter after Gum's first write must see its value on a container that does not set it.
    [Fact]
    public void SharedEffect_WhenTheGameChangesAFloatAfterGumWroteIt_AnUnsetContainerReadsTheGamesValue()
    {
        Color[] data = Render(
            Containers(b => { }, a => a.SetShaderParameter("Level", 1f)),
            afterFirstFrame: (effect, _) => effect.Parameters["Level"]!.SetValue(0.9f));

        ShouldBeNear(Channel(data, 0, 'R'), 230);
        Channel(data, 1, 'R').ShouldBe(255);
    }

    [Fact]
    public void SharedEffect_WhenTheGameChangesAVectorAfterGumWroteIt_AnUnsetContainerReadsTheGamesValue()
    {
        Color[] data = Render(
            Containers(b => { }, a => a.SetShaderParameter("Vec", 1f, 0f)),
            afterFirstFrame: (effect, _) => effect.Parameters["Vec"]!.SetValue(new Vector2(0.8f, 0.2f)));

        ShouldBeNear(Channel(data, 0, 'G'), 204);
        ShouldBeNear(Channel(data, 0, 'B'), 51);
    }

    [Fact]
    public void SharedEffect_WhenTheGameChangesTheValueTwice_AnUnsetContainerReadsTheLatestOne()
    {
        Color[] data = Render(
            Containers(b => { }, a => a.SetShaderParameter("Level", 1f)),
            afterFirstFrame: (effect, _) =>
            {
                effect.Parameters["Level"]!.SetValue(0.9f);
            },
            afterSecondFrame: (effect, _) => effect.Parameters["Level"]!.SetValue(0.2f));

        ShouldBeNear(Channel(data, 0, 'R'), 51);
    }

    // The history belongs to the Effect instance, so an effect that replaces another (a reload) is read afresh.
    [Fact]
    public void SharedEffect_WhenTheEffectIsReplaced_TheNewEffectsOwnValueIsTheBaseline()
    {
        Effect? reloaded = null;
        try
        {
            Color[] data = Render(
                Containers(b => { }, a => a.SetShaderParameter("Level", 1f)),
                afterFirstFrame: (effect, containers) =>
                {
                    reloaded = CompileDefaultsEffect(effect.GraphicsDevice);
                    reloaded.Parameters["Level"]!.SetValue(0.3f);
                    foreach (ContainerRuntime container in containers)
                    {
                        container.RenderTargetEffect = reloaded;
                    }
                });

            ShouldBeNear(Channel(data, 0, 'R'), 77);
            Channel(data, 1, 'R').ShouldBe(255);
        }
        finally
        {
            reloaded?.Dispose();
        }
    }

    // RenderToCapture draws twice by default, which lets the container that sets a value redraw between the
    // game's change and the unset container. A real frame draws once: here the unset container is the first
    // thing drawn after the game's change.
    [Fact]
    public void SharedEffect_WhenTheGameChangesAFloatBetweenSingleDrawFrames_TheFirstUnsetDrawReadsTheGamesValue()
    {
        Color[] data = Render(
            Containers(b => { }, a => a.SetShaderParameter("Level", 1f)),
            afterFirstFrame: (effect, _) => effect.Parameters["Level"]!.SetValue(0.9f),
            drawsPerFrame: 1);

        ShouldBeNear(Channel(data, 0, 'R'), 230);
    }

    [Fact]
    public void SharedEffect_WithACompanion_WhenTheGameChangesTheValueBetweenSingleDrawFrames_TheUnsetContainerReadsTheGamesValueAndANotSetCompanion()
    {
        Color[] data = Render(
            Containers(b => { }, a => a.SetShaderParameter("Level", 1f)),
            afterFirstFrame: (effect, _) => effect.Parameters["Level"]!.SetValue(0.9f),
            drawsPerFrame: 1,
            slang: CompanionSlang);

        ShouldBeNear(Channel(data, 0, 'R'), 230);
        Channel(data, 0, 'G').ShouldBe(0);
        Channel(data, 1, 'G').ShouldBe(255);
    }

    [Fact]
    public void ApplyShaderParameters_WhenTheGameChangesAValueGumWrote_TheNextCallThatDoesNotSetItKeepsTheGamesValue()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Effect effect = CompileDefaultsEffect(game.GraphicsDevice);

        ShaderParameterCollection setsLevel = new();
        setsLevel.Set("Level", ShaderParameterValue.FromFloat(1f));
        ShaderParameterCollection setsNothing = new();

        Renderer.ApplyShaderParameters(setsLevel, effect);
        effect.Parameters["Level"]!.SetValue(0.9f);
        Renderer.ApplyShaderParameters(setsNothing, effect);

        effect.Parameters["Level"]!.GetValueSingle().ShouldBe(0.9f);

        // And it stays the baseline once another container has set the value and let go of it again.
        Renderer.ApplyShaderParameters(setsLevel, effect);
        Renderer.ApplyShaderParameters(setsNothing, effect);

        effect.Parameters["Level"]!.GetValueSingle().ShouldBe(0.9f);
    }

    // ApplyShaderParameters runs for every container with an effect on every draw: once the baselines are
    // captured, applying and restoring must not allocate.
    [Fact]
    public void ApplyShaderParameters_InTheSteadyState_DoesNotAllocate()
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        using Effect effect = CompileDefaultsEffect(game.GraphicsDevice);

        ShaderParameterCollection setsBoth = new();
        setsBoth.Set("Level", ShaderParameterValue.FromFloat(1f));
        setsBoth.Set("Vec", ShaderParameterValue.FromVector2(1f, 0f));
        ShaderParameterCollection setsNothing = new();

        for (int i = 0; i < 10; i++)
        {
            Renderer.ApplyShaderParameters(setsBoth, effect);
            Renderer.ApplyShaderParameters(setsNothing, effect);
        }

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < 1000; i++)
        {
            Renderer.ApplyShaderParameters(setsBoth, effect);
            Renderer.ApplyShaderParameters(setsNothing, effect);
        }
        long allocated = GC.GetAllocatedBytesForCurrentThread() - before;

        allocated.ShouldBe(0);
    }

    private static Effect CompileDefaultsEffect(GraphicsDevice gd) => CompileEffect(gd, DefaultsSlang);

    private static Effect CompileEffect(GraphicsDevice gd, string slang)
    {
        Result<SlangFxConversion, ShaderError[]> conversion = SlangFrontend.ConvertToFx(
            slang, new SlangConvertOptions { SourceName = "Defaults.slang" });
        conversion.IsSuccess.ShouldBeTrue(conversion.IsFailure ? string.Join("\n", conversion.Error.Select(e => e.Message)) : "");
        EffectCompiler compiler = new();
        Result<CompiledShader, ShaderError[]> compileResult =
            compiler.Compile(conversion.Value.FxText, new CompilerOptions { Target = PlatformTarget.OpenGL }, TestContext.Current.CancellationToken);
        compileResult.IsSuccess.ShouldBeTrue(compileResult.IsFailure ? string.Join("\n", compileResult.Error.Select(e => e.Message)) : "");
        return new Effect(gd, compileResult.Value.Data);
    }

    private static void ShouldBeNear(int actual, int expected) =>
        Math.Abs(actual - expected).ShouldBeLessThanOrEqualTo(2, $"expected about {expected}, got {actual}");

    private static int Channel(Color[] data, int containerIndex, char channel)
    {
        Color pixel = data[(20 * CaptureSize) + (containerIndex * ContainerSpacing) + 20];
        return channel switch { 'R' => pixel.R, 'G' => pixel.G, _ => pixel.B };
    }

    private static Action<ContainerRuntime>[] Containers(params Action<ContainerRuntime>[] setters) => setters;

    private Color[] Render(Action<ContainerRuntime>[] setters, Action<ContainerRuntime>? changeAfterFirstFrame = null, float? gameSetsLevel = null,
        Action<Effect, ContainerRuntime[]>? afterFirstFrame = null, Action<Effect, ContainerRuntime[]>? afterSecondFrame = null,
        int drawsPerFrame = 2, string slang = DefaultsSlang)
    {
        using MinimalGame game = new();
        game.RunOneFrame();
        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        using Effect effect = CompileEffect(gd, slang);

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

        Color[] data = RenderToCapture(gd, renderer, managers, drawsPerFrame);
        if (changeAfterFirstFrame != null)
        {
            changeAfterFirstFrame(containers[0]);
            data = RenderToCapture(gd, renderer, managers, drawsPerFrame);
        }
        if (afterFirstFrame != null)
        {
            afterFirstFrame(effect, containers);
            data = RenderToCapture(gd, renderer, managers, drawsPerFrame);
        }
        if (afterSecondFrame != null)
        {
            afterSecondFrame(effect, containers);
            data = RenderToCapture(gd, renderer, managers, drawsPerFrame);
        }
        return data;
    }

    private static Color[] RenderToCapture(GraphicsDevice gd, Renderer renderer, SystemManagers managers, int draws)
    {
        using RenderTarget2D capture = new(gd, CaptureSize, CaptureSize, false, SurfaceFormat.Color, DepthFormat.None, 0,
            RenderTargetUsage.PreserveContents);

        for (int i = 0; i < draws; i++)
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
