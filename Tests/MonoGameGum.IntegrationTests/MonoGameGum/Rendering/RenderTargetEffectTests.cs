using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using ShadowDusk.Compiler;
using ShadowDusk.Core;
using Shouldly;
using Xunit;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// Proves that a render-target container's <see cref="ContainerRuntime.RenderTargetEffect"/> is
/// bound to the SpriteBatch when the container's cached texture is blitted back to the screen
/// (issue #816). A real <see cref="BasicEffect"/> stands in for a user post-process shader so the
/// binding can be verified without shipping a compiled .fx in the test project.
/// </summary>
public class RenderTargetEffectTests : BaseTestClass
{
    [Fact]
    public void RenderTargetEffect_ShouldBeBound_WhenRenderTargetContainerIsDrawn()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        ContainerRuntime container = new();
        container.Width = 100;
        container.Height = 100;
        container.IsRenderTarget = true;

        TextRuntime text = new();
        text.Text = "Hello";
        container.AddChild(text);

        BasicEffect effect = new BasicEffect(game.GraphicsDevice);
        container.RenderTargetEffect = effect;

        container.AddToManagers(managers, null);
        container.UpdateLayout();

        try
        {
            // First Draw does one-time render-target setup; draw twice so the effect-bound blit
            // is captured in LastFrameDrawStates on a steady-state frame.
            renderer.Draw(managers);
            renderer.Draw(managers);

            bool effectWasBound = renderer.SpriteRenderer.LastFrameDrawStates
                .Any(state => ReferenceEquals(state.Effect, effect));

            effectWasBound.ShouldBeTrue();
        }
        finally
        {
            effect.Dispose();
        }
    }

    [Fact]
    public void RenderTargetEffect_ShouldNotBeBound_WhenContainerHasNoEffect()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        BasicEffect effect = new BasicEffect(game.GraphicsDevice);

        ContainerRuntime container = new();
        container.Width = 100;
        container.Height = 100;
        container.IsRenderTarget = true;

        TextRuntime text = new();
        text.Text = "Hello";
        container.AddChild(text);

        container.AddToManagers(managers, null);
        container.UpdateLayout();

        try
        {
            renderer.Draw(managers);
            renderer.Draw(managers);

            bool effectWasBound = renderer.SpriteRenderer.LastFrameDrawStates
                .Any(state => ReferenceEquals(state.Effect, effect));

            effectWasBound.ShouldBeFalse();
        }
        finally
        {
            effect.Dispose();
        }
    }

    // Same grayscale shader the RenderTargetEffectScreen sample uses, compiled at runtime via
    // ShadowDusk. Kept here (duplicated) so this test fully reproduces the sample's pipeline.
    private const string GrayscaleFx = @"
#if OPENGL
    #define VS_SHADERMODEL vs_3_0
    #define PS_SHADERMODEL ps_3_0
#else
    #define VS_SHADERMODEL vs_4_0_level_9_1
    #define PS_SHADERMODEL ps_4_0_level_9_1
#endif

Texture2D SpriteTexture;

