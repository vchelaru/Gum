using System;
using System.IO;
using Gum;
using Gum.GueDeriving;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using ShadowDusk.Core;
using Shouldly;
using Xunit;
using Color = Microsoft.Xna.Framework.Color;
#if WINDOWSDX
using System.Windows.Forms;
#endif

namespace GumShadowDusk.Tests;

/// <summary>
/// UseShadowDusk() on a real graphics device (#7053): the compile target is read from the device, and a
/// .fx or .slang file resolved through it draws. One source file, compiled by the MonoGame WindowsDX, KNI
/// WindowsDX and KNI DesktopGL test projects. The DirectX projects create a device on a window that is never
/// shown, so nothing takes focus.
/// </summary>
public class ShadowDuskDeviceTests
{
#if WINDOWSDX
    private const PlatformTarget ExpectedTarget = PlatformTarget.DirectX;
#else
    private const PlatformTarget ExpectedTarget = PlatformTarget.OpenGL;
#endif

    private const int CaptureSize = 128;

    private const string GrayscaleSlang = @"
Texture2D SpriteTexture;
SamplerState SpriteTextureSampler;

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TextureCoordinates : TEXCOORD0;
};

[shader(""fragment"")]
float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float4 color = SpriteTexture.Sample(SpriteTextureSampler, input.TextureCoordinates) * input.Color;
    float gray = dot(color.rgb, float3(0.299, 0.587, 0.114));
    return float4(gray, gray, gray, color.a);
}
";

    // The standard MonoGame header, so one .fx compiles for both OpenGL and DirectX (DirectX refuses ps_3_0).
    private const string GrayscaleFx = @"
#if OPENGL
    #define PS_SHADERMODEL ps_3_0
#else
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
    public void Detect_RealDevice_ReportsItsBackend()
    {
        using DeviceHost host = DeviceHost.CreateOrSkip();

        ShadowDuskTargetDetector.Detect(host.GraphicsDevice).ShouldBe(ExpectedTarget);
    }

    [Theory]
    [InlineData(".fx", GrayscaleFx)]
    [InlineData(".slang", GrayscaleSlang)]
    public void UseShadowDusk_SourceShaderFile_GraysThePixels(string extension, string source)
    {
        using DeviceHost host = DeviceHost.CreateOrSkip();
        GumService service = new GumService();
        string shaderPath = Path.Combine(Path.GetTempPath(), "GumShadowDuskDevice_" + Guid.NewGuid().ToString("N") + extension);
        File.WriteAllText(shaderPath, source);
        try
        {
            service.Initialize(host.GraphicsDevice);
            GumServiceShadowDuskExtensions.UseShadowDusk(service);

            ContainerRuntime container = new();
            container.Width = 64;
            container.Height = 64;
            container.IsRenderTarget = true;
#pragma warning disable CS0618
            ColoredRectangleRuntime red = new();
#pragma warning restore CS0618
            red.Width = 64;
            red.Height = 64;
            red.Color = Color.Red;
            container.AddChild(red);
            container.AddToManagers(service.SystemManagers, null);
            container.UpdateLayout();

            container.SourceShaderFile = shaderPath;
            container.RenderTargetEffect.ShouldNotBeNull();

            Color pixel = RenderAndSample(host.GraphicsDevice, service);

            Math.Abs(pixel.R - pixel.G).ShouldBeLessThan(25);
            Math.Abs(pixel.R - pixel.B).ShouldBeLessThan(25);
            // Gray, not the black an unbound shader would draw.
            pixel.R.ShouldBeGreaterThan((byte)40);
        }
        finally
        {
            Gum.Wireframe.CustomSetPropertyOnRenderable.RenderTargetEffectResolver = null;
            File.Delete(shaderPath);
            if (service.IsInitialized)
            {
                service.Uninitialize();
            }
        }
    }

    private static Color RenderAndSample(GraphicsDevice graphicsDevice, GumService service)
    {
        using RenderTarget2D capture = new(graphicsDevice, CaptureSize, CaptureSize, false, SurfaceFormat.Color,
            DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

        // The first draw does one-time render-target setup, so draw twice.
        for (int i = 0; i < 2; i++)
        {
            graphicsDevice.SetRenderTarget(capture);
            graphicsDevice.Clear(Color.Black);
            service.Renderer.Draw(service.SystemManagers);
        }
        graphicsDevice.SetRenderTarget(null);

        Color[] data = new Color[CaptureSize * CaptureSize];
        capture.GetData(data);
        return data[(20 * CaptureSize) + 20];
    }

    /// <summary>A graphics device with nothing visible: a hidden window on the DirectX projects, a Game on GL.</summary>
    private sealed class DeviceHost : IDisposable
    {
#if WINDOWSDX
        private readonly Form _form;

        private DeviceHost(Form form, GraphicsDevice graphicsDevice)
        {
            _form = form;
            GraphicsDevice = graphicsDevice;
        }
#else
        private readonly Game _game;

        private DeviceHost(Game game)
        {
            _game = game;
            GraphicsDevice = game.GraphicsDevice;
        }
#endif

        public GraphicsDevice GraphicsDevice { get; }

        public static DeviceHost CreateOrSkip()
        {
#if WINDOWSDX
            Assert.SkipUnless(OperatingSystem.IsWindows(), "DirectX needs Windows.");

            // Touching Handle creates the window without showing it.
            Form form = new Form { Width = CaptureSize, Height = CaptureSize };
            IntPtr handle = form.Handle;
            PresentationParameters parameters = new PresentationParameters
            {
                BackBufferWidth = CaptureSize,
                BackBufferHeight = CaptureSize,
                BackBufferFormat = SurfaceFormat.Color,
                DepthStencilFormat = DepthFormat.None,
                DeviceWindowHandle = handle,
                IsFullScreen = false,
            };

            try
            {
                return new DeviceHost(form, new GraphicsDevice(GraphicsAdapter.DefaultAdapter, GraphicsProfile.HiDef, parameters));
            }
            catch (NoSuitableGraphicsDeviceException exception)
            {
                // A machine with no DirectX 11 adapter at all (some CI runners); a machine with one must pass.
                form.Dispose();
                Assert.Skip("No DirectX 11 graphics device on this machine: " + exception.Message);
                throw;
            }
#else
            HostGame game = new HostGame();
            game.RunOneFrame();
            return new DeviceHost(game);
#endif
        }

        public void Dispose()
        {
#if WINDOWSDX
            GraphicsDevice.Dispose();
            _form.Dispose();
#else
            _game.Dispose();
#endif
        }
    }

#if !WINDOWSDX
    private sealed class HostGame : Game
    {
        public HostGame()
        {
            new GraphicsDeviceManager(this) { GraphicsProfile = GraphicsProfile.HiDef };
        }
    }
#endif
}