sampler2D SpriteTextureSampler = sampler_state
{
    Texture = <SpriteTexture>;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float4 color = tex2D(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
    float gray = dot(color.rgb, float3(0.299, 0.587, 0.114));
    return float4(gray, gray, gray, color.a);
}

technique SpriteDrawing
{
    pass P0
    {
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
";

    [Fact]
    public void RenderTargetEffect_GrayscaleShader_ActuallyGraysThePixels()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        // 1) Compile the sample's grayscale .fx with ShadowDusk (no content pipeline).
        EffectCompiler compiler = new();
        Result<CompiledShader, ShaderError[]> compileResult =
            compiler.Compile(GrayscaleFx, new CompilerOptions { Target = PlatformTarget.OpenGL });

        compileResult.IsSuccess.ShouldBeTrue(
            compileResult.IsFailure
                ? "ShadowDusk failed to compile the grayscale shader:\n" +
                    string.Join("\n", compileResult.Error.Select(e => e.Message))
                : "");

        using Effect grayscale = new(gd, compileResult.Value.Data);

        // 2) A solid-red render-target container.
        ContainerRuntime container = new();
        container.X = 0;
        container.Y = 0;
        container.Width = 64;
        container.Height = 64;
        container.IsRenderTarget = true;

#pragma warning disable CS0618 // ColoredRectangleRuntime is obsolete; simplest solid fill without the shape dependency.
        ColoredRectangleRuntime red = new();
#pragma warning restore CS0618
        red.Width = 64;
        red.Height = 64;
        red.Color = Color.Red;
        container.AddChild(red);

        container.AddToManagers(managers, null);
        container.UpdateLayout();

        // 3) Render with the grayscale effect, then again without it as a control.
        container.RenderTargetEffect = grayscale;
        Color withEffect = RenderToCaptureAndSample(gd, renderer, managers);

        container.RenderTargetEffect = null;
        Color withoutEffect = RenderToCaptureAndSample(gd, renderer, managers);

        // Control: the unmodified blit is clearly red (red channel dominates).
        withoutEffect.R.ShouldBeGreaterThan((byte)150);
        ((int)withoutEffect.R - withoutEffect.G).ShouldBeGreaterThan(80);

        // With grayscale: the channels collapse to roughly equal (R ≈ G ≈ B).
        Math.Abs(withEffect.R - withEffect.G).ShouldBeLessThan(25);
        Math.Abs(withEffect.R - withEffect.B).ShouldBeLessThan(25);
    }

    [Fact]
    public void RenderTargetEffect_GrayscaleShader_GraysThePixels_UnderCameraZoom()
    {
        using MinimalGame game = new();
        game.RunOneFrame();

        GraphicsDevice gd = game.GraphicsDevice;
        SystemManagers managers = SystemManagers.Default;
        Renderer renderer = managers.Renderer;

        EffectCompiler compiler = new();
        Result<CompiledShader, ShaderError[]> compileResult =
            compiler.Compile(GrayscaleFx, new CompilerOptions { Target = PlatformTarget.OpenGL });
        compileResult.IsSuccess.ShouldBeTrue(
            compileResult.IsFailure ? string.Join("\n", compileResult.Error.Select(e => e.Message)) : "");
        using Effect grayscale = new(gd, compileResult.Value.Data);

        ContainerRuntime container = new();
        container.X = 0;
        container.Y = 0;
        container.Width = 100;
        container.Height = 100;
        container.IsRenderTarget = true;

#pragma warning disable CS0618
        ColoredRectangleRuntime red = new();
#pragma warning restore CS0618
        red.Width = 100;
        red.Height = 100;
        red.Color = Color.Red;
        container.AddChild(red);

        container.AddToManagers(managers, null);
        container.UpdateLayout();

        // Reproduce the sample's GumService.EnableZoomToWindow() — it just sets a camera zoom.
        renderer.Camera.Zoom = 2f;

        container.RenderTargetEffect = grayscale;
        Color withEffect = RenderToCaptureAndSample(gd, renderer, managers);

        Math.Abs(withEffect.R - withEffect.G).ShouldBeLessThan(25);
        Math.Abs(withEffect.R - withEffect.B).ShouldBeLessThan(25);
    }

    /// <summary>
    /// Renders the current managers tree into an off-screen capture target (PreserveContents so
    /// Gum's own mid-frame render-target switches don't wipe it), drawing twice so the first
    /// frame's one-time render-target setup doesn't skew the captured pixels, then returns a
    /// pixel sampled from inside the 64x64 container at the top-left.
    /// </summary>
    private static Color RenderToCaptureAndSample(GraphicsDevice gd, Renderer renderer, SystemManagers managers)
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

        // Container sits at (0,0) sized 64x64; sample well inside it.
        return data[(20 * w) + 20];
    }

    /// <summary>
    /// Minimal Game host that initializes a fresh <see cref="GumService"/> per test so
    /// <see cref="Renderer.Draw(SystemManagers)"/> can be invoked against a live device.
    /// </summary>
    private class MinimalGame : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        public GumService GumService { get; }

        public MinimalGame()
        {
            LoaderManager.Self?.DisposeAndClear();
            _graphics = new GraphicsDeviceManager(this);
            GumService = new GumService();
        }

        protected override void Initialize()
        {
            base.Initialize();
            GumService.Initialize(this, Gum.Forms.DefaultVisualsVersion.V2);
        }

        protected override void Update(GameTime gameTime) { }
        protected override void Draw(GameTime gameTime) => GraphicsDevice.Clear(Color.CornflowerBlue);

        protected override void Dispose(bool disposing)
        {
            if (GumService.IsInitialized)
            {
                GumService.Uninitialize();
            }
            LoaderManager.Self?.DisposeAndClear();
            base.Dispose(disposing);
        }
    }
}
